using System;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.Config;
using ImGuiOverlay.DMA;
using ImGuiOverlay.Theme;
using ImGuiOverlay.Input;

namespace ImGuiOverlay.ABI
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI CONTROLLER
    //
    //  Lifecycle owner for all Arena Breakout Infinite subsystems.
    //  Usage inside OverlayApp:
    //
    //    private ABIController _abi;
    //
    //    // In OnLoad(), after DmaMemory.Init():
    //    _abi = new ABIController(theme);
    //    _abi.Start();
    //
    //    // In OnRender(), after _winMgr.Render():
    //    _abi.Render(backgroundDrawList);
    //
    //    // In OnClosing():
    //    _abi.Stop();
    //
    // /////////////////////////////////////////////////////////////////////////////////////

    public sealed class ABIController : IDisposable
    {
        private readonly OverlayTheme _theme;
        private readonly ABIGameConfig _cfg;

        private bool _running;

        // Aimbot state
        private Vector2? _lastAimScreen;
        private long     _lastAimTicks;
        private int      _aimbotDbgTick;
        private Vector2  _moveAccum;
        private Vector3  _lastAimWorld;

        // Triggerbot state
        private bool _triggerbotFiring;
        private long _triggerbotFireAt;   // Stopwatch ticks when we should click
        private long _triggerbotReleaseAt;

        // Calibration state
        public enum CalibState { Idle, WaitingDelay, WaitingResult, Done }
        public static CalibState  CalibStatus      = CalibState.Idle;
        public static float       CalibPxPerCountX = 0f;
        public static float       CalibPxPerCountY = 0f;
        public static string      CalibLog         = "Not run yet.";
        private static Vector2    _calibPreScreen;
        private static int        _calibSentX;
        private static int        _calibSentY;

        public ABIGameConfig Config => _cfg;
        public bool IsRunning       => _running;

        public ABIController(OverlayTheme theme)
        {
            _theme = theme;
            _cfg   = ConfigManager.Load<ABIGameConfig>("abi_config.json");
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  Lifecycle
        // /////////////////////////////////////////////////////////////////////////////////////

        public void Start()
        {
            if (_running || !DmaMemory.IsAttached) return;
            _running = true;
            TimerResolution.Enable1ms();
            ABIPlayers.StartCache();
            ABILoot.Start();
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            ABIPlayers.Stop();
            ABILoot.Stop();
            TimerResolution.Disable1ms();
        }

        public void Dispose()
        {
            Stop();
            ConfigManager.Save(_cfg, "abi_config.json");
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  Per-frame tick (call from OverlayApp.OnUpdate or OnRender)
        // /////////////////////////////////////////////////////////////////////////////////////

        public void Tick()
        {
            if (!_running) return;
            RunAimbot();
            RunTriggerbot();
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  Render  ??  call every frame AFTER ImGui.NewFrame
        // /////////////////////////////////////////////////////////////////////////////////////

        public void Render()
        {
            if (!_running) return;

            float zoom = 1f;
            if (ABIPlayers.TryGetZoom(out var zi) && zi.Valid)
                zoom = MathF.Max(1f, zi.Zoom);

            // ESP ?? player ESP only when actors are known
            if (ABIPlayers.ActorList.Count > 0)
                ABIESP.Render(_cfg, _theme, zoom);

            // Loot ESP + widget run independently ?? loot exists even when no players are around
            ABILootESP.Render(_cfg, ABIPlayers.LocalPosition, ABIPlayers.Camera, zoom);
            ABILootWidget.Render(_cfg, ABIPlayers.LocalPosition);

            // Debug widgets
            ABIPlayerWidget.Enabled = _cfg.DrawPlayerWidget;
            ABIPlayerWidget.Render(_cfg);

            // Aimbot FOV circle overlay
            DrawAimbotOverlay(zoom);
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  Aimbot
        // /////////////////////////////////////////////////////////////////////////////////////

        private void RunAimbot()
        {
            if (!_cfg.AimbotEnabled) return;
            if (CalibStatus == CalibState.WaitingDelay || CalibStatus == CalibState.WaitingResult) return;
            if (!IsKeyHeld(_cfg.AimbotKey)) { _moveAccum = Vector2.Zero; _lastAimScreen = null; _lastAimWorld = Vector3.Zero; return; }

            _aimbotDbgTick++;
            bool log = false;

            if (log) Logger.Info($"[Aimbot] Key held. Running={_running} Attached={DmaMemory.IsAttached}");

            if (!ABIPlayers.TryGetFrame(out var fr))
            {
                if (log) Logger.Info("[Aimbot] TryGetFrame failed ?? no frame published yet");
                return;
            }
            if (fr.Positions == null || fr.Positions.Count == 0)
            {
                if (log) Logger.Info($"[Aimbot] Frame OK but Positions empty (stamp={fr.Stamp})");
                return;
            }

            if (log) Logger.Info($"[Aimbot] Frame OK ?? {fr.Positions.Count} actors, cam fov={fr.Cam.Fov:F1}");

            var io    = ImGui.GetIO();
            float W   = io.DisplaySize.X;
            float H   = io.DisplaySize.Y;
            var center = new Vector2(W * 0.5f, H * 0.5f);

            float zoom = 1f;
            if (ABIPlayers.TryGetZoom(out var zi) && zi.Valid) zoom = MathF.Max(1f, zi.Zoom);

            float maxFov = MathF.Max(16f, _cfg.AimbotFovPx);
            float bestMetric = float.MaxValue;
            Vector3 bestAimWorld = default;
            Vector2 bestAimScreen = default;
            bool found = false;

            int cDead=0, cVis=0, cDist=0, cW2S=0, cFov=0;

            foreach (var ap in fr.Positions)
            {
                if (ap.IsDead) { cDead++; continue; }
                if (_cfg.AimbotRequireVisible && !ap.IsVisible) { cVis++; continue; }

                float distM = Vector3.Distance(ABIPlayers.LocalPosition, ap.Position) / 100f;
                if (distM > _cfg.AimbotMaxMeters) { cDist++; continue; }

                // Pick aim point ?? with sanity checks on bone positions
                Vector3 aimWorld = ap.Position; // root fallback
                if (ABIPlayers.TryGetSkeleton(ap.Pawn, out var bones) && bones != null
                    && _cfg.AimbotTargetBone < bones.Length)
                {
                    var bone = bones[_cfg.AimbotTargetBone];
                    // Bone must be finite and within 300cm of root (garbage reads are often 0,0,0 or wildly off)
                    if (float.IsFinite(bone.X) && float.IsFinite(bone.Y) && float.IsFinite(bone.Z)
                        && Vector3.Distance(bone, ap.Position) < 30000f   // 300m in UU
                        && bone != Vector3.Zero)
                        aimWorld = bone;
                }

                // Screen position must land on screen ??20% margin
                if (!ABIMath.WorldToScreen(aimWorld, ABIPlayers.Camera, W, H, zoom, out var pt)) { cW2S++; continue; }
                if (pt.X < -W * 0.2f || pt.X > W * 1.2f || pt.Y < -H * 0.2f || pt.Y > H * 1.2f) { cW2S++; continue; }

                float dist = Vector2.Distance(center, pt);
                if (dist > maxFov) { cFov++; continue; }
                if (dist < bestMetric) { bestMetric = dist; bestAimWorld = aimWorld; bestAimScreen = pt; found = true; }
            }

            if (!found)
            {
                if (log) Logger.Info($"[Aimbot] No target ?? dead={cDead} notVisible={cVis} tooFar={cDist} w2sFail={cW2S} outsideFov={cFov} fov={maxFov:F0}px screen={W:F0}x{H:F0}");
                return;
            }

            // ???? Mouse delta ??????????????????????????????????????????????????????????????????????????????????????????????????????????
            var screenDelta = bestAimScreen - center;

            // Bone glitch guard: if the world position jumped >100 UU since last frame, skip.
            // Screen position naturally changes as we move the camera ?? world position should not.
            if (_lastAimWorld != Vector3.Zero && Vector3.Distance(bestAimWorld, _lastAimWorld) > 100f)
            {
                Logger.Info($"[Aimbot] Skipped ?? world pos jumped {Vector3.Distance(bestAimWorld, _lastAimWorld):F0} UU (bone glitch)");
                _lastAimWorld = bestAimWorld;
                return;
            }
            _lastAimWorld = bestAimWorld;

            if (MathF.Abs(screenDelta.X) < _cfg.AimbotDeadzonePx &&
                MathF.Abs(screenDelta.Y) < _cfg.AimbotDeadzonePx)
                return;

            _lastAimScreen = bestAimScreen;
            _lastAimTicks  = Stopwatch.GetTimestamp();

            // Accumulate fractional moves ?? only send whole counts.
            // This prevents the 1-count-overshoot oscillation when sensitivity is high.
            _moveAccum += screenDelta * _cfg.AimbotPixelPower;

            int mx = (int)MathF.Truncate(_moveAccum.X);
            int my = (int)MathF.Truncate(_moveAccum.Y);
            _moveAccum -= new Vector2(mx, my);   // keep the remainder

            if (_cfg.AimbotInvertY) my = -my;

            if (mx == 0 && my == 0)
            {
                if (log) Logger.Info($"[Aimbot] Accumulating ?? accum=({_moveAccum.X:F2},{_moveAccum.Y:F2}) screenDelta=({screenDelta.X:F1},{screenDelta.Y:F1})");
                return;
            }

            if (log) Logger.Info($"[Aimbot] move({mx},{my})  screenDelta=({screenDelta.X:F1},{screenDelta.Y:F1})  accum=({_moveAccum.X:F2},{_moveAccum.Y:F2})");
            if (Device.connected)
                Device.move(mx, my);
        }

        private static bool IsKeyHeld(int vk) => InputManager.IsKeyDown(vk);

        private void RunTriggerbot()
        {
            if (!_cfg.TriggerbotEnabled) return;

            long now = Stopwatch.GetTimestamp();
            long ticksPerMs = Stopwatch.Frequency / 1000;

            // Handle pending release
            if (_triggerbotFiring)
            {
                if (now >= _triggerbotReleaseAt)
                {
                    Device.press(MakcuMouseButton.Left, 0);
                    _triggerbotFiring = false;
                }
                return;
            }

            // Handle pending press
            if (_triggerbotFireAt != 0 && now >= _triggerbotFireAt)
            {
                _triggerbotFireAt    = 0;
                _triggerbotFiring    = true;
                _triggerbotReleaseAt = now + ticksPerMs * _cfg.TriggerbotDurationMs;
                Device.press(MakcuMouseButton.Left, 1);
                Logger.Info("[Trigger] Fire!");
                return;
            }

            if (!IsKeyHeld(_cfg.TriggerbotKey)) { _triggerbotFireAt = 0; return; }

            if (!ABIPlayers.TryGetFrame(out var fr) || fr.Positions == null) return;

            var io     = ImGui.GetIO();
            float W    = io.DisplaySize.X, H = io.DisplaySize.Y;
            var center = new Vector2(W * 0.5f, H * 0.5f);
            float zoom = 1f;
            if (ABIPlayers.TryGetZoom(out var zi) && zi.Valid) zoom = MathF.Max(1f, zi.Zoom);

            const float CrosshairPx = 12f;
            bool onTarget = false;

            foreach (var ap in fr.Positions)
            {
                if (ap.IsDead) continue;
                if (_cfg.TriggerbotRequireVisible && !ap.IsVisible) continue;

                if (ABIPlayers.TryGetSkeleton(ap.Pawn, out var bn) && bn != null)
                {
                    foreach (var b in bn)
                    {
                        if (!float.IsFinite(b.X) || b == Vector3.Zero) continue;
                        if (!ABIMath.WorldToScreen(b, ABIPlayers.Camera, W, H, zoom, out var bpt)) continue;
                        if (Vector2.Distance(center, bpt) <= CrosshairPx) { onTarget = true; break; }
                    }
                }
                if (onTarget) break;

                if (!ABIMath.WorldToScreen(ap.Position, ABIPlayers.Camera, W, H, zoom, out var pt)) continue;
                if (Vector2.Distance(center, pt) <= CrosshairPx) { onTarget = true; break; }
            }

            if (onTarget && _triggerbotFireAt == 0)
            {
                _triggerbotFireAt = now + ticksPerMs * _cfg.TriggerbotDelayMs;
                Logger.Info($"[Trigger] On target ?? firing in {_cfg.TriggerbotDelayMs}ms");
            }
            else if (!onTarget)
            {
                _triggerbotFireAt = 0;
            }
        }

        public void TriggerCalibration()
        {
            if (CalibStatus == CalibState.WaitingDelay || CalibStatus == CalibState.WaitingResult)
            {
                CalibLog = "Already running ?? wait for it to finish.";
                return;
            }

            CalibStatus = CalibState.WaitingDelay;
            CalibLog    = "Waiting 10s ?? aim at a static target...";
            Logger.Info("[Calib] Started. Waiting 10 seconds.");

            Task.Run(async () =>
            {
                // ???? 10 second countdown ????????????????????????????????????????????????????????????????????????????????????
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalSeconds < 10.0)
                {
                    int left = 10 - (int)sw.Elapsed.TotalSeconds;
                    CalibLog = $"Firing in {left}s ?? keep aim on target...";
                    await Task.Delay(200);
                }

                // ???? Read pre-move position ??????????????????????????????????????????????????????????????????????????????
                CalibLog = "Reading pre-move target position...";
                Vector2 preScreen = default;
                int sentX = 0, sentY = 0;
                bool found = false;

                if (ABIPlayers.TryGetFrame(out var fr) && fr.Positions != null)
                {
                    var io = ImGui.GetIO();
                    float W = io.DisplaySize.X, H = io.DisplaySize.Y;
                    var center = new Vector2(W * 0.5f, H * 0.5f);
                    float zoom = 1f;
                    if (ABIPlayers.TryGetZoom(out var zi) && zi.Valid) zoom = MathF.Max(1f, zi.Zoom);

                    float bestDist = float.MaxValue;
                    foreach (var ap in fr.Positions)
                    {
                        if (ap.IsDead) continue;
                        Vector3 aw = ap.Position;
                        if (ABIPlayers.TryGetSkeleton(ap.Pawn, out var bn) && bn != null && _cfg.AimbotTargetBone < bn.Length)
                        { var b = bn[_cfg.AimbotTargetBone]; if (float.IsFinite(b.X) && b != Vector3.Zero) aw = b; }
                        if (!ABIMath.WorldToScreen(aw, ABIPlayers.Camera, W, H, zoom, out var pt)) continue;
                        float d = Vector2.Distance(center, pt);
                        if (d < bestDist) { bestDist = d; preScreen = pt; found = true; }
                    }

                    if (found)
                    {
                        var delta = preScreen - center;
                        sentX = delta.X >= 0 ? 1 : -1;
                        sentY = delta.Y >= 0 ? 1 : -1;
                    }
                }

                if (!found)
                {
                    CalibLog = "No target found ?? look at an enemy and try again.";
                    CalibStatus = CalibState.Done;
                    return;
                }

                Logger.Info($"[Calib] Pre=({preScreen.X:F1},{preScreen.Y:F1})  Sending move({sentX},{sentY})");
                CalibLog = $"Pre=({preScreen.X:F1},{preScreen.Y:F1})  Sending move({sentX},{sentY})...";
                CalibStatus = CalibState.WaitingResult;

                // ???? Send the move ????????????????????????????????????????????????????????????????????????????????????????????????
                if (Device.connected) Device.move(sentX, sentY);

                // ???? Wait 500ms for camera to settle ????????????????????????????????????????????????????????????
                await Task.Delay(500);

                // ???? Read post-move position ????????????????????????????????????????????????????????????????????????????
                Vector2 postScreen = default;
                bool foundPost = false;

                if (ABIPlayers.TryGetFrame(out var fr2) && fr2.Positions != null)
                {
                    var io2 = ImGui.GetIO();
                    float W2 = io2.DisplaySize.X, H2 = io2.DisplaySize.Y;
                    var center2 = new Vector2(W2 * 0.5f, H2 * 0.5f);
                    float zoom2 = 1f;
                    if (ABIPlayers.TryGetZoom(out var zi2) && zi2.Valid) zoom2 = MathF.Max(1f, zi2.Zoom);

                    float bestDist2 = float.MaxValue;
                    foreach (var ap in fr2.Positions)
                    {
                        if (ap.IsDead) continue;
                        Vector3 aw = ap.Position;
                        if (ABIPlayers.TryGetSkeleton(ap.Pawn, out var bn) && bn != null && _cfg.AimbotTargetBone < bn.Length)
                        { var b = bn[_cfg.AimbotTargetBone]; if (float.IsFinite(b.X) && b != Vector3.Zero) aw = b; }
                        if (!ABIMath.WorldToScreen(aw, ABIPlayers.Camera, W2, H2, zoom2, out var pt)) continue;
                        float d = Vector2.Distance(center2, pt);
                        if (d < bestDist2) { bestDist2 = d; postScreen = pt; foundPost = true; }
                    }
                }

                if (!foundPost)
                {
                    CalibLog = "Could not read target after move.";
                    CalibStatus = CalibState.Done;
                    return;
                }

                // ???? Compute result ??????????????????????????????????????????????????????????????????????????????????????????????
                float dxPx = (preScreen.X - postScreen.X) * sentX;
                float dyPx = (preScreen.Y - postScreen.Y) * sentY;
                CalibPxPerCountX = dxPx;
                CalibPxPerCountY = dyPx;
                float idealX = dxPx > 0.5f ? 1f / dxPx : 0f;
                float idealY = dyPx > 0.5f ? 1f / dyPx : 0f;

                CalibLog = $"Pre=({preScreen.X:F1},{preScreen.Y:F1})  Post=({postScreen.X:F1},{postScreen.Y:F1})\n" +
                           $"px/count: X={dxPx:F1}  Y={dyPx:F1}\n" +
                           $"Ideal power: X={idealX:F4}  Y={idealY:F4}";
                Logger.Info($"[Calib] {CalibLog.Replace('\n', ' ')}");
                CalibStatus = CalibState.Done;
            });
        }

        private void DrawAimbotOverlay(float zoom)
        {
            if (!_cfg.AimbotEnabled) return;

            var io    = ImGui.GetIO();
            var center = io.DisplaySize * 0.5f;
            var dl    = ImGui.GetBackgroundDrawList();

            float r = MathF.Max(4f, _cfg.AimbotFovPx);
            dl.AddCircle(center, r, 0x54FFFFFF, 64, 1.6f);

            // Aim-line to last target (fades after 350 ms)
            const double maxAgeMs = 350.0;
            if (_lastAimScreen.HasValue)
            {
                double ageMs = (Stopwatch.GetTimestamp() - _lastAimTicks) * 1000.0 / Stopwatch.Frequency;
                if (ageMs <= maxAgeMs)
                {
                    var tgt = _lastAimScreen.Value;
                    dl.AddLine(center, tgt, 0xE617E617, 2f);
                    dl.AddCircleFilled(tgt, 4f, 0xE617E617, 24);
                    dl.AddCircle(tgt, 4f, 0xE6000000, 24, 1.6f);
                }
            }
        }
    }
}