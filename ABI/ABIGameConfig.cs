using System.Numerics;

namespace ImGuiOverlay.ABI
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI GAME CONFIG  ¡¤  All Arena Breakout Infinite feature settings.
    //  Serialized to JSON via ConfigManager ("abi_config.json").
    // /////////////////////////////////////////////////////////////////////////////////////

    public sealed class ABIGameConfig
    {
        // //// Player ESP //////////////////////////////////////////////////////////////
        public bool  DrawBoxes             = true;
        public bool  DrawNames             = true;
        public bool  DrawDistance          = true;
        public bool  DrawSkeletons         = false;
        public bool  ShowDebug             = false;
        public float MaxDistance           = 800f;   // meters
        public float MaxSkeletonDistance   = 300f;

        // //// Death markers ///////////////////////////////////////////////////////////
        public bool  DrawDeathMarkers    = true;
        public float DeathMarkerMaxDist  = 1200f;
        public float DeathMarkerBaseSize = 10f;

        // //// Label / box colors /////////////////////////////////////////////////////
        public Vector4 ColorPlayer        = new(1f,    0.25f, 0.25f, 1f);
        public Vector4 ColorBot           = new(0f,    0.6f,  1f,    1f);
        public Vector4 ColorBoxVisible    = new(0.20f, 1.00f, 0.20f, 1f);
        public Vector4 ColorBoxInvisible  = new(1.00f, 0.50f, 0.00f, 1f);
        public Vector4 ColorSkelVisible   = new(1.00f, 1.00f, 1.00f, 1f);
        public Vector4 ColorSkelInvisible = new(0.70f, 0.70f, 0.70f, 1f);
        public Vector4 DeadFill           = new(0f,    0f,    0f,    1f);
        public Vector4 DeadOutline        = new(1f,    0.84f, 0f,    1f);

        // //// Aimbot //////////////////////////////////////////////////////////////////
        public bool  AimbotEnabled        = false;
        public bool  AimbotRequireVisible = true;
        public bool  AimbotTargetAIOnly   = false;
        public bool  AimbotTargetPMCOnly  = false;
        public bool  AimbotHeadshotAI     = false;
        public bool  AimbotRandomBone     = false;
        public int   AimbotTargetBone     = ABISkeleton.IDX_Head;
        public float AimbotMaxMeters      = 400f;
        public float AimbotFovPx          = 90f;
        public float AimbotPixelPower     = 0.01f;  // mouse counts per screen pixel
        public float AimbotDeadzonePx     = 1.5f;
        public bool  AimbotInvertY        = false;
        public int   AimbotKey            = 0x02; // VK_RBUTTON

        // //// Triggerbot /////////////////////////////////////////////////////////////
        public bool  TriggerbotEnabled    = false;
        public int   TriggerbotKey        = 0x02; // VK_RBUTTON
        public int   TriggerbotDelayMs    = 50;   // ms delay before firing
        public int   TriggerbotDurationMs = 80;   // ms to hold the fire button
        public bool  TriggerbotRequireVisible = true;
        public bool  DrawGroundLoot         = true;
        public bool  DrawGroundLootNames    = true;
        public bool  DrawGroundLootDistance = true;
        public float GroundLootMaxDistance  = 150f;
        public float GroundLootMarkerSize   = 6f;

        // //// Containers //////////////////////////////////////////////////////////////
        public bool  DrawContainers         = true;
        public bool  DrawContainerNames     = true;
        public bool  DrawContainerDistance  = true;
        public bool  DrawEmptyContainers    = true;
        public float ContainerMaxDistance   = 200f;
        public float ContainerMarkerSize    = 8f;
        public Vector4 ContainerFilledColor = new(1f,   0.84f, 0f,   1f);
        public Vector4 ContainerEmptyColor  = new(0.5f, 0.5f,  0.5f, 1f);

        // //// Loot price / colors /////////////////////////////////////////////////////
        public int    LootMinPriceRegular   = 5000;
        public int    LootMinPriceImportant = 50000;
        public bool   LootShowPrice         = true;
        public string LootFilterInclude     = "";
        public string LootFilterExclude     = "";
        public Vector4 LootRegularColor     = new(0f, 1f,    0.5f, 1f);
        public Vector4 LootImportantColor   = new(1f, 0.1f,  1f,   1f);

        // //// Debug widgets ///////////////////////////////////////////////////////////
        public bool    DrawPlayerWidget          = false;  // toggle via menu

        // //// Loot widget (real-time list) ////////////////////////////////////////////
        public bool    DrawLootWidget            = true;
        public int     LootWidgetMaxItems        = 20;
        public int     LootWidgetMinPrice        = 5000;
        public bool    LootWidgetShowDistance    = true;
        public Vector4 LootWidgetBackground      = new(0.08f, 0.08f, 0.08f, 0.92f);
        public Vector4 LootWidgetImportantColor  = new(1f,    0.65f, 0.1f,  1f);
        public Vector2 LootWidgetPosition        = new(20f, 400f);
    }
}