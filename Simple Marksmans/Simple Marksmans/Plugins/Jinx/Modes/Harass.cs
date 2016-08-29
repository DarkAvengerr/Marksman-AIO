#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Harass.cs" company="EloBuddy">
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

namespace Simple_Marksmans.Plugins.Jinx.Modes
{
    internal class Harass : Jinx
    {
        public static void Execute()
        {
            if (!Settings.Harass.UseQ)
                return;

            if (HasMinigun)
            {
                foreach (var source in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(GetRealRocketLauncherRange()) && !Player.Instance.IsInAutoAttackRange(x)).Where(source => IsPreAttack && Player.Instance.ManaPercent >= Settings.Harass.MinManaQ))
                {
                    Q.Cast();
                    Orbwalker.ForcedTarget = source;
                    return;
                }
            }
            else if(!EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(GetRealRocketLauncherRange()) && !Player.Instance.IsInAutoAttackRange(x)) && HasRocketLauncher)
            {
                Q.Cast();
                Orbwalker.ForcedTarget = null;
            }
        }
    }
}
