#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="PermaActive.cs" company="EloBuddy">
// // 
// //  Marksman AIO
// // 
// //  Copyright (C) 2016 Krystian Tenerowicz
// // 
// //  This program is free software: you can redistribute it and/or modify
// //  it under the terms of the GNU General Public License as published by
// //  the Free Software Foundation, either version 3 of the License, or
// //  (at your option) any later version.
// // 
// //  This program is distributed in the hope that it will be useful,
// //  but WITHOUT ANY WARRANTY; without even the implied warranty of
// //  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// //  GNU General Public License for more details.
// // 
// //  You should have received a copy of the GNU General Public License
// //  along with this program.  If not, see http://www.gnu.org/licenses/. 
// //  </copyright>
// //  <summary>
// // 
// //  Email: geroelobuddy@gmail.com
// //  PayPal: geroelobuddy@gmail.com
// //  </summary>
// //  ---------------------------------------------------------------------
#endregion
using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Kalista.Modes
{
    internal class PermaActive : Kalista
    {
        public static void Execute()
        {
            if (Player.Instance.IsDead)
                return;

            Orbwalker.ForcedTarget = null;

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (target != null && !target.IsDead && Q.IsReady() && Settings.Combo.UseQ && !target.HasSpellShield() &&
                !target.HasUndyingBuffA() && Player.Instance.GetSpellDamage(target, SpellSlot.Q) >= target.TotalHealthWithShields())
            {
                Q.Cast(Q.GetPrediction(target).CastPosition);
                Console.WriteLine("[DEBUG] Casting Q to ks");
            }
            if (E.IsReady() && Settings.Combo.UseE)
            {
                if(EntityManager.Heroes.Enemies.Any(unit => unit.IsValid && !unit.IsDead && unit.IsValidTarget(E.Range) && unit.IsTargetKillableByRend()))
                {
                    E.Cast();
                    Console.WriteLine("[DEBUG] Casting E to ks");
                }
            }

            if (E.IsReady() && (Settings.JungleLaneClear.UseEToStealBuffs || Settings.JungleLaneClear.UseEToStealDragon))
            {
                if (Settings.JungleLaneClear.UseEToStealDragon)
                {
                    if(EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, E.Range).Any(unit => (unit.BaseSkinName.Contains("Baron") || unit.BaseSkinName.Contains("Dragon") || unit.BaseSkinName.Contains("RiftHerald")) && unit.IsTargetKillableByRend()))
                    {
                        Console.WriteLine("[DEBUG] Casting E to ks baron");
                        E.Cast();
                    }
                }

                if (Settings.JungleLaneClear.UseEToStealBuffs)
                {
                    if (EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, E.Range).Any(unit => unit.IsValidTarget(E.Range) && (unit.BaseSkinName.Contains("Blue") || unit.BaseSkinName.Contains("Red")) && !unit.BaseSkinName.Contains("Mini") && unit.IsTargetKillableByRend()))
                    {
                        Console.WriteLine("[DEBUG] Casting E to ks blue ["+Game.Time+"]");
                        E.Cast();
                    }
                }
            }
        }
    }
}