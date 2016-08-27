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
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Lucian.Modes
{
    internal class JungleClear : Lucian
    {
        public static void Execute()
        {
            var jungleMinions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Player.Instance.GetAutoAttackRange()).ToList();

            if (!jungleMinions.Any())
                return;

            if (!Q.IsReady() || !Settings.LaneClear.UseQInJungleClear ||
                !(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ) || jungleMinions.Count <= 1 ||
                HasPassiveBuff || Player.Instance.HasSheenBuff())
                return;

            foreach (var jungleMinion in from jungleMinion in jungleMinions let rectangle = new Geometry.Polygon.Rectangle(Player.Instance.Position.To2D(),
                Player.Instance.Position.Extend(jungleMinion, 900 - jungleMinion.Distance(Player.Instance)),
                10) let count = jungleMinions.Count(
                    minion => new Geometry.Polygon.Circle(minion.Position, jungleMinion.BoundingRadius).Points.Any(
                        rectangle.IsInside)) where count >= 2 select jungleMinion)
            {
                Q.Cast(jungleMinion);
            }
        }
    }
}
