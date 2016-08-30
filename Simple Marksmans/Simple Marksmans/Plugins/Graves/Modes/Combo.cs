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
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Spells;
using SharpDX;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Graves.Modes
{
    internal class Combo : Graves
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (target != null)
                {
                    var qPrediction = Q.GetPrediction(target);

                    if (qPrediction.HitChancePercent >= 75)
                    {
                        var distToPlayer =
                            Player.Instance.Position.Extend(qPrediction.CastPosition, 1000)
                                .CutVectorNearWall(1000)
                                .Distance(Player.Instance);

                        if (target.Distance(Player.Instance) < distToPlayer)
                        {
                            Q.Cast(qPrediction.CastPosition);
                            Console.WriteLine("[DEBUG] Q cast on {0}", target.ChampionName);
                        }
                        else if(!Player.Instance.Position.IsWallBetween(qPrediction.CastPosition))
                        {
                            Q.Cast(qPrediction.CastPosition);
                            Console.WriteLine("[DEBUG] Q cast on {0} v2", target.ChampionName);
                        }
                    }
                }
            }

            if (W.IsReady() && Settings.Combo.UseW && Player.Instance.Mana - WMana[W.Level] > EMana + RMana)
            {
                var immobileEnemies = EntityManager.Heroes.Enemies.Where(
                    x =>
                        x.IsValidTarget(W.Range) &&
                        (x.GetMovementBlockedDebuffDuration() > 0.5f ||
                         x.Buffs.Any(
                             m =>
                                 m.Name.ToLowerInvariant() == "zhonyasringshield" ||
                                 m.Name.ToLowerInvariant() == "bardrstasis"))).ToList();

                if(immobileEnemies.Any())
                {
                    foreach (var enemy in immobileEnemies)
                    {
                        if (enemy.Buffs.Any(m => m.Name.ToLowerInvariant() == "zhonyasringshield" ||
                                                 m.Name.ToLowerInvariant() == "bardrstasis"))
                        {
                            var buffTime =
                                enemy.Buffs.FirstOrDefault(m => m.Name.ToLowerInvariant() == "zhonyasringshield" ||
                                                                m.Name.ToLowerInvariant() == "bardrstasis");
                            if (buffTime != null && buffTime.EndTime - Game.Time < 1 &&
                                buffTime.EndTime - Game.Time > 0.3 &&
                                enemy.IsValidTarget(W.Range))
                            {
                                W.Cast(enemy.ServerPosition);
                            }
                        }
                        else if (enemy.IsValidTarget(W.Range))
                        {
                            W.Cast(enemy.ServerPosition);
                        }
                    }
                }
                else
                {
                    W.CastIfItWillHit();

                    var wTarget = TargetSelector.GetTarget(W.Range, DamageType.Physical);

                    if (wTarget != null && W.IsReady())
                    {
                        var wPrediction = W.GetPrediction(wTarget);

                        if (wPrediction.HitChancePercent >= 80)
                        {
                            W.Cast(wPrediction.CastPosition);
                        }
                    }
                }
            }

            if (E.IsReady() && Settings.Combo.UseE && Settings.Misc.EUsageMode == 0 && GetAmmoCount < 2)
            {
                var heroClient = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 425,
                    DamageType.Physical);
                var position = Vector3.Zero;

                if (Settings.Misc.EMode == 0)
                {
                    if (heroClient != null && Player.Instance.HealthPercent > 50 && heroClient.HealthPercent < 30)
                    {
                        if (!Player.Instance.Position.Extend(Game.CursorPos, 420)
                            .To3D()
                            .IsVectorUnderEnemyTower() &&
                            (!heroClient.IsMelee ||
                             Player.Instance.Position.Extend(Game.CursorPos, 420)
                                 .IsInRange(heroClient, heroClient.GetAutoAttackRange() * 1.5f)))
                        {
                            Console.WriteLine("[DEBUG] 1v1 Game.CursorPos");
                            position = Game.CursorPos.Distance(Player.Instance) > E.Range
                                ? Player.Instance.Position.Extend(Game.CursorPos, 420).To3D()
                                : Game.CursorPos;
                        }
                    }
                    else if (heroClient != null)
                    {
                        var closest =
                            EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(1300))
                                .OrderBy(x => x.Distance(Player.Instance)).ToArray()[0];

                        var list =
                            SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 420,
                                1300,
                                heroClient.IsMelee ? heroClient.GetAutoAttackRange() * 2 : heroClient.GetAutoAttackRange())
                                .Where(
                                    x =>
                                        !x.Key.To3D().IsVectorUnderEnemyTower() &&
                                        x.Key.IsInRange(Prediction.Position.PredictUnitPosition(closest, 850),
                                            Player.Instance.GetAutoAttackRange() - 50))
                                .Select(source => source.Key)
                                .ToList();

                        if (list.Any())
                        {
                            var paths =
                                EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(1300))
                                    .Select(x => x.Path)
                                    .Count(result => result != null && result.Last().Distance(Player.Instance) < 300);

                            var asc = Misc.SortVectorsByDistance(list, heroClient.Position.To2D())[0].To3D();
                            if (Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange()) == 0 &&
                                !EntityManager.Heroes.Enemies.Where(x => x.Distance(Player.Instance) < 1000).Any(
                                    x => Prediction.Position.PredictUnitPosition(x, 800)
                                        .IsInRange(asc,
                                            x.IsMelee ? x.GetAutoAttackRange() * 2 : x.GetAutoAttackRange())))
                            {
                                position = asc;

                                Console.WriteLine("[DEBUG] Paths low sorting Ascending");
                            }
                            else if (Player.Instance.CountEnemiesInRange(1000) <= 2 && (paths == 0 || paths == 1) &&
                                     ((closest.Health < Player.Instance.GetAutoAttackDamage(closest, true) * 2) ||
                                      (Orbwalker.LastTarget is AIHeroClient &&
                                       Orbwalker.LastTarget.Health <
                                       Player.Instance.GetAutoAttackDamage(closest, true) * 2)))
                            {
                                position = asc;
                            }
                            else
                            {
                                position =
                                    Misc.SortVectorsByDistanceDescending(list, heroClient.Position.To2D())[0].To3D();
                                Console.WriteLine("[DEBUG] Paths high sorting Descending");
                            }
                        }
                        else Console.WriteLine("[DEBUG] 1v1 not found positions...");
                    }

                    if (position != Vector3.Zero && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(900)))
                    {
                        E.Cast(position);
                        return;
                    }
                }
                else if (Settings.Misc.EMode == 1)
                {
                    var enemies = Player.Instance.CountEnemiesInRange(1300);
                    var pos = Game.CursorPos.Distance(Player.Instance) > E.Range
                        ? Player.Instance.Position.Extend(Game.CursorPos, 420).To3D()
                        : Game.CursorPos;

                    if (!pos.IsVectorUnderEnemyTower())
                    {
                        if (heroClient != null)
                        {
                            if (enemies == 1 && heroClient.HealthPercent + 15 < Player.Instance.HealthPercent)
                            {
                                if (heroClient.IsMelee &&
                                    !pos.IsInRange(Prediction.Position.PredictUnitPosition(heroClient, 850),
                                        heroClient.GetAutoAttackRange() + 150))
                                {
                                    E.Cast(pos);
                                    return;
                                }
                                else if (!heroClient.IsMelee)
                                {
                                    E.Cast(pos);
                                    return;
                                }
                            }
                            else if (enemies == 1 &&
                                     !pos.IsInRange(Prediction.Position.PredictUnitPosition(heroClient, 850),
                                         heroClient.GetAutoAttackRange()))
                            {
                                E.Cast(pos);
                                return;
                            }
                            else if (enemies == 2 && Player.Instance.CountAlliesInRange(850) >= 1)
                            {
                                E.Cast(pos);
                                return;
                            }
                            else if (enemies >= 2)
                            {
                                if (
                                    !EntityManager.Heroes.Enemies.Any(
                                        x =>
                                            pos.IsInRange(Prediction.Position.PredictUnitPosition(x, 850),
                                                x.IsMelee ? x.GetAutoAttackRange() + 150 : x.GetAutoAttackRange())))
                                {
                                    E.Cast(pos);
                                    return;
                                }
                            }
                            E.Cast(pos);
                            return;
                        }
                    }
                }
            }

            if (R.IsReady() && Settings.Combo.UseR)
            {
                var t = R.GetTarget();

                if (t != null)
                {
                    var rPrediction = R.GetPrediction(t);
                    if (rPrediction.HitChancePercent > 75)
                    {
                        if (Settings.Combo.RMinEnemiesHit > 0 &&
                            GetRSplashHits(t).Count() >= Settings.Combo.RMinEnemiesHit)
                        {
                            R.Cast(t);
                            Console.WriteLine("KS R");
                        } else if (Settings.Combo.RMinEnemiesHit == 0 && !t.HasUndyingBuffA() &&
                                   t.TotalHealthWithShields() < (GetRSplashHits(rPrediction.CastPosition).Any(x=>x.NetworkId == t.NetworkId) ? Damage.GetRDamage(t, true) : Damage.GetRDamage(t)))
                        {
                            R.Cast(rPrediction.CastPosition);
                        }
                    }
                    
                }

                var t2 = TargetSelector.GetTarget(1700, DamageType.Physical);

                if (t2 != null && t2.Distance(Player.Instance) > R.Range && !t2.HasUndyingBuffA() && t2.TotalHealthWithShields() < Damage.GetRDamage(t2) * 0.8f)
                {
                    if (GetRSplashHits(t2).Any(x => x.NetworkId == t2.NetworkId))
                    {
                        var p2 = Prediction.Position.PredictConeSpell(t2, 800, (int) (Math.PI/180*70), 750, 3000,
                            Player.Instance.Position.Extend(t2, 1000).To3D());

                        if (p2.HitChance >= HitChance.Medium)
                        {
                            R.Cast(Player.Instance.Position.Extend(p2.CastPosition, R.Range).To3D());
                            Console.WriteLine("KS R");
                        }
                    }
                }
            }
        }
    }
}