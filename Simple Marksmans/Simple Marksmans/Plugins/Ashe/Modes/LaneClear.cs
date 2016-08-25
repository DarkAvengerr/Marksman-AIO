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

using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Simple_Marksmans.Plugins.Ashe.Modes
{
    internal class LaneClear : Ashe
    {
        public static void Execute()
        {
            var laneMinions =
                EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position,
                    W.Range).ToList();

            if (!laneMinions.Any() &&
                !(!Settings.LaneClear.EnableIfNoEnemies ||
                  Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) >
                  Settings.LaneClear.AllowedEnemies))
                return;

            if (Q.IsReady() && Settings.LaneClear.UseQInLaneClear && Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ && IsPreAttack && laneMinions.Count > 3)
            {
                Q.Cast();
            }

            if (W.IsReady() && Settings.LaneClear.UseWInLaneClear && Player.Instance.ManaPercent >= Settings.LaneClear.MinManaW && laneMinions.Count > 3)
            {
                foreach (var objAiMinion in laneMinions)
                {
                    var poly = new Geometry.Polygon.Sector(Player.Instance.Position, Game.CursorPos,
                        (float) (Math.PI/180*40), 950, 9).Points.ToArray();

                    for (var i = 1; i < 10; i++)
                    {
                        var qPred = Prediction.Position.PredictLinearMissile(objAiMinion, 1100, 20, 25, 1200, 0,
                            Player.Instance.Position.Extend(poly[i], 20).To3D());

                        if (qPred.CollisionObjects.Any())
                        {
                            var xd = EntityManager.MinionsAndMonsters.GetLineFarmLocation(
                                qPred.GetCollisionObjects<Obj_AI_Minion>(), 120, 1200);

                            if (xd.HitNumber <= 2)
                                continue;

                            W.Cast(xd.CastPosition);
                            break;
                        }
                    }
                }
            }

        }
    }
}