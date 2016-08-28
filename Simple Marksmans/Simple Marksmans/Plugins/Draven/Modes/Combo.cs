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
using EloBuddy.SDK.Spells;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Draven.Modes
{
    internal class Combo : Draven
    {
        public static void Execute()
        {
            if (DravenRMissile != null && Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name.ToLowerInvariant() == "dravenrdoublecast" && R.IsReady())
            {
                var pos = Player.Instance.Position.Extend(DravenRMissile.Position,
                    DravenRMissile.Distance(Player.Instance));
                var rectangle = new Geometry.Polygon.Rectangle(Player.Instance.Position.To2D(), pos, 160);
                var entitiesInside =
                    EntityManager.Heroes.Enemies.Where(x => x.IsValid() && rectangle.IsInside(x)).ToList();

                if (entitiesInside.Count == 0)
                    return;

                var cloesestEnemy = entitiesInside.OrderBy(x => x.Distance(DravenRMissile.Position)).First();

                if (cloesestEnemy.TotalHealthWithShields() < Player.Instance.GetSpellDamage(cloesestEnemy, SpellSlot.R))
                {
                    var posAfter = Prediction.Position.PredictUnitPosition(cloesestEnemy, 1000 + (int)(cloesestEnemy.Distance(DravenRMissile.Position) / 1900) * 1000); // r return delay

                    if (rectangle.IsInside(posAfter))
                    {
                        R.Cast();
                        Console.WriteLine("[DEBUG] hehe xd");
                    }
                }
            }

            if (E.IsReady() && Settings.Combo.UseE && Player.Instance.Mana - 70 > 145)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (target != null)
                {
                    var ePrediction = E.GetPrediction(target);

                    if (ePrediction.HitChance == HitChance.High)
                    {
                        E.Cast(ePrediction.CastPosition);
                        return;
                    }
                }
            }
            if (!R.IsReady() || !Settings.Combo.UseR)
                return;


            if (Player.Instance.CountEnemiesInRange(900) == 1)
            {
                var target = TargetSelector.GetTarget(900, DamageType.Physical);
                if (target != null && Player.Instance.HealthPercent > target.HealthPercent && !target.HasUndyingBuffA() &&
                    target.TotalHealthWithShields() <
                    Player.Instance.GetSpellDamage(target, SpellSlot.R) +
                     Player.Instance.GetAutoAttackDamage(target, true)*3 && target.TotalHealthWithShields() > Player.Instance.GetAutoAttackDamage(target, true) * 3)
                {
                    var rPrediction = R.GetPrediction(target);
                    if (rPrediction.HitChance == HitChance.High)
                    {
                        R.Cast(rPrediction.CastPosition);
                        return;
                    }
                }
            }

            if (Player.Instance.CountEnemiesInRange(1500) > Player.Instance.CountAlliesInRange(1500))
                return;


            foreach (var rPrediction in EntityManager.Heroes.Enemies.Where(unit => unit.IsValidTarget(1500)).Select(enemy => Prediction.Manager.GetPrediction(new Prediction.Manager.PredictionInput
            {
                CollisionTypes = {CollisionType.AiHeroClient},
                Delay = 250,
                From = Player.Instance.Position,
                Range = 1500,
                Radius = 160,
                RangeCheckFrom = Player.Instance.Position,
                Speed = 1900,
                Target = enemy,
                Type = SkillShotType.Linear
            })).Where(rPrediction => rPrediction.RealHitChancePercent >= 60).Where(rPrediction => rPrediction.GetCollisionObjects<AIHeroClient>().Length >= 2))
            {
                R.Cast(rPrediction.CastPosition);
                return;
            }
        }
    }
}