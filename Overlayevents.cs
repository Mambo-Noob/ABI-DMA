using System;

namespace ImGuiOverlay.Core
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  OVERLAY EVENTS
    //  Static event bus used to bridge the static WndProc callback back to
    //  instance-level overlay code (e.g. toggle menu from INSERT key).
    // /////////////////////////////////////////////////////////////////////////////////////

    public static class OverlayEvents
    {
        public static event Action? OnToggleMenu;
        public static event Action? OnQuit;

        public static void RaiseToggleMenu() => OnToggleMenu?.Invoke();
        public static void RaiseQuit()       => OnQuit?.Invoke();
    }
}