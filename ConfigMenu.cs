using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.ABI;
using ImGuiOverlay.Config;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.UI.Menus
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  CONFIG MENU
    //  Theme profiles, live theme editor, ABI config save/load, color presets.
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class ConfigMenu : MenuBase
    {
        private string[]       _profiles   = Array.Empty<string>();
        private int            _profileIdx = 0;
        private string         _saveName   = "my_config";
        private double         _saveFlash  = 0;

        // ABI config ¡ª injected after ABIController is created
        private ABIGameConfig  _abiConfig  = new();

        public ConfigMenu(OverlaySettings s, OverlayTheme t) : base(s, t) { }

        /// <summary>Wire the live ABIGameConfig so Save/Reload operate on the actual running state.</summary>
        public void SetABIConfig(ABIGameConfig cfg) => _abiConfig = cfg;

        public override void Render()
        {
            // //// ABI CONFIG /////////////////////////////////////////////////////////////////
            SectionHeader("ABI CONFIG");

            if (ImGui.Button("Save ABI Config"))
            {
                ConfigManager.Save(_abiConfig, "abi_config.json");
                DMA.Logger.Info("[Config] abi_config.json saved");
            }
            ImGui.SameLine();
            if (ImGui.Button("Reload ABI Config"))
            {
                var reloaded = ConfigManager.Load<ABIGameConfig>("abi_config.json");
                CopyFields(reloaded, _abiConfig);
                DMA.Logger.Info("[Config] abi_config.json reloaded");
            }
            ImGui.SameLine();
            ImGui.TextDisabled("(abi_config.json)");

            // //// THEME PROFILES /////////////////////////////////////////////////////////////
            SectionHeader("THEME PROFILES");

            RefreshProfiles();

            if (_profiles.Length > 0)
            {
                ImGui.SetNextItemWidth(200);
                ImGui.Combo("##profiles", ref _profileIdx, _profiles, _profiles.Length);
                ImGui.SameLine();
                if (ImGui.Button("Load"))
                {
                    var loaded = ConfigManager.Load<OverlayTheme>(_profiles[_profileIdx] + ".theme.json");
                    CopyFields(loaded, Theme);
                    Theme.Apply();
                }
                ImGui.SameLine();
                if (ImGui.Button("Delete"))
                    ConfigManager.Delete(_profiles[_profileIdx] + ".theme.json");
            }
            else ImGui.TextDisabled("No saved profiles");

            ImGui.SetNextItemWidth(150);
            var saveNameBuf = System.Text.Encoding.UTF8.GetBytes(_saveName.PadRight(64));
            if (ImGui.InputText("##savename", saveNameBuf, (uint)saveNameBuf.Length))
                _saveName = System.Text.Encoding.UTF8.GetString(saveNameBuf).TrimEnd('\0').Trim();

            ImGui.SameLine();
            bool flash = ImGui.GetTime() - _saveFlash < 1.0;
            if (flash) ImGui.PushStyleColor(ImGuiCol.Button, Theme.Accent);
            if (ImGui.Button("Save Theme"))
            {
                ConfigManager.Save(Theme, _saveName + ".theme.json");
                _saveFlash = ImGui.GetTime();
            }
            if (flash) ImGui.PopStyleColor();

            // //// ACCENT COLOR ///////////////////////////////////////////////////////////////
            SectionHeader("ACCENT COLOR");

            if (ImGui.ColorEdit4("Accent##main", ref Theme.Accent,
                ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.PickerHueBar))
            {
                var a = Theme.Accent;
                Theme.AccentHovered = new Vector4(Math.Min(a.X + 0.08f, 1f), Math.Min(a.Y + 0.08f, 1f), a.Z, a.W);
                Theme.AccentActive  = new Vector4(Math.Max(a.X - 0.07f, 0f), Math.Max(a.Y - 0.07f, 0f), a.Z, a.W);
                Theme.AccentDim     = new Vector4(a.X, a.Y, a.Z, 0.35f);
                Theme.Apply();
            }

            // //// WINDOW STYLE ///////////////////////////////////////////////////////////////
            SectionHeader("WINDOW STYLE");

            bool changed = false;
            changed |= ImGui.ColorEdit4("Window BG",    ref Theme.WindowBg);
            changed |= ImGui.ColorEdit4("Child BG",     ref Theme.ChildBg);
            changed |= ImGui.ColorEdit4("Popup BG",     ref Theme.PopupBg);
            changed |= ImGui.ColorEdit4("Border",       ref Theme.WindowBorder);
            changed |= ImGui.ColorEdit4("Title BG",     ref Theme.TitleBg);
            changed |= ImGui.ColorEdit4("Title Active", ref Theme.TitleBgActive);
            ImGui.Spacing();
            changed |= ImGui.SliderFloat("Window Rounding", ref Theme.WindowRounding, 0f, 20f, "%.1f");
            changed |= ImGui.SliderFloat("Child Rounding",  ref Theme.ChildRounding,  0f, 12f, "%.1f");
            changed |= ImGui.SliderFloat("Frame Rounding",  ref Theme.FrameRounding,  0f, 10f, "%.1f");
            changed |= ImGui.SliderFloat("Popup Rounding",  ref Theme.PopupRounding,  0f, 12f, "%.1f");
            changed |= ImGui.SliderFloat("Tab Rounding",    ref Theme.TabRounding,    0f, 8f,  "%.1f");
            changed |= ImGui.SliderFloat("Grab Rounding",   ref Theme.GrabRounding,   0f, 8f,  "%.1f");
            changed |= ImGui.SliderFloat("Scrollbar Size",  ref Theme.ScrollbarSize,  4f, 20f, "%.1f");

            SectionHeader("NAVBAR STYLE");
            changed |= ImGui.ColorEdit4("NavBar BG",       ref Theme.NavBarBg);
            changed |= ImGui.ColorEdit4("NavBar Border",   ref Theme.NavBarBorder);
            changed |= ImGui.SliderFloat("NavBar Height",  ref Theme.NavBarHeight, 30f, 70f, "%.1f");
            changed |= ImGui.SliderFloat("NavBar Rounding",ref Theme.NavBarRounding, 0f, 20f, "%.1f");
            changed |= ImGui.ColorEdit4("Nav Item Hover",  ref Theme.NavItemBgHover);
            changed |= ImGui.ColorEdit4("Nav Accent Line", ref Theme.NavItemAccentLine);
            changed |= ImGui.SliderFloat("Accent Line H",  ref Theme.NavAccentLineHeight, 1f, 5f, "%.1f");

            SectionHeader("TEXT");
            changed |= ImGui.ColorEdit4("Text",           ref Theme.Text);
            changed |= ImGui.ColorEdit4("Text Disabled",  ref Theme.TextDisabled);
            changed |= ImGui.SliderFloat("Alpha",         ref Theme.Alpha, 0.1f, 1.0f, "%.2f");
            changed |= ImGui.SliderFloat("Disabled Alpha",ref Theme.DisabledAlpha, 0.1f, 1.0f, "%.2f");

            if (changed) Theme.Apply();

            // //// PRESETS ////////////////////////////////////////////////////////////////////
            SectionHeader("PRESETS");

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.30f, 0.70f, 1f));
            if (ImGui.Button("Blue"))   { ApplyPreset(Preset.Blue);   Theme.Apply(); }
            ImGui.PopStyleColor(); ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.60f, 0.15f, 0.15f, 1f));
            if (ImGui.Button("Red"))    { ApplyPreset(Preset.Red);    Theme.Apply(); }
            ImGui.PopStyleColor(); ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.10f, 0.50f, 0.25f, 1f));
            if (ImGui.Button("Green"))  { ApplyPreset(Preset.Green);  Theme.Apply(); }
            ImGui.PopStyleColor(); ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.50f, 0.20f, 0.70f, 1f));
            if (ImGui.Button("Purple")) { ApplyPreset(Preset.Purple); Theme.Apply(); }
            ImGui.PopStyleColor();
        }

        // //// Helpers ////////////////////////////////////////////////////////////////////////
        private void RefreshProfiles()
        {
            var files = ConfigManager.ListConfigs("");
            var names = new System.Collections.Generic.List<string>();
            foreach (var f in files)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                if (name.EndsWith(".theme")) names.Add(name[..^6]);
            }
            _profiles = names.ToArray();
        }

        /// <summary>Reflection-free deep-copy via JSON round-trip.</summary>
        private static void CopyFields<T>(T src, T dst)
        {
            var json   = Newtonsoft.Json.JsonConvert.SerializeObject(src);
            var target = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            if (target == null) return;
            foreach (var f in typeof(T).GetFields())
                f.SetValue(dst, f.GetValue(target));
        }

        // //// Presets ///////////////////////////////////////////////////////////////////////
        private enum Preset { Blue, Red, Green, Purple }

        private void ApplyPreset(Preset p)
        {
            var a = p switch
            {
                Preset.Red    => new Vector4(0.96f, 0.27f, 0.27f, 1f),
                Preset.Green  => new Vector4(0.22f, 0.85f, 0.45f, 1f),
                Preset.Purple => new Vector4(0.65f, 0.30f, 0.95f, 1f),
                _             => new Vector4(0.27f, 0.52f, 0.96f, 1f),
            };
            Theme.Accent            = a;
            Theme.AccentHovered     = new Vector4(Math.Min(a.X + 0.08f, 1f), a.Y, a.Z, a.W);
            Theme.AccentActive      = new Vector4(Math.Max(a.X - 0.07f, 0f), a.Y, a.Z, a.W);
            Theme.AccentDim         = new Vector4(a.X, a.Y, a.Z, 0.35f);
            Theme.SliderGrab        = a;
            Theme.CheckMark         = a;
            Theme.NavItemAccentLine = a;
            Theme.EspBoxColor       = a;
        }
    }
}