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

using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Jhin.Modes
{
    internal class PermaActive : Jhin
    {
        public static void Execute()
        {
            if (Settings.Misc.EnableKillsteal)
            {
                if (W.IsReady() && !IsCastingR)
                {
                    if (EntityManager.Heroes.Enemies.Any(
                        x =>
                            x.Distance(Player.Instance) < W.Range && x.IsHPBarRendered &&
                            Damage.IsTargetKillableFromW(x)))
                    {
                        foreach (var rPrediction in
                            EntityManager.Heroes.Enemies.Where(
                                x => x.IsValidTarget(W.Range) && !x.IsDead && Damage.IsTargetKillableFromW(x))
                                .OrderBy(
                                    x => x.Health)
                                .Where(target => !target.HasUndyingBuffA() && !target.HasSpellShield())
                                .Select(target => W.GetPrediction(target))
                                .Where(rPrediction => rPrediction.HitChance >= HitChance.High))
                        {
                            W.Cast(rPrediction.CastPosition);
                            return;
                        }
                    }
                    else if (EntityManager.Heroes.Enemies.Count(x => x.IsValidTarget(W.Range) && HasSpottedBuff(x)) < 2)
                    {
                        foreach (
                            var target in
                                EntityManager.Heroes.Enemies.Where(
                                    x => !x.IsDead && x.IsUserInvisibleFor(250) && Damage.IsTargetKillableFromW(x)))
                        {
                            var data = target.GetVisibilityTrackerData();

                            if (!(Game.Time*1000 - data.LastVisibleGameTime*1000 < 2000) ||
                                !(data.LastHealthPercent > 0))
                                continue;

                            W.Cast(
                                data.LastPosition.Extend(data.LastPath,
                                    target.MoveSpeed*1 + (Game.Time*1000 - data.LastVisibleGameTime*1000)/1000).To3D());
                            break;
                        }
                    }
                }
            }

            if (!IsCastingR || !Settings.Combo.UseR || GetCurrentShootsRCount < 1)
                return;
            
            if ((Settings.Combo.RMode != 0 || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) &&
                (Settings.Combo.RMode != 1 || !Settings.Combo.RKeybind) && Settings.Combo.RMode != 2)
                return;

            if (EntityManager.Heroes.Enemies.Any(x=> x.Distance(Player.Instance) < R.Range && IsInsideRRange(x) && x.IsHPBarRendered))
            {
                foreach (var rPrediction in EntityManager.Heroes.Enemies.Where(x=> x.IsValidTarget(R.Range) && !x.IsDead && IsInsideRRange(x))
                    .OrderBy(
                        x => x.Health).Where(target => !target.HasUndyingBuffA() && !target.HasSpellShield()).Select(target => R.GetPrediction(target)).Where(rPrediction => rPrediction.HitChance >= HitChance.High))
                {
                    R.Cast(rPrediction.CastPosition);
                    return;
                }
            }
            else if(Settings.Combo.EnableFowPrediction)
            {
                foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsUserInvisibleFor(250)))
                {
                    var data = enemy.GetVisibilityTrackerData();
                    if (!(Game.Time*1000 - data.LastVisibleGameTime*1000 < 2000) || !(data.LastHealthPercent > 0) ||
                        !IsInsideRRange(data.LastPosition)) continue;

                    var eta = data.LastPosition.Distance(Player.Instance)/5000;

                    R.Cast(data.LastPosition.Extend(data.LastPath, enemy.MoveSpeed*eta).To3D());

                    return;
                }
            }
        }
    }
}