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
using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Urgot.Modes
{
    internal class PermaActive : Urgot
    {
        public static void Execute()
        {
            if (Settings.Misc.EnableKillsteal && !Player.Instance.IsRecalling())
            {
                foreach (
                    var qPrediction in
                        EntityManager.Heroes.Enemies.Where(
                            x => x.IsValidTarget(Q.Range) && !x.HasUndyingBuffA() && x.TotalHealthWithShields() < Player.Instance.GetSpellDamage(x, SpellSlot.Q))
                            .Select(source => Q.GetPrediction(source))
                            .Where(qPrediction => qPrediction.HitChance == HitChance.High))
                {
                    Q.Cast(qPrediction.CastPosition);
                    return;
                }
            }
            if (!W.IsReady())
                return;

            var incomingDamage = IncomingDamage.GetIncomingDamage(Player.Instance);

            if (!(incomingDamage/Player.Instance.TotalHealthWithShields()*100 > Settings.Misc.MinDamage) &&
                !(incomingDamage > Player.Instance.Health))
                return;

            Console.WriteLine("casting W too much incoming damage...");
            W.Cast();
        }
    }
}