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

namespace Simple_Marksmans.Plugins.KogMaw.Modes
{
    internal class Combo : KogMaw
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ && Player.Instance.Mana - 40 > 80)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                if (target != null && !target.HasSpellShield() && !target.HasUndyingBuffA())
                {
                    var qPrediction = Q.GetPrediction(target);

                    if (qPrediction.HitChancePercent > 50)
                        Q.Cast(qPrediction.CastPosition);
                }
            }

            if (W.IsReady() && Settings.Combo.UseW && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(W.Range)))
            {
                W.Cast();
            }

            if (E.IsReady() && Settings.Combo.UseE && Player.Instance.Mana - EMana[E.Level] > 80)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);

                if (target != null && !target.HasSpellShield() && !target.HasUndyingBuffA())
                {
                    var ePrediction = E.GetPrediction(target);

                    if (ePrediction.HitChancePercent > 80)
                        E.Cast(ePrediction.CastPosition);
                }
            }

            if (R.IsReady() && Settings.Combo.UseR && !Settings.Combo.UseROnlyToKs)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

                if (HasKogMawRBuff && GetKogMawRBuff.Count <= Settings.Combo.RAllowedStacks && (Player.Instance.Mana - (GetKogMawRBuff.Count + 1)*50 > 80))
                {
                    if (target != null && target.HealthPercent < Settings.Combo.RMaxHealth && !target.HasSpellShield() && !target.HasUndyingBuffA())
                    {
                        var rPrediction = R.GetPrediction(target);

                        if (rPrediction.HitChance >= HitChance.High)
                            R.Cast(rPrediction.CastPosition);
                    }
                } else if (!HasKogMawRBuff)
                {
                    if (target != null && target.HealthPercent < Settings.Combo.RMaxHealth && !target.HasSpellShield() && !target.HasUndyingBuffA())
                    {
                        var rPrediction = R.GetPrediction(target);

                        if (rPrediction.HitChance >= HitChance.High)
                            R.Cast(rPrediction.CastPosition);
                    }
                }
            }
        }
    }
}