using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace HighFpsCursor
{
    [HarmonyPatch]
    public static class Patch_CaptureTick_AllEntities
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
                long now = Stopwatch.GetTimestamp();

                if (!RenderInterp.HasTick)
                {
                    RenderInterp.LastTickStamp = now;
                    RenderInterp.TickDeltaStamp = Stopwatch.Frequency / 60;
                    RenderInterp.HasTick = true;
                }
                else
                {
                    long dt = now - RenderInterp.LastTickStamp;
                    if (dt > 0) RenderInterp.TickDeltaStamp = dt;
                    RenderInterp.LastTickStamp = now;
                }

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    var n = Main.npc[i];
                    if (n == null || !n.active) { RenderInterp.NPC[i].Has = false; continue; }

                    ref var f = ref RenderInterp.NPC[i];
                    if (!f.Has)
                    {
                        f.PrevPos = f.CurrPos = n.position;
                        f.PrevRot = f.CurrRot = n.rotation;
                        f.Has = true;
                    }
                    else
                    {
                        f.PrevPos = f.CurrPos;
                        f.CurrPos = n.position;
                        f.PrevRot = f.CurrRot;
                        f.CurrRot = n.rotation;
                    }
                }

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    var p = Main.projectile[i];
                    if (p == null || !p.active) { RenderInterp.Proj[i].Has = false; continue; }

                    ref var f = ref RenderInterp.Proj[i];
                    if (!f.Has)
                    {
                        f.PrevPos = f.CurrPos = p.position;
                        f.PrevRot = f.CurrRot = p.rotation;
                        f.Has = true;
                    }
                    else
                    {
                        f.PrevPos = f.CurrPos;
                        f.CurrPos = p.position;
                        f.PrevRot = f.CurrRot;
                        f.CurrRot = p.rotation;
                    }
                }
            }
            catch
            {
            }
        }
    }
}
