using System;
using System.Numerics;
using ImGuiNET;
using ImGuiOverlay.ABI;
using ImGuiOverlay.Config;
using ImGuiOverlay.Theme;

namespace ImGuiOverlay.UI.Menus
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI ESP MENU
    // /////////////////////////////////////////////////////////////////////////////////////

    public class ABIEspMenu : MenuBase
    {
        private ABIGameConfig _abi;

        public ABIEspMenu(OverlaySettings s, OverlayTheme t, ABIGameConfig abi) : base(s, t) => _abi = abi;

        public override void Render()
        {
            SectionHeader("PLAYER ESP");

            ImGui.Checkbox("Enable Boxes",    ref _abi.DrawBoxes);
            ImGui.SameLine(180);
            ImGui.Checkbox("Names",           ref _abi.DrawNames);

            ImGui.Checkbox("Distance",        ref _abi.DrawDistance);
            ImGui.SameLine(180);
            ImGui.Checkbox("Skeletons",       ref _abi.DrawSkeletons);

            ImGui.SliderFloat("Max Distance (m)",     ref _abi.MaxDistance,         50f,  3000f, "%.0f");
            ImGui.SliderFloat("Skeleton Distance (m)", ref _abi.MaxSkeletonDistance, 25f,  2000f, "%.0f");

            SectionHeader("DEBUG WIDGETS");

            ImGui.Checkbox("Player Debug Widget", ref _abi.DrawPlayerWidget);
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.55f, 0.55f, 0.55f, 1f), "(shows all actors + reads)");

            SectionHeader("DEATH MARKERS");

            ImGui.Checkbox("Draw Death Markers",        ref _abi.DrawDeathMarkers);
            ImGui.SliderFloat("Marker Max Distance (m)", ref _abi.DeathMarkerMaxDist,  50f, 5000f, "%.0f");
            ImGui.SliderFloat("Marker Base Size (px)",   ref _abi.DeathMarkerBaseSize,  4f,   24f, "%.0f");

            SectionHeader("COLORS");

            ColorEdit("Player Label",    ref _abi.ColorPlayer);         ImGui.SameLine();
            ColorEdit("Bot Label",       ref _abi.ColorBot);
            ColorEdit("Box Visible",     ref _abi.ColorBoxVisible);     ImGui.SameLine();
            ColorEdit("Box Invisible",   ref _abi.ColorBoxInvisible);
            ColorEdit("Skel Visible",    ref _abi.ColorSkelVisible);    ImGui.SameLine();
            ColorEdit("Skel Invisible",  ref _abi.ColorSkelInvisible);
            ColorEdit("Dead Fill",       ref _abi.DeadFill);            ImGui.SameLine();
            ColorEdit("Dead Outline",    ref _abi.DeadOutline);
        }
    }

    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI LOOT MENU
    // /////////////////////////////////////////////////////////////////////////////////////

    public class ABILootMenu : MenuBase
    {
        private ABIGameConfig _abi;

        public ABILootMenu(OverlaySettings s, OverlayTheme t, ABIGameConfig abi) : base(s, t) => _abi = abi;

        public override void Render()
        {
            SectionHeader("GROUND LOOT");

            ImGui.Checkbox("Draw Ground Loot",         ref _abi.DrawGroundLoot);
            ImGui.Checkbox("Show Names##glnames",      ref _abi.DrawGroundLootNames);
            ImGui.SameLine(180);
            ImGui.Checkbox("Show Distance##gldist",    ref _abi.DrawGroundLootDistance);
            ImGui.SliderFloat("Item Max Distance (m)", ref _abi.GroundLootMaxDistance, 10f, 500f, "%.0f");
            ImGui.SliderFloat("Marker Size (px)",       ref _abi.GroundLootMarkerSize,  2f,  16f, "%.0f");

            SectionHeader("CONTAINERS");

            ImGui.Checkbox("Draw Containers",          ref _abi.DrawContainers);
            ImGui.Checkbox("Show Names##cnames",       ref _abi.DrawContainerNames);
            ImGui.SameLine(180);
            ImGui.Checkbox("Show Distance##cdist",     ref _abi.DrawContainerDistance);
            ImGui.Checkbox("Show Empty",               ref _abi.DrawEmptyContainers);
            ImGui.SliderFloat("Container Max Distance (m)", ref _abi.ContainerMaxDistance, 10f, 500f, "%.0f");
            ImGui.SliderFloat("Container Marker (px)",       ref _abi.ContainerMarkerSize,  2f,  16f, "%.0f");
            ColorEdit("Filled Color", ref _abi.ContainerFilledColor); ImGui.SameLine();
            ColorEdit("Empty Color",  ref _abi.ContainerEmptyColor);

            SectionHeader("PRICE FILTERS");

            ImGui.InputInt("Min Price (Regular)",   ref _abi.LootMinPriceRegular);
            Tooltip("Items below this price won't be shown");
            ImGui.InputInt("Min Price (Important)", ref _abi.LootMinPriceImportant);
            Tooltip("Items above this price get a star marker and special color");
            ImGui.Checkbox("Show Prices on Labels", ref _abi.LootShowPrice);

            ImGui.Spacing();
            ColorEdit("Regular Color",   ref _abi.LootRegularColor);   ImGui.SameLine();
            ColorEdit("Important Color", ref _abi.LootImportantColor);

            SectionHeader("TEXT FILTERS");

            var inc = _abi.LootFilterInclude;
            var exc = _abi.LootFilterExclude;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("Include (comma-sep)##linc", ref inc, 256)) _abi.LootFilterInclude = inc;
            Tooltip("Only show items matching these terms");
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("Exclude (comma-sep)##lexc", ref exc, 256)) _abi.LootFilterExclude = exc;
            Tooltip("Hide items matching these terms");

            SectionHeader("LOOT WIDGET");

            ImGui.Checkbox("Enable Widget", ref _abi.DrawLootWidget);
            Tooltip("Floating window listing the most valuable nearby loot");
            ImGui.InputInt("Max Items##wmax", ref _abi.LootWidgetMaxItems);
            _abi.LootWidgetMaxItems = Math.Clamp(_abi.LootWidgetMaxItems, 5, 50);
            ImGui.InputInt("Widget Min Price##wmin", ref _abi.LootWidgetMinPrice);
            ImGui.Checkbox("Show Distance in Widget", ref _abi.LootWidgetShowDistance);
            ColorEdit("Important Item Color##wic", ref _abi.LootWidgetImportantColor);
        }
    }

    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI AIMBOT MENU
    // /////////////////////////////////////////////////////////////////////////////////////

    public class ABIAimbotMenu : MenuBase
    {
        private ABIGameConfig  _abi;
        private ABIController  _ctrl;

        private static readonly string[] BoneNames = {
            "Pelvis","Spine_01","Spine_02","Spine_03","Neck","Head",
            "Clavicle_L","UpperArm_L","LowerArm_L","Hand_L",
            "Clavicle_R","UpperArm_R","LowerArm_R","Hand_R",
            "Thigh_L","Calf_L","Foot_L","Thigh_R","Calf_R","Foot_R"
        };

        private static readonly int[] BoneIndices = {
            ABISkeleton.IDX_Pelvis, ABISkeleton.IDX_Spine_01, ABISkeleton.IDX_Spine_02,
            ABISkeleton.IDX_Spine_03, ABISkeleton.IDX_Neck, ABISkeleton.IDX_Head,
            ABISkeleton.IDX_Clavicle_L, ABISkeleton.IDX_UpperArm_L, ABISkeleton.IDX_LowerArm_L,
            ABISkeleton.IDX_Hand_L, ABISkeleton.IDX_Clavicle_R, ABISkeleton.IDX_UpperArm_R,
            ABISkeleton.IDX_LowerArm_R, ABISkeleton.IDX_Hand_R, ABISkeleton.IDX_Thigh_L,
            ABISkeleton.IDX_Calf_L, ABISkeleton.IDX_Foot_L, ABISkeleton.IDX_Thigh_R,
            ABISkeleton.IDX_Calf_R, ABISkeleton.IDX_Foot_R
        };

        private static readonly int[] VkKeys   = { 0x02, 0x01, 0x12, 0x10, 0x11, 0x05, 0x06 };
        private static readonly string[] VkNames = { "RButton", "LButton", "Alt", "Shift", "Ctrl", "Mouse4", "Mouse5" };

        public ABIAimbotMenu(OverlaySettings s, OverlayTheme t, ABIGameConfig abi, ABIController ctrl) : base(s, t)
        { _abi = abi; _ctrl = ctrl; }

        public override void Render()
        {
            var io = ImGui.GetIO();
            int cx = (int)(io.DisplaySize.X * 0.5f);
            int cy = (int)(io.DisplaySize.Y * 0.5f);

            // //// DEVICE STATUS //////////////////////////////////////////////////////////////
            SectionHeader("DEVICE STATUS");

            var dl = ImGui.GetWindowDrawList();

            // -- Makcu status --
            bool makcuConnected   = ImGuiOverlay.Input.Device.connected;
            bool makcuConnecting  = !makcuConnected && ImGuiOverlay.Input.Device.version == "";
            // Red = disconnected, Orange = connecting (version string empty, not yet confirmed), Green = connected
            var makcuColor = makcuConnected
                ? new Vector4(0.15f, 0.85f, 0.25f, 1f)   // green
                : makcuConnecting
                    ? new Vector4(1f,   0.55f, 0.05f, 1f) // orange
                    : new Vector4(0.90f, 0.15f, 0.15f, 1f); // red

            var cursorPos = ImGui.GetCursorScreenPos();
            float circleR = 6f;
            var circleCenter = new Vector2(cursorPos.X + circleR, cursorPos.Y + ImGui.GetTextLineHeight() * 0.5f);
            dl.AddCircleFilled(circleCenter, circleR, OverlayTheme.ToU32(makcuColor));
            dl.AddCircle(circleCenter, circleR, OverlayTheme.ToU32(new Vector4(0f, 0f, 0f, 0.4f)), 0, 1.5f);
            ImGui.SetCursorScreenPos(new Vector2(cursorPos.X + circleR * 2 + 8f, cursorPos.Y));

            string makcuStatusLabel = makcuConnected ? "Connected" : makcuConnecting ? "Connecting..." : "Disconnected";
            ImGui.TextColored(makcuColor, $"Makcu  ¡ª  {makcuStatusLabel}");

            // -- InputManager status --
            bool imReady       = ImGuiOverlay.Input.InputManager.IsReady;
            bool imInitializing = ImGuiOverlay.Input.InputManager.IsInitializing;
            var imColor = imReady
                ? new Vector4(0.15f, 0.85f, 0.25f, 1f)
                : imInitializing
                    ? new Vector4(1f,   0.55f, 0.05f, 1f)
                    : new Vector4(0.90f, 0.15f, 0.15f, 1f);

            cursorPos = ImGui.GetCursorScreenPos();
            circleCenter = new Vector2(cursorPos.X + circleR, cursorPos.Y + ImGui.GetTextLineHeight() * 0.5f);
            dl.AddCircleFilled(circleCenter, circleR, OverlayTheme.ToU32(imColor));
            dl.AddCircle(circleCenter, circleR, OverlayTheme.ToU32(new Vector4(0f, 0f, 0f, 0.4f)), 0, 1.5f);
            ImGui.SetCursorScreenPos(new Vector2(cursorPos.X + circleR * 2 + 8f, cursorPos.Y));

            string imStatusLabel = imReady ? "Ready" : imInitializing ? "Initializing..." : "Not Ready";
            ImGui.TextColored(imColor, $"InputManager  ¡ª  {imStatusLabel}");

            ImGui.Spacing();

            SectionHeader("MOUSE DEBUG");
            ImGui.Text($"Screen center:   {cx}, {cy}");

            ImGui.Spacing();
            SectionHeader("CALIBRATION");
            ImGui.TextDisabled("Aim at an enemy, then click Calibrate.");
            ImGui.TextDisabled("Sends 1 mouse count, measures camera shift in pixels.");

            if (ImGui.Button("Calibrate px/count"))
                _ctrl.TriggerCalibration();

            ImGui.SameLine();
            var statusCol = ABIController.CalibStatus switch
            {
                ABIController.CalibState.WaitingDelay  => new System.Numerics.Vector4(1f, 0.5f, 0f, 1f),
                ABIController.CalibState.WaitingResult => new System.Numerics.Vector4(1f, 0.8f, 0f, 1f),
                ABIController.CalibState.Done          => new System.Numerics.Vector4(0f, 0.9f, 0f, 1f),
                _                                      => new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f),
            };
            ImGui.TextColored(statusCol, ABIController.CalibStatus.ToString());

            ImGui.TextWrapped(ABIController.CalibLog);

            if (ABIController.CalibStatus == ABIController.CalibState.Done && ABIController.CalibPxPerCountY > 0.5f)
            {
                float ideal = 1f / ABIController.CalibPxPerCountY;
                ImGui.Spacing();
                ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.9f, 1f, 1f),
                    $"Suggested Speed value: {ideal:F3}");
                if (ImGui.Button("Apply suggested Speed"))
                    _abi.AimbotPixelPower = Math.Clamp(ideal, 0.001f, 0.02f);
            }

            ImGui.Spacing();

            ImGui.Checkbox("Enable Aimbot",    ref _abi.AimbotEnabled);
            ImGui.SameLine(180);
            ImGui.Checkbox("Require Visible",  ref _abi.AimbotRequireVisible);

            ImGui.Checkbox("Headshot AI",      ref _abi.AimbotHeadshotAI);
            ImGui.SameLine(180);
            ImGui.Checkbox("Only AI",          ref _abi.AimbotTargetAIOnly);

            ImGui.Checkbox("Only PMC",         ref _abi.AimbotTargetPMCOnly);
            ImGui.SameLine(180);
            ImGui.Checkbox("Random Bone",      ref _abi.AimbotRandomBone);

            ImGui.Spacing();

            // Bone picker
            int boneIdx = Array.IndexOf(BoneIndices, _abi.AimbotTargetBone);
            if (boneIdx < 0) boneIdx = ABISkeleton.IDX_Head;
            if (ImGui.BeginCombo("Target Bone", BoneNames[Math.Clamp(boneIdx, 0, BoneNames.Length - 1)]))
            {
                for (int i = 0; i < BoneNames.Length; i++)
                {
                    bool sel = (i == boneIdx);
                    if (ImGui.Selectable(BoneNames[i], sel)) _abi.AimbotTargetBone = BoneIndices[i];
                    if (sel) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            SectionHeader("PARAMETERS");

            ImGui.SliderFloat("Max Distance (m)",  ref _abi.AimbotMaxMeters,  10f,   2000f, "%.0f");
            ImGui.SliderFloat("FOV (px)",           ref _abi.AimbotFovPx,      50f,   1200f, "%.0f");
            Tooltip("Pixel radius around crosshair where aimbot activates");
            ImGui.SliderFloat("Speed",              ref _abi.AimbotPixelPower, 0.001f, 0.04f,  "%.4f");
            Tooltip("Mouse counts per screen pixel ?? 1.0 to start, lower if overshooting");
            ImGui.SliderFloat("Deadzone (px)",      ref _abi.AimbotDeadzonePx, 0f,    8f,    "%.1f");
            ImGui.Checkbox("Invert Y",              ref _abi.AimbotInvertY);
            Tooltip("Flip vertical ?? only needed if aimbot moves up when it should move down");

            SectionHeader("MOUSE TEST");
            ImGui.TextDisabled("Raw relative moves:");
            if (ImGui.Button("Right +50")) ImGuiOverlay.Input.Device.move( 50,  0); ImGui.SameLine();
            if (ImGui.Button("Left  -50")) ImGuiOverlay.Input.Device.move(-50,  0); ImGui.SameLine();
            if (ImGui.Button("Down  +50")) ImGuiOverlay.Input.Device.move(  0, 50); ImGui.SameLine();
            if (ImGui.Button("Up    -50")) ImGuiOverlay.Input.Device.move(  0,-50);

            ImGui.Spacing();
            if (ImGui.Button("Right +100")) ImGuiOverlay.Input.Device.move(100,   0); ImGui.SameLine();
            if (ImGui.Button("Down  +100")) ImGuiOverlay.Input.Device.move(  0, 100);

            SectionHeader("TRIGGER KEY");

            int keyIdx = Array.IndexOf(VkKeys, _abi.AimbotKey);
            if (keyIdx < 0) keyIdx = 0;
            if (ImGui.BeginCombo("Hold Key", VkNames[keyIdx]))
            {
                for (int i = 0; i < VkKeys.Length; i++)
                {
                    bool sel = (i == keyIdx);
                    if (ImGui.Selectable(VkNames[i], sel)) _abi.AimbotKey = VkKeys[i];
                    if (sel) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            SectionHeader("TRIGGERBOT");

            ImGui.Checkbox("Enable Triggerbot",       ref _abi.TriggerbotEnabled);
            ImGui.SameLine(180);
            ImGui.Checkbox("Require Visible##tb",     ref _abi.TriggerbotRequireVisible);

            int tbKeyIdx = Array.IndexOf(VkKeys, _abi.TriggerbotKey);
            if (tbKeyIdx < 0) tbKeyIdx = 0;
            if (ImGui.BeginCombo("Trigger Key", VkNames[tbKeyIdx]))
            {
                for (int i = 0; i < VkKeys.Length; i++)
                {
                    bool sel = (i == tbKeyIdx);
                    if (ImGui.Selectable(VkNames[i], sel)) _abi.TriggerbotKey = VkKeys[i];
                    if (sel) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            Tooltip("Hold this key to enable triggerbot scanning");

            ImGui.SliderInt("Delay (ms)",    ref _abi.TriggerbotDelayMs,    0, 300);
            Tooltip("Delay before firing after crosshair lands on target");
            ImGui.SliderInt("Duration (ms)", ref _abi.TriggerbotDurationMs, 10, 300);
            Tooltip("How long to hold the fire button");
        }
    }
}