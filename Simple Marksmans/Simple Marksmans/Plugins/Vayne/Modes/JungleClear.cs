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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Simple_Marksmans.Plugins.Vayne.Modes
{
    internal class JungleClear : Vayne
    {
        public static void Execute()
        {
            var jungleMinions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, E.Range).ToList();

            if (!jungleMinions.Any())
                return;

            if (IsPostAttack && Q.IsReady() && Settings.LaneClear.UseQToJungleClear)
            {
                if (Player.Instance.Position.Extend(Game.CursorPos, 299)
                    .IsInRange(Orbwalker.LastTarget, Player.Instance.GetAutoAttackRange()))
                {
                    Q.Cast(Player.Instance.Position.Extend(Game.CursorPos, 285).To3D());
                }
            }
        }
    }
}
