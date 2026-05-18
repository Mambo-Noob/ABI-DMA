using System.Runtime.InteropServices;
using System.Threading;

namespace ImGuiOverlay.ABI
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  TIMER RESOLUTION  ·  Sets Windows multimedia timer to 1 ms for tight sleep loops
    // /////////////////////////////////////////////////////////////////////////////////////

    internal static class TimerResolution
    {
        [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint uMilliseconds);

        private static int _ref;

        public static void Enable1ms()  { if (Interlocked.Increment(ref _ref) == 1) timeBeginPeriod(1); }
        public static void Disable1ms() { if (Interlocked.Decrement(ref _ref) == 0) timeEndPeriod(1); }
    }
}
