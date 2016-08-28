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

namespace Simple_Marksmans.Plugins.Urgot.Modes
{
    internal class Combo : Urgot
    {
        public static void Execute()
        {
            if (R.IsReady() && Settings.Combo.UseR && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(R.Range)) && Player.Instance.Mana > 300)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);

                if (target != null)
                {
                    var damage = Player.Instance.GetSpellDamage(target, SpellSlot.Q) * 2 + Player.Instance.GetAutoAttackDamage(target, true) * 2;
                    if (damage > target.Health && target.HealthPercent > 40 && target.Position.CountEnemiesInRange(600) < 2 && Player.Instance.HealthPercent > target.HealthPercent && !target.IsUnderTurret())
                    {
                        R.Cast(target);
                        return;
                    }
                    if (Player.Instance.IsUnderTurret() && Player.Instance.HealthPercent > 25 && Player.Instance.HealthPercent > target.HealthPercent)
                    {
                        R.Cast(target);
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

                    if (ePrediction.HitChance >= HitChance.High)
                    {
                        if (Player.Instance.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires - Game.Time < 1 || target.Health < Player.Instance.GetSpellDamage(target, SpellSlot.E))
                        {
                            E.Cast(ePrediction.CastPosition);
                            return;
                        }
                    }
                }
            }

            if (Q.IsReady() && Settings.Combo.UseQ)
            {
                if (CorrosiveDebufTargets.Any(unit => unit is AIHeroClient && unit.IsValidTarget(1300)))
                {
                    foreach (
                        var corrosiveDebufTarget in
                            CorrosiveDebufTargets.Where(unit => unit is AIHeroClient && unit.IsValidTarget(1300)))
                    {
                        Q.Range = 1300;
                        Q.AllowedCollisionCount = -1;
                        Q.Cast(corrosiveDebufTarget.Position);
                    }
                }
                else
                {
                    Q.Range = 900;
                    Q.AllowedCollisionCount = 0;
                    var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                    if (target != null)
                    {
                        var qPrediciton = Q.GetPrediction(target);
                        if (!qPrediciton.GetCollisionObjects<Obj_AI_Minion>().Any() && qPrediciton.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPrediciton.CastPosition);
                            return;
                        }
                    }
                }
            }

            if (!W.IsReady() || !Settings.Combo.UseW || !(Player.Instance.Mana - 50 + 5*(E.Level - 1) > 220))
                return;
            {
                if (Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange()) < 1 &&
                    !CorrosiveDebufTargets.Any(unit => unit is AIHeroClient && unit.IsValidTarget(1200)))
                    return;

                W.Cast();
            }
        }
    }
}