using System;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.Config;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.UI.Menus
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  MENU BASE  — all ABI menu panels inherit this
    //  Provides: SectionHeader, ColorEdit, Tooltip helpers.
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public abstract class MenuBase
    {
        protected readonly OverlaySettings Settings;
        protected readonly OverlayTheme    Theme;

        protected MenuBase(OverlaySettings settings, OverlayTheme theme)
        {
            Settings = settings;
            Theme    = theme;
        }

        public abstract void Render();

        // //// Styled helpers /////////////////////////////////////////////////////////////////
        protected void SectionHeader(string label)
        {
            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.Text, Theme.Accent);
            ImGui.TextUnformatted($"  {label}");
            ImGui.PopStyleColor();
            var sepColor = new Vector4(Theme.Accent.X, Theme.Accent.Y, Theme.Accent.Z, 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Separator, sepColor);
            ImGui.Separator();
            ImGui.PopStyleColor();
            ImGui.Spacing();
        }

        protected void ColorEdit(string label, ref Vector4 color)
        {
            ImGui.PushID(label);
            ImGui.ColorEdit4(label, ref color,
                ImGuiColorEditFlags.NoInputs |
                ImGuiColorEditFlags.AlphaBar |
                ImGuiColorEditFlags.AlphaPreviewHalf);
            ImGui.PopID();
        }

        protected void Tooltip(string text)
        {
            // ImGuiHoveredFlags.DelayNormal = 1 << 11 = 2048
            if (ImGui.IsItemHovered((ImGuiHoveredFlags)2048))
            {
                ImGui.PushStyleColor(ImGuiCol.PopupBg, Theme.PopupBg);
                ImGui.SetTooltip(text);
                ImGui.PopStyleColor();
            }
        }
    }
}
