using System;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.Config;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.Rendering
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  ESP RENDERER
    //  All ESP drawing goes through here â€? boxes, skeletons, bars, snaplines.
    //  Uses the background ImDrawList for overlay (drawn over game world).
    //  Zero allocation hot path: all draw calls go directly to native draw list.
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class EspRenderer
    {
        private readonly OverlaySettings _settings;
        private readonly OverlayTheme    _theme;

        // Skeleton bone pairs (index into a standard 15-bone array)
        private static readonly (int, int)[] BonePairs = {
            (0,  1),  // head -> neck
            (1,  2),  // neck -> spine
            (2,  3),  // spine -> hip
            (2,  4),  // spine -> L-shoulder
            (4,  5),  // L-shoulder -> L-elbow
            (5,  6),  // L-elbow -> L-wrist
            (2,  7),  // spine -> R-shoulder
            (7,  8),  // R-shoulder -> R-elbow
            (8,  9),  // R-elbow -> R-wrist
            (3, 10),  // hip -> L-hip
            (10,11),  // L-hip -> L-knee
            (11,12),  // L-knee -> L-ankle
            (3, 13),  // hip -> R-hip
            (13,14),  // R-hip -> R-knee
            (14,15),  // R-knee -> R-ankle
        };

        public EspRenderer(OverlaySettings settings, OverlayTheme theme)
        {
            _settings = settings;
            _theme    = theme;
        }

        // //// Main draw entry point //////////////////////////////////////////////////////////////////////////////////////////////////
        public void Render(ImDrawListPtr dl, ReadOnlySpan<EspEntity> entities, Vector2 screenSize)
        {
            if (!_settings.EspEnabled) return;

            var screenCenter = screenSize * 0.5f;

            foreach (ref readonly var e in entities)
            {
                if (!e.IsValid) continue;
                if (_settings.EspTeamCheck && e.IsTeam) continue;
                if (e.Distance > _settings.EspMaxDistance) continue;

                // Pick color based on team / visibility
                var boxColor = e.IsTeam
                    ? _theme.EspBoxColorTeam
                    : (_settings.EspVisibilityCheck && e.IsVisible
                        ? _theme.EspBoxColor
                        : _theme.EspBoxColorEnemy);

                if (_settings.EspBoxes)
                {
                    if (_settings.EspBoxCorners)
                        DrawCornerBox(dl, e.BoxMin, e.BoxMax, boxColor, _theme.EspBoxThickness);
                    else
                        DrawFullBox(dl, e.BoxMin, e.BoxMax, boxColor, _theme.EspBoxThickness, _theme.EspBoxRounding);
                }

                if (_settings.EspHealthBar)
                    DrawHealthBar(dl, e.BoxMin, e.BoxMax, e.HealthFraction);

                if (_settings.EspSkeleton && e.BonePositions != null)
                    DrawSkeleton(dl, e.BonePositions);

                if (_settings.EspHeadCircle && e.HeadPos.X > 0)
                    DrawHeadCircle(dl, e.HeadPos, e.BoxMax.Y - e.BoxMin.Y);

                if (_settings.EspNames && !string.IsNullOrEmpty(e.Name))
                    DrawName(dl, e.BoxMin, e.BoxMax, e.Name);

                if (_settings.EspDistance)
                    DrawDistance(dl, e.BoxMax, e.Distance);

                if (_settings.EspSnaplines)
                    DrawSnapline(dl, screenCenter, e.BoxMin, e.BoxMax);
            }
        }

        // //// Full bounding box //////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawFullBox(ImDrawListPtr dl, Vector2 min, Vector2 max,
                                  Vector4 color, float thickness, float rounding)
        {
            dl.AddRect(min, max, OverlayTheme.ToU32(color), rounding, ImDrawFlags.None, thickness);
        }

        // //// Corner-style box ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawCornerBox(ImDrawListPtr dl, Vector2 min, Vector2 max,
                                    Vector4 color, float thickness)
        {
            uint c = OverlayTheme.ToU32(color);
            float w = max.X - min.X;
            float h = max.Y - min.Y;
            float cw = w * 0.22f; // corner width = 22% of box
            float ch = h * 0.22f;

            // Top-lABI
            dl.AddLine(min,                           min + new Vector2(cw, 0),  c, thickness);
            dl.AddLine(min,                           min + new Vector2(0, ch),  c, thickness);
            // Top-right
            dl.AddLine(new Vector2(max.X, min.Y),     new Vector2(max.X - cw, min.Y), c, thickness);
            dl.AddLine(new Vector2(max.X, min.Y),     new Vector2(max.X, min.Y + ch), c, thickness);
            // Bottom-lABI
            dl.AddLine(new Vector2(min.X, max.Y),     new Vector2(min.X + cw, max.Y), c, thickness);
            dl.AddLine(new Vector2(min.X, max.Y),     new Vector2(min.X, max.Y - ch), c, thickness);
            // Bottom-right
            dl.AddLine(max,                           max - new Vector2(cw, 0),  c, thickness);
            dl.AddLine(max,                           max - new Vector2(0, ch),  c, thickness);
        }

        // //// Health bar (lABI side, vertical) //////////////////////////////////////////////////////////////////////////
        private void DrawHealthBar(ImDrawListPtr dl, Vector2 min, Vector2 max, float fraction)
        {
            float barW  = _theme.EspHealthBarWidth;
            float gap   = _theme.EspHealthBarGap;
            float x     = min.X - gap - barW;
            float yFull = max.Y;
            float yTop  = min.Y;
            float filled= yFull - (yFull - yTop) * Math.Clamp(fraction, 0f, 1f);

            // Background
            dl.AddRectFilled(new Vector2(x, yTop), new Vector2(x + barW, yFull),
                OverlayTheme.ToU32(_theme.EspHealthBg), 1f);
            // Bar
            var healthColor = HealthGradient(fraction);
            dl.AddRectFilled(new Vector2(x, filled), new Vector2(x + barW, yFull),
                OverlayTheme.ToU32(healthColor), 1f);
        }

        private static Vector4 HealthGradient(float t)
        {
            // Green â†? Yellow â†? Red
            if (t > 0.5f)
                return Vector4.Lerp(new Vector4(1, 0.85f, 0, 1), new Vector4(0.22f, 0.85f, 0.40f, 1), (t - 0.5f) * 2);
            else
                return Vector4.Lerp(new Vector4(0.96f, 0.27f, 0.27f, 1), new Vector4(1, 0.85f, 0, 1), t * 2);
        }

        // //// Skeleton ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawSkeleton(ImDrawListPtr dl, Vector2[] bones)
        {
            if (bones.Length < 16) return;
            uint c = OverlayTheme.ToU32(_theme.EspSkeletonColor);
            float t = _theme.EspSkeletonThick;
            foreach (var (a, b) in BonePairs)
            {
                if (b >= bones.Length) continue;
                var pa = bones[a]; var pb = bones[b];
                if (pa.X == 0 || pb.X == 0) continue;  // offscreen sentinel
                dl.AddLine(pa, pb, c, t);
            }
        }

        // //// Head circle //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawHeadCircle(ImDrawListPtr dl, Vector2 headPos, float boxHeight)
        {
            float r = boxHeight * 0.07f; // ~7% of box height
            dl.AddCircle(headPos, r, OverlayTheme.ToU32(_theme.EspHeadCircleColor),
                0, _theme.EspHeadCircleThick);
        }

        // //// Name label //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawName(ImDrawListPtr dl, Vector2 min, Vector2 max, string name)
        {
            var textSize = ImGui.CalcTextSize(name);
            var pos = new Vector2((min.X + max.X) / 2 - textSize.X / 2, min.Y - textSize.Y - 2);
            dl.AddText(pos, OverlayTheme.ToU32(_theme.EspNameColor), name);
        }

        // //// Distance label ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawDistance(ImDrawListPtr dl, Vector2 boxMax, float dist)
        {
            string label = $"{(int)dist}m";
            var textSize = ImGui.CalcTextSize(label);
            var pos = new Vector2(boxMax.X - textSize.X / 2, boxMax.Y + 2);
            dl.AddText(pos, OverlayTheme.ToU32(_theme.EspDistanceColor), label);
        }

        // //// Snapline ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawSnapline(ImDrawListPtr dl, Vector2 from, Vector2 boxMin, Vector2 boxMax)
        {
            var target = new Vector2((boxMin.X + boxMax.X) / 2, boxMax.Y);
            dl.AddLine(from, target, OverlayTheme.ToU32(_theme.EspSnaplineColor), _theme.EspSnaplineThick);
        }
    }

    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  DATA STRUCT  (fill from your game memory reader)
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public readonly struct EspEntity
    {
        public readonly bool     IsValid;
        public readonly bool     IsTeam;
        public readonly bool     IsVisible;
        public readonly float    Distance;
        public readonly Vector2  BoxMin;
        public readonly Vector2  BoxMax;
        public readonly Vector2  HeadPos;
        public readonly float    HealthFraction;  // 0..1
        public readonly string?  Name;
        public readonly Vector2[]? BonePositions; // screen-space bone positions [0..15]

        public EspEntity(bool isValid, bool isTeam, bool isVisible, float dist,
                         Vector2 boxMin, Vector2 boxMax, Vector2 headPos,
                         float healthFrac, string? name, Vector2[]? bones)
        {
            IsValid        = isValid;
            IsTeam         = isTeam;
            IsVisible      = isVisible;
            Distance       = dist;
            BoxMin         = boxMin;
            BoxMax         = boxMax;
            HeadPos        = headPos;
            HealthFraction = healthFrac;
            Name           = name;
            BonePositions  = bones;
        }
    }
}
