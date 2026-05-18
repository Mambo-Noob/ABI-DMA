using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using ImGuiOverlay.DMA;
using VmmSharpEx.Scatter;

namespace ImGuiOverlay.ABI
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI PLAYERS  ??  Background cache threads ?? all hot paths use scatter reads.
    //
    //  Thread layout:
    //    ABI.World      50 ms   ?? GWorld chain, actor array ptr, camera mgr ptr
    //    ABI.Camera      1 ms   ?? camera + local pos + ctrl yaw  (scatter: 3 values)
    //    ABI.Players   150 ms   ?? enumerate actors, resolve component ptrs (scatter)
    //    ABI.Vitals      8 ms   ?? find HealthSet ptr for new actors (scatter)
    //    ABI.Positions   1 ms   ?? pos + health + vis + death per actor (scatter)
    //    ABI.Skeletons  16 ms   ?? bone transforms per actor (scatter: header + bones)
    //    ABI.FramePulse  1 ms   ?? publishes coherent Frame for renderer
    //    ABI.Weapon      8 ms   ?? weapon zoom (scatter: 3 ptrs + 2 floats)
    // /////////////////////////////////////////////////////////////////////////////////////

    public static class ABIPlayers
    {
        // //// World / camera pointers ////////////////////////////////////////////////////
        public static ulong UWorld, UGameInstance, GameState, PersistentLevel;
        public static ulong ActorArray; public static int ActorCount;
        public static ulong LocalPlayers, PlayerController, PlayerArray; public static int PlayerCount;
        public static ulong LocalPawn, LocalRoot, LocalState, LocalCameraMgr;

        // //// Published snapshots ////////////////////////////////////////////////////////
        public static Vector3          LocalPosition;
        public static FMinimalViewInfo Camera;
        public static float            CtrlYaw;

        // //// Origin bias ///////////////////////////////////////////////////////////////
        private static Vector3 _originBias;
        private static Vector3 _prevLocalWorld;
        private static bool    _havePrevLocal;
        public static Vector3  GetOriginBias() => _originBias;

        // //// Public actor lists /////////////////////////////////////////////////////////
        public static List<ABIPlayer> ActorList      = new();
        public static List<ActorPos>  ActorPositions = new();
        public static readonly object Sync           = new();

        // //// Skeleton cache /////////////////////////////////////////////////////////////
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<ulong, Vector3[]>
            _skelCache = new();

        // //// Coherent frame ////////////////////////////////////////////////////////////
        public struct Frame
        {
            public FMinimalViewInfo              Cam;
            public Vector3                       Local;
            public List<ActorPos>                Positions;
            public Dictionary<ulong, Vector3[]>  Skeletons;
            public long                          Stamp;
        }

        private static int   _frameSeq;
        private static Frame _frameBuf;

        // //// Camera seqlock buf ////////////////////////////////////////////////////////
        private static int             _camSeq;
        private static FMinimalViewInfo _camBuf;
        private static Vector3          _camLocalBuf;
        private static float            _ctrlYawBuf;

        // //// Zoom //////////////////////////////////////////////////////////////////////
        public struct ZoomInfo { public bool Valid; public float Zoom, ScopeMag, Progress; public ulong WeaponMan, CurrentWeapon, ZoomComp; public long Stamp; }
        private static readonly object _zoomSync = new();
        private static ZoomInfo _zoom;
        public static bool TryGetZoom(out ZoomInfo zi) { lock (_zoomSync) { zi = _zoom; return zi.Valid; } }

        // //// Actor data ////////////////////////////////////////////////////////////////
        public struct ABIPlayer
        {
            public readonly ulong  Pawn, Mesh, Root, ASC, HealthSet, DeathComp;
            public readonly string Name;
            public readonly bool   IsBot;
            public ABIPlayer(ulong pawn, ulong mesh, ulong root, string name, bool isBot,
                             ulong asc, ulong healthSet, ulong deathComp)
            { Pawn=pawn; Mesh=mesh; Root=root; Name=name; IsBot=isBot; ASC=asc; HealthSet=healthSet; DeathComp=deathComp; }
        }

        public struct ActorPos
        {
            public ulong   Pawn;
            public Vector3 Position;
            public float   Health, HealthMax;
            public bool    IsDead, IsVisible, HasFreshSkeleton, DeadByDeathComp;
        }

        private struct ActorCacheEntry
        { public ulong Pawn, Mesh, Root, ASC, DeathComp, HealthSet; public bool IsBot; public string Name; }

        private static readonly Dictionary<ulong, ActorCacheEntry> _actorCache     = new(1024);

        private static bool _running;
        private static long _lastEnumTicks;
        private const  int  ENUM_PERIOD_MS = 150;

        // /////////////////////////////////////////////////////////////////////////////////////
        //  Public API
        // /////////////////////////////////////////////////////////////////////////////////////

        public static void StartCache()
        {
            if (_running || !DmaMemory.IsAttached) return;
            _running = true;

            void Spawn(ThreadStart fn, string name, ThreadPriority pri) =>
                new Thread(fn) { IsBackground = true, Priority = pri, Name = name }.Start();

            Spawn(CacheWorldLoop,     "ABI.World",      ThreadPriority.AboveNormal);
            Spawn(CacheCameraLoop,    "ABI.Camera",     ThreadPriority.Highest);
            Spawn(CachePlayersLoop,   "ABI.Players",    ThreadPriority.AboveNormal);
            Spawn(CacheVitalsLoop,    "ABI.Vitals",     ThreadPriority.AboveNormal);
            Spawn(CachePositionsLoop, "ABI.Positions",  ThreadPriority.AboveNormal);
            Spawn(CacheSkeletonsLoop, "ABI.Skeletons",  ThreadPriority.Normal);
            Spawn(FramePulseLoop,     "ABI.FramePulse", ThreadPriority.Highest);
            Spawn(CacheWeaponZoomLoop,"ABI.Weapon",     ThreadPriority.AboveNormal);
        }

        public static void Stop() => _running = false;

        public static bool TryGetFrame(out Frame f)
        {
            f = default;
            for (int i = 0; i < 3; i++)
            {
                int s1 = Volatile.Read(ref _frameSeq);
                if ((s1 & 1) != 0) continue;
                f = _frameBuf;
                Thread.MemoryBarrier();
                int s2 = Volatile.Read(ref _frameSeq);
                if (s1 == s2 && (s2 & 1) == 0 && f.Positions != null) return true;
            }
            return false;
        }

        public static bool TryGetSkeleton(ulong pawn, out Vector3[] pts)
        {
            // Read directly from live cache -- always fresher than the frame snapshot
            if (_skelCache.TryGetValue(pawn, out pts)) return pts != null;
            pts = null; return false;
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  WORLD LOOP  (sequential reads ?? only runs at 50 ms, no scatter needed)
        // /////////////////////////////////////////////////////////////////////////////////////

        private static void CacheWorldLoop()
        {
            while (_running) { try { CacheWorld(); } catch { } Sleep(50); }
        }

        private static bool CacheWorld()
        {
            UWorld = DmaMemory.Read<ulong>(DmaMemory.Base + ABIOffsets.GWorld);
            if (UWorld == 0) return false;

            UGameInstance   = DmaMemory.Read<ulong>(UWorld + ABIOffsets.UWorld_OwningGameInstance);
            GameState       = DmaMemory.Read<ulong>(UWorld + ABIOffsets.UWorld_GameState);
            PersistentLevel = DmaMemory.Read<ulong>(UWorld + ABIOffsets.UWorld_PersistentLevel);
            ActorArray      = DmaMemory.Read<ulong>(PersistentLevel + ABIOffsets.ULevel_ActorArray);
            ActorCount      = DmaMemory.Read<int>(PersistentLevel + ABIOffsets.ULevel_ActorArray + 0x08);
            LocalPlayers     = DmaMemory.Read<ulong>(DmaMemory.Read<ulong>(UGameInstance + ABIOffsets.UGameInstance_LocalPlayers));
            PlayerController = DmaMemory.Read<ulong>(LocalPlayers + ABIOffsets.UPlayer_PlayerController);
            LocalCameraMgr   = DmaMemory.Read<ulong>(PlayerController + ABIOffsets.APlayerController_PlayerCameraManager);
            LocalPawn        = DmaMemory.Read<ulong>(PlayerController + ABIOffsets.APlayerController_AcknowledgedPawn);
            LocalRoot        = DmaMemory.Read<ulong>(LocalPawn + ABIOffsets.AActor_RootComponent);
            PlayerArray      = DmaMemory.Read<ulong>(GameState + ABIOffsets.AGameStateBase_PlayerArray);
            PlayerCount      = DmaMemory.Read<int>(GameState + ABIOffsets.AGameStateBase_PlayerCount);
            return true;
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  CAMERA LOOP  ?? scatter: cam + localPos + ctrlYaw in one round
        // /////////////////////////////////////////////////////////////////////////////////////

        private static void CacheCameraLoop()
        {
            while (_running)
            {
                try
                {
                    if (LocalCameraMgr == 0 || LocalRoot == 0 || PlayerController == 0)
                    { Sleep(1); continue; }

                    ulong camAddr     = LocalCameraMgr + ABIOffsets.APlayerCameraManager_CameraCachePrivate + 0x10;
                    ulong localWAddr  = LocalRoot + ABIOffsets.USceneComponent_RelativeLocation;
                    ulong ctrlRotAddr = PlayerController + ABIOffsets.AController_ControlRotation;

                    using var sc = DmaMemory.CreateScatterNoCache();
                    sc.PrepareReadValue<FMinimalViewInfo>(camAddr);
                    sc.PrepareReadValue<Vector3>(localWAddr);
                    sc.PrepareReadValue<Rotator>(ctrlRotAddr);
                    sc.Execute();

                    sc.ReadValue<FMinimalViewInfo>(camAddr,     out var cam);
                    sc.ReadValue<Vector3>         (localWAddr,  out var localWorld);
                    sc.ReadValue<Rotator>         (ctrlRotAddr, out var ctrlRot);

                    // Origin-bias jump correction
                    if (_havePrevLocal)
                    {
                        var jump = _prevLocalWorld - localWorld;
                        if (jump.Length() > 5000f) _originBias += jump;
                    }
                    _prevLocalWorld = localWorld;
                    _havePrevLocal  = true;

                    var localBiased = localWorld + _originBias;
                    cam.Location   += _originBias;
                    float ctrlYaw   = ctrlRot.Yaw;

                    Interlocked.Increment(ref _camSeq);
                    _camBuf      = cam;
                    _camLocalBuf = localBiased;
                    _ctrlYawBuf  = ctrlYaw;
                    Camera        = cam;
                    LocalPosition = localBiased;
                    CtrlYaw       = ctrlYaw;
                    Interlocked.Increment(ref _camSeq);
                }
                catch { }
                Sleep(1);
            }
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  PLAYERS LOOP  ?? scatter: batch-read pawn name IDs, then scatter component ptrs
        // /////////////////////////////////////////////////////////////////////////////////////

        private static void CachePlayersLoop()
        {
            while (_running)
            {
                try
                {
                    var now  = System.Diagnostics.Stopwatch.GetTimestamp();
                    double ms   = now * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                    double last = _lastEnumTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                    if (ms - last < ENUM_PERIOD_MS) { Sleep(10); continue; }
                    _lastEnumTicks = now;

                    if (ActorArray == 0 || ActorCount <= 0) { lock (Sync) ActorList = new(); Sleep(10); continue; }

                    int take = Math.Min(ActorCount, 2048);

                    // Round 1: read all actor pointers
                    var ptrs = DmaMemory.ReadArray<ulong>(ActorArray, take);
                    if (ptrs == null) continue;

                    // Round 2: scatter-read the FName index (uint at +24) for every non-zero actor
                    var validPtrs = new List<ulong>(64);
                    foreach (var p in ptrs) { if (p != 0 && p != LocalPawn) validPtrs.Add(p); }
                    if (validPtrs.Count == 0) { lock (Sync) ActorList = new(); continue; }

                    uint[] nameIds = new uint[validPtrs.Count];
                    using (var sc = DmaMemory.CreateScatter())
                    {
                        foreach (var p in validPtrs) sc.PrepareReadValue<uint>(p + 24);
                        sc.Execute();
                        for (int i = 0; i < validPtrs.Count; i++)
                            sc.ReadValue<uint>(validPtrs[i] + 24, out nameIds[i]);
                    }

                    // Filter by name, then scatter-read component ptrs for new actors
                    var needPtrs = new List<(ulong pawn, bool isBot)>(32);
                    for (int i = 0; i < validPtrs.Count; i++)
                    {
                        string name = ABINamePool.GetName(nameIds[i]);
                        if (string.IsNullOrEmpty(name)) continue;
                        if (!name.Contains("BP_UamCharacter") &&
                            !name.Contains("BP_UamAICharacter") &&
                            !name.Contains("BP_UamRangeCharacter_C")) continue;

                        bool isBot = name.Contains("AI");
                        ulong pawn = validPtrs[i];

                        _actorCache.TryGetValue(pawn, out var ac);
                        ac.IsBot = isBot; ac.Name = name;
                        _actorCache[pawn] = ac;

                        if (ac.Mesh == 0 || ac.Root == 0 || ac.ASC == 0 || ac.DeathComp == 0)
                            needPtrs.Add((pawn, isBot));
                    }

                    // Round 3: scatter-read missing component ptrs (only for uncached actors)
                    if (needPtrs.Count > 0)
                    {
                        using var sc = DmaMemory.CreateScatter();
                        foreach (var (pawn, _) in needPtrs)
                        {
                            _actorCache.TryGetValue(pawn, out var ac);
                            if (ac.Mesh     == 0) sc.PrepareReadValue<ulong>(pawn + ABIOffsets.ACharacter_Mesh);
                            if (ac.Root     == 0) sc.PrepareReadValue<ulong>(pawn + ABIOffsets.AActor_RootComponent);
                            if (ac.ASC      == 0) sc.PrepareReadValue<ulong>(pawn + (ulong)ABIOffsetsExt.OFF_PAWN_ASC);
                            if (ac.DeathComp== 0) sc.PrepareReadValue<ulong>(pawn + (ulong)ABIOffsetsExt.OFF_PAWN_DEATHCOMP);
                        }
                        sc.Execute();

                        foreach (var (pawn, _) in needPtrs)
                        {
                            _actorCache.TryGetValue(pawn, out var ac);
                            if (ac.Mesh     == 0) { sc.ReadValue<ulong>(pawn + ABIOffsets.ACharacter_Mesh,             out ac.Mesh);     }
                            if (ac.Root     == 0) { sc.ReadValue<ulong>(pawn + ABIOffsets.AActor_RootComponent,        out ac.Root);     }
                            if (ac.ASC      == 0) { sc.ReadValue<ulong>(pawn + (ulong)ABIOffsetsExt.OFF_PAWN_ASC,      out ac.ASC);      }
                            if (ac.DeathComp== 0) { sc.ReadValue<ulong>(pawn + (ulong)ABIOffsetsExt.OFF_PAWN_DEATHCOMP,out ac.DeathComp);}
                            _actorCache[pawn] = ac;
                        }
                    }

                    // Build final ActorList
                    var tmp = new List<ABIPlayer>(validPtrs.Count);
                    foreach (var p in validPtrs)
                    {
                        if (!_actorCache.TryGetValue(p, out var ac) || string.IsNullOrEmpty(ac.Name)) continue;
                        if (!ac.Name.Contains("BP_UamCharacter") &&
                            !ac.Name.Contains("BP_UamAICharacter") &&
                            !ac.Name.Contains("BP_UamRangeCharacter_C")) continue;
                        tmp.Add(new ABIPlayer(p, ac.Mesh, ac.Root, ac.Name, ac.IsBot, ac.ASC, ac.HealthSet, ac.DeathComp));
                    }

                    lock (Sync) ActorList = tmp;
                }
                catch { }
            }
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  VITALS LOOP  ?? scatter: read attr set array header + entries
        // /////////////////////////////////////////////////////////////////////////////////////

        private static void CacheVitalsLoop()
        {
            while (_running)
            {
                try
                {
                    List<ABIPlayer> actors;
                    lock (Sync) actors = ActorList.Count == 0 ? null : new List<ABIPlayer>(ActorList);
                    if (actors == null || actors.Count == 0) { Sleep(8); continue; }

                    // Find actors still missing HealthSet
                    var need = new List<ABIPlayer>(16);
                    foreach (var a in actors)
                    {
                        if (a.ASC == 0) continue;
                        if (_actorCache.TryGetValue(a.Pawn, out var ac) && ac.HealthSet != 0) continue;
                        need.Add(a);
                    }
                    if (need.Count == 0) { Sleep(8); continue; }

                    // Round 1: scatter-read ASC attr set array headers
                    var headers = new (ulong data, int num)[need.Count];
                    using (var sc = DmaMemory.CreateScatter())
                    {
                        foreach (var a in need)
                        {
                            ulong basePtr = a.ASC + (ulong)ABIOffsetsExt.OFF_ASC_ATTRSETS;
                            sc.PrepareReadValue<ulong>(basePtr);
                            sc.PrepareReadValue<int>(basePtr + 0x8);
                        }
                        sc.Execute();
                        for (int i = 0; i < need.Count; i++)
                        {
                            ulong basePtr = need[i].ASC + (ulong)ABIOffsetsExt.OFF_ASC_ATTRSETS;
                            sc.ReadValue<ulong>(basePtr,        out headers[i].data);
                            sc.ReadValue<int>(basePtr + 0x8,    out headers[i].num);
                        }
                    }

                    // Round 2: scatter-read set entries (up to 8 per actor)
                    using (var sc = DmaMemory.CreateScatter())
                    {
                        for (int i = 0; i < need.Count; i++)
                        {
                            if (headers[i].data == 0 || headers[i].num <= 0) continue;
                            int scan = Math.Min(headers[i].num, 8);
                            for (int e = 0; e < scan; e++)
                                sc.PrepareReadValue<ulong>(headers[i].data + (ulong)(e * 8));
                        }
                        sc.Execute();

                        for (int i = 0; i < need.Count; i++)
                        {
                            if (headers[i].data == 0 || headers[i].num <= 0) continue;
                            int scan = Math.Min(headers[i].num, 8);
                            for (int e = 0; e < scan; e++)
                            {
                                sc.ReadValue<ulong>(headers[i].data + (ulong)(e * 8), out ulong hs);
                                if (hs == 0) continue;
                                // Validate by reading health values
                                float h  = DmaMemory.Read<float>(hs + (ulong)ABIOffsetsExt.OFF_ATTR_HEALTH);
                                float hm = DmaMemory.Read<float>(hs + (ulong)ABIOffsetsExt.OFF_ATTR_HEALTHMAX);
                                if (hm > 1f && hm < 5000f && h >= -50f && h < hm * 2.5f)
                                {
                                    _actorCache.TryGetValue(need[i].Pawn, out var ac2);
                                    ac2.HealthSet = hs;
                                    _actorCache[need[i].Pawn] = ac2;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }
                Sleep(8);
            }
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  POSITIONS LOOP  ?? scatter: all actor positions + health + vis + death in one round
        // /////////////////////////////////////////////////////////////////////////////////////

        private static void CachePositionsLoop()
        {
            while (_running)
            {
                try
                {
                    List<ABIPlayer> actors;
                    lock (Sync) actors = ActorList.Count == 0 ? null : new List<ABIPlayer>(ActorList);
                    if (actors == null || actors.Count == 0) { Sleep(4); continue; }

                    // Round 1: scatter-read ctwPtr, deathInfo, health, healthMax, vis timers for all actors
                    using var sc = DmaMemory.CreateScatterNoCache();

                    var bias = _originBias;

                    foreach (var a in actors)
                    {
                        if (a.Root != 0) sc.PrepareReadValue<ulong>  (a.Root + ABIOffsets.USceneComponent_ComponentToWorld_Ptr);
                        if (a.DeathComp != 0) sc.PrepareReadValue<ulong>(a.DeathComp + (ulong)ABIOffsetsExt.OFF_DEATHCOMP_DEATHINFO);
                        if (_actorCache.TryGetValue(a.Pawn, out var ac) && ac.HealthSet != 0)
                        {
                            sc.PrepareReadValue<float>(ac.HealthSet + (ulong)ABIOffsetsExt.OFF_ATTR_HEALTH);
                            sc.PrepareReadValue<float>(ac.HealthSet + (ulong)ABIOffsetsExt.OFF_ATTR_HEALTHMAX);
                        }
                        if (a.Mesh != 0)
                        {
                            sc.PrepareReadValue<float>(a.Mesh + ABIOffsetsExt.OFF_MESH_TIMERS + (ulong)ABIOffsetsExt.OFF_LASTONSCREEN);
                            sc.PrepareReadValue<float>(a.Mesh + ABIOffsetsExt.OFF_MESH_TIMERS + (ulong)ABIOffsetsExt.OFF_LASTSUBMIT);
                        }
                    }

                    sc.Execute();

                    // Round 2: scatter-read FTransform for each valid ctwPtr
                    var ctwPtrs = new ulong[actors.Count];
                    for (int i = 0; i < actors.Count; i++)
                    {
                        var a = actors[i];
                        if (a.Root != 0)
                            sc.ReadValue<ulong>(a.Root + ABIOffsets.USceneComponent_ComponentToWorld_Ptr, out ctwPtrs[i]);
                    }

                    using var sc2 = DmaMemory.CreateScatterNoCache();
                    for (int i = 0; i < actors.Count; i++)
                        if (ctwPtrs[i] != 0) sc2.PrepareReadValue<FTransform>(ctwPtrs[i]);
                    sc2.Execute();

                    // Assemble positions
                    var posList = new List<ActorPos>(actors.Count);
                    for (int i = 0; i < actors.Count; i++)
                    {
                        var a = actors[i];
                        try
                        {
                            Vector3 pos = default;
                            if (ctwPtrs[i] != 0 && sc2.ReadValue<FTransform>(ctwPtrs[i], out var ctw))
                                pos = ctw.Translation + bias;
                            else continue;
                            // Reject unspawned / below-map actors
                            if (!float.IsFinite(pos.X) || !float.IsFinite(pos.Y) || !float.IsFinite(pos.Z))
                                continue;
                            
                            // Reject positions that haven't moved from origin (uninitialized actors)
                            if (pos.LengthSquared() < 1f)
                                continue;
                            
                            // Reject actors stacked underground ¡ª adjust Z threshold to your map's lowest floor
                            // Typical ABI map floor is around Z = -5000 to -10000 UU; anything below is fake
                            if (pos.Z < -50000f)
                                continue;
                            float health = 0, healthMax = 0;
                            if (_actorCache.TryGetValue(a.Pawn, out var ac) && ac.HealthSet != 0)
                            {
                                sc.ReadValue<float>(ac.HealthSet + (ulong)ABIOffsetsExt.OFF_ATTR_HEALTH,    out health);
                                sc.ReadValue<float>(ac.HealthSet + (ulong)ABIOffsetsExt.OFF_ATTR_HEALTHMAX, out healthMax);
                            }

                            bool isDead = false;
                            if (a.DeathComp != 0)
                            {
                                sc.ReadValue<ulong>(a.DeathComp + (ulong)ABIOffsetsExt.OFF_DEATHCOMP_DEATHINFO, out ulong di);
                                isDead = di != 0;
                            }
                            // Fallback: treat as dead if health is at or below zero and we have a valid max
                            if (!isDead && healthMax > 1f && health <= 0f)
                                isDead = true;

                            bool isVisible = false;
                            if (a.Mesh != 0)
                            {
                                sc.ReadValue<float>(a.Mesh + ABIOffsetsExt.OFF_MESH_TIMERS + (ulong)ABIOffsetsExt.OFF_LASTONSCREEN, out float lastOn);
                                sc.ReadValue<float>(a.Mesh + ABIOffsetsExt.OFF_MESH_TIMERS + (ulong)ABIOffsetsExt.OFF_LASTSUBMIT,   out float lastSub);
                                // lastOn = last time mesh was confirmed on-screen (higher = more recent)
                                // lastSub = last render submission time
                                // Visible when the on-screen timer is recent relative to the submit timer
                                isVisible = (lastSub - lastOn) >= 0f && (lastSub - lastOn) < ABIOffsetsExt.VIS_TICK;
                            }

                            posList.Add(new ActorPos
                            {
                                Pawn      = a.Pawn,
                                Position  = pos,
                                Health    = health,
                                HealthMax = healthMax,
                                IsDead    = isDead,
                                IsVisible = isVisible
                            });
                        }
                        catch { }
                    }

                    ActorPositions = posList;
                }
                catch { }
                Sleep(1);
            }
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  FRAME PULSE  -- publishes a coherent snapshot every ~1 ms.
        //  No DMA here. Camera is read live; skeletons come from _skelCache.
        // /////////////////////////////////////////////////////////////////////////////////////

        private static void FramePulseLoop()
        {
            while (_running)
            {
                try
                {
                    var positions = ActorPositions;
                    if (positions == null || positions.Count == 0) { Sleep(1); continue; }

                    // Snapshot skeleton cache for this frame (zero-copy dict copy)
                    var skeletons = new Dictionary<ulong, Vector3[]>(_skelCache);

                    Interlocked.Increment(ref _frameSeq);
                    _frameBuf = new Frame
                    {
                        Cam       = Camera,
                        Local     = LocalPosition,
                        Positions = positions,
                        Skeletons = skeletons,
                        Stamp     = System.Diagnostics.Stopwatch.GetTimestamp()
                    };
                    Interlocked.Increment(ref _frameSeq);
                }
                catch { }
                Sleep(1);
            }
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  SKELETON CACHE LOOP  -- 2 DMA scatter round-trips per 16 ms tick.
        //  Runs at Normal priority so it never starves the camera or positions loops.
        // /////////////////////////////////////////////////////////////////////////////////////

        private static void CacheSkeletonsLoop()
        {
            const int SZ = 0x30;

            while (_running)
            {
                try
                {
                    List<ABIPlayer> actors;
                    lock (Sync) actors = ActorList.Count == 0 ? null : new List<ABIPlayer>(ActorList);
                    if (actors == null || actors.Count == 0) { Sleep(1); continue; }

                    var bias = _originBias;

                    // Round 1: bone array ptr+count, MESH CTW ptr (read from Mesh, not Root).
                    // The mesh component has its own CTW at +0x210 which includes the character's
                    // yaw and the correct world-space origin (mesh foot pivot), unlike the capsule
                    // root CTW which sits at capsule center with near-identity rotation.
                    using var scHdr = DmaMemory.CreateScatterNoCache();
                    foreach (var a in actors)
                    {
                        if (a.Mesh == 0) continue;
                        ulong arr = a.Mesh + ABIOffsets.USkeletalMeshComponent_CachedComponentSpaceTransforms;
                        scHdr.PrepareReadValue<ulong>(arr);          // bone data ptr
                        scHdr.PrepareReadValue<int>  (arr + 0x8);    // bone count
                        scHdr.PrepareReadValue<ulong>(a.Mesh + ABIOffsets.USceneComponent_ComponentToWorld_Ptr); // mesh CTW ptr
                    }
                    scHdr.Execute();

                    // Round 2: dereference mesh CTW ptr + all bone FTransforms in one scatter
                    var headers = new (ulong data, int count, ulong meshCtwPtr)[actors.Count];
                    using var scData = DmaMemory.CreateScatterNoCache();
                    for (int i = 0; i < actors.Count; i++)
                    {
                        var a = actors[i];
                        if (a.Mesh == 0) continue;
                        ulong arr = a.Mesh + ABIOffsets.USkeletalMeshComponent_CachedComponentSpaceTransforms;
                        scHdr.ReadValue<ulong>(arr,       out headers[i].data);
                        scHdr.ReadValue<int>  (arr + 0x8, out headers[i].count);
                        scHdr.ReadValue<ulong>(a.Mesh + ABIOffsets.USceneComponent_ComponentToWorld_Ptr, out headers[i].meshCtwPtr);

                        if (headers[i].data == 0 || headers[i].count <= 0) continue;
                        if (headers[i].meshCtwPtr != 0)
                            scData.PrepareReadValue<FTransform>(headers[i].meshCtwPtr);
                        foreach (int bi in ABISkeleton.FetchIndices)
                            if (bi < headers[i].count)
                                scData.PrepareReadValue<FTransform>(headers[i].data + (ulong)(bi * SZ));
                    }
                    scData.Execute();

                    // Assemble world-space bone positions into _skelCache
                    var seen = new HashSet<ulong>(actors.Count);
                    for (int i = 0; i < actors.Count; i++)
                    {
                        var a = actors[i];
                        seen.Add(a.Pawn);
                        if (a.Mesh == 0) continue;
                        if (headers[i].data == 0 || headers[i].count <= 0) continue;
                        if (headers[i].meshCtwPtr == 0) continue;
                        if (!scData.ReadValue<FTransform>(headers[i].meshCtwPtr, out var meshCtw)) continue;

                        // meshCtw is the mesh component's full world transform:
                        // Translation = mesh pivot in world (near feet), Rotation = character yaw+pitch+roll.
                        // Just add the origin bias ¡ª no reconstruction needed.
                        meshCtw.Translation += bias;
                        if (!ABISkeleton.IsSanePublic(meshCtw)) continue;

                        var pts = new Vector3[ABISkeleton.FetchIndices.Length];
                        bool ok = true;
                        for (int b = 0; b < ABISkeleton.FetchIndices.Length; b++)
                        {
                            int bi = ABISkeleton.FetchIndices[b];
                            if (bi >= headers[i].count)
                            { pts[b] = b > 0 && pts[0] != Vector3.Zero ? pts[0] : meshCtw.Translation; continue; }
                            if (!scData.ReadValue<FTransform>(headers[i].data + (ulong)(bi * SZ), out var boneCS)
                                || !float.IsFinite(boneCS.Translation.X))
                            { ok = false; break; }
                            pts[b] = ABIMath.TransformPosition(meshCtw, boneCS.Translation);
                        }
                        if (ok) _skelCache[a.Pawn] = pts;
                    }

                    // Evict stale pawns
                    foreach (var k in _skelCache.Keys)
                        if (!seen.Contains(k)) _skelCache.TryRemove(k, out _);
                }
                catch { }
                Thread.Yield();  // run as fast as DMA allows ¡ª no artificial sleep
            }
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  WEAPON ZOOM LOOP  ?? scatter: weaponMan + currentWeapon + zoomComp + 2 floats
        // /////////////////////////////////////////////////////////////////////////////////////

        private static void CacheWeaponZoomLoop()
        {
            while (_running)
            {
                try
                {
                    if (LocalPawn == 0 || ABIOffsetsExt.OFF_PAWN_WEAPONMAN == 0) { Sleep(8); continue; }

                    // Round 1: follow ptr chain ?? three sequential pointers
                    ulong wmAddr  = LocalPawn + (ulong)ABIOffsetsExt.OFF_PAWN_WEAPONMAN;
                    using var sc1 = DmaMemory.CreateScatterNoCache();
                    sc1.PrepareReadValue<ulong>(wmAddr);
                    sc1.Execute();
                    sc1.ReadValue<ulong>(wmAddr, out ulong weaponMan);

                    if (weaponMan == 0) { lock (_zoomSync) _zoom = default; Sleep(8); continue; }

                    ulong cwAddr = weaponMan + ABIOffsetsExt.OFF_WEAPON_CURRENT;
                    using var sc2 = DmaMemory.CreateScatterNoCache();
                    sc2.PrepareReadValue<ulong>(cwAddr);
                    sc2.Execute();
                    sc2.ReadValue<ulong>(cwAddr, out ulong currentWeapon);

                    if (currentWeapon == 0) { lock (_zoomSync) _zoom = default; Sleep(8); continue; }

                    ulong zcAddr = currentWeapon + ABIOffsetsExt.OFF_WEAPON_ZOOMCOMP;
                    using var sc3 = DmaMemory.CreateScatterNoCache();
                    sc3.PrepareReadValue<ulong>(zcAddr);
                    sc3.Execute();
                    sc3.ReadValue<ulong>(zcAddr, out ulong zoomComp);

                    if (zoomComp == 0) { lock (_zoomSync) _zoom = default; Sleep(8); continue; }

                    // Round 2: scatter-read both zoom floats at once
                    ulong smAddr   = zoomComp + ABIOffsetsExt.OFF_ZOOM_SCOPEMAG;
                    ulong progAddr = zoomComp + ABIOffsetsExt.OFF_ZOOM_PROGRESSRATE;
                    using var sc4  = DmaMemory.CreateScatterNoCache();
                    sc4.PrepareReadValue<float>(smAddr);
                    sc4.PrepareReadValue<float>(progAddr);
                    sc4.Execute();
                    sc4.ReadValue<float>(smAddr,   out float scopeMag);
                    sc4.ReadValue<float>(progAddr, out float progress);

                    float zoom = 1f + (scopeMag - 1f) * Math.Clamp(progress, 0f, 1f);
                    lock (_zoomSync)
                        _zoom = new ZoomInfo
                        {
                            Valid = true, Zoom = zoom, ScopeMag = scopeMag, Progress = progress,
                            WeaponMan = weaponMan, CurrentWeapon = currentWeapon, ZoomComp = zoomComp,
                            Stamp = System.Diagnostics.Stopwatch.GetTimestamp()
                        };
                }
                catch { lock (_zoomSync) _zoom = default; }
                Sleep(8);
            }
        }

        private static void Sleep(int ms) => Thread.Sleep(ms);
    }
}