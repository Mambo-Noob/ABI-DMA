using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.ABI
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI ESP  ¡¤  Draws player boxes, skeletons, health bars, name labels and death markers.
    //  Consumes the ABIPlayers frame snapshot; call Render() each frame.
    // /////////////////////////////////////////////////////////////////////////////////////

    public static class ABIESP
    {
        public static void Render(ABIGameConfig cfg, OverlayTheme theme, float zoomEff)
        {
            if (!ABIPlayers.TryGetFrame(out var fr)) return;
            var positions = fr.Positions;
            if (positions == null || positions.Count == 0) return;

            List<ABIPlayers.ABIPlayer> actors;
            lock (ABIPlayers.Sync)
            {
                if (ABIPlayers.ActorList.Count == 0) return;
                actors = new List<ABIPlayers.ABIPlayer>(ABIPlayers.ActorList);
            }

            // Build pos map
            var posMap = new Dictionary<ulong, ABIPlayers.ActorPos>(positions.Count);
            foreach (var ap in positions) posMap[ap.Pawn] = ap;

            var list = ImGui.GetForegroundDrawList();
            var io   = ImGui.GetIO();
            float scrW = io.DisplaySize.X, scrH = io.DisplaySize.Y;
            float zoom = MathF.Max(1f, zoomEff);

            // Always use the live camera ¡ª it is updated by CacheCameraLoop at ~1ms
            // regardless of when the frame snapshot was published. This eliminates the
            // lag when rotating the view because fr.Cam can be multiple ms stale.
            var cam   = ABIPlayers.Camera;
            var local = ABIPlayers.LocalPosition;

            bool W2S(in Vector3 ws, out Vector2 sp)
                => ABIMath.WorldToScreen(ws, cam, scrW, scrH, zoom, out sp);

            for (int i = 0; i < actors.Count; i++)
            {
                if (!posMap.TryGetValue(actors[i].Pawn, out var ap)) continue;
                if (IsBogusPos(ap.Position)) continue;

                float distM = Vector3.Distance(local, ap.Position) / 100f;
                if (distM > cfg.MaxDistance) continue;

                bool hasBones = ABIPlayers.TryGetSkeleton(actors[i].Pawn, out var bones) && bones != null && bones.Length >= 20;

                // Derive head and foot screen positions directly from bones when possible.
                // Fall back to ap.Position (capsule centre) only when bones aren't ready.
                Vector2 headSc = default, footSc = default;
                bool hasHeadFoot = false;
                if (hasBones)
                {
                    // Average the two feet so crouching / uneven ground is handled correctly
                    Vector3 feetWorld = (bones![ABISkeleton.IDX_Foot_L] + bones[ABISkeleton.IDX_Foot_R]) * 0.5f;
                    hasHeadFoot = W2S(bones[ABISkeleton.IDX_Head], out headSc)
                               && W2S(feetWorld,                    out footSc);
                    if (!hasHeadFoot) { headSc = footSc = default; }
                }

                // Need at least one valid screen point to render anything
                Vector2 fallbackSc = default;
                if (!hasHeadFoot && !W2S(ap.Position, out fallbackSc)) continue;
                // For death markers use a sensible anchor
                Vector2 markerSc = hasHeadFoot ? footSc : fallbackSc;

                // //// Death markers ///////////////////////////////////////////////////
                if (ap.IsDead)
                {
                    if (cfg.DrawDeathMarkers && distM <= cfg.DeathMarkerMaxDist)
                    {
                        DrawDiamond(list, markerSc, distM, cfg.DeathMarkerBaseSize,
                            OverlayTheme.ToU32(cfg.DeadFill),
                            OverlayTheme.ToU32(cfg.DeadOutline));
                        if (cfg.DrawDistance)
                            list.AddText(new Vector2(markerSc.X - 14, markerSc.Y + cfg.DeathMarkerBaseSize + 4),
                                0xFFFFFFFF, $"{distM:F1} m");
                    }
                    continue;
                }

                bool isVis = ap.IsVisible;
                uint clrName = OverlayTheme.ToU32(actors[i].IsBot ? cfg.ColorBot : cfg.ColorPlayer);
                uint clrBox  = OverlayTheme.ToU32(isVis ? cfg.ColorBoxVisible  : cfg.ColorBoxInvisible);
                uint clrSkel = OverlayTheme.ToU32(isVis ? cfg.ColorSkelVisible : cfg.ColorSkelInvisible);

                // //// Bounding box ////////////////////////////////////////////////////
                Vector2 min2, max2;

                if (hasHeadFoot)
                {
                    // Use all 8 landmark bones for the tightest accurate box
                    int[] bb = {
                        ABISkeleton.IDX_Head,    ABISkeleton.IDX_Neck,
                        ABISkeleton.IDX_Spine_03, ABISkeleton.IDX_Pelvis,
                        ABISkeleton.IDX_Hand_L,  ABISkeleton.IDX_Hand_R,
                        ABISkeleton.IDX_Foot_L,  ABISkeleton.IDX_Foot_R
                    };

                    Vector2 bbMin = headSc, bbMax = headSc;
                    foreach (int bi in bb)
                    {
                        if (!W2S(bones![bi], out var sp)) continue;
                        if (sp.X < bbMin.X) bbMin.X = sp.X;
                        if (sp.Y < bbMin.Y) bbMin.Y = sp.Y;
                        if (sp.X > bbMax.X) bbMax.X = sp.X;
                        if (sp.Y > bbMax.Y) bbMax.Y = sp.Y;
                    }

                    float wBox = MathF.Max(18f, bbMax.X - bbMin.X);
                    float hBox = MathF.Max(42f, bbMax.Y - bbMin.Y);
                    float padX = MathF.Min(12f, wBox * 0.12f);
                    float padY = MathF.Min(14f, hBox * 0.10f);
                    min2 = new Vector2(bbMin.X - padX, bbMin.Y - padY);
                    max2 = new Vector2(bbMax.X + padX, bbMax.Y + padY);
                }
                else
                {
                    // No bones yet ?? estimate from capsule centre
                    float bh = Math.Clamp(150f / MathF.Max(distM, 3f), 30f, 250f);
                    float bw = bh * 0.40f;
                    min2 = new Vector2(fallbackSc.X - bw / 2, fallbackSc.Y - bh);
                    max2 = new Vector2(fallbackSc.X + bw / 2, fallbackSc.Y);
                }

                if (cfg.DrawBoxes)    DrawCornerBox(list, min2, max2, clrBox, 1.5f);
                if (cfg.DrawNames)    list.AddText(new Vector2((min2.X + max2.X) * 0.5f - 18, min2.Y - 18), clrName, actors[i].IsBot ? "BOT" : "PMC");
                if (cfg.DrawDistance) list.AddText(new Vector2((min2.X + max2.X) * 0.5f - 12, max2.Y + 4),  0xFFFFFFFF, $"{distM:F1} m");

                if (ap.HealthMax > 1f)
                    DrawHealthBar(list, new Vector2(min2.X, min2.Y - 8f), max2.X - min2.X, ap.Health, ap.HealthMax);

                if (cfg.DrawSkeletons && hasBones && distM <= cfg.MaxSkeletonDistance)
                    ABISkeleton.Draw(list, bones!, cam, scrW, scrH, clrSkel, zoom);
            }
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  Helpers
        // /////////////////////////////////////////////////////////////////////////////////////

        public static bool IsBogusPos(in Vector3 p)
            => MathF.Abs(p.X) <= 0.5f && MathF.Abs(p.Y) <= 0.5f && MathF.Abs(p.Z + 90f) <= 1.0f;

        private static void DrawCornerBox(ImDrawListPtr list, Vector2 min, Vector2 max, uint color, float t)
        {
            float w = max.X - min.X;
            float h = max.Y - min.Y;
            float c = MathF.Min(20, MathF.Min(w * 0.25f, h * 0.25f));

            list.AddLine(min, new(min.X + c, min.Y), color, t);
            list.AddLine(min, new(min.X, min.Y + c), color, t);
            list.AddLine(new(max.X - c, min.Y), new(max.X, min.Y), color, t);
            list.AddLine(new(max.X, min.Y), new(max.X, min.Y + c), color, t);
            list.AddLine(new(min.X, max.Y - c), new(min.X, max.Y), color, t);
            list.AddLine(new(min.X, max.Y), new(min.X + c, max.Y), color, t);
            list.AddLine(new(max.X - c, max.Y), max, color, t);
            list.AddLine(max, new(max.X, max.Y - c), color, t);
        }

        private static void DrawDiamond(ImDrawListPtr list, Vector2 center, float distM,
                                         float baseSizePx, uint fill, uint outline)
        {
            float sz = Math.Clamp(baseSizePx * (120f / MathF.Max(distM, 8f)), baseSizePx * 0.4f, baseSizePx * 1.2f);
            var p0 = new Vector2(center.X, center.Y - sz);
            var p1 = new Vector2(center.X + sz, center.Y);
            var p2 = new Vector2(center.X, center.Y + sz);
            var p3 = new Vector2(center.X - sz, center.Y);
            list.AddQuadFilled(p0, p1, p2, p3, fill);
            float th = MathF.Max(1.2f, baseSizePx * 0.16f);
            list.AddLine(p0, p1, outline, th); list.AddLine(p1, p2, outline, th);
            list.AddLine(p2, p3, outline, th); list.AddLine(p3, p0, outline, th);
        }

        private static void DrawHealthBar(ImDrawListPtr list, Vector2 topLeft, float width,
                                           float health, float maxHealth)
        {
            const float h = 5f;
            float pct = Math.Clamp(maxHealth > 0f ? health / maxHealth : 0f, 0f, 1f);
            var bgMin = topLeft;
            var bgMax = new Vector2(topLeft.X + width, topLeft.Y + h);
            list.AddRectFilled(bgMin, bgMax, 0xD9260F26, 2f); // dark red bg
            float fillW = width * pct;
            if (fillW > 0.5f)
                list.AddRectFilled(bgMin, new Vector2(topLeft.X + fillW, topLeft.Y + h), 0xF226E617, 2f); // green fill
            list.AddRect(bgMin, bgMax, 0xFF000000, 2f, ImDrawFlags.None, 1f);
        }
    }
}