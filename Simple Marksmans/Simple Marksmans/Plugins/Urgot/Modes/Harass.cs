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

using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

namespace Simple_Marksmans.Plugins.Urgot.Modes
{
    internal class Harass : Urgot
    {
        public static void Execute()
        {
            if (E.IsReady() && Settings.Harass.UseE && Player.Instance.ManaPercent >= Settings.Harass.MinManaQ)
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

            if (!Q.IsReady() || !Settings.Harass.UseQ || !(Player.Instance.ManaPercent >= Settings.Harass.MinManaQ))
                return;

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
                if (target == null)
                    return;

                var qPrediciton = Q.GetPrediction(target);
                if (qPrediciton.GetCollisionObjects<Obj_AI_Minion>().Any() || qPrediciton.HitChance < HitChance.High)
                    return;

                Q.Cast(qPrediciton.CastPosition);
            }
        }
    }
}