using System.Numerics;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.Config
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  OVERLAY SETTINGS
    //  All runtime toggle/value settings for the overlay features.
    //  Separate from OverlayTheme (visuals) ?? this is behavior/data.
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public class OverlaySettings
    {
        // //// General //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool   OverlayEnabled     = true;
        public float  OverlayOpacity     = 1.0f;
        public bool   ShowFps            = true;
        public string ActiveThemeFile    = "theme_default.json";

        // //// Display ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool   Fullscreen         = false;
        public int    TargetMonitor      = 0;   // 0 = primary
        public bool   VSync              = false;
        public bool   FpsLimitEnabled    = false;
        public int    FpsLimit           = 144;

        // //// ESP //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool   EspEnabled         = true;
        public bool   EspBoxes           = true;
        public bool   EspBoxCorners      = false;  // corner-style box vs full box
        public bool   EspSkeleton        = true;
        public bool   EspHeadCircle      = true;
        public bool   EspNames           = true;
        public bool   EspDistance        = true;
        public bool   EspHealthBar       = true;
        public bool   EspSnaplines       = false;
        public float  EspMaxDistance     = 500f;
        public bool   EspTeamCheck       = true;   // don't draw team members
        public bool   EspVisibilityCheck = true;   // different color when visible

        // //// Aimbot / Targeting (stub) //////////////////////////////////////////////////////////////////////////////////////////
        public bool   AimbotEnabled      = false;
        public float  AimbotFov          = 8.0f;
        public float  AimbotSmoothing    = 5.0f;
        public string AimbotBone         = "Head";
        public bool   AimbotVisOnly      = true;
        public bool   AimbotTeamCheck    = true;

        // //// Radar //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public bool   RadarEnabled       = true;
        public float  RadarScale         = 1.0f;   // world units per radar pixel
        public float  RadarZoom          = 3.0f;
        public bool   RadarRotate        = true;   // rotate with player yaw
        public bool   RadarShowFov       = true;
        public bool   RadarShowGrid      = true;
        public bool   RadarShowNames     = false;
        public Vector2 RadarPosition     = new(20, 60);
        public Vector2 RadarSize         = new(200, 200);

        // //// Window Layout (persisted across restarts) /////////////////////////////////////
        public float  NavBarX           = 100f;
        public float  NavBarY           = 30f;
        public float  MenuX             = 100f;
        public float  MenuY             = 90f;
        public float  MenuW             = 380f;
        public float  MenuH             = 500f;
        public int    ActiveTab         = 0;
        public bool   Watermark          = true;
        public bool   Crosshair          = false;
        public float  CrosshairSize      = 8f;
        public float  CrosshairGap       = 4f;
        public float  CrosshairThickness = 1.5f;
        public bool   SpectatorList      = true;
    }
}