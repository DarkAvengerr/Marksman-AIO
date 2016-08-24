#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Harass.cs" company="EloBuddy">
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

using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Kalista.Modes
{
    internal class Harass : Kalista
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Harass.UseQ && !Player.Instance.IsDashing() &&
                Player.Instance.ManaPercent >= Settings.Harass.MinManaForQ)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (target != null)
                {
                    if (!target.HasSpellShield() && !target.HasUndyingBuffA())
                    {
                        var pred = Q.GetPrediction(target);
                        if (pred.HitChancePercent > 85 && pred.CollisionObjects.Length == 0)
                        {
                            Q.Cast(pred.CastPosition);
                        }
                    }
                }
            }

            if (!E.IsReady() || !Settings.Harass.UseE || !(Player.Instance.ManaPercent >= Settings.Harass.MinManaForE))
                return;

            var enemy =
                EntityManager.Heroes.Enemies.FirstOrDefault(x => x.IsValidTarget(E.Range) && x.HasRendBuff() &&
                                                                 x.GetRendBuff().Count > Settings.Harass.MinStacksForE);

            if (enemy == null)
                return;


            if (Settings.Harass.UseEIfManaWillBeRestored &&
                EntityManager.MinionsAndMonsters.CombinedAttackable.Count(
                    x => x.IsValidTarget(E.Range) && x.IsTargetKillableByRend()) >= 2)
            {
                E.Cast();
            }
            else
            {
                E.Cast();
            }
        }
    }
}
