using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using ImGuiOverlay.ABI;
using ImGuiOverlay.Config;
using ImGuiOverlay.DMA;
using ImGuiOverlay.Theme;
using ImGuiOverlay.UI;

namespace ImGuiOverlay.Core
{
    public class OverlayApp : IDisposable
    {
        private IntPtr _hwnd;
        private IntPtr _hInstance;
        private int    _width, _height;
        private int    _monX,  _monY;

        private static WndProcDelegate? _wndProcDelegate;
        private static GCHandle         _wndProcPin;
        private const  string           ClassName = "ImGuiOverlayWnd";

        private nint _swapChain;
        private nint _device;
        private nint _context;
        private nint _rtv;

        private static readonly Guid IID_ID3D11Texture2D =
            new Guid(0x6f15aaf2, 0xd208, 0x4e89,
                     0x9a, 0xb4, 0x48, 0x95, 0x35, 0xd3, 0x4f, 0x9c);

        private OverlayTheme         _theme    = null!;
        private OverlaySettings      _settings = null!;
        private OverlayWindowManager _winMgr   = null!;
        private ABIController        _abi      = null!;

        private bool _running  = true;
        private bool _disposed = false;

        public struct MonitorInfo
        {
            public string Name;
            public int    X, Y, Width, Height;
            public bool   IsPrimary;
        }

        public void Run()
        {
            _settings  = ConfigManager.Load<OverlaySettings>("overlay_settings.json");
            _hInstance = NativeMethods.GetModuleHandle(null);

            var mons   = GetMonitors();
            int monIdx = Math.Clamp(_settings.TargetMonitor, 0, Math.Max(0, mons.Count - 1));
            var mon    = mons.Count > 0 ? mons[monIdx] : new MonitorInfo { Width = 1920, Height = 1080 };

            _width  = mon.Width;
            _height = mon.Height;
            _monX   = mon.X;
            _monY   = mon.Y;

            CreateWin32Window();
            Console.WriteLine("[OverlayApp] Window created");
            InitDX11();
            Console.WriteLine("[OverlayApp] DX11 initialized");
            InitImGui();
            Console.WriteLine("[OverlayApp] ImGui initialized");
            InitApp();
            Console.WriteLine("[OverlayApp] App initialized ?? entering main loop");

            var fpsTimer = System.Diagnostics.Stopwatch.StartNew();

            while (_running)
            {
                PumpMessages();
                if (!_running) break;

                _abi.Tick();

                ImGuiImplDX11.NewFrame();
                ImGuiImplWin32.NewFrame();
                ImGui.NewFrame();

                var size = new Vector2(_width, _height);
                _winMgr.Render(size);
                _abi.Render();

                ImGui.Render();

                // Solid black ?? fuser handles compositing
                DX11.Context_ClearRenderTargetView(_context, _rtv, 0f, 0f, 0f, 1f);
                DX11.Context_OMSetRenderTargets(_context, _rtv);
                ImGuiImplDX11.RenderDrawData(ImGui.GetDrawData());

                // VSync: syncInterval 1 = wait for vblank, 0 = present immediately
                uint syncInterval = _settings.VSync ? 1u : 0u;
                DX11.SwapChain_Present(_swapChain, syncInterval, 0);

                // Software FPS limiter (only active when VSync is off)
                if (!_settings.VSync && _settings.FpsLimitEnabled && _settings.FpsLimit > 0)
                {
                    double targetMs = 1000.0 / _settings.FpsLimit;
                    double elapsed  = fpsTimer.Elapsed.TotalMilliseconds;
                    double sleepMs  = targetMs - elapsed;
                    if (sleepMs > 1.5)
                        System.Threading.Thread.Sleep((int)(sleepMs - 1.0));
                    // Spin for sub-millisecond precision
                    while (fpsTimer.Elapsed.TotalMilliseconds < targetMs) { }
                }
                fpsTimer.Restart();
            }

            Cleanup();
        }

        private void CreateWin32Window()
        {
            _wndProcDelegate = WndProc;
            _wndProcPin      = GCHandle.Alloc(_wndProcDelegate);

            var classNameHandle = GCHandle.Alloc(ClassName, GCHandleType.Pinned);
            unsafe
            {
                fixed (char* pName = ClassName)
                {
                    var wc = new WNDCLASSEX
                    {
                        cbSize        = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                        style         = CS.HREDRAW | CS.VREDRAW,
                        lpfnWndProc   = _wndProcDelegate,
                        hInstance     = _hInstance,
                        hCursor       = NativeMethods.LoadCursor(IntPtr.Zero, (IntPtr)32512),
                        lpszMenuName  = IntPtr.Zero,
                        lpszClassName = (IntPtr)pName,
                    };

                    ushort atom = NativeMethods.RegisterClassEx(ref wc);
                    if (atom == 0)
                        throw new InvalidOperationException($"RegisterClassExW failed: {Marshal.GetLastWin32Error()}");
                }
            }
            classNameHandle.Free();

            // No WS_EX_LAYERED / WS_EX_TRANSPARENT ?? solid black window
            // fuser handles compositing on the capture card side
            uint exStyle = WS.EX_TOPMOST | WS.EX_NOACTIVATE | WS.EX_TOOLWINDOW;
            uint style   = WS.POPUP | WS.VISIBLE;

            _hwnd = NativeMethods.CreateWindowEx(
                exStyle, ClassName, ClassName,
                style,
                _monX, _monY, _width, _height,
                IntPtr.Zero, IntPtr.Zero, _hInstance, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
                throw new InvalidOperationException($"CreateWindowExW failed: {Marshal.GetLastWin32Error()}");

            NativeMethods.ShowWindow(_hwnd, SW.SHOW);
            NativeMethods.UpdateWindow(_hwnd);
        }

        private void InitDX11()
        {
            var sd = new DXGI_SWAP_CHAIN_DESC
            {
                BufferCount   = 2,
                BufferDesc    = new DXGI_MODE_DESC
                {
                    Width            = (uint)_width,
                    Height           = (uint)_height,
                    Format           = DXGI_FORMAT.R8G8B8A8_UNORM,
                    RefreshRate      = new DXGI_RATIONAL { Numerator = 60, Denominator = 1 },
                    ScanlineOrdering = 0,
                    Scaling          = 0,
                },
                SampleDesc    = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                BufferUsage   = DXGI_USAGE.RENDER_TARGET_OUTPUT,
                OutputWindow  = _hwnd,
                Windowed      = 1,
                SwapEffect    = DXGI_SWAP_EFFECT.DISCARD,
                Flags         = DXGI_SWAP_CHAIN_FLAG.ALLOW_MODE_SWITCH,
            };

            uint[] featureLevels = {
                D3D_FEATURE_LEVEL.LEVEL_11_0,
                D3D_FEATURE_LEVEL.LEVEL_10_1,
                D3D_FEATURE_LEVEL.LEVEL_10_0,
            };

            int hr = NativeMethods.D3D11CreateDeviceAndSwapChain(
                IntPtr.Zero,
                D3D_DRIVER_TYPE.HARDWARE,
                IntPtr.Zero,
                0,
                featureLevels,
                (uint)featureLevels.Length,
                D3D11_SDK_VERSION.Value,
                ref sd,
                out nint sc,
                out nint dev,
                out _,
                out nint ctx);

            if (hr < 0)
                throw new InvalidOperationException($"D3D11CreateDeviceAndSwapChain failed: 0x{hr:X8}");

            _swapChain = sc;
            _device    = dev;
            _context   = ctx;

            CreateRenderTarget();
        }

        private unsafe void CreateRenderTarget()
        {
            var iid = IID_ID3D11Texture2D;
            nint backBuf;
            int hr = DX11.SwapChain_GetBuffer(_swapChain, 0, &iid, &backBuf);
            if (hr < 0 || backBuf == 0)
                throw new InvalidOperationException($"GetBuffer failed: 0x{hr:X8}");

            nint rtv;
            hr = DX11.Device_CreateRenderTargetView(_device, backBuf, 0, &rtv);
            DX11.Release(backBuf);

            if (hr < 0)
                throw new InvalidOperationException($"CreateRenderTargetView failed: 0x{hr:X8}");

            _rtv = rtv;
        }

        private void CleanupRenderTarget()
        {
            if (_rtv != 0) { DX11.Release(_rtv); _rtv = 0; }
        }

        private void InitImGui()
        {
            ImGui.CreateContext();
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            // ViewportsEnable off ?? causes ghost OS windows, not needed here
            io.ConfigWindowsMoveFromTitleBarOnly = true;
            // No ini ?? layout is code-driven
            unsafe { io.NativePtr->IniFilename = null; }

            io.DisplaySize = new Vector2(_width, _height);

            if (!ImGuiImplWin32.Init(_hwnd))
                throw new InvalidOperationException("ImGui_ImplWin32_Init failed");

            if (!ImGuiImplDX11.Init(_device, _context))
                throw new InvalidOperationException("ImGui_ImplDX11_Init failed");

            LoadFonts(io);

            _theme = ConfigManager.Exists(_settings.ActiveThemeFile)
                   ? ConfigManager.Load<OverlayTheme>(_settings.ActiveThemeFile)
                   : new OverlayTheme();
            _theme.Apply();
        }

        private void InitApp()
        {
            _winMgr = new OverlayWindowManager(_settings, _theme);
            _abi    = new ABIController(_theme);
            _winMgr.SetABIController(_abi);

            OverlayEvents.OnToggleMenu += () => _winMgr.ToggleMenu();
            OverlayEvents.OnQuit       += () => _running = false;

            DmaMemory.OnAttached += () =>
            {
                Logger.Info($"[OverlayApp] DMA attached ?? PID={DmaMemory.Pid}  Base=0x{DmaMemory.Base:X}");
                _abi.Start();
            };
            DmaMemory.OnDetached += () =>
            {
                Logger.Warn("[OverlayApp] DMA detached ?? waiting for game...");
                _abi.Stop();
            };

            if (DmaMemory.IsAttached)
            {
                Logger.Info($"[OverlayApp] DMA already attached at startup ?? PID={DmaMemory.Pid}");
                _abi.Start();
            }
            else
            {
                Logger.Warn("[OverlayApp] Waiting for game process...");
            }
        }

        private void PumpMessages()
        {
            while (NativeMethods.PeekMessage(out MSG msg, IntPtr.Zero, 0, 0, PM.REMOVE))
            {
                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessage(ref msg);
                if (msg.message == WM.QUIT)
                    _running = false;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if (ImGuiImplWin32.WndProcHandler(hwnd, msg, wParam, lParam))
                return (IntPtr)1;

            switch (msg)
            {
                case WM.KEYDOWN:
                    int key = (int)wParam;
                    if (key == VK.INSERT) OverlayEvents.RaiseToggleMenu();
                    if (key == VK.END)    { NativeMethods.PostQuitMessage(0); _running = false; }
                    break;

                case WM.SIZE:
                    if (_swapChain != 0)
                    {
                        uint newW = (uint)(lParam.ToInt64() & 0xFFFF);
                        uint newH = (uint)((lParam.ToInt64() >> 16) & 0xFFFF);
                        if (newW > 0 && newH > 0)
                        {
                            _width  = (int)newW;
                            _height = (int)newH;
                            CleanupRenderTarget();
                            DX11.SwapChain_ResizeBuffers(_swapChain, 0, newW, newH,
                                DXGI_FORMAT.R8G8B8A8_UNORM, 0);
                            CreateRenderTarget();
                            ImGui.GetIO().DisplaySize = new Vector2(_width, _height);
                        }
                    }
                    break;

                case WM.DESTROY:
                    NativeMethods.PostQuitMessage(0);
                    _running = false;
                    break;
            }

            return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        private void Cleanup()
        {
            if (_disposed) return;
            _disposed = true;

            Logger.Info("[OverlayApp] Shutting down...");

            _abi?.Dispose();
            ConfigManager.Save(_settings, "overlay_settings.json");
            ConfigManager.Save(_abi?.Config, "abi_config.json");
            if (_theme != null)
                ConfigManager.Save(_theme, _settings.ActiveThemeFile);
            Logger.Shutdown();

            ImGuiImplDX11.Shutdown();
            ImGuiImplWin32.Shutdown();
            ImGui.DestroyContext();

            CleanupRenderTarget();
            if (_context   != 0) { DX11.Release(_context);   _context   = 0; }
            if (_swapChain != 0) { DX11.Release(_swapChain); _swapChain = 0; }
            if (_device    != 0) { DX11.Release(_device);    _device    = 0; }

            if (_hwnd != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
            NativeMethods.UnregisterClass(ClassName, _hInstance);

            if (_wndProcPin.IsAllocated) _wndProcPin.Free();
            _wndProcDelegate = null;
        }

        public void Dispose() => Cleanup();

        private static void LoadFonts(ImGuiIOPtr io)
        {
            io.Fonts.Clear();
            if (System.IO.File.Exists("fonts/Poppins-Medium.ttf"))
                io.Fonts.AddFontFromFileTTF("fonts/Poppins-Medium.ttf", 14f);
            else
                io.Fonts.AddFontDefault();
            io.Fonts.Build();
        }

        public static IReadOnlyList<MonitorInfo> GetMonitors()
        {
            var list = new List<MonitorInfo>();
            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMon, _, ref rc, _) =>
            {
                var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
                if (NativeMethods.GetMonitorInfo(hMon, ref mi))
                {
                    var info = new MonitorInfo
                    {
                        Name      = mi.szDevice,
                        X         = mi.rcMonitor.Left,
                        Y         = mi.rcMonitor.Top,
                        Width     = mi.rcMonitor.Right  - mi.rcMonitor.Left,
                        Height    = mi.rcMonitor.Bottom - mi.rcMonitor.Top,
                        IsPrimary = (mi.dwFlags & 1u) != 0,
                    };
                    if (info.IsPrimary) list.Insert(0, info);
                    else                list.Add(info);
                }
                return true;
            }, IntPtr.Zero);
            return list;
        }
    }
}