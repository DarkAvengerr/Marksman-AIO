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
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Tristana.Modes
{
    internal class PermaActive : Tristana
    {
        public static void Execute()
        {
            if (R.IsReady() && Settings.Combo.UseR && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(R.Range) && x.HealthPercent < 50))
            {
                foreach (var target in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range)).OrderBy(TargetSelector.GetPriority))
                {
                    var incomingDamage = IncomingDamage.GetIncomingDamage(target);

                    var damage = (incomingDamage + Damage.GetEPhysicalDamage(target) +
                                 Damage.GetRDamage(target)) - 25;

                    if (target.Hero == Champion.Blitzcrank && !target.HasBuff("BlitzcrankManaBarrierCD") && !target.HasBuff("ManaBarrier"))
                    {
                        damage -= target.Mana / 2;
                    }

                    if (target.Distance(Player.Instance) < Player.Instance.GetAutoAttackRange() &&
                        target.TotalHealthWithShields() > Player.Instance.GetAutoAttackDamage(target, true)*2 && target.TotalHealthWithShields() < damage)
                    {
                        R.Cast(target);
                        Console.WriteLine("[DEBUG] Casting R on : {0} to killsteal ! v 1", target.Hero);
                    }
                }
            }
        }
    }
}