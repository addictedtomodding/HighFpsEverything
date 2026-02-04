using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;

namespace HighFpsCursor
{
    public struct PosRotFrame
    {
        public Vector2 PrevPos;
        public Vector2 CurrPos;
        public float PrevRot;
        public float CurrRot;
        public bool Has;
    }

    public struct ApplyState
    {
        public bool Valid;
        public bool IsNPC;
        public int Id;
        public Vector2 Pos;
        public float Rot;
    }

    public static class RenderInterp
    {
        public static PosRotFrame[] NPC = new PosRotFrame[Main.maxNPCs];
        public static PosRotFrame[] Proj = new PosRotFrame[Main.maxProjectiles];

        public static long LastTickStamp;
        public static long TickDeltaStamp = Stopwatch.Frequency / 60;
        public static bool HasTick;

        public static float AlphaNow()
        {
            long dt = TickDeltaStamp;
            if (dt <= 0) return 1f;

            long now = Stopwatch.GetTimestamp();
            float a = (float)(now - LastTickStamp) / (float)dt;
            if (a < 0f) a = 0f;
            if (a > 1f) a = 1f;
            return a;
        }
    }

}