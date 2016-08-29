#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="LaneClear.cs" company="EloBuddy">
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

namespace Simple_Marksmans.Plugins.Tristana.Modes
{
    internal class LaneClear : Tristana
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            var laneMinions =
                EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position,
                    Player.Instance.GetAutoAttackRange()).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            if (Q.IsReady() && Settings.LaneClear.UseQInLaneClear && IsPreAttack && laneMinions.Count >= 3)
            {
                Q.Cast();
            }

            if (IsPreAttack && EntityManager.MinionsAndMonsters.EnemyMinions.Any(x => x.IsValidTarget(Player.Instance.GetAutoAttackRange()) && HasExplosiveChargeBuff(x)))
            {
                foreach (var enemy in EntityManager.MinionsAndMonsters.EnemyMinions.Where(x => x.IsValidTarget(Player.Instance.GetAutoAttackRange()) && HasExplosiveChargeBuff(x)))
                {
                    if (!EntityManager.MinionsAndMonsters.EnemyMinions.Any(
                        x =>
                            x.IsValidTarget(Player.Instance.GetAutoAttackRange()) &&
                            x.Health < Player.Instance.GetAutoAttackDamage(x, true) &&
                            x.NetworkId != enemy.NetworkId))
                    {
                        Console.WriteLine("[DEBUG] Changing orbwalker's forced target : {0}", "Minion ...");
                        Orbwalker.ForcedTarget = enemy;
                    }
                    else
                    {
                        Orbwalker.ForcedTarget = null;
                    }
                }
            }

            if (!E.IsReady() || Player.Instance.IsUnderHisturret() || !Settings.LaneClear.UseEInLaneClear ||
                !(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaE))
                return;

            var minion = laneMinions.OrderByDescending(x => x.CountEnemyMinionsInRange(200)).ToArray();

            if (minion.Any() && minion[0].CountEnemyMinionsInRange(200) >= 3)
            {
                E.Cast(minion[0]);
            }
        }
    }
}