#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Harass.cs" company="EloBuddy">
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Corki.Modes
{
    internal class Harass : Corki
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Harass.UseQ && Player.Instance.ManaPercent >= Settings.Harass.MinManaToUseQ && !HasSheenBuff)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                if (target != null && !target.HasUndyingBuffA() && !target.HasSpellShield())
                {
                    var prediction = Q.GetPrediction(target);

                    if (prediction.HitChancePercent >= 70)
                    {
                        Q.Cast(prediction.CastPosition);
                    }
                }
            }

            if (E.IsReady() && Settings.Harass.UseE && Player.Instance.ManaPercent >= Settings.Harass.MinManaToUseE && !HasSheenBuff)
            {
                var target = TargetSelector.GetTarget(650, DamageType.Magical);

                if (target != null && !target.HasUndyingBuffA() && target.Distance(Player.Instance) < 500)
                {
                    E.Cast();
                }
            }

            if (R.IsReady() && Settings.Harass.UseR && Player.Instance.ManaPercent >= Settings.Harass.MinManaToUseR && Player.Instance.Spellbook.GetSpell(SpellSlot.R).Ammo >= Settings.Harass.MinStacksToUseR && !HasSheenBuff)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

                if (target != null && !target.HasUndyingBuffA() && !target.HasSpellShield())
                {
                    var prediction = R.GetPrediction(target);

                    if (prediction.CollisionObjects != null && Settings.Harass.RAllowCollision)
                    {
                        var first =
                            prediction.CollisionObjects.OrderBy(x => x.Distance(Player.Instance))
                                .FirstOrDefault();

                        if (first != null)
                        {
                            var enemy = GetCollisionObjects<AIHeroClient>(first).FirstOrDefault(x => x.NetworkId == target.NetworkId);
                            if (enemy != null)
                            {
                                R.Cast(first);
                            }
                        }
                    }
                    else if (prediction.HitChancePercent >= 85)
                    {
                        R.Cast(prediction.CastPosition);
                    }
                }
            }
        }
    }
}
