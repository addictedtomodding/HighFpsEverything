using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;

namespace HighFpsCursor
{
    internal static class TickContext
    {
        internal static bool Ready;
        internal static bool CurrentlyTicking;
        internal static int TickIndex;

        [HarmonyPatch(typeof(Main), "Update")]
        private static class Patch_Main_Update_Tick
        {
            private static void Prefix()
            {
                if (!Ready) return;
                CurrentlyTicking = true;
                TickIndex++;
                LightingFixes.BeginTick();
            }

            private static void Postfix()
            {
                if (!Ready) return;
                CurrentlyTicking = false;
            }
        }
    }

    internal static class Bootstrap
    {
        private static bool _installed;

        [HarmonyPatch(typeof(Main), "ClientInitialize")]
        private static class Patch_Main_ClientInitialize
        {
            private static void Postfix()
            {
                if (_installed) return;
                _installed = true;

                TickContext.Ready = true;

                var harmony = new Harmony("HighFpsCursor.Bootstrap");
                LightingFixes.InstallLate(harmony);
            }
        }
    }

    internal static class LightingFixes
    {
        internal struct LightSample
        {
            public int X;
            public int Y;
            public Vector3 Color;

            public LightSample(int x, int y, Vector3 c)
            {
                X = x;
                Y = y;
                Color = c;
            }
        }

        private static readonly List<LightSample> _tickLights = new List<LightSample>(4096);
        private static readonly List<LightSample> _drawLights = new List<LightSample>(2048);

        private static int _lastAppliedTick;
        private static bool _replaying;
        private static bool _installedLate;

        internal static void BeginTick()
        {
            _tickLights.Clear();
        }

        internal static void InstallLate(Harmony h)
        {
            if (_installedLate) return;
            _installedLate = true;

            var tLighting = AccessTools.TypeByName("Terraria.Lighting");
            var tLightingEngine = AccessTools.TypeByName("Terraria.Graphics.Light.LightingEngine");

            if (tLighting == null || tLightingEngine == null)
                return;

            var mLightingAddLight = AccessTools.Method(tLighting, "AddLight", new[] { typeof(int), typeof(int), typeof(float), typeof(float), typeof(float) });
            var mEngineApply = AccessTools.Method(tLightingEngine, "ApplyPerFrameLights", Type.EmptyTypes);
            var mEngineAddLight = AccessTools.Method(tLightingEngine, "AddLight", new[] { typeof(int), typeof(int), typeof(Vector3) });

            if (mLightingAddLight == null || mEngineApply == null || mEngineAddLight == null)
                return;

            h.Patch(mLightingAddLight,
                prefix: new HarmonyMethod(typeof(LightingFixes).GetMethod(nameof(Prefix_Lighting_AddLight), BindingFlags.NonPublic | BindingFlags.Static)));

            h.Patch(mEngineApply,
                prefix: new HarmonyMethod(typeof(LightingFixes).GetMethod(nameof(Prefix_Engine_ApplyPerFrameLights), BindingFlags.NonPublic | BindingFlags.Static)));

            h.Patch(mEngineAddLight,
                postfix: new HarmonyMethod(typeof(LightingFixes).GetMethod(nameof(Postfix_Engine_AddLight_CaptureTick), BindingFlags.NonPublic | BindingFlags.Static)));
        }

        private static bool Prefix_Lighting_AddLight(int i, int j, float r, float g, float b)
        {
            if (!TickContext.Ready) return true;
            if (_replaying) return true;

            bool usingNewLighting = false;
            try
            {
                var p = AccessTools.Property(AccessTools.TypeByName("Terraria.Lighting"), "UsingNewLighting");
                if (p != null && p.PropertyType == typeof(bool))
                    usingNewLighting = (bool)p.GetValue(null, null);
            }
            catch { }

            if (!usingNewLighting)
                return true;

            if (!TickContext.CurrentlyTicking)
            {
                _drawLights.Add(new LightSample(i, j, new Vector3(r, g, b)));
                return false;
            }

            return true;
        }

        private static void Postfix_Engine_AddLight_CaptureTick(int x, int y, Vector3 color)
        {
            if (!TickContext.Ready) return;
            if (_replaying) return;

            if (TickContext.CurrentlyTicking)
                _tickLights.Add(new LightSample(x, y, color));
        }

        private static void Prefix_Engine_ApplyPerFrameLights(object __instance)
        {
            if (!TickContext.Ready) return;

            int tick = TickContext.TickIndex;

            if (tick != _lastAppliedTick)
            {
                _lastAppliedTick = tick;
                _drawLights.Clear();
                return;
            }

            if (_tickLights.Count == 0 && _drawLights.Count == 0)
                return;

            var mAddLight = AccessTools.Method(__instance.GetType(), "AddLight", new[] { typeof(int), typeof(int), typeof(Vector3) });
            if (mAddLight == null)
                return;

            _replaying = true;
            try
            {
                for (int k = 0; k < _tickLights.Count; k++)
                {
                    var s = _tickLights[k];
                    mAddLight.Invoke(__instance, new object[] { s.X, s.Y, s.Color });
                }

                for (int k = 0; k < _drawLights.Count; k++)
                {
                    var s = _drawLights[k];
                    mAddLight.Invoke(__instance, new object[] { s.X, s.Y, s.Color });
                }
            }
            finally
            {
                _replaying = false;
            }

            _drawLights.Clear();
        }
    }
}

