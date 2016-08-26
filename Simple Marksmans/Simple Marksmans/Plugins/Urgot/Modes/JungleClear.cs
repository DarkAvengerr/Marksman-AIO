#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="JungleClear.cs" company="EloBuddy">
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

namespace Simple_Marksmans.Plugins.Urgot.Modes
{
    internal class JungleClear : Urgot
    {
        public static void Execute()
        {
            var jungleMinions =
                EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Q.Range).ToList();

            if (!jungleMinions.Any())
                return;

            if (Q.IsReady() && Settings.LaneClear.UseQInJungleClear &&
                Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ)
            {
                if (Settings.LaneClear.UseQInLaneClear && CorrosiveDebufTargets.Any(unit => unit is Obj_AI_Minion && unit.IsValidTarget(1300)))
                {
                    if (CorrosiveDebufTargets.Any(unit => unit is Obj_AI_Minion && unit.IsValidTarget(1300)))
                    {
                        foreach (
                            var minion in
                                from minion in
                                    CorrosiveDebufTargets.Where(
                                        unit => unit is Obj_AI_Minion && unit.IsValidTarget(1300))
                                select minion)
                        {
                            Q.Cast(minion.Position);
                            break;
                        }
                    }
                }
                else if (Settings.LaneClear.UseQInLaneClear)
                {
                    foreach (var minion in from minion in jungleMinions
                        let qPrediction = Q.GetPrediction(minion)
                        where qPrediction.Collision == false
                        select minion)
                    {
                        Q.Cast(minion);
                    }
                }
            }


            if (E.IsReady() && Settings.LaneClear.UseEInJungleClear &&
                Player.Instance.ManaPercent >= Settings.LaneClear.MinManaE)
            {
                var farmPosition =
                    EntityManager.MinionsAndMonsters.GetCircularFarmLocation(
                        EntityManager.MinionsAndMonsters.Monsters.Where(
                            x => x.IsValidTarget(E.Range) && x.HealthPercent > 10), 250, 900, 250, 1550);

                if (farmPosition.HitNumber > 1)
                {
                    E.Cast(farmPosition.CastPosition);
                }
            }
        }
    }
}
