using System;
using System.Runtime.InteropServices;

namespace ImGuiOverlay.Core
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ImGui Win32 backend ¡ª thin P/Invoke wrapper around cimgui's
    //  imgui_impl_win32 exported functions.
    //
    //  Requires cimgui.dll compiled with IMGUI_IMPL_WIN32 backend enabled.
    //  Drop the correct cimgui.dll (with backends) next to the exe.
    // /////////////////////////////////////////////////////////////////////////////////////

    public static unsafe class ImGuiImplWin32
    {
        private const string Lib = "cimgui";

        [DllImport(Lib, EntryPoint = "ImGui_ImplWin32_Init", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte _Init(IntPtr hwnd);

        [DllImport(Lib, EntryPoint = "ImGui_ImplWin32_InitForOpenGL", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte _InitForOpenGL(IntPtr hwnd);

        [DllImport(Lib, EntryPoint = "ImGui_ImplWin32_Shutdown", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _Shutdown();

        [DllImport(Lib, EntryPoint = "ImGui_ImplWin32_NewFrame", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _NewFrame();

        // Returns LRESULT ¡ª non-zero means ImGui handled the message
        [DllImport(Lib, EntryPoint = "ImGui_ImplWin32_WndProcHandler", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _WndProcHandler(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam);

        [DllImport(Lib, EntryPoint = "ImGui_ImplWin32_GetDpiScaleForHwnd", CallingConvention = CallingConvention.Cdecl)]
        private static extern float _GetDpiScaleForHwnd(IntPtr hwnd);

        [DllImport(Lib, EntryPoint = "ImGui_ImplWin32_GetDpiScaleForMonitor", CallingConvention = CallingConvention.Cdecl)]
        private static extern float _GetDpiScaleForMonitor(IntPtr monitor);

        // ©¤©¤ Public API ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        public static bool Init(IntPtr hwnd)            => _Init(hwnd) != 0;
        public static void Shutdown()                   => _Shutdown();
        public static void NewFrame()                   => _NewFrame();
        public static float GetDpiScale(IntPtr hwnd)   => _GetDpiScaleForHwnd(hwnd);

        /// <summary>
        /// Call from your WndProc. Returns true if ImGui consumed the message.
        /// </summary>
        public static bool WndProcHandler(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam)
            => _WndProcHandler(hwnd, msg, wParam, lParam) != IntPtr.Zero;
    }
}