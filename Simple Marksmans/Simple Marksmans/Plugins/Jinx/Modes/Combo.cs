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
using EloBuddy.SDK.Enumerations;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Jinx.Modes
{
    internal class Combo : Jinx
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ)
            {
                var target = TargetSelector.GetTarget(GetRealRocketLauncherRange(), DamageType.Physical);

                if (target != null)
                {
                    if (target.Distance(Player.Instance) < GetRealMinigunRange() && HasRocketLauncher &&
                        target.TotalHealthWithShields() > Player.Instance.GetAutoAttackDamage(target, true)*2.2f)
                    {
                        Q.Cast();
                        return;
                    }

                    if (target.Distance(Player.Instance) > GetRealMinigunRange() &&
                        target.Distance(Player.Instance) < GetRealRocketLauncherRange() && !HasRocketLauncher)
                    {
                        Q.Cast();
                        return;
                    }
                    if (HasMinigun &&  GetMinigunStacks >= 2 &&
                        target.TotalHealthWithShields() < Player.Instance.GetAutoAttackDamage(target, true)*2.2f)
                    {
                        Q.Cast();
                        return;
                    }
                }
            }

            if (W.IsReady() && Settings.Combo.UseW && !Player.Instance.Position.IsVectorUnderEnemyTower())
            {
                var target =
                    EntityManager.Heroes.Enemies.Where(
                        x =>
                            x.IsValidTarget(W.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield() &&
                            x.Distance(Player.Instance) > Settings.Combo.WMinDistanceToTarget)
                        .OrderByDescending(x => Player.Instance.GetSpellDamage(x, SpellSlot.W)).FirstOrDefault();

                if (target != null)
                {
                    var wPrediction = W.GetPrediction(target);
                    if (wPrediction.HitChance == HitChance.High)
                    {
                        W.Cast(wPrediction.CastPosition);
                        return;
                    }
                }
            }

            if (E.IsReady() && Settings.Combo.UseE)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

                if (target != null)
                {
                    var ePrediction = E.GetPrediction(target);
                    if (ePrediction.HitChance == HitChance.High && ePrediction.CastPosition.Distance(target) > 150)
                    {
                        E.Cast(ePrediction.CastPosition);
                        return;
                    }
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR || Player.Instance.Position.IsVectorUnderEnemyTower())
                return;

            var t = TargetSelector.GetTarget(3000, DamageType.Physical);

            if (t == null || t.HasUndyingBuffA() || !(t.Distance(Player.Instance) > GetRealRocketLauncherRange() + 100))
                return;

            var health = t.TotalHealthWithShields() - IncomingDamage.GetIncomingDamage(t);

            if (health > 0 && health < Damage.GetRDamage(t))
            {
                var rPrediction = R.GetPrediction(t);

                if (rPrediction.HitChance != HitChance.High)
                    return;

                R.Cast(rPrediction.CastPosition);
                Console.WriteLine("KS ULT");
            }
            else
            {
                var rPrediction = R.GetPrediction(t);

                if (t.CountEnemiesInRange(225) < 5 || rPrediction.HitChance != HitChance.High)
                    return;

                R.Cast(rPrediction.CastPosition);
                Console.WriteLine("AOE ULT");
            }
        }
    }
}