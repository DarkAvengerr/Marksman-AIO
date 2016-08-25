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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Simple_Marksmans.Plugins.Vayne.Modes
{
    internal class PermaActive : Vayne
    {
        public static void Execute()
        {
            if (E.IsReady() && Settings.Combo.UseE && Settings.Misc.EMode == 0)
            {
                var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 300, DamageType.Physical);

                if (target != null)
                {
                    var enemies = Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange() + 300);

                    if (WillEStun(target))
                    {
                        E.Cast(target);
                    }
                    else if (enemies > 1)
                    {
                        foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && WillEStun(x)).OrderByDescending(TargetSelector.GetPriority))
                        {
                            E.Cast(enemy);
                        }
                    }
                }
            }
        }
    }
}
