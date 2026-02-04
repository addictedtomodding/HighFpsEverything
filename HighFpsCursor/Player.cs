using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;

namespace HighFpsCursor
{
    public static class PlayerRenderInterpState
    {
        public static Vector2 PrevPos;
        public static Vector2 CurrPos;
        public static long LastTickStamp;
        public static long TickDeltaStamp;
        public static bool HasFrame;
    }

    [HarmonyPatch]
    public static class Patch_CapturePlayerTick
    {
        static MethodBase TargetMethod()
        {
            var t = typeof(Main);
            var m = AccessTools.Method(t, "Update", new Type[] { typeof(GameTime) });
            if (m != null) return m;
            m = AccessTools.Method(t, "DoUpdate", new Type[] { typeof(GameTime) });
            return m;
        }

        static void Postfix()
        {
            try
            {
                if (Main.player == null) return;
                int i = Main.myPlayer;
                if (i < 0 || i >= Main.player.Length) return;

                var p = Main.player[i];
                if (p == null) return;

                long now = Stopwatch.GetTimestamp();

                if (!PlayerRenderInterpState.HasFrame)
                {
                    PlayerRenderInterpState.PrevPos = p.position;
                    PlayerRenderInterpState.CurrPos = p.position;
                    PlayerRenderInterpState.LastTickStamp = now;
                    PlayerRenderInterpState.TickDeltaStamp = Stopwatch.Frequency / 60;
                    PlayerRenderInterpState.HasFrame = true;
                    return;
                }

                long dt = now - PlayerRenderInterpState.LastTickStamp;
                if (dt > 0) PlayerRenderInterpState.TickDeltaStamp = dt;

                PlayerRenderInterpState.PrevPos = PlayerRenderInterpState.CurrPos;
                PlayerRenderInterpState.CurrPos = p.position;
                PlayerRenderInterpState.LastTickStamp = now;
            }
            catch
            {
            }
        }
    }
}