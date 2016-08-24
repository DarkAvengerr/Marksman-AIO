#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="PermaActive.cs" company="EloBuddy">
// 
//  Marksman AIO
// 
//  Copyright (C) 2016 Krystian Tenerowicz
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see http://www.gnu.org/licenses/. 
//  </copyright>
//  <summary>
// 
//  Email: geroelobuddy@gmail.com
//  PayPal: geroelobuddy@gmail.com
//  </summary>
//  --------------------------------------------------------------------------------------------------------------------
#endregion

using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Corki.Modes
{
    internal class PermaActive : Corki
    {
        public static void Execute()
        {
            if (R.IsReady())
            {
                foreach (var pred in EntityManager.Heroes.Enemies.Where(x=> !x.IsDead && x.IsValidTarget(R.Range) && x.Health < Damage.GetSpellDamage(x, SpellSlot.R)).Select(unit => R.GetPrediction(unit)).Where(pred => !pred.Collision))
                {
                    R.Cast(pred.CastPosition);
                }
            }

            if (R.IsReady() && Settings.Misc.AutoHarassEnabled && Player.Instance.Spellbook.GetSpell(SpellSlot.R).Ammo >= Settings.Misc.MinStacksToUseR && !HasSheenBuff)
            {
                if (HasBigRMissile && !(HasBigRMissile && Settings.Misc.UseBigBomb))
                    return;

                foreach (
                    var enemy in
                        EntityManager.Heroes.Enemies.Where(
                            hero =>
                                !hero.IsDead && hero.IsValidTarget(R.Range) && !hero.HasSpellShield() &&
                                !hero.HasUndyingBuffA() && Settings.Misc.IsAutoHarassEnabledFor(hero))
                            .OrderByDescending(TargetSelector.GetPriority).ThenBy(x => x.Distance(Player.Instance)))
                {
                    var prediction = R.GetPrediction(enemy);

                    if (prediction.Collision && prediction.CollisionObjects != null && Settings.Combo.RAllowCollision)
                    {
                        var first =
                            prediction.CollisionObjects.OrderBy(x => x.Distance(Player.Instance))
                                .FirstOrDefault();

                        if (first != null)
                        {
                            var e =
                                GetCollisionObjects<Obj_AI_Base>(first)
                                    .FirstOrDefault(x => x.NetworkId == enemy.NetworkId);
                            if (e != null)
                            {
                                R.Cast(first);
                            }
                        }
                    }
                    else if (prediction.HitChancePercent >= 60)
                    {
                        R.Cast(prediction.CastPosition);
                    }
                }
            }
        }
    }
}