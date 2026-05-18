using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;

namespace ImGuiOverlay.Theme
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  OVERLAY THEME  ·  One file to rule all visuals
    //  Every color, rounding, thickness, spacing is here.
    //  Load/save from JSON via ConfigManager for runtime editing.
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class OverlayTheme
    {
        // //// Window ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 WindowBg             = new(0.08f, 0.08f, 0.10f, 0.97f);
        public Vector4 WindowBorder         = new(0.20f, 0.20f, 0.25f, 1.00f);
        public float   WindowRounding       = 8f;
        public float   WindowBorderSize     = 1f;
        public Vector2 WindowPadding        = new(12f, 10f);
        public float   WindowTitleAlign     = 0.5f; // 0=lABI, 0.5=center, 1=right

        // //// Child Windows //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 ChildBg              = new(0.06f, 0.06f, 0.08f, 0.80f);
        public float   ChildRounding        = 6f;
        public float   ChildBorderSize      = 1f;

        // //// Popups ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 PopupBg              = new(0.10f, 0.10f, 0.13f, 0.98f);
        public float   PopupRounding        = 6f;
        public float   PopupBorderSize      = 1f;

        // //// Title Bar //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 TitleBg             = new(0.05f, 0.05f, 0.07f, 1.00f);
        public Vector4 TitleBgActive       = new(0.10f, 0.10f, 0.15f, 1.00f);
        public Vector4 TitleBgCollapsed    = new(0.05f, 0.05f, 0.07f, 0.80f);

        // //// Header (CollapsingHeader, TreeNode, Selectable) //////////////////////////////////////////////
        public Vector4 Header              = new(0.20f, 0.22f, 0.30f, 0.70f);
        public Vector4 HeaderHovered       = new(0.25f, 0.28f, 0.40f, 0.85f);
        public Vector4 HeaderActive        = new(0.30f, 0.34f, 0.50f, 1.00f);

        // //// Accent / Primary Color ////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 Accent              = new(0.27f, 0.52f, 0.96f, 1.00f);  // Blue
        public Vector4 AccentHovered       = new(0.35f, 0.60f, 1.00f, 1.00f);
        public Vector4 AccentActive        = new(0.20f, 0.42f, 0.86f, 1.00f);
        public Vector4 AccentDim           = new(0.27f, 0.52f, 0.96f, 0.35f);

        // //// Buttons //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 Button             = new(0.18f, 0.20f, 0.28f, 1.00f);
        public Vector4 ButtonHovered      = new(0.27f, 0.52f, 0.96f, 0.80f);
        public Vector4 ButtonActive       = new(0.20f, 0.42f, 0.86f, 1.00f);
        public float   ButtonRounding     = 5f;
        public Vector2 ButtonPadding      = new(10f, 5f);

        // //// Frames (InputText, Combo, Sliders) ////////////////////////////////////////////////////////////////////////
        public Vector4 FrameBg            = new(0.12f, 0.12f, 0.16f, 1.00f);
        public Vector4 FrameBgHovered     = new(0.18f, 0.18f, 0.24f, 1.00f);
        public Vector4 FrameBgActive      = new(0.22f, 0.22f, 0.30f, 1.00f);
        public float   FrameRounding      = 4f;
        public float   FrameBorderSize    = 0f;
        public Vector2 FramePadding       = new(8f, 5f);

        // //// Scrollbar //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 ScrollbarBg        = new(0.05f, 0.05f, 0.07f, 1.00f);
        public Vector4 ScrollbarGrab      = new(0.25f, 0.25f, 0.35f, 1.00f);
        public Vector4 ScrollbarGrabHov   = new(0.35f, 0.35f, 0.50f, 1.00f);
        public Vector4 ScrollbarGrabAct   = new(0.27f, 0.52f, 0.96f, 1.00f);
        public float   ScrollbarSize      = 10f;
        public float   ScrollbarRounding  = 5f;

        // //// Slider / Drag //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 SliderGrab         = new(0.27f, 0.52f, 0.96f, 1.00f);
        public Vector4 SliderGrabActive   = new(0.35f, 0.60f, 1.00f, 1.00f);
        public float   GrabMinSize        = 12f;
        public float   GrabRounding       = 4f;

        // //// CheckMark / Radio //////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 CheckMark          = new(0.27f, 0.52f, 0.96f, 1.00f);

        // //// Separator //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 Separator          = new(0.22f, 0.22f, 0.30f, 1.00f);
        public Vector4 SeparatorHovered   = new(0.27f, 0.52f, 0.96f, 0.80f);
        public Vector4 SeparatorActive    = new(0.27f, 0.52f, 0.96f, 1.00f);

        // //// Text ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 Text               = new(0.92f, 0.92f, 0.95f, 1.00f);
        public Vector4 TextDisabled       = new(0.45f, 0.45f, 0.50f, 1.00f);
        public Vector4 TextSelectedBg     = new(0.27f, 0.52f, 0.96f, 0.35f);

        // //// Tabs ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 Tab                = new(0.10f, 0.10f, 0.14f, 1.00f);
        public Vector4 TabHovered         = new(0.27f, 0.52f, 0.96f, 0.70f);
        public Vector4 TabActive          = new(0.18f, 0.20f, 0.30f, 1.00f);
        public Vector4 TabUnfocused       = new(0.08f, 0.08f, 0.10f, 1.00f);
        public Vector4 TabUnfocusedActive = new(0.14f, 0.14f, 0.20f, 1.00f);
        public float   TabRounding        = 4f;
        public float   TabBorderSize      = 0f;

        // //// Docking //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 DockingPreview     = new(0.27f, 0.52f, 0.96f, 0.50f);
        public Vector4 DockingEmptyBg     = new(0.06f, 0.06f, 0.08f, 1.00f);

        // //// NavBar ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 NavBarBg           = new(0.07f, 0.07f, 0.10f, 0.97f);
        public Vector4 NavBarBorder       = new(0.18f, 0.18f, 0.25f, 1.00f);
        public float   NavBarRounding     = 8f;
        public float   NavBarBorderSize   = 1f;
        public float   NavBarHeight       = 44f;
        public Vector4 NavItemBg          = new(0.00f, 0.00f, 0.00f, 0.00f);
        public Vector4 NavItemBgHover     = new(0.27f, 0.52f, 0.96f, 0.18f);
        public Vector4 NavItemBgActive    = new(0.27f, 0.52f, 0.96f, 0.30f);
        public Vector4 NavItemText        = new(0.72f, 0.72f, 0.80f, 1.00f);
        public Vector4 NavItemTextActive  = new(0.92f, 0.92f, 0.95f, 1.00f);
        public Vector4 NavItemAccentLine  = new(0.27f, 0.52f, 0.96f, 1.00f);
        public float   NavItemPaddingX    = 14f;
        public float   NavItemRounding    = 5f;
        public float   NavAccentLineHeight= 2f;

        // //// ESP / Overlay Drawing ////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 EspBoxColor        = new(0.27f, 0.52f, 0.96f, 1.00f);
        public Vector4 EspBoxColorEnemy   = new(0.96f, 0.27f, 0.27f, 1.00f);
        public Vector4 EspBoxColorTeam    = new(0.27f, 0.96f, 0.45f, 1.00f);
        public float   EspBoxThickness    = 1.5f;
        public float   EspBoxRounding     = 2f;
        public Vector4 EspHealthColor     = new(0.22f, 0.85f, 0.40f, 1.00f);
        public Vector4 EspHealthBg        = new(0.10f, 0.10f, 0.12f, 0.80f);
        public float   EspHealthBarWidth  = 4f;
        public float   EspHealthBarGap    = 2f;
        public Vector4 EspNameColor       = new(0.92f, 0.92f, 0.95f, 1.00f);
        public Vector4 EspSkeletonColor   = new(0.80f, 0.80f, 0.90f, 0.85f);
        public float   EspSkeletonThick   = 1.0f;
        public Vector4 EspSnaplineColor   = new(0.27f, 0.52f, 0.96f, 0.60f);
        public float   EspSnaplineThick   = 0.8f;
        public Vector4 EspHeadCircleColor = new(0.92f, 0.92f, 0.95f, 0.90f);
        public float   EspHeadCircleThick = 1.0f;
        public Vector4 EspDistanceColor   = new(0.65f, 0.65f, 0.70f, 1.00f);

        // //// Radar / Map Canvas ////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Vector4 RadarBg            = new(0.05f, 0.06f, 0.08f, 0.96f);
        public Vector4 RadarBorder        = new(0.20f, 0.20f, 0.28f, 1.00f);
        public float   RadarBorderSize    = 1.5f;
        public float   RadarRounding      = 6f;
        public Vector4 RadarGridColor     = new(0.18f, 0.18f, 0.24f, 0.60f);
        public float   RadarGridThick     = 0.5f;
        public Vector4 RadarPlayerSelf    = new(0.27f, 0.96f, 0.45f, 1.00f);
        public Vector4 RadarPlayerEnemy   = new(0.96f, 0.27f, 0.27f, 1.00f);
        public Vector4 RadarPlayerTeam    = new(0.27f, 0.52f, 0.96f, 1.00f);
        public float   RadarPlayerDotSize = 5f;
        public float   RadarPlayerTriSize = 7f;  // triangle pointing direction
        public Vector4 RadarFOVColor      = new(0.27f, 0.52f, 0.96f, 0.20f);
        public Vector4 RadarFOVBorder     = new(0.27f, 0.52f, 0.96f, 0.50f);
        public Vector4 RadarCrosshair     = new(0.30f, 0.30f, 0.40f, 0.70f);
        public float   RadarCrosshairThick= 0.5f;

        // //// Spacing / Layout ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public float   ItemSpacingX       = 8f;
        public float   ItemSpacingY       = 5f;
        public float   ItemInnerSpacingX  = 6f;
        public float   ItemInnerSpacingY  = 4f;
        public float   IndentSpacing      = 20f;
        public float   ColumnsMinSpacing  = 6f;

        // //// Misc ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public float   Alpha              = 1.0f;
        public float   DisabledAlpha      = 0.60f;
        public float   MouseCursorScale   = 1.0f;
        public bool    AntiAliasedLines   = true;
        public bool    AntiAliasedFill    = true;

        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  Apply to ImGui style
        // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Apply()
        {
            var style = ImGui.GetStyle();

            // Geometry
            style.WindowRounding       = WindowRounding;
            style.WindowBorderSize     = WindowBorderSize;
            style.WindowPadding        = WindowPadding;
            style.WindowTitleAlign     = new Vector2(WindowTitleAlign, 0.5f);
            style.ChildRounding        = ChildRounding;
            style.ChildBorderSize      = ChildBorderSize;
            style.PopupRounding        = PopupRounding;
            style.PopupBorderSize      = PopupBorderSize;
            style.FrameRounding        = FrameRounding;
            style.FrameBorderSize      = FrameBorderSize;
            style.FramePadding         = FramePadding;
            style.ItemSpacing          = new Vector2(ItemSpacingX, ItemSpacingY);
            style.ItemInnerSpacing     = new Vector2(ItemInnerSpacingX, ItemInnerSpacingY);
            style.IndentSpacing        = IndentSpacing;
            style.ColumnsMinSpacing    = ColumnsMinSpacing;
            style.ScrollbarSize        = ScrollbarSize;
            style.ScrollbarRounding    = ScrollbarRounding;
            style.GrabMinSize          = GrabMinSize;
            style.GrabRounding         = GrabRounding;
            style.TabRounding          = TabRounding;
            style.TabBorderSize        = TabBorderSize;
            style.ButtonTextAlign      = new Vector2(0.5f, 0.5f);
            style.Alpha                = Alpha;
            style.DisabledAlpha        = DisabledAlpha;

            // Colors
            style.Colors[(int)ImGuiCol.WindowBg]             = WindowBg;
            style.Colors[(int)ImGuiCol.ChildBg]              = ChildBg;
            style.Colors[(int)ImGuiCol.PopupBg]              = PopupBg;
            style.Colors[(int)ImGuiCol.Border]               = WindowBorder;
            style.Colors[(int)ImGuiCol.BorderShadow]         = new Vector4(0, 0, 0, 0);
            style.Colors[(int)ImGuiCol.TitleBg]              = TitleBg;
            style.Colors[(int)ImGuiCol.TitleBgActive]        = TitleBgActive;
            style.Colors[(int)ImGuiCol.TitleBgCollapsed]     = TitleBgCollapsed;
            style.Colors[(int)ImGuiCol.MenuBarBg]            = NavBarBg;
            style.Colors[(int)ImGuiCol.ScrollbarBg]          = ScrollbarBg;
            style.Colors[(int)ImGuiCol.ScrollbarGrab]        = ScrollbarGrab;
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = ScrollbarGrabHov;
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive]  = ScrollbarGrabAct;
            style.Colors[(int)ImGuiCol.CheckMark]            = CheckMark;
            style.Colors[(int)ImGuiCol.SliderGrab]           = SliderGrab;
            style.Colors[(int)ImGuiCol.SliderGrabActive]     = SliderGrabActive;
            style.Colors[(int)ImGuiCol.Button]               = Button;
            style.Colors[(int)ImGuiCol.ButtonHovered]        = ButtonHovered;
            style.Colors[(int)ImGuiCol.ButtonActive]         = ButtonActive;
            style.Colors[(int)ImGuiCol.Header]               = Header;
            style.Colors[(int)ImGuiCol.HeaderHovered]        = HeaderHovered;
            style.Colors[(int)ImGuiCol.HeaderActive]         = HeaderActive;
            style.Colors[(int)ImGuiCol.Separator]            = Separator;
            style.Colors[(int)ImGuiCol.SeparatorHovered]     = SeparatorHovered;
            style.Colors[(int)ImGuiCol.SeparatorActive]      = SeparatorActive;
            style.Colors[(int)ImGuiCol.ResizeGrip]           = AccentDim;
            style.Colors[(int)ImGuiCol.ResizeGripHovered]    = Accent;
            style.Colors[(int)ImGuiCol.ResizeGripActive]     = AccentActive;
            style.Colors[(int)ImGuiCol.Tab]                  = Tab;
            style.Colors[(int)ImGuiCol.TabHovered]           = TabHovered;
            // Tab color names changed in ImGui 1.90.9:
            //   TabActive          -> TabSelected        (index 35)
            //   TabUnfocused       -> TabDimmed          (index 37)
            //   TabUnfocusedActive -> TabDimmedSelected  (index 38)
            // Use raw indices to stay compatible across binding versions.
            style.Colors[35] = TabActive;        // TabSelected
            style.Colors[37] = TabUnfocused;     // TabDimmed
            style.Colors[38] = TabUnfocusedActive; // TabDimmedSelected
            style.Colors[(int)ImGuiCol.DockingPreview]       = DockingPreview;
            style.Colors[(int)ImGuiCol.DockingEmptyBg]       = DockingEmptyBg;
            style.Colors[(int)ImGuiCol.Text]                 = Text;
            style.Colors[(int)ImGuiCol.TextDisabled]         = TextDisabled;
            style.Colors[(int)ImGuiCol.TextSelectedBg]       = TextSelectedBg;
            style.Colors[(int)ImGuiCol.FrameBg]              = FrameBg;
            style.Colors[(int)ImGuiCol.FrameBgHovered]       = FrameBgHovered;
            style.Colors[(int)ImGuiCol.FrameBgActive]        = FrameBgActive;
            // NavHighlight -> NavCursor in 1.90.9 (index 54)
            style.Colors[54] = Accent;           // NavCursor (was NavHighlight)
            style.Colors[(int)ImGuiCol.NavWindowingHighlight]= Accent;
            style.Colors[(int)ImGuiCol.NavWindowingDimBg]    = new Vector4(0, 0, 0, 0.4f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg]     = new Vector4(0, 0, 0, 0.5f);
        }

        // Helper: Convert Vector4 RGBA to uint (for DrawList)
        public static uint ToU32(Vector4 col)
            => ImGui.ColorConvertFloat4ToU32(col);

        // Helper: same but with alpha override
        public static uint ToU32(Vector4 col, float alpha)
            => ImGui.ColorConvertFloat4ToU32(new Vector4(col.X, col.Y, col.Z, col.W * alpha));
    }
}
