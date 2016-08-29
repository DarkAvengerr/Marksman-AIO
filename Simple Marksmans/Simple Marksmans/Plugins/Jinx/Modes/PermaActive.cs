#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="PermaActive.cs" company="EloBuddy">
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
    internal class PermaActive : Jinx
    {
        public static void Execute()
        {
            if (W.IsReady() && Settings.Misc.WKillsteal && Player.Instance.Mana - 90 > 100 &&
                !Player.Instance.Position.IsVectorUnderEnemyTower())
            {
                if (EntityManager.Heroes.Enemies.Any(
                    x =>
                        x.IsValidTarget(W.Range) && !x.HasSpellShield() && !x.HasUndyingBuffA() &&
                        x.Distance(Player.Instance) > Settings.Combo.WMinDistanceToTarget))
                {
                    foreach (
                        var enemy in
                            EntityManager.Heroes.Enemies.Where(
                                x =>
                                    x.IsValidTarget(W.Range) && !x.HasSpellShield() && !x.HasUndyingBuffA() &&
                                    x.Distance(Player.Instance) > Settings.Combo.WMinDistanceToTarget))
                    {
                        var health = enemy.TotalHealthWithShields() - IncomingDamage.GetIncomingDamage(enemy);
                        var wDamage = Player.Instance.GetSpellDamage(enemy, SpellSlot.W);
                        var wPrediction = W.GetPrediction(enemy);

                        if (health < wDamage && wPrediction.HitChance == HitChance.High)
                        {
                            W.Cast(wPrediction.CastPosition);
                            return;
                        }

                        if (!(health < wDamage + Damage.GetRDamage(enemy)) || !R.IsReady() ||
                            !(R.GetPrediction(enemy).HitChancePercent > 70) || !(wPrediction.HitChancePercent > 65))
                            continue;

                        W.Cast(wPrediction.CastPosition);
                        return;
                    }
                }
                if (Settings.Harass.UseW && Player.Instance.ManaPercent >= Settings.Harass.MinManaW && !IsPreAttack)
                {
                    foreach (var wPrediction in EntityManager.Heroes.Enemies.Where(
                        x =>
                            x.IsValidTarget(W.Range) && Settings.Harass.IsWHarassEnabledFor(x) &&
                            x.Distance(Player.Instance) > GetRealRocketLauncherRange())
                        .Where(enemy => enemy.IsValidTarget(W.Range))
                        .Select(enemy => W.GetPrediction(enemy))
                        .Where(wPrediction => wPrediction.HitChancePercent > 70))
                    {
                        W.Cast(wPrediction.CastPosition);
                        return;
                    }
                }
            }
            if (R.IsReady() && Settings.Combo.UseR)
            {
                var target = TargetSelector.GetTarget(Settings.Combo.RRangeKeybind, DamageType.Physical);

                if (target == null || !Settings.Combo.RKeybind)
                    return;

                var rPrediciton = R.GetPrediction(target);
                if (rPrediciton.HitChance == HitChance.High)
                {
                    R.Cast(rPrediciton.CastPosition);
                }
            }


            if (!E.IsReady() || !Settings.Combo.AutoE || !(Player.Instance.Mana - 50 > 100))
                return;

            foreach (
                var enemy in
                    EntityManager.Heroes.Enemies.Where(
                        x =>
                            x.IsValidTarget(E.Range) &&
                            (x.GetMovementBlockedDebuffDuration() > 0.7f ||
                             x.Buffs.Any(
                                 m =>
                                     m.Name.ToLowerInvariant() == "zhonyasringshield" ||
                                     m.Name.ToLowerInvariant() == "bardrstasis"))))
            {
                if (enemy.Buffs.Any(m => m.Name.ToLowerInvariant() == "zhonyasringshield" ||
                                         m.Name.ToLowerInvariant() == "bardrstasis"))
                {
                    var buffTime = enemy.Buffs.FirstOrDefault(m => m.Name.ToLowerInvariant() == "zhonyasringshield" ||
                                                                   m.Name.ToLowerInvariant() == "bardrstasis");
                    if (buffTime != null && buffTime.EndTime - Game.Time < 1 && buffTime.EndTime - Game.Time > 0.3 && enemy.IsValidTarget(E.Range))
                    {
                        E.Cast(enemy.ServerPosition);
                    }
                } else if (enemy.IsValidTarget(E.Range))
                {
                    E.Cast(enemy.ServerPosition);
                }
                Console.WriteLine("Name : {0} | Duration : {1}", enemy.Hero,
                    enemy.GetMovementBlockedDebuffDuration());
            }
        }
    }
}