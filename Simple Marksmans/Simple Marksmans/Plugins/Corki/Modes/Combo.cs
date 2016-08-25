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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Corki.Modes
{
    internal class Combo : Corki
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ && !HasSheenBuff)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                if (target != null && !target.HasUndyingBuffA() && !target.HasSpellShield())
                {
                    var prediction = Q.GetPrediction(target);

                    if (prediction.HitChancePercent >= 80)
                    {
                        Q.Cast(prediction.CastPosition);
                    }
                }
            }

            if (W.IsReady() && Settings.Combo.UseW && !HasPackagesBuff && Player.Instance.CountEnemiesInRange(1500) == 1 && Player.Instance.Mana > QMana[Q.Level] + WMana + EMana + RMana)
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);

                if (target != null && !target.IsUnderHisturret() && target.HealthPercent < Player.Instance.HealthPercent && target.Health < Damage.GetComboDamage(target, 2, 2))
                {
                    W.Cast(Player.Instance.Position.Extend(target, 580).To3D());
                    Misc.PrintInfoMessage("Engaging on <font color=\"#ff1493\">"+target.Hero+"</font> because he can be killed from combo.");
                }
            }

            if (E.IsReady() && Settings.Combo.UseE && !HasSheenBuff)
            {
                var target = TargetSelector.GetTarget(650, DamageType.Mixed);

                if (target != null && !target.HasUndyingBuffA() && target.Distance(Player.Instance) < 500)
                {
                    E.Cast();
                }
            }

            if (R.IsReady() && Settings.Combo.UseR && Player.Instance.Spellbook.GetSpell(SpellSlot.R).Ammo >= Settings.Combo.MinStacksForR && !HasSheenBuff)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

                if (target != null && !target.HasUndyingBuffA() && !target.HasSpellShield())
                {
                    var prediction = R.GetPrediction(target);

                    if (prediction.Collision && prediction.CollisionObjects != null && Settings.Combo.RAllowCollision)
                    {
                        var first =
                            prediction.CollisionObjects.OrderBy(x => x.Distance(Player.Instance))
                                .FirstOrDefault();

                        if (first != null)
                        {
                            var enemy = GetCollisionObjects<Obj_AI_Base>(first).FirstOrDefault(x=>x.NetworkId == target.NetworkId);
                            if (enemy != null)
                            {
                                R.Cast(first);
                            }
                        }
                    } else if (target.HealthPercent <= 50 ? prediction.HitChancePercent >= 25 : prediction.HitChancePercent >= 40)
                    {
                        R.Cast(prediction.CastPosition);
                    }
                }
            }
        }
    }
}