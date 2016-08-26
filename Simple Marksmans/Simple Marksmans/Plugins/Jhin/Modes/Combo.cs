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

namespace Simple_Marksmans.Plugins.Jhin.Modes
{
    internal class Combo : Jhin
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ && Player.Instance.Mana - (30 + (Q.Level - 1)*5) > 100)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (target != null && !target.HasSpellShield() && !target.HasUndyingBuffA())
                {
                    Q.Cast(target);
                }
            }

            if (W.IsReady() && Settings.Combo.UseW && Player.Instance.Mana - (50 + (Q.Level - 1)*10) > 100 &&
                EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(W.Range) && HasSpottedBuff(x)) &&
                Player.Instance.CountEnemiesInRange(500) < 2)
            {
                foreach (var wPrediction in from wPrediction in EntityManager.Heroes.Enemies.Where(
                    x => x.IsValidTarget(W.Range) && HasSpottedBuff(x) && !x.HasUndyingBuffA())
                    .OrderBy(x => x.HealthPercent)
                    .ThenByDescending(x => x.Distance(Player.Instance))
                    .Select(target => W.GetPrediction(target))
                    .Where(wPrediction => wPrediction.HitChance >= HitChance.High && !wPrediction.GetCollisionObjects<AIHeroClient>().Any()) let count = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range))
                        .Select(enemy => Prediction.Position.PredictUnitPosition(enemy, 1000))
                        .Count(position => position.Distance(Player.Instance) < Player.Instance.GetAutoAttackRange()) where count < 2 select wPrediction)
                {
                    W.Cast(wPrediction.CastPosition);
                    break;
                }
            }

            if (!E.IsReady() || !Settings.Combo.UseE || Orbwalker.CanAutoAttack)
                return;

            foreach (var target in from target in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range))
                let duration = target.GetMovementBlockedDebuffDuration() * 1000
                where
                    !(duration <= 0)
                where duration > 400
                select target)
            {
                E.Cast(target.Position);
            }
        }
    }
}