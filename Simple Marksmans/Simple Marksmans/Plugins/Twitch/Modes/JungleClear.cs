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
using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Simple_Marksmans.Plugins.Twitch.Modes
{
    internal class JungleClear : Twitch
    {
        public static void Execute()
        {
            var jungleMinions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, E.Range).ToList();

            if (!jungleMinions.Any())
                return;

            if (E.IsReady() && Settings.JungleClear.UseE && Player.Instance.ManaPercent >= Settings.JungleClear.EMinMana)
            {
                if (
                    EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, E.Range)
                        .Any(
                            unit =>
                                unit.IsValidTarget(E.Range) && (unit.BaseSkinName.Contains("Baron") || unit.BaseSkinName.Contains("Dragon") ||
                                 unit.BaseSkinName.Contains("RiftHerald") || unit.BaseSkinName.Contains("Blue") ||
                                 unit.BaseSkinName.Contains("Red") || unit.BaseSkinName.Contains("Crab")) && !unit.BaseSkinName.Contains("Mini") &&
                                Damage.IsTargetKillableByE(unit)))
                {
                    Console.WriteLine("[DEBUG] Casting E to ks blue [" + Game.Time + "]");
                    E.Cast();
                }
            }

            if (W.IsReady() && Settings.JungleClear.UseW && Player.Instance.ManaPercent >= Settings.JungleClear.WMinMana)
            {
                var c = EntityManager.MinionsAndMonsters.GetCircularFarmLocation(jungleMinions, 200, 950,
                    250, 1400);

                if (c.HitNumber > 1)
                {
                    W.Cast(c.CastPosition);
                }
            }
        }
    }
}
