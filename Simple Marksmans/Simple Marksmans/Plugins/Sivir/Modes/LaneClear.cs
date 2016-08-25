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

namespace Simple_Marksmans.Plugins.Sivir.Modes
{
    internal class LaneClear : Sivir
    {
        public static void Execute()
        {
            var laneMinions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position,
                    Q.Range).ToList();

            if (!laneMinions.Any() && !(!Settings.LaneClear.EnableIfNoEnemies ||
                                        Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) >
                                        Settings.LaneClear.AllowedEnemies))
                return;

            if (Q.IsReady() && Settings.LaneClear.UseQInLaneClear &&
                Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ)
            {
                var farmLocation = EntityManager.MinionsAndMonsters.GetLineFarmLocation(laneMinions, 100, 1200);

                if (farmLocation.HitNumber > 2)
                {
                    Q.Cast(farmLocation.CastPosition);
                }
            }

            if (!IsPostAttack || !W.IsReady() || !Settings.LaneClear.UseWInLaneClear || !(Player.Instance.ManaPercent >= Settings.LaneClear.WMinMana))
                return;

            if (laneMinions.Count(x => x.Distance(Player.Instance) < Player.Instance.GetAutoAttackRange()) != 0 && laneMinions.Count > 3)
            {
                W.Cast();
            }
        }
    }
}