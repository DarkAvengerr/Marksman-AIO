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

namespace Simple_Marksmans.Plugins.Sivir.Modes
{
    internal class PermaActive : Sivir
    {
        public static void Execute()
        {
            if (!IsPostAttack && Q.IsReady() && Settings.Harass.AutoHarass)
            {
                foreach (var immobileEnemy in EntityManager.Heroes.Enemies.Where(x =>
                {
                    if (!x.IsValidTarget(Q.Range) || !x.IsImmobile())
                        return false;

                    var immobileDuration = x.GetMovementBlockedDebuffDuration();
                    var eta = x.Distance(Player.Instance)/Player.Instance.Spellbook.GetSpell(SpellSlot.Q).SData.MissileSpeed;

                    return immobileDuration > eta;

                }).OrderByDescending(TargetSelector.GetPriority).ThenBy(x=> Q.GetPrediction(x).HitChancePercent))
                {
                    Console.WriteLine("[DEBUG] Casting Q on immobile target {0}", immobileEnemy.Hero);
                    Q.Cast(Q.GetPrediction(immobileEnemy).CastPosition);
                }
            }

            if (!IsPostAttack && Q.IsReady() && Settings.Combo.UseQ)
            {
                foreach (var immobileEnemy in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(Q.Range) && (x.Health - IncomingDamage.GetIncomingDamage(x)) < Player.Instance.GetSpellDamage(x, SpellSlot.Q)).OrderByDescending(TargetSelector.GetPriority).ThenBy(x => Q.GetPrediction(x).HitChancePercent))
                {
                    Console.WriteLine("[DEBUG] Casting Q on {0} to killsteal", immobileEnemy.Hero);
                    Q.Cast(Q.GetPrediction(immobileEnemy).CastPosition);
                }
            }
        }
    }
}