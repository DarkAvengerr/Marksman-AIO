#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="LaneClear.cs" company="EloBuddy">
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
    internal class LaneClear : Urgot
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            var laneMinions =
                EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position,
                    1200).ToList();

            if (!laneMinions.Any())
            {
                return;
            }
            if (Q.IsReady() && Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ)
            {
                if (Settings.Misc.AutoHarass && Settings.Combo.UseQ && CorrosiveDebufTargets.Any(unit => unit is AIHeroClient && unit.IsValidTarget(1300)))
                {
                    foreach (
                        var corrosiveDebufTarget in
                            CorrosiveDebufTargets.Where(unit => unit is AIHeroClient && unit.IsValidTarget(1300)))
                    {
                        Q.Range = 1300;
                        Q.AllowedCollisionCount = -1;
                        Q.Cast(corrosiveDebufTarget.Position);
                        break;
                    }
                }
                else if (CanILaneClear() && Settings.LaneClear.UseQInLaneClear && CorrosiveDebufTargets.Any(unit => unit is Obj_AI_Minion && unit.IsValidTarget(1300)))
                {
                    if (CorrosiveDebufTargets.Any(unit => unit is Obj_AI_Minion && unit.IsValidTarget(1300)))
                    {
                        foreach (
                            var minion in
                                from minion in
                                    CorrosiveDebufTargets.Where(
                                        unit => unit is Obj_AI_Minion && unit.IsValidTarget(1300))
                                let hpPrediction = Prediction.Health.GetPrediction(minion,
                                    (int) (minion.Distance(Player.Instance)/1550*1000 + 250))
                                where
                                    hpPrediction > 0 &&
                                    hpPrediction < Player.Instance.GetSpellDamage(minion, SpellSlot.Q)
                                select minion)
                        {
                            Q.Cast(minion.Position);
                            break;
                        }
                    }
                }
                else if (CanILaneClear() && Settings.LaneClear.UseQInLaneClear)
                {
                    foreach (var minion in (from minion in laneMinions let hpPrediction = Prediction.Health.GetPrediction(minion,
                        (int) (minion.Distance(Player.Instance)/1550*1000 + 250)) where hpPrediction > 0 &&
                                                                                        hpPrediction < Player.Instance.GetSpellDamage(minion, SpellSlot.Q) let qPrediction = Q.GetPrediction(minion) where qPrediction.Collision == false select minion).Where(minion => !minion.IsDead))
                    {
                        Q.Cast(minion);
                    }
                }
            }

            if (!E.IsReady() || !(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaE))
                return;

            if (Settings.Combo.UseE && Settings.Misc.AutoHarass && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(E.Range)))
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
                        }
                    }
                } 
            } else if (CanILaneClear() && Settings.LaneClear.UseEInLaneClear && Player.Instance.CountEnemyMinionsInRange(900) > 3)
            {
                var farmPosition =
                    EntityManager.MinionsAndMonsters.GetCircularFarmLocation(
                        EntityManager.MinionsAndMonsters.EnemyMinions.Where(
                            x => x.IsValidTarget(E.Range) && x.HealthPercent > 10), 250, 900, 250, 1550);

                if (farmPosition.HitNumber > 2)
                {
                    E.Cast(farmPosition.CastPosition);
                }
            }
        }
    }
}