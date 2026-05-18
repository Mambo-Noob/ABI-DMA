using System;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.DMA;

namespace ImGuiOverlay.ABI
{
    internal static class ABISkeleton
    {
        // //// Raw mesh bone indices ////////////////////////////////////////////////////
        private const int Pelvis      = 1;
        private const int Spine_01    = 12, Spine_02 = 13, Spine_03 = 14, Neck = 15, Head = 16;
        private const int Thigh_L     = 2,  Calf_L   = 4,  Foot_L  = 5;
        private const int Thigh_R     = 7,  Calf_R   = 9,  Foot_R  = 10;
        private const int Clavicle_L  = 50, UpperArm_L = 51, LowerArm_L = 52, Hand_L = 54;
        private const int Clavicle_R  = 20, UpperArm_R = 21, LowerArm_R = 22, Hand_R = 24;

        // Exposed to ABIPlayers for scatter loop
        internal static readonly int[] FetchIndices = {
            Pelvis,
            Spine_01, Spine_02, Spine_03, Neck, Head,
            Clavicle_L, UpperArm_L, LowerArm_L, Hand_L,
            Clavicle_R, UpperArm_R, LowerArm_R, Hand_R,
            Thigh_L, Calf_L, Foot_L,
            Thigh_R, Calf_R, Foot_R
        };

        // The highest raw bone index we ever fetch ！ used to validate bone count
        internal static readonly int MaxFetchIndex;

        static ABISkeleton()
        {
            int max = 0;
            foreach (int idx in FetchIndices) if (idx > max) max = idx;
            MaxFetchIndex = max;
        }

        // //// Public indices into worldPoints[] /////////////////////////////////////////
        public const int IDX_Pelvis     = 0;
        public const int IDX_Spine_01   = 1;
        public const int IDX_Spine_02   = 2;
        public const int IDX_Spine_03   = 3;
        public const int IDX_Neck       = 4;
        public const int IDX_Head       = 5;
        public const int IDX_Clavicle_L = 6;
        public const int IDX_UpperArm_L = 7;
        public const int IDX_LowerArm_L = 8;
        public const int IDX_Hand_L     = 9;
        public const int IDX_Clavicle_R = 10;
        public const int IDX_UpperArm_R = 11;
        public const int IDX_LowerArm_R = 12;
        public const int IDX_Hand_R     = 13;
        public const int IDX_Thigh_L    = 14;
        public const int IDX_Calf_L     = 15;
        public const int IDX_Foot_L     = 16;
        public const int IDX_Thigh_R    = 17;
        public const int IDX_Calf_R     = 18;
        public const int IDX_Foot_R     = 19;

        // //// Sanity check (exposed so ABIPlayers can reuse it) /////////////////////////
        internal static bool IsSanePublic(in FTransform t) => IsSane(t);

        private static bool IsSane(in FTransform t)
            => float.IsFinite(t.Scale3D.X) && float.IsFinite(t.Scale3D.Y) && float.IsFinite(t.Scale3D.Z)
            && float.IsFinite(t.Translation.X) && float.IsFinite(t.Translation.Y) && float.IsFinite(t.Translation.Z)
            && float.IsFinite(t.Rotation.W)
            && (Math.Abs(t.Scale3D.X) > 1e-4f || Math.Abs(t.Scale3D.Y) > 1e-4f || Math.Abs(t.Scale3D.Z) > 1e-4f)
            && Math.Abs(t.Translation.X) < 5e6f && Math.Abs(t.Translation.Y) < 5e6f && Math.Abs(t.Translation.Z) < 5e6f;

        // /////////////////////////////////////////////////////////////////////////////////////
        //  TryGetWorldBones  (kept for callers outside ABIPlayers)
        // /////////////////////////////////////////////////////////////////////////////////////
        public static bool TryGetWorldBones(ulong mesh, in FTransform ctwOverride, out Vector3[] worldPoints)
        {
            worldPoints = null;
            if (!IsSane(ctwOverride)) return false;

            try
            {
                ulong arr   = mesh + ABIOffsets.USkeletalMeshComponent_CachedComponentSpaceTransforms;
                ulong data  = DmaMemory.Read<ulong>(arr);
                int   count = DmaMemory.Read<int>(arr + 0x8);
                if (data == 0 || count <= 0 || MaxFetchIndex >= count) return false;

                const int SZ = 0x30;
                worldPoints = new Vector3[FetchIndices.Length];

                // Scatter-read all bone transforms in one shot
                using var sc = DmaMemory.CreateScatter(VmmSharpEx.Options.VmmFlags.NOCACHE);
                foreach (int bi in FetchIndices)
                    sc.PrepareReadValue<FTransform>(data + (ulong)(bi * SZ));
                sc.Execute();

                for (int i = 0; i < FetchIndices.Length; i++)
                {
                    if (!sc.ReadValue<FTransform>(data + (ulong)(FetchIndices[i] * SZ), out var boneCS))
                        return false;
                    if (!float.IsFinite(boneCS.Translation.X)) return false;
                    worldPoints[i] = ABIMath.TransformPosition(ctwOverride, boneCS.Translation);
                }

                return true;
            }
            catch { return false; }
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  Draw  ，  Renders skeleton lines onto an ImGui draw list
        // /////////////////////////////////////////////////////////////////////////////////////
        public static void Draw(ImDrawListPtr list, Vector3[] wp, FMinimalViewInfo cam,
                                float w, float h, uint color, float zoom = 1f)
        {
            zoom = MathF.Max(1f, float.IsFinite(zoom) ? zoom : 1f);

            // Project a world point to screen ！ returns false if behind camera
            bool Proj(int idx, out Vector2 s) =>
                ABIMath.WorldToScreen(wp[idx], cam, w, h, zoom, out s);

            void Seg(int a, int b, float thickness = 1.5f)
            {
                if (Proj(a, out var A) && Proj(b, out var B))
                    list.AddLine(A, B, color, thickness);
            }

            // //// Spine ！ slightly thicker as the main axis //////////////////////
            Seg(IDX_Pelvis,   IDX_Spine_01, 1.5f);
            Seg(IDX_Spine_01, IDX_Spine_02, 1.5f);
            Seg(IDX_Spine_02, IDX_Spine_03, 1.5f);
            Seg(IDX_Spine_03, IDX_Neck,     1.5f);

            // //// Head circle ////////////////////////////////////////////////////
            // Draw a small circle at the head position scaled by distance
            if (Proj(IDX_Head, out var headS) && Proj(IDX_Neck, out var neckS))
            {
                float headR = MathF.Max(3f, (headS - neckS).Length() * 0.6f);
                list.AddCircle(headS, headR, color, 12, 1.5f);
            }

            // //// Shoulder crossbar: Clavicle_L ！ Neck ！ Clavicle_R //////////////
            Seg(IDX_Clavicle_L, IDX_Clavicle_R, 1.2f);

            // //// Arms ///////////////////////////////////////////////////////////
            Seg(IDX_Clavicle_L, IDX_UpperArm_L, 1.3f);
            Seg(IDX_UpperArm_L, IDX_LowerArm_L, 1.3f);
            Seg(IDX_LowerArm_L, IDX_Hand_L,     1.3f);

            Seg(IDX_Clavicle_R, IDX_UpperArm_R, 1.3f);
            Seg(IDX_UpperArm_R, IDX_LowerArm_R, 1.3f);
            Seg(IDX_LowerArm_R, IDX_Hand_R,     1.3f);

            // //// Hip crossbar: Thigh_L ！ Pelvis ！ Thigh_R ///////////////////////
            Seg(IDX_Thigh_L, IDX_Thigh_R, 1.2f);

            // //// Legs ///////////////////////////////////////////////////////////
            Seg(IDX_Pelvis,  IDX_Thigh_L, 1.3f);
            Seg(IDX_Thigh_L, IDX_Calf_L,  1.3f);
            Seg(IDX_Calf_L,  IDX_Foot_L,  1.3f);

            Seg(IDX_Pelvis,  IDX_Thigh_R, 1.3f);
            Seg(IDX_Thigh_R, IDX_Calf_R,  1.3f);
            Seg(IDX_Calf_R,  IDX_Foot_R,  1.3f);
        }
    }
}