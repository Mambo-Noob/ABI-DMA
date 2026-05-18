using System;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.Config;
using ImGuiOverlay.Core;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.UI.Menus
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  MISC MENU
    //  Display settings (VSync, FPS limit, monitor, fullscreen) and other miscellaneous options.
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class MiscMenu : MenuBase
    {
        private string[] _monitorLabels    = Array.Empty<string>();
        private double   _monitorRefreshTime = -999;

        public MiscMenu(OverlaySettings s, OverlayTheme t) : base(s, t) { }

        public override void Render()
        {
            // //// DISPLAY ////////////////////////////////////////////////////////////////////
            SectionHeader("DISPLAY");

            // Refresh monitor list at most once per 2 seconds
            if (ImGui.GetTime() - _monitorRefreshTime > 2.0)
            {
                _monitorRefreshTime = ImGui.GetTime();
                var mons = OverlayApp.GetMonitors();
                _monitorLabels = new string[mons.Count];
                for (int i = 0; i < mons.Count; i++)
                {
                    var m = mons[i];
                    _monitorLabels[i] = $"Monitor {i}  {m.Width}x{m.Height}"
                                      + (m.IsPrimary ? "  [Primary]" : $"  ({m.Name})");
                }
            }

            if (_monitorLabels.Length > 0)
            {
                ImGui.SetNextItemWidth(300);
                ImGui.Combo("Target Monitor", ref Settings.TargetMonitor, _monitorLabels, _monitorLabels.Length);
            }
            else
            {
                ImGui.TextDisabled("No monitors detected");
            }

            ImGui.Checkbox("Borderless Fullscreen", ref Settings.Fullscreen);

            ImGui.Spacing();

            // //// VSYNC & FPS LIMIT /////////////////////////////////////////////////////////
            SectionHeader("PERFORMANCE");

            ImGui.Checkbox("Enable VSync", ref Settings.VSync);
            Tooltip("Synchronise present rate to monitor refresh ¡ª reduces tearing but adds latency");

            ImGui.Spacing();

            ImGui.Checkbox("Enable FPS Limit", ref Settings.FpsLimitEnabled);
            Tooltip("Cap the render loop to a custom framerate");

            if (Settings.FpsLimitEnabled)
            {
                ImGui.SetNextItemWidth(150);
                ImGui.SliderInt("FPS Limit", ref Settings.FpsLimit, 15, 500);
                Tooltip("Target frames per second when the limiter is active");
                if (Settings.FpsLimit < 15) Settings.FpsLimit = 15;
            }

            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.75f, 0.2f, 1f),
                "Display changes require a restart to take effect.");
            if (ImGui.Button("Save & Restart"))
            {
                ConfigManager.Save(Settings, "overlay_settings.json");
                var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(exe))
                {
                    System.Diagnostics.Process.Start(exe);
                    System.Environment.Exit(0);
                }
            }
            Tooltip("Saves display settings and relaunches the overlay");

            // //// HUD OPTIONS ///////////////////////////////////////////////////////////////
            SectionHeader("HUD");

            ImGui.Checkbox("Watermark",    ref Settings.Watermark);
            ImGui.SameLine(180);
            ImGui.Checkbox("Show FPS",     ref Settings.ShowFps);

            ImGui.Checkbox("Crosshair",    ref Settings.Crosshair);
            if (Settings.Crosshair)
            {
                ImGui.SetNextItemWidth(200);
                ImGui.SliderFloat("Size##ch",      ref Settings.CrosshairSize,      1f, 30f,  "%.1f");
                ImGui.SetNextItemWidth(200);
                ImGui.SliderFloat("Gap##ch",       ref Settings.CrosshairGap,       0f, 20f,  "%.1f");
                ImGui.SetNextItemWidth(200);
                ImGui.SliderFloat("Thickness##ch", ref Settings.CrosshairThickness, 0.5f, 6f, "%.1f");
            }

            ImGui.Spacing();
            ImGui.Checkbox("Spectator List", ref Settings.SpectatorList);
        }
    }
}