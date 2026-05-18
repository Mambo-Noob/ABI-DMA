using System;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.ABI;
using ImGuiOverlay.Config;
using ImGuiOverlay.Core;
using ImGuiOverlay.Theme;
using ImGuiOverlay.UI.Menus;

namespace ImGuiOverlay.UI
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  OVERLAY WINDOW MANAGER
    //  Dockspace + nav bar + ABI menu panels + HUD elements.
    //  All demo menus (generic ESP/Aimbot/Radar/Visuals) are replaced by ABI-specific ones.
    //  INSERT = toggle menu   END = close
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class OverlayWindowManager
    {
        private readonly OverlaySettings _settings;
        private readonly OverlayTheme    _theme;
        private readonly NavigationBar   _navBar;

        // ABI menus ¡ª set via SetABIController()
        private ABIEspMenu?    _espMenu;
        private ABILootMenu?   _lootMenu;
        private ABIAimbotMenu? _aimbotMenu;
        private ConfigMenu     _configMenu;
        private MiscMenu       _miscMenu;

        private bool _menuOpen  = true;
        private bool _firstFrame = true;
        private uint _dockspaceId;

        public bool MenuOpen => _menuOpen;

        public OverlayWindowManager(OverlaySettings settings, OverlayTheme theme)
        {
            _settings   = settings;
            _theme      = theme;
            _navBar     = new NavigationBar(theme);
            _configMenu = new ConfigMenu(settings, theme);
            _miscMenu   = new MiscMenu(settings, theme);

            // Restore persisted layout
            _navBar.Position  = new Vector2(settings.NavBarX, settings.NavBarY);
            _navBar.ActiveTab = settings.ActiveTab;
        }

        public void ToggleMenu() => _menuOpen = !_menuOpen;

        /// <summary>
        /// Wire the live ABIController after it is created in OverlayApp.
        /// Creates the three ABI menu panels from the shared config object.
        /// </summary>
        public void SetABIController(ABIController abi)
        {
            var cfg = abi.Config;
            _espMenu     = new ABIEspMenu(_settings, _theme, cfg);
            _lootMenu    = new ABILootMenu(_settings, _theme, cfg);
            _aimbotMenu  = new ABIAimbotMenu(_settings, _theme, cfg, abi);
            _configMenu.SetABIConfig(cfg);
        }

        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  RENDER
        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Render(Vector2 displaySize)
        {
            // //// Full-screen invisible dockspace root //////////////////////////////////////
            var viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.WorkPos);
            ImGui.SetNextWindowSize(viewport.WorkSize);
            ImGui.SetNextWindowViewport(viewport.ID);

            var dockFlags = ImGuiWindowFlags.NoDocking
                          | ImGuiWindowFlags.NoTitleBar
                          | ImGuiWindowFlags.NoCollapse
                          | ImGuiWindowFlags.NoResize
                          | ImGuiWindowFlags.NoMove
                          | ImGuiWindowFlags.NoBringToFrontOnFocus
                          | ImGuiWindowFlags.NoNavFocus
                          | ImGuiWindowFlags.NoBackground;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            bool open = true;
            ImGui.Begin("##dockspace_root", ref open, dockFlags);
            ImGui.PopStyleVar(3);

            _dockspaceId = ImGui.GetID("MainDockSpace");
            ImGui.DockSpace(_dockspaceId, Vector2.Zero, ImGuiDockNodeFlags.PassthruCentralNode);

            if (_firstFrame) { _firstFrame = false; SetupDefaultDockLayout(); }

            ImGui.End();

            // //// Nav bar + menu panels ////////////////////////////////////////////////////
            if (_menuOpen)
            {
                _navBar.Render();
                RenderMainMenu(displaySize);
            }

            // //// HUD //////////////////////////////////////////////////////////////////////
            if (_settings.Watermark)  RenderWatermark(displaySize);
            if (_settings.Crosshair)  RenderCrosshair(displaySize);
        }

        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  MAIN MENU WINDOW
        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void RenderMainMenu(Vector2 displaySize)
        {
            float navH = _theme.NavBarHeight + 10f;

            // First frame: restore saved position/size; afterwards ImGui owns them freely
            ImGui.SetNextWindowSize(new Vector2(_settings.MenuW, _settings.MenuH), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(_settings.MenuX, _settings.MenuY),  ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(_settings.OverlayOpacity);

            var tabLabel = _navBar.GetActiveTabLabel();
            bool winOpen = true;

            if (!ImGui.Begin($"{tabLabel}###main_menu", ref winOpen, ImGuiWindowFlags.NoScrollbar))
            { ImGui.End(); return; }
            if (!winOpen) { _menuOpen = false; ImGui.End(); return; }

            // Persist current layout every frame so exit-save captures latest state
            var wPos  = ImGui.GetWindowPos();
            var wSize = ImGui.GetWindowSize();
            _settings.MenuX      = wPos.X;
            _settings.MenuY      = wPos.Y;
            _settings.MenuW      = wSize.X;
            _settings.MenuH      = wSize.Y;
            _settings.NavBarX    = _navBar.Position.X;
            _settings.NavBarY    = _navBar.Position.Y;
            _settings.ActiveTab  = _navBar.ActiveTab;

            ImGui.BeginChild("##menu_content", Vector2.Zero, (ImGuiChildFlags)0, ImGuiWindowFlags.NoScrollbar);

            // If ABI menus not yet wired (no DMA), show status
            if (_espMenu == null)
            {
                ImGui.TextDisabled("Waiting for DMA attach...");
            }
            else
            {
                switch (_navBar.ActiveTab)
                {
                    case 0: _espMenu.Render();     break;
                    case 1: _lootMenu!.Render();   break;
                    case 2: _aimbotMenu!.Render(); break;
                    case 3: _configMenu.Render();  break;
                    case 4: _miscMenu.Render();    break;
                }
            }

            ImGui.EndChild();
            ImGui.End();
        }

        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  DOCK LAYOUT
        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void SetupDefaultDockLayout()
        {
            var size = ImGui.GetMainViewport().WorkSize;
            DockBuilder.RemoveNode(_dockspaceId);
            DockBuilder.AddNode(_dockspaceId, ImGuiDockNodeFlags.PassthruCentralNode);
            DockBuilder.SetNodeSize(_dockspaceId, size);
            DockBuilder.Finish(_dockspaceId);
        }

        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  HUD ELEMENTS
        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void RenderWatermark(Vector2 displaySize)
        {
            var dl   = ImGui.GetForegroundDrawList();
            var text = "Mambo ABI  v0.2";
            var ts   = ImGui.CalcTextSize(text);
            var pos  = new Vector2(displaySize.X - ts.X - 12, 10);

            dl.AddRectFilled(pos - new Vector2(6, 4), pos + ts + new Vector2(6, 4),
                OverlayTheme.ToU32(_theme.WindowBg), 4f);
            dl.AddRect(pos - new Vector2(6, 4), pos + ts + new Vector2(6, 4),
                OverlayTheme.ToU32(_theme.WindowBorder), 4f, ImDrawFlags.None, 1f);
            dl.AddText(pos, OverlayTheme.ToU32(_theme.Accent), text);
        }

        private void RenderFps()
        {
            var dl  = ImGui.GetForegroundDrawList();
            var io  = ImGui.GetIO();
            var txt = $"FPS: {(int)io.Framerate}";
            dl.AddText(new Vector2(10, 10), OverlayTheme.ToU32(_theme.TextDisabled), txt);
        }

        private void RenderCrosshair(Vector2 displaySize)
        {
            var dl  = ImGui.GetForegroundDrawList();
            var c   = displaySize * 0.5f;
            float s = _settings.CrosshairSize;
            float g = _settings.CrosshairGap;
            float t = _settings.CrosshairThickness;
            uint col = OverlayTheme.ToU32(_theme.Accent);

            dl.AddLine(new Vector2(c.X - s - g, c.Y), new Vector2(c.X - g, c.Y), col, t);
            dl.AddLine(new Vector2(c.X + g, c.Y),     new Vector2(c.X + s + g, c.Y), col, t);
            dl.AddLine(new Vector2(c.X, c.Y - s - g), new Vector2(c.X, c.Y - g), col, t);
            dl.AddLine(new Vector2(c.X, c.Y + g),     new Vector2(c.X, c.Y + s + g), col, t);
        }
    }
}