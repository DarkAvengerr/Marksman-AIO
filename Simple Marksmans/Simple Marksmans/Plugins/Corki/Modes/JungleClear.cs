#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="JungleClear.cs" company="EloBuddy">
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
    internal class JungleClear : Corki
    {
        public static void Execute()
        {
            var jungleMinions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position,
                Player.Instance.GetAutoAttackRange() + 250);

            if (jungleMinions == null)
                return;

            var minions = jungleMinions as IList<Obj_AI_Minion> ?? jungleMinions.ToList();

            if (Q.IsReady() && Settings.JungleClear.UseQ &&
                Player.Instance.ManaPercent >= Settings.JungleClear.MinManaToUseQ && !HasSheenBuff)
            {
                var farmLoc = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(minions, 250, 825, 250, 1000);

                if (farmLoc.HitNumber >= 1)
                {
                    Q.Cast(farmLoc.CastPosition);
                }
            }

            if (E.IsReady() && Settings.JungleClear.UseE &&
                Player.Instance.ManaPercent >= Settings.JungleClear.MinManaToUseE && !HasSheenBuff)
            {
                if (minions.ToList().Any(x => x.Distance(Player.Instance) < 450))
                {
                    E.Cast();
                }
            }

            if (R.IsReady() && Settings.JungleClear.UseR &&
                Player.Instance.ManaPercent >= Settings.JungleClear.MinManaToUseR && Player.Instance.Spellbook.GetSpell(SpellSlot.R).Ammo >= Settings.JungleClear.MinStacksToUseR && !HasSheenBuff)
            {
                var target = minions.OrderBy(x => x.Distance(Player.Instance)).FirstOrDefault();

                if (target != null)
                {
                    var prediction = R.GetPrediction(target);

                    if (prediction.CollisionObjects != null && Settings.JungleClear.RAllowCollision)
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
