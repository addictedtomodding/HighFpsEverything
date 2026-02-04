using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using HarmonyLib;
using Terraria;

namespace HighFpsCursor
{
    internal static class MenuFpsFix
    {
        private static readonly long Step = Stopwatch.Frequency / 60;
        private static long _lastStepStamp;
        private static bool _allowStep;

        private static float GetMenuXMovement()
        {
            if (!Main.gameMenu)
            {
                _lastStepStamp = 0;
                _allowStep = true;
                return 4f;
            }

            long now = Stopwatch.GetTimestamp();
            if (_lastStepStamp == 0)
            {
                _lastStepStamp = now;
                _allowStep = true;
                return 0f;
            }

            long delta = now - _lastStepStamp;
            _lastStepStamp = now;

            double dt = (double)delta / (double)Stopwatch.Frequency;
            if (dt < 0.0) dt = 0.0;
            if (dt > 0.25) dt = 0.25;

            _allowStep = true;

            return (float)(240.0 * dt);
        }


        [HarmonyPatch(typeof(Main), "DrawMenu")]
        private static class Patch_DrawMenu_MenuXMovement_Transpile
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = new List<CodeInstruction>(instructions);

                var targetField = AccessTools.Field(typeof(Main), "MenuXMovement");
                var getter = AccessTools.Method(typeof(MenuFpsFix), nameof(GetMenuXMovement));

                for (int i = 0; i < list.Count - 1; i++)
                {
                    if (list[i].opcode == OpCodes.Ldc_R4 &&
                        list[i].operand is float f &&
                        Math.Abs(f - 4f) < 0.0001f &&
                        list[i + 1].opcode == OpCodes.Stsfld &&
                        list[i + 1].operand is System.Reflection.FieldInfo fi &&
                        fi == targetField)
                    {
                        list[i] = new CodeInstruction(OpCodes.Call, getter);
                        break;
                    }
                }

                return list;
            }
        }

        [HarmonyPatch(typeof(Star), "UpdateStars")]
        private static class Patch_Star_UpdateStars_MenuRate
        {
            private static bool Prefix()
            {
                if (Main.gameMenu && !_allowStep)
                    return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(Cloud), "UpdateClouds")]
        private static class Patch_Cloud_UpdateClouds_MenuRate
        {
            private static bool Prefix()
            {
                if (Main.gameMenu && !_allowStep)
                    return false;
                return true;
            }
        }
    }
}
