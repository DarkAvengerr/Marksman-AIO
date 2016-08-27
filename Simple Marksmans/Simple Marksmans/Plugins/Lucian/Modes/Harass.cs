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

namespace Simple_Marksmans.Plugins.Lucian.Modes
{
    internal class Harass : Lucian
    {
        public static void Execute()
        {
            if (!Q.IsReady() || !Settings.Harass.UseQ || !(Player.Instance.ManaPercent >= Settings.Harass.MinManaQ))
                return;

            foreach (
                var enemy in
                    EntityManager.Heroes.Enemies.Where(
                        x => x.IsValidTarget(900) && Settings.Harass.IsAutoHarassEnabledFor(x))
                        .OrderByDescending(x => Player.Instance.GetSpellDamage(x, SpellSlot.Q)))
            {
                if (enemy.IsValidTarget(Q.Range))
                {
                    Q.Cast(enemy);
                    return;
                }

                if (!enemy.IsValidTarget(900))
                    continue;

                foreach (
                    var entity in
                        from entity in
                            EntityManager.MinionsAndMonsters.CombinedAttackable.Where(
                                x => x.IsValidTarget(Q.Range))
                        let pos =
                            Player.Instance.Position.Extend(entity, 900 - Player.Instance.Distance(entity))
                        let targetpos = Prediction.Position.PredictUnitPosition(enemy, 250)
                        let rect = new Geometry.Polygon.Rectangle(entity.Position.To2D(), pos, 10)
                        where
                            new Geometry.Polygon.Circle(targetpos, enemy.BoundingRadius).Points.Any(
                                rect.IsInside)
                        select entity)
                {
                    Q.Cast(entity);
                    return;
                }
            }
        }
    }
}
