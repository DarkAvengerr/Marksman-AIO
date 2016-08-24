#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="LaneClear.cs" company="EloBuddy">
// 
//  Marksman AIO
// 
//  Copyright (C) 2016 Krystian Tenerowicz
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see http://www.gnu.org/licenses/. 
//  </copyright>
//  <summary>
// 
//  Email: geroelobuddy@gmail.com
//  PayPal: geroelobuddy@gmail.com
//  </summary>
//  --------------------------------------------------------------------------------------------------------------------
#endregion

using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Simple_Marksmans.Plugins.Corki.Modes
{
    internal class LaneClear : Corki
    {
        public static void Execute()
        {
            var laneMinions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                Player.Instance.Position,
                Player.Instance.GetAutoAttackRange() + 250);

            if (laneMinions == null &&
                !(!Settings.LaneClear.EnableIfNoEnemies ||
                  Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) >
                  Settings.LaneClear.AllowedEnemies))
                return;
            
            var minions = laneMinions as IList<Obj_AI_Minion> ?? laneMinions.ToList();

            if (Q.IsReady() && Settings.LaneClear.UseQ &&
                Player.Instance.ManaPercent >= Settings.LaneClear.MinManaToUseQ && !HasSheenBuff)
            {
                var farmLoc = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minions.Where(x => x.Health < Damage.GetSpellDamage(x, SpellSlot.Q)), 250, 825, 250, 1000);

                if (farmLoc.HitNumber >= Settings.LaneClear.MinMinionsKilledToUseQ)
                {
                    Q.Cast(farmLoc.CastPosition);
                }
            }

            if (E.IsReady() && Settings.LaneClear.UseE &&
                Player.Instance.ManaPercent >= Settings.LaneClear.MinManaToUseE && !HasSheenBuff)
            {
                if (minions.ToList().Any(x => x.Distance(Player.Instance) < 450))
                {
                    E.Cast();
                }
            }

            if (R.IsReady() && Settings.LaneClear.UseR &&
                Player.Instance.ManaPercent >= Settings.LaneClear.MinManaToUseR && Player.Instance.Spellbook.GetSpell(SpellSlot.R).Ammo >= Settings.LaneClear.MinStacksToUseR && !HasSheenBuff)
            {
                var target = minions.OrderBy(x => x.Distance(Player.Instance)).FirstOrDefault();

                if (target != null)
                {
                    var prediction = R.GetPrediction(target);

                    if (prediction.CollisionObjects != null && Settings.LaneClear.RAllowCollision)
                    {
                        var first =
                            prediction.CollisionObjects.OrderBy(x => x.Distance(Player.Instance))
                                .FirstOrDefault();

                        if (first != null)
                        {
                            var enemy =
                                GetCollisionObjects<Obj_AI_Minion>(first)
                                    .FirstOrDefault(x => x.NetworkId == target.NetworkId);
                            if (enemy != null)
                            {
                                R.Cast(first);
                            }
                        }
                    }
                    else if (target.HealthPercent <= 50
                        ? prediction.HitChancePercent >= 50
                        : prediction.HitChancePercent >= 80)
                    {
                        R.Cast(prediction.CastPosition);
                    }
                }
            }
        }
    }
}