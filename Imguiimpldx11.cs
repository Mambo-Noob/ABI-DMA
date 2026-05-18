using System;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace ImGuiOverlay.Core
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ImGui DX11 backend ¡ª thin P/Invoke wrapper around cimgui's
    //  imgui_impl_dx11 exported functions.
    //
    //  Requires cimgui.dll compiled with IMGUI_IMPL_DX11 backend enabled.
    //  ID3D11Device* and ID3D11DeviceContext* are passed as raw IntPtr.
    // /////////////////////////////////////////////////////////////////////////////////////

    public static unsafe class ImGuiImplDX11
    {
        private const string Lib = "cimgui";

        [DllImport(Lib, EntryPoint = "ImGui_ImplDX11_Init", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte _Init(nint device, nint deviceContext);

        [DllImport(Lib, EntryPoint = "ImGui_ImplDX11_Shutdown", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _Shutdown();

        [DllImport(Lib, EntryPoint = "ImGui_ImplDX11_NewFrame", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _NewFrame();

        [DllImport(Lib, EntryPoint = "ImGui_ImplDX11_RenderDrawData", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _RenderDrawData(IntPtr drawData);

        // Optional: call when device is lost / resources need rebuild
        [DllImport(Lib, EntryPoint = "ImGui_ImplDX11_InvalidateDeviceObjects", CallingConvention = CallingConvention.Cdecl)]
        private static extern void _InvalidateDeviceObjects();

        [DllImport(Lib, EntryPoint = "ImGui_ImplDX11_CreateDeviceObjects", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte _CreateDeviceObjects();

        // ©¤©¤ Public API ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        /// <param name="device">ID3D11Device*</param>
        /// <param name="deviceContext">ID3D11DeviceContext*</param>
        public static bool Init(nint device, nint deviceContext)
            => _Init(device, deviceContext) != 0;

        public static void Shutdown()  => _Shutdown();
        public static void NewFrame()  => _NewFrame();

        public static void RenderDrawData(ImDrawDataPtr drawData)
            => _RenderDrawData((nint)drawData.NativePtr);

        public static void InvalidateDeviceObjects() => _InvalidateDeviceObjects();
        public static bool CreateDeviceObjects()     => _CreateDeviceObjects() != 0;
    }
}