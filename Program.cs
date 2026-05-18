using System;
using System.Runtime.InteropServices;
using ImGuiOverlay.Core;
using ImGuiOverlay.DMA;

internal static class Program
{
    [System.STAThread]
    private static void Main()
    {
        AllocConsole();

        DmaMemory.Init(device: "fpga", useMemMap: true);

        DmaMemory.OnAttached += () =>
            Console.WriteLine($"[Program] Attached ¡ª PID={DmaMemory.Pid}  Base=0x{DmaMemory.Base:X}");

        DmaMemory.OnDetached += () =>
            Console.WriteLine("[Program] Process lost ¡ª waiting to re-attach...");

        try
        {
            using var app = new OverlayApp();
            app.Run();
        }
        catch (Exception ex)
        {
            string msg = $"FATAL: {ex.GetType().Name}\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}";
            Console.WriteLine(msg);
            MessageBox(IntPtr.Zero, msg, "ImGuiOverlay Crash", 0x10); // MB_ICONERROR
        }

        DmaMemory.Dispose();
    }

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}