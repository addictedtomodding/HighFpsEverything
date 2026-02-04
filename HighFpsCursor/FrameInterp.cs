using System;
using System.Diagnostics;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;

namespace HighFpsCursor
{
    [HarmonyPatch(typeof(Main), "Draw")]
    public static class Patch_FrameInterpolation_Draw
    {
        private struct PlayerFrameState
        {
            public bool Applied;
            public Vector2 SavedPos;
            public Vector2 InterpPos;

            public Vector2 SavedItemLocation;
            public bool HadItemLocation;
        }

        private static Vector2[] _savedNpcPos = new Vector2[Main.maxNPCs];
        private static float[] _savedNpcRot = new float[Main.maxNPCs];
        private static bool[] _touchedNpc = new bool[Main.maxNPCs];

        private static Vector2[] _savedProjPos = new Vector2[Main.maxProjectiles];
        private static float[] _savedProjRot = new float[Main.maxProjectiles];
        private static bool[] _touchedProj = new bool[Main.maxProjectiles];

        private static void Prefix(ref PlayerFrameState __state)
        {
            __state = default;

            try
            {
                if (!RenderInterp.HasTick) return;
                if (!PlayerRenderInterpState.HasFrame) return;

                long dt = RenderInterp.TickDeltaStamp;
                if (dt <= 0) return;

                int my = Main.myPlayer;
                if (Main.player == null) return;
                if (my < 0 || my >= Main.player.Length) return;

                var plr = Main.player[my];
                if (plr == null) return;

                long now = Stopwatch.GetTimestamp();
                float alpha = (float)(now - RenderInterp.LastTickStamp) / (float)dt;
                if (alpha < 0f) alpha = 0f;
                if (alpha > 1f) alpha = 1f;

                Vector2 saved = plr.position;
                Vector2 interp = Vector2.Lerp(PlayerRenderInterpState.PrevPos, PlayerRenderInterpState.CurrPos, alpha);

                if ((PlayerRenderInterpState.CurrPos - saved).LengthSquared() > 2000000f)
                    return;

                __state.Applied = true;
                __state.SavedPos = saved;
                __state.InterpPos = interp;

                plr.position = interp;

                __state.SavedItemLocation = plr.itemLocation;
                __state.HadItemLocation = true;

                Vector2 delta = interp - saved;
                plr.itemLocation += delta;

                Array.Clear(_touchedNpc, 0, _touchedNpc.Length);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    var n = Main.npc[i];
                    if (n == null || !n.active) continue;

                    ref var f = ref RenderInterp.NPC[i];
                    if (!f.Has) continue;

                    _savedNpcPos[i] = n.position;
                    _savedNpcRot[i] = n.rotation;
                    _touchedNpc[i] = true;

                    n.position = Vector2.Lerp(f.PrevPos, f.CurrPos, alpha);
                    n.rotation = MathHelper.Lerp(f.PrevRot, f.CurrRot, alpha);
                }

                Array.Clear(_touchedProj, 0, _touchedProj.Length);
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    var p = Main.projectile[i];
                    if (p == null || !p.active) continue;

                    ref var f = ref RenderInterp.Proj[i];
                    if (!f.Has) continue;

                    _savedProjPos[i] = p.position;
                    _savedProjRot[i] = p.rotation;
                    _touchedProj[i] = true;

                    p.position = Vector2.Lerp(f.PrevPos, f.CurrPos, alpha);
                    p.rotation = MathHelper.Lerp(f.PrevRot, f.CurrRot, alpha);
                }
            }
            catch
            {
                __state = default;
            }
        }

        private static void Postfix(ref PlayerFrameState __state)
        {
            try
            {
                if (__state.Applied)
                {
                    int my = Main.myPlayer;
                    if (Main.player != null && my >= 0 && my < Main.player.Length && Main.player[my] != null)
                    {
                        var plr = Main.player[my];
                        Vector2 after = plr.position;

                        if ((after - __state.InterpPos).LengthSquared() < 0.25f)
                        {
                            plr.position = __state.SavedPos;
                        }
                        else
                        {
                            PlayerRenderInterpState.PrevPos = after;
                            PlayerRenderInterpState.CurrPos = after;
                            PlayerRenderInterpState.LastTickStamp = Stopwatch.GetTimestamp();
                        }

                        if (__state.HadItemLocation)
                        {
                            plr.itemLocation = __state.SavedItemLocation;
                        }
                    }
                }

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!_touchedNpc[i]) continue;
                    var n = Main.npc[i];
                    if (n != null)
                    {
                        n.position = _savedNpcPos[i];
                        n.rotation = _savedNpcRot[i];
                    }
                    _touchedNpc[i] = false;
                }

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (!_touchedProj[i]) continue;
                    var p = Main.projectile[i];
                    if (p != null)
                    {
                        p.position = _savedProjPos[i];
                        p.rotation = _savedProjRot[i];
                    }
                    _touchedProj[i] = false;
                }
            }
            catch
            {
            }
        }
    }
}
