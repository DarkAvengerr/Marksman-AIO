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
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Lucian.Modes
{
    internal class LaneClear : Lucian
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            var laneMinions =
                EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, 1000).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            if (!Q.IsReady() || !Settings.LaneClear.UseQInLaneClear ||
                !(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ) || laneMinions.Count <= 1 ||
                HasPassiveBuff || Player.Instance.HasSheenBuff())
                return;

            foreach (var objAiMinion in from objAiMinion in laneMinions let rectangle = new Geometry.Polygon.Rectangle(Player.Instance.Position.To2D(),
                Player.Instance.Position.Extend(objAiMinion, 900 - objAiMinion.Distance(Player.Instance)),
                10) let count = laneMinions.Count(
                    minion => new Geometry.Polygon.Circle(minion.Position, objAiMinion.BoundingRadius).Points.Any(
                        rectangle.IsInside)) where count >= Settings.LaneClear.MinMinionsHitQ select objAiMinion)
            {
                Q.Cast(objAiMinion);
            }
        }
    }
}