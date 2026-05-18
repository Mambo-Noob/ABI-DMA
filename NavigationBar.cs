using System;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.Config;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.UI
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  NAVIGATION BAR  -  5 tabs + FPS counter on the right
    //  ESP  |  Loot  |  Aimbot  |  Config  |  Misc
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class NavigationBar
    {
        private readonly OverlayTheme _theme;

        private bool    _dragging   = false;
        private Vector2 _dragOffset = Vector2.Zero;
        public  Vector2 Position    = new(100, 30);

        public int  ActiveTab { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        private readonly (int id, string label)[] _tabs =
        {
            (0, "ESP"),
            (1, "Loot"),
            (2, "Aimbot"),
            (3, "Config"),
            (4, "Misc"),
        };

        public NavigationBar(OverlayTheme theme) => _theme = theme;

        public void Render()
        {
            if (!IsVisible) return;

            var io     = ImGui.GetIO();
            var dl     = ImGui.GetForegroundDrawList();
            float height = _theme.NavBarHeight;
            float tabW   = 90f;

            // FPS string ¡ª reserve space on the right
            var fpsText  = $"FPS: {(int)io.Framerate}";
            var fpsSize  = ImGui.CalcTextSize(fpsText);
            float fpsPad = 12f;
            float fpsAreaW = fpsSize.X + fpsPad * 2f;

            float totalW = _tabs.Length * tabW + 20f + fpsAreaW;

            var pos  = Position;
            var size = new Vector2(totalW, height);

            // //// Hover check /////////////////////////////////////////////////////
            bool hovered = io.MousePos.X >= pos.X && io.MousePos.X <= pos.X + totalW
                        && io.MousePos.Y >= pos.Y && io.MousePos.Y <= pos.Y + height;

            // //// Drag ¡ª only on the drag handle (left 20px), not on tabs ////////
            float tabsStartX = pos.X + 20f;
            bool  overHandle = hovered && io.MousePos.X < tabsStartX;

            if (overHandle && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _dragging   = true;
                _dragOffset = io.MousePos - pos;
            }
            if (_dragging && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                Position = io.MousePos - _dragOffset;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                _dragging = false;

            pos = Position;

            // //// Background + border ///////////////////////////////////////////////
            float r = _theme.NavBarRounding;
            dl.AddRectFilled(pos, pos + size, OverlayTheme.ToU32(_theme.NavBarBg), r);
            dl.AddRect(pos, pos + size, OverlayTheme.ToU32(_theme.NavBarBorder), r,
                ImDrawFlags.None, _theme.NavBarBorderSize);

            // //// Drag handle dots //////////////////////////////////////////////////
            uint dotCol = OverlayTheme.ToU32(_theme.NavItemText);
            float cx = pos.X + 10f, cy = pos.Y + height / 2;
            for (int d = -1; d <= 1; d++)
                dl.AddCircleFilled(new Vector2(cx, cy + d * 5), 1.5f, dotCol);

            // //// Tabs //////////////////////////////////////////////////////////////
            float startX = pos.X + 20f;
            for (int i = 0; i < _tabs.Length; i++)
            {
                var (id, label) = _tabs[i];
                bool active = ActiveTab == id;

                var tabMin = new Vector2(startX + i * tabW, pos.Y);
                var tabMax = new Vector2(startX + (i + 1) * tabW, pos.Y + height);
                var center = (tabMin + tabMax) * 0.5f;

                bool tabHov = io.MousePos.X >= tabMin.X && io.MousePos.X <= tabMax.X
                           && io.MousePos.Y >= tabMin.Y && io.MousePos.Y <= tabMax.Y;

                if (active)
                    dl.AddRectFilled(tabMin + new Vector2(2, 2), tabMax - new Vector2(2, 2),
                        OverlayTheme.ToU32(_theme.NavItemBgActive), _theme.NavItemRounding);
                else if (tabHov)
                    dl.AddRectFilled(tabMin + new Vector2(2, 2), tabMax - new Vector2(2, 2),
                        OverlayTheme.ToU32(_theme.NavItemBgHover), _theme.NavItemRounding);

                // Accent underline on active tab
                if (active)
                {
                    float lh = _theme.NavAccentLineHeight;
                    dl.AddRectFilled(
                        new Vector2(tabMin.X + 8, tabMax.Y - lh - 2),
                        new Vector2(tabMax.X - 8, tabMax.Y - 2),
                        OverlayTheme.ToU32(_theme.NavItemAccentLine), lh * 0.5f);
                }

                var textCol  = active
                    ? OverlayTheme.ToU32(_theme.NavItemTextActive)
                    : OverlayTheme.ToU32(_theme.NavItemText);
                var textSize = ImGui.CalcTextSize(label);
                dl.AddText(center - textSize * 0.5f, textCol, label);

                if (tabHov && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    ActiveTab = id;
            }

            // //// FPS counter ¡ª right side of navbar ////////////////////////////////
            float fpsX  = pos.X + totalW - fpsAreaW + fpsPad;
            float fpsY  = pos.Y + (height - fpsSize.Y) * 0.5f;
            uint  fpsCol = OverlayTheme.ToU32(_theme.NavItemText);
            dl.AddText(new Vector2(fpsX, fpsY), fpsCol, fpsText);
        }

        public string GetActiveTabLabel()
            => _tabs[ActiveTab < _tabs.Length ? ActiveTab : 0].label;
    }
}