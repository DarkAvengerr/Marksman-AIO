#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Combo.cs" company="EloBuddy">
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

    internal class Combo : Kalista
    {
        public static void Execute()
        {
            if (Settings.Combo.JumpOnMinions && Orbwalker.CanAutoAttack)
            {
                var target = TargetSelector.GetTarget(1500, DamageType.Physical);

                if (target != null && !Player.Instance.IsInAutoAttackRange(target))
                {
                    Orbwalker.ForcedTarget =
                        EntityManager.MinionsAndMonsters.CombinedAttackable.FirstOrDefault(
                            unit => Player.Instance.IsInRange(unit, Player.Instance.GetAutoAttackRange()));
                }
            }

            if (E.IsReady() && Settings.Combo.UseE)
            {
                var enemiesWithRendBuff =
                    EntityManager.Heroes.Enemies.Count(
                        unit => unit.IsValid && unit.IsValidTarget(E.Range) && unit.HasRendBuff());

                if(enemiesWithRendBuff == 0)
                    return;
                
                if (Settings.Combo.UseEToSlow)
                {
                    var count =
                        EntityManager.MinionsAndMonsters.CombinedAttackable.Count(
                            unit => unit.IsValid && unit.IsValidTarget(E.Range) && unit.IsTargetKillableByRend());

                    if (count >= Settings.Combo.UseEToSlowMinMinions)
                    {
                        Console.WriteLine("[DEBUG] Casting E to slow.");
                        E.Cast();
                    }
                }

                if (Settings.Combo.UseEBeforeEnemyLeavesRange && enemiesWithRendBuff == 1)
                {
                    var enemyUnit =
                        EntityManager.Heroes.Enemies.Find(unit => !unit.IsDead && unit.IsValid && unit.IsValidTarget(E.Range) && unit.HasRendBuff());

                    if (enemyUnit != null && enemyUnit.CanCastEOnUnit() && enemyUnit.Distance(Player.Instance) > E.Range - 100)
                    {
                        var percentDamage = enemyUnit.GetRendDamageOnTarget()/enemyUnit.TotalHealthWithShields()*100;
                        if (percentDamage >= Settings.Combo.MinDamagePercToUseEBeforeEnemyLeavesRange)
                        {
                            E.Cast();
                            Console.WriteLine("[DEBUG] Casting E cause it will deal "+percentDamage+" percent of enemy hp.");
                        }
                    }
                }

                if (Settings.Combo.UseEBeforeDeath && Player.Instance.HealthPercent < 5 && IncomingDamage.GetIncomingDamage(Player.Instance) > Player.Instance.Health)
                {
                    E.Cast();
                    Console.WriteLine("[DEBUG] Casting E before death.");
                }
            }
        }
    }
}