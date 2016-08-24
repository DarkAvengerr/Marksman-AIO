#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="JungleClear.cs" company="EloBuddy">
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
using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;

namespace Simple_Marksmans.Plugins.Kalista.Modes
{
    internal class JungleClear : Kalista
    {
        public static void Execute()
        {
            if (!EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Q.Range).Any())
                return;

            if (Q.IsReady() && Settings.JungleLaneClear.UseQ && !Player.Instance.IsDashing() && Player.Instance.ManaPercent >= Settings.JungleLaneClear.MinManaForQ)
            {
                var minions =
                    EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Q.Range).Where(
                        x => x.Health < Player.Instance.GetSpellDamage(x, SpellSlot.Q)).ToList();

                if (!minions.Any())
                    return;

                foreach (var minion in minions)
                {
                    var qPrediction = Q.GetPrediction(minion);
                    var collisionableObjects =
                        qPrediction.GetCollisionObjects<Obj_AI_Base>()
                            .Where(x => x.Health < Player.Instance.GetSpellDamage(x, SpellSlot.Q))
                            .ToList();

                    foreach (var minionC in collisionableObjects)
                    {
                        if (minionC == null)
                            continue;

                        var id = collisionableObjects.FindIndex(x => x == minionC);
                        var collisionObjects = new List<Obj_AI_Base> { minionC };

                        for (var i = id; i < collisionableObjects.Count - 1; i++)
                        {
                            if (!(collisionableObjects[id].Health <=
                                  Player.Instance.GetSpellDamage(collisionableObjects[id], SpellSlot.Q))) continue;

                            collisionObjects.Add(collisionableObjects[id]);
                            id++;
                        }

                        var rectangleP = new Geometry.Polygon.Rectangle(Player.Instance.Position,
                            collisionableObjects[id].Position, 70);

                        var list =
                            EntityManager.MinionsAndMonsters.EnemyMinions.Where(
                                x =>
                                    x.IsValidTarget(1500) &&
                                    new Geometry.Polygon.Circle(x.Position, x.BoundingRadius).Points.Any(
                                        xx => rectangleP.IsInside(xx)))
                                .Where(xd => xd.Health <= Player.Instance.GetSpellDamage(xd, SpellSlot.Q))
                                .ToList();

                        if (list.Count < 2)
                            continue;

                        var interectionPoint = rectangleP.Points[0].Intersection(rectangleP.Points[2],
                            rectangleP.Points[1], rectangleP.Points[3]);

                        Q.Cast(interectionPoint.Point.To3D());
                    }
                }
            }

            if (E.IsReady() && Settings.JungleLaneClear.UseE &&
                Player.Instance.ManaPercent >= Settings.JungleLaneClear.MinManaForE)
            {
                var minions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, E.Range).Where(unit => unit.IsTargetKillableByRend());

                if (minions.Count() >= Settings.JungleLaneClear.MinMinionsForE)
                {
                    E.Cast();
                }
            }
        }
    }
}
