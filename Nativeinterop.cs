using System;
using System.Runtime.InteropServices;

namespace ImGuiOverlay.Core
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  WIN32 + DXGI + D3D11  ¡¤  Raw P/Invoke declarations
    //  Everything needed for:
    //    - Window creation (WNDCLASSEX, CreateWindowEx, message pump)
    //    - DX11 device + swapchain
    //    - Render target view
    // /////////////////////////////////////////////////////////////////////////////////////

    // ©¤©¤ Delegates ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

    public delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam);

    // ©¤©¤ Structs ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEX
    {
        public int    cbSize;
        public RECT   rcMonitor;
        public RECT   rcWork;
        public uint   dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    // WNDCLASSEXW ¡ª fully blittable, no managed strings.
    // lpszClassName and lpszMenuName are passed as raw pointers (pin a string or use a literal).
    [StructLayout(LayoutKind.Sequential)]
    public struct WNDCLASSEX
    {
        public uint            cbSize;
        public uint            style;
        public WndProcDelegate lpfnWndProc;
        public int             cbClsExtra;
        public int             cbWndExtra;
        public IntPtr          hInstance;
        public IntPtr          hIcon;
        public IntPtr          hCursor;
        public IntPtr          hbrBackground;
        public IntPtr          lpszMenuName;   // const wchar_t* ¡ª null for no menu
        public IntPtr          lpszClassName;  // const wchar_t* ¡ª pinned string
        public IntPtr          hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr  hwnd;
        public uint    message;
        public UIntPtr wParam;
        public IntPtr  lParam;
        public uint    time;
        public int     ptX, ptY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    // ©¤©¤ DXGI / D3D11 structs ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_MODE_DESC
    {
        public uint           Width;
        public uint           Height;
        public DXGI_RATIONAL  RefreshRate;
        public uint           Format;           // DXGI_FORMAT
        public uint           ScanlineOrdering; // DXGI_MODE_SCANLINE_ORDER
        public uint           Scaling;          // DXGI_MODE_SCALING
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_SAMPLE_DESC
    {
        public uint Count;
        public uint Quality;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_SWAP_CHAIN_DESC
    {
        public DXGI_MODE_DESC   BufferDesc;
        public DXGI_SAMPLE_DESC SampleDesc;
        public uint             BufferUsage;   // DXGI_USAGE
        public uint             BufferCount;
        public IntPtr           OutputWindow;  // HWND
        public int              Windowed;      // Win32 BOOL = 4 bytes, NOT C# bool (1 byte)
        public uint             SwapEffect;    // DXGI_SWAP_EFFECT
        public uint             Flags;
    }

    // ©¤©¤ Constants ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

    public static class WS
    {
        public const uint OVERLAPPED       = 0x00000000;
        public const uint POPUP            = 0x80000000;
        public const uint VISIBLE          = 0x10000000;
        public const uint EX_TOPMOST       = 0x00000008;
        public const uint EX_LAYERED       = 0x00080000;
        public const uint EX_TRANSPARENT   = 0x00000020;
        public const uint EX_NOACTIVATE    = 0x08000000;
        public const uint EX_TOOLWINDOW    = 0x00000080;
    }

    public static class SW
    {
        public const int SHOW = 5;
        public const int HIDE = 0;
    }

    public static class PM
    {
        public const uint REMOVE = 0x0001;
        public const uint NOREMOVE = 0x0000;
    }

    public static class WM
    {
        public const uint DESTROY   = 0x0002;
        public const uint QUIT      = 0x0012;
        public const uint KEYDOWN   = 0x0100;
        public const uint KEYUP     = 0x0101;
        public const uint SYSKEYDOWN = 0x0104;
        public const uint SIZE      = 0x0005;
        public const uint SETFOCUS  = 0x0007;
        public const uint KILLFOCUS = 0x0008;
        public const uint LBUTTONDOWN = 0x0201;
        public const uint RBUTTONDOWN = 0x0204;
        public const uint MOUSEWHEEL  = 0x020A;
    }

    public static class VK
    {
        public const int INSERT = 0x2D;
        public const int END    = 0x23;
        public const int DELETE = 0x2E;
    }

    public static class CS
    {
        public const uint HREDRAW = 0x0002;
        public const uint VREDRAW = 0x0001;
    }

    public static class LWA
    {
        public const uint COLORKEY = 0x00000001;
        public const uint ALPHA    = 0x00000002;
    }

    // DXGI formats (only what we need)
    public static class DXGI_FORMAT
    {
        public const uint UNKNOWN              = 0;
        public const uint R8G8B8A8_UNORM       = 28;
        public const uint R8G8B8A8_UNORM_SRGB  = 29;
        public const uint B8G8R8A8_UNORM       = 87;
    }

    public static class DXGI_USAGE
    {
        public const uint RENDER_TARGET_OUTPUT = 0x00000020;
    }

    public static class DXGI_SWAP_EFFECT
    {
        public const uint DISCARD       = 0;
        public const uint SEQUENTIAL    = 1;
        public const uint FLIP_DISCARD  = 4;
    }

    public static class DXGI_SWAP_CHAIN_FLAG
    {
        public const uint ALLOW_MODE_SWITCH = 2;
    }

    public static class D3D_DRIVER_TYPE
    {
        public const uint UNKNOWN   = 0;
        public const uint HARDWARE  = 1;
        public const uint REFERENCE = 2;
        public const uint NULL_TYPE = 3;
        public const uint SOFTWARE  = 5;
        public const uint WARP      = 6;
    }

    public static class D3D_FEATURE_LEVEL
    {
        public const uint LEVEL_11_1 = 0xb100;
        public const uint LEVEL_11_0 = 0xb000;
        public const uint LEVEL_10_1 = 0xa100;
        public const uint LEVEL_10_0 = 0xa000;
    }

    public static class D3D11_SDK_VERSION
    {
        public const uint Value = 7;
    }

    public static class D3D11_CLEAR
    {
        public const uint DEPTH   = 0x1;
        public const uint STENCIL = 0x2;
    }

    // ©¤©¤ P/Invoke ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

    public static class NativeMethods
    {
        // ©¤©¤ User32 ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        [DllImport("user32.dll", EntryPoint = "RegisterClassExW")]
        public static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            uint    dwExStyle,
            string  lpClassName,
            string  lpWindowName,
            uint    dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr  hWndParent,
            IntPtr  hMenu,
            IntPtr  hInstance,
            IntPtr  lpParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow")]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool PeekMessage(out MSG lpMsg, IntPtr hwnd,
            uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        // Monitor enumeration
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor,
            ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        // ©¤©¤ Kernel32 ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        // ©¤©¤ d3d11.dll ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern int D3D11CreateDeviceAndSwapChain(
            nint             pAdapter,
            uint             DriverType,
            nint             Software,
            uint             Flags,
            uint[]?          pFeatureLevels,
            uint             FeatureLevels,
            uint             SDKVersion,
            ref DXGI_SWAP_CHAIN_DESC pSwapChainDesc,
            out nint         ppSwapChain,
            out nint         ppDevice,
            out uint         pFeatureLevel,
            out nint         ppImmediateContext);

    }

    // /////////////////////////////////////////////////////////////////////////////////////
    //  DX11  ¡¤  COM vtable calls using raw unsafe delegate* function pointers.
    //
    //  Marshal.GetDelegateForFunctionPointer<T> is NOT supported under NativeAOT.
    //  We use C# 9+ unmanaged function pointers (delegate*<...>) instead ¡ª
    //  these are direct call-throughs with zero managed overhead and full AOT support.
    //
    //  Vtable slot reference (x64, all inherit IUnknown slots 0-2):
    //
    //  IUnknown:
    //    0  QueryInterface
    //    1  AddRef
    //    2  Release
    //
    //  IDXGISwapChain (IUnknown ¡ú IDXGIObject ¡ú IDXGIDeviceSubObject ¡ú IDXGISwapChain):
    //    3  SetPrivateData       6  GetParent
    //    4  SetPrivateDataInterface  7  GetDevice
    //    5  GetPrivateData
    //    8  Present              ¡û used
    //    9  GetBuffer            ¡û used
    //   10  SetFullscreenState
    //   11  GetFullscreenState
    //   12  GetDesc
    //   13  ResizeBuffers        ¡û used
    //
    //  ID3D11Device (IUnknown ¡ú ID3D11Device):
    //    3  CreateBuffer         8  CreateUnorderedAccessView
    //    4  CreateTexture1D      9  CreateRenderTargetView   ¡û used
    //    5  CreateTexture2D     10  CreateDepthStencilView
    //    6  CreateTexture3D     11  CreateShaderResourceView1
    //    7  CreateShaderResourceView
    //
    //  ID3D11DeviceContext (IUnknown ¡ú ID3D11DeviceChild ¡ú ID3D11DeviceContext):
    //   33  OMSetRenderTargets   ¡û used
    //   44  RSSetViewports       ¡û used
    //   50  ClearRenderTargetView ¡û used
    // /////////////////////////////////////////////////////////////////////////////////////

    public static unsafe class DX11
    {
        // ©¤©¤ Vtable slot reader ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
        // Reads vtable[slot] from a COM pointer and returns it as a raw nint.
        // No reflection, no delegates ¡ª fully NativeAOT safe.

        private static nint Slot(void* comPtr, int slot)
        {
            var vtbl = *(nint*)comPtr;          // vtable pointer is at offset 0
            return *((nint*)vtbl + slot);        // dereference slot
        }

        // ©¤©¤ IUnknown ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        public static uint Release(nint punk)
        {
            if (punk == 0) return 0;
            var fn = (delegate* unmanaged[Stdcall]<nint, uint>)Slot((void*)punk, 2);
            return fn(punk);
        }

        // ©¤©¤ IDXGISwapChain ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        public static int SwapChain_Present(nint sc, uint syncInterval, uint flags)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint, uint, int>)Slot((void*)sc, 8);
            return fn(sc, syncInterval, flags);
        }

        public static int SwapChain_GetBuffer(nint sc, uint buffer, Guid* riid, nint* ppSurface)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint, Guid*, nint*, int>)Slot((void*)sc, 9);
            return fn(sc, buffer, riid, ppSurface);
        }

        public static int SwapChain_ResizeBuffers(nint sc, uint count,
            uint w, uint h, uint fmt, uint flags)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint, uint, uint, uint, uint, int>)Slot((void*)sc, 13);
            return fn(sc, count, w, h, fmt, flags);
        }

        // ©¤©¤ ID3D11Device ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        public static int Device_CreateRenderTargetView(nint device,
            nint resource, nint desc, nint* ppRtv)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, nint, nint, nint*, int>)Slot((void*)device, 9);
            return fn(device, resource, desc, ppRtv);
        }

        // ©¤©¤ ID3D11DeviceContext ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

        public static void Context_OMSetRenderTargets(nint ctx, nint rtv)
        {
            // void OMSetRenderTargets(UINT NumViews, RTV* const*, DSV*)
            var fn = (delegate* unmanaged[Stdcall]<nint, uint, nint*, nint, void>)Slot((void*)ctx, 33);
            fn(ctx, 1, &rtv, 0);
        }

        public static void Context_ClearRenderTargetView(nint ctx, nint rtv,
            float r, float g, float b, float a)
        {
            // void ClearRenderTargetView(RTV*, const FLOAT[4])
            float* color = stackalloc float[4] { r, g, b, a };
            var fn = (delegate* unmanaged[Stdcall]<nint, nint, float*, void>)Slot((void*)ctx, 50);
            fn(ctx, rtv, color);
        }

        public static void Context_RSSetViewports(nint ctx, uint width, uint height)
        {
            var vp = new D3D11_VIEWPORT
            {
                TopLeftX = 0f, TopLeftY = 0f,
                Width    = width, Height = height,
                MinDepth = 0f,  MaxDepth = 1f,
            };
            var fn = (delegate* unmanaged[Stdcall]<nint, uint, D3D11_VIEWPORT*, void>)Slot((void*)ctx, 44);
            fn(ctx, 1, &vp);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct D3D11_VIEWPORT
    {
        public float TopLeftX;
        public float TopLeftY;
        public float Width;
        public float Height;
        public float MinDepth;
        public float MaxDepth;
    }
}