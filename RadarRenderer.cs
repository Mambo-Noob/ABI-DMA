using System;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.Config;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.Rendering
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  RADAR RENDERER
    //  Draws a mini-map / radar canvas as a dockable ImGui window.
    //  Supports drag-to-move, scroll-to-zoom, player icons, FOV cone.
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class RadarRenderer
    {
        private readonly OverlaySettings _settings;
        private readonly OverlayTheme    _theme;

        private float _zoom = 1.0f;  // runtime zoom (separate from settings default)

        public RadarRenderer(OverlaySettings settings, OverlayTheme theme)
        {
            _settings = settings;
            _theme    = theme;
            _zoom     = settings.RadarZoom;
        }

        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  Render  â€? call inside an ImGui window or as standalone
        //  localPlayer : position + yaw of the player (world space)
        //  entities    : positions + yaw of all other entities
        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Render(RadarLocalPlayer local, ReadOnlySpan<RadarEntity> entities)
        {
            if (!_settings.RadarEnabled) return;

            ImGui.SetNextWindowSize(_settings.RadarSize, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(_settings.RadarPosition, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0f);  // We draw our own background

            var flags = ImGuiWindowFlags.NoScrollbar
                      | ImGuiWindowFlags.NoScrollWithMouse
                      | ImGuiWindowFlags.NoTitleBar
                      | ImGuiWindowFlags.NoBringToFrontOnFocus;

            if (!ImGui.Begin("##radar", flags)) { ImGui.End(); return; }

            var dl      = ImGui.GetWindowDrawList();
            var winPos  = ImGui.GetWindowPos();
            var winSize = ImGui.GetWindowSize();

            // //// Save position for config ////////////////////////////////////////////////////////////////////////////////////
            _settings.RadarPosition = winPos;
            _settings.RadarSize     = winSize;

            // //// Background //////////////////////////////////////////////////////////////////////////////////////////////////////////////
            dl.AddRectFilled(winPos, winPos + winSize,
                OverlayTheme.ToU32(_theme.RadarBg), _theme.RadarRounding);
            dl.AddRect(winPos, winPos + winSize,
                OverlayTheme.ToU32(_theme.RadarBorder), _theme.RadarRounding,
                ImDrawFlags.None, _theme.RadarBorderSize);

            var center = winPos + winSize * 0.5f;

            // //// Grid ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (_settings.RadarShowGrid)
                DrawGrid(dl, winPos, winSize, center);

            // //// Crosshair //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            uint crossCol = OverlayTheme.ToU32(_theme.RadarCrosshair);
            dl.AddLine(new Vector2(center.X, winPos.Y + 4),
                       new Vector2(center.X, winPos.Y + winSize.Y - 4), crossCol, _theme.RadarCrosshairThick);
            dl.AddLine(new Vector2(winPos.X + 4, center.Y),
                       new Vector2(winPos.X + winSize.X - 4, center.Y), crossCol, _theme.RadarCrosshairThick);

            // //// FOV cone ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (_settings.RadarShowFov)
                DrawFovCone(dl, center, local.Yaw, winSize.X * 0.42f);

            // //// Entities ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            float scale = _zoom / (_settings.RadarScale > 0 ? _settings.RadarScale : 1f);

            foreach (ref readonly var e in entities)
            {
                var radarPos = WorldToRadar(local, e.WorldPos, center, scale);
                if (!IsInBounds(radarPos, winPos, winSize, 8)) continue;

                var dotColor = e.IsTeam ? _theme.RadarPlayerTeam : _theme.RadarPlayerEnemy;
                DrawPlayerIcon(dl, radarPos, e.Yaw - local.Yaw, dotColor, e.IsTeam);

                if (_settings.RadarShowNames && !string.IsNullOrEmpty(e.Name))
                {
                    dl.AddText(radarPos + new Vector2(7, -6),
                        OverlayTheme.ToU32(_theme.Text), e.Name);
                }
            }

            // //// Self (always center) ////////////////////////////////////////////////////////////////////////////////////////////
            DrawSelfIcon(dl, center);

            // //// Zoom scroll //////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (ImGui.IsWindowHovered())
            {
                float scroll = ImGui.GetIO().MouseWheel;
                if (scroll != 0)
                {
                    _zoom = Math.Clamp(_zoom + scroll * 0.15f, 0.3f, 8.0f);
                    _settings.RadarZoom = _zoom;
                }
            }

            ImGui.End();
        }

        // //// Grid ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawGrid(ImDrawListPtr dl, Vector2 winPos, Vector2 winSize, Vector2 center)
        {
            uint gc = OverlayTheme.ToU32(_theme.RadarGridColor);
            float step = Math.Max(winSize.X, winSize.Y) / 5f;

            for (float x = center.X % step; x < winPos.X + winSize.X; x += step)
                dl.AddLine(new Vector2(x, winPos.Y), new Vector2(x, winPos.Y + winSize.Y), gc, _theme.RadarGridThick);
            for (float y = center.Y % step; y < winPos.Y + winSize.Y; y += step)
                dl.AddLine(new Vector2(winPos.X, y), new Vector2(winPos.X + winSize.X, y), gc, _theme.RadarGridThick);
        }

        // //// FOV Cone ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawFovCone(ImDrawListPtr dl, Vector2 center, float yawDeg, float radius)
        {
            float fovHalf = 35f * MathF.PI / 180f;  // ~70Â° FOV
            float yaw     = (yawDeg - 90f) * MathF.PI / 180f;
            float a1      = yaw - fovHalf;
            float a2      = yaw + fovHalf;

            dl.PathLineTo(center);
            int  steps = 20;
            for (int i = 0; i <= steps; i++)
            {
                float a = a1 + (a2 - a1) * i / steps;
                dl.PathLineTo(center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius);
            }
            dl.PathFillConvex(OverlayTheme.ToU32(_theme.RadarFOVColor));

            dl.PathLineTo(center);
            for (int i = 0; i <= steps; i++)
            {
                float a = a1 + (a2 - a1) * i / steps;
                dl.PathLineTo(center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius);
            }
            dl.PathStroke(OverlayTheme.ToU32(_theme.RadarFOVBorder), ImDrawFlags.Closed, 1f);
        }

        // //// Player dot / triangle icon ////////////////////////////////////////////////////////////////////////////////////////
        private void DrawPlayerIcon(ImDrawListPtr dl, Vector2 pos, float relYawDeg, Vector4 color, bool isTeam)
        {
            uint c = OverlayTheme.ToU32(color);
            if (_settings.RadarRotate)
            {
                // Draw triangle pointing in direction
                float sz  = _theme.RadarPlayerTriSize;
                float yaw = relYawDeg * MathF.PI / 180f;
                float yawUp = yaw - MathF.PI / 2f;

                var tip   = pos + new Vector2(MathF.Cos(yawUp), MathF.Sin(yawUp)) * sz;
                float bAngle = 2.4f;
                var bl  = pos + new Vector2(MathF.Cos(yawUp + bAngle), MathF.Sin(yawUp + bAngle)) * sz * 0.7f;
                var br  = pos + new Vector2(MathF.Cos(yawUp - bAngle), MathF.Sin(yawUp - bAngle)) * sz * 0.7f;

                dl.AddTriangleFilled(tip, bl, br, c);
            }
            else
            {
                dl.AddCircleFilled(pos, _theme.RadarPlayerDotSize, c);
            }
        }

        // //// Self icon //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void DrawSelfIcon(ImDrawListPtr dl, Vector2 center)
        {
            float sz = _theme.RadarPlayerTriSize + 2f;
            uint  c  = OverlayTheme.ToU32(_theme.RadarPlayerSelf);
            // Always point straight up
            var tip = center + new Vector2(0, -sz);
            var bl  = center + new Vector2(-sz * 0.65f, sz * 0.5f);
            var br  = center + new Vector2( sz * 0.65f, sz * 0.5f);
            dl.AddTriangleFilled(tip, bl, br, c);
            dl.AddTriangle(tip, bl, br, OverlayTheme.ToU32(_theme.RadarBorder), 1f);
        }

        // //// World â†? Radar //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static Vector2 WorldToRadar(RadarLocalPlayer local,
                                            Vector2 worldPos, Vector2 center, float scale)
        {
            var delta = worldPos - local.WorldPos;
            if (local.Yaw != 0) delta = RotateVec(delta, -local.Yaw * MathF.PI / 180f);
            return center + new Vector2(delta.X, -delta.Y) * scale;
        }

        private static Vector2 RotateVec(Vector2 v, float rad)
            => new(v.X * MathF.Cos(rad) - v.Y * MathF.Sin(rad),
                   v.X * MathF.Sin(rad) + v.Y * MathF.Cos(rad));

        private static bool IsInBounds(Vector2 p, Vector2 winPos, Vector2 winSize, float pad)
            => p.X > winPos.X + pad && p.X < winPos.X + winSize.X - pad
            && p.Y > winPos.Y + pad && p.Y < winPos.Y + winSize.Y - pad;
    }

    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  DATA STRUCTS
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public readonly struct RadarLocalPlayer
    {
        public readonly Vector2 WorldPos;
        public readonly float   Yaw;
        public RadarLocalPlayer(Vector2 pos, float yaw) { WorldPos = pos; Yaw = yaw; }
    }

    public readonly struct RadarEntity
    {
        public readonly bool    IsTeam;
        public readonly Vector2 WorldPos;
        public readonly float   Yaw;
        public readonly string? Name;
        public RadarEntity(bool isTeam, Vector2 worldPos, float yaw, string? name = null)
        { IsTeam = isTeam; WorldPos = worldPos; Yaw = yaw; Name = name; }
    }
}
