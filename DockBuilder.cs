using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace ImGuiOverlay.Core
{
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //  DOCK BUILDER  â€?  P/Invoke wrappers for imgui_internal DockBuilder functions
    //
    //  ImGui.NET's NuGet package (1.90.x) only wraps imgui.h â€? the DockBuilder
    //  functions live in imgui_internal.h and are not included.
    //
    //  We call them directly through the native cimgui.dll / libcimgui.so that
    //  ImGui.NET bundles, because cimgui does export them.
    //
    //  Usage:
    //      DockBuilder.RemoveNode(dockspaceId);
    //      DockBuilder.AddNode(dockspaceId, ImGuiDockNodeFlags.None);
    //      DockBuilder.SetNodeSize(dockspaceId, size);
    //      uint right = DockBuilder.SplitNode(dockspaceId, ImGuiDir.LABI, 0.25f,
    //                                         out uint lABIId, out _);
    //      DockBuilder.DockWindow("My Window", lABIId);
    //      DockBuilder.Finish(dockspaceId);
    // //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    internal static unsafe class DockBuilder
    {
#if Windows
        private const string Lib = "cimgui.dll";
#elif Linux
        private const string Lib = "libcimgui.so";
#else
        private const string Lib = "cimgui";
#endif

        // //// Raw P/Invoke ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igDockBuilderRemoveNode(uint node_id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint igDockBuilderAddNode(uint node_id, ImGuiDockNodeFlags flags);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igDockBuilderSetNodeSize(uint node_id, Vector2 size);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint igDockBuilderSplitNode(
            uint node_id, ImGuiDir split_dir, float size_ratio_for_node_at_dir,
            uint* out_id_at_dir, uint* out_id_at_opposite_dir);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igDockBuilderDockWindow(byte* window_name, uint node_id);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void igDockBuilderFinish(uint node_id);

        // //// Safe wrappers //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void RemoveNode(uint nodeId)
            => igDockBuilderRemoveNode(nodeId);

        public static uint AddNode(uint nodeId, ImGuiDockNodeFlags flags = ImGuiDockNodeFlags.None)
            => igDockBuilderAddNode(nodeId, flags);

        public static void SetNodeSize(uint nodeId, Vector2 size)
            => igDockBuilderSetNodeSize(nodeId, size);

        public static uint SplitNode(uint nodeId, ImGuiDir splitDir, float ratio,
            out uint outIdAtDir, out uint outIdAtOppositeDir)
        {
            uint a, b;
            uint result = igDockBuilderSplitNode(nodeId, splitDir, ratio, &a, &b);
            outIdAtDir         = a;
            outIdAtOppositeDir = b;
            return result;
        }

        public static void DockWindow(string windowName, uint nodeId)
        {
            // Stack-allocate a null-terminated UTF-8 byte sequence
            int byteCount = System.Text.Encoding.UTF8.GetByteCount(windowName) + 1;
            byte* buf = stackalloc byte[byteCount];
            fixed (char* src = windowName)
            {
                System.Text.Encoding.UTF8.GetBytes(src, windowName.Length, buf, byteCount);
            }
            buf[byteCount - 1] = 0;
            igDockBuilderDockWindow(buf, nodeId);
        }

        public static void Finish(uint nodeId)
            => igDockBuilderFinish(nodeId);
    }
}
