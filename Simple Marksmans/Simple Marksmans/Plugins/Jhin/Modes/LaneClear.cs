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

namespace Simple_Marksmans.Plugins.Jhin.Modes
{
    internal class LaneClear : Jhin
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            var laneMinions =
                EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, Q.Range).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            if (Q.IsReady() && Settings.LaneClear.UseQInLaneClear && Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ && Game.Time * 1000 - LastLaneClear > 250)
            {
                foreach (var minion in laneMinions.Where(x => x.Health < Damage.GetQDamage(x)))
                {
                    var count = 0;
                    var kminion = laneMinions.Where(x => x.NetworkId != minion.NetworkId && x.Distance(minion) < 400)
                            .OrderBy(x => x.Distance(minion));

                    if (kminion.Any())
                    {
                        count++;
                        var kMinion = kminion.First();
                        if (kMinion != null && kMinion.Health < Damage.GetQDamage(kMinion) * 1.35f)
                        {
                            count++;
                            var lminion =
                                kminion.Where(x => x.NetworkId != kMinion.NetworkId && x.Distance(kMinion) < 400)
                                    .OrderBy(x => x.Distance(kMinion));

                            if (lminion.Any())
                            {
                                var lMinion = lminion.First();

                                if (lMinion != null && lMinion.Health < Damage.GetQDamage(lMinion) * 1.70f)
                                {
                                    count++;
                                    var nminion =
                                        lminion.Where(x => x.NetworkId != lMinion.NetworkId && x.Distance(lMinion) < 400)
                                            .OrderBy(x => x.Distance(lMinion));
                                    if (nminion.Any())
                                    {
                                        var nMinion = nminion.First();

                                        if (nMinion != null && nMinion.Health < Damage.GetQDamage(nMinion) * 2.05f)
                                        {
                                            count++;
                                        }
                                    }
                                } else continue;
                            } else continue;
                        } else continue;
                    }
                    if (count <= 2)
                        continue;

                    Q.Cast(minion);
                    break;
                }
                LastLaneClear = Game.Time*1000;
            }

            if (!W.IsReady() || !Settings.LaneClear.UseWInLaneClear ||
                !(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaW))
                return;

            var farmLocation = EntityManager.MinionsAndMonsters.GetLineFarmLocation(laneMinions, 40, 2500);

            if (farmLocation.HitNumber > 2)
            {
                W.Cast(farmLocation.CastPosition);
            }
        }
    }
}