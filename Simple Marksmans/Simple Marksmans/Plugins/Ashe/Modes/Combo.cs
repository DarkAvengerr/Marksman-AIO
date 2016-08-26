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
using EloBuddy.SDK.Enumerations;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Ashe.Modes
{
    internal class Combo : Ashe
    {
        public static void Execute()
        {
            if (Q.IsReady() && IsPreAttack && Settings.Combo.UseQ)
            {
                var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange(), DamageType.Physical);

                if (target != null)
                {
                    if (EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(Player.Instance.GetAutoAttackRange() - 50)))
                    {
                        Q.Cast();
                    }
                }
            }

            if (W.IsReady() && Settings.Combo.UseW && Player.Instance.Mana - 50 > 100)
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);

                if (target != null)
                {
                    var wPrediction = GetWPrediction(target);

                    if (wPrediction != null && wPrediction.HitChance >= HitChance.High)
                    {
                        W.Cast(wPrediction.CastPosition);
                    }
                }
            }

            if (E.IsReady() && Settings.Combo.UseE)
            {
                foreach (var source in EntityManager.Heroes.Enemies.Where(x=> x.IsUserInvisibleFor(500)))
                {
                    var data = source.GetVisibilityTrackerData();

                    if (data.LastHealthPercent < 25 && data.LastPosition.Distance(Player.Instance) < 1000)
                    {
                        E.Cast(data.LastPath);
                    }
                }
            }

            if (R.IsReady() && Settings.Combo.UseR)
            {
                var target = TargetSelector.GetTarget(Settings.Combo.RMaximumRange, DamageType.Physical);

                if (target != null && !target.IsUnderTurret() && !target.HasSpellShield() && !target.HasUndyingBuffA() && target.Distance(Player.Instance) > Settings.Combo.RMinimumRange && target.Health - IncomingDamage.GetIncomingDamage(target) > 100)
                {
                    var damage = 0f;

                    if (Player.Instance.Mana > 200 && target.IsValidTarget(W.Range))
                    {
                        damage = Player.Instance.GetSpellDamage(target, SpellSlot.R) +
                                 Player.Instance.GetSpellDamage(target, SpellSlot.W) +
                                 Player.Instance.GetAutoAttackDamage(target)*4;
                    }
                    else if (Player.Instance.Mana > 150 && target.IsValidTarget(W.Range))
                        damage = Player.Instance.GetSpellDamage(target, SpellSlot.R) +
                                 Player.Instance.GetAutoAttackDamage(target)*4;

                    var rPrediction = R.GetPrediction(target);

                    if (damage > target.TotalHealthWithShields() && (rPrediction.HitChance >= HitChance.High))
                    {
                        R.Cast(rPrediction.CastPosition);
                    }
                }
            }
        }
    }
}