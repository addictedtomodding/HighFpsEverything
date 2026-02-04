using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Terraria;
using Terraria.UI;

namespace HighFpsCursor
{
    public static class MouseSample
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        public static void Update()
        {
            try
            {
                if (Main.instance == null || Main.instance.Window == null)
                    return;

                if (!Main.hasFocus)
                    return;

                if (Main.screenWidth <= 0 || Main.screenHeight <= 0)
                    return;

                IntPtr hwnd = Main.instance.Window.Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                if (!GetCursorPos(out POINT p))
                    return;

                if (!ScreenToClient(hwnd, ref p))
                    return;

                if (!GetClientRect(hwnd, out RECT r))
                    return;

                int clientW = r.Right - r.Left;
                int clientH = r.Bottom - r.Top;
                if (clientW <= 0 || clientH <= 0)
                    return;

                float sx = Main.screenWidth / (float)clientW;
                float sy = Main.screenHeight / (float)clientH;

                int x = (int)(p.X * sx);
                int y = (int)(p.Y * sy);

                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x > Main.screenWidth - 1) x = Main.screenWidth - 1;
                if (y > Main.screenHeight - 1) y = Main.screenHeight - 1;

                Main.mouseX = x;
                Main.mouseY = y;
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_Cursor_DrawPaths
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(Main)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.Name.IndexOf("Cursor", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static void Prefix()
        {
            MouseSample.Update();
        }
    }

    [HarmonyPatch(typeof(UserInterface), "Draw")]
    public static class Patch_Cursor_UI
    {
        static void Prefix()
        {
            MouseSample.Update();
        }
    }
}
