#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Jinx.cs" company="EloBuddy">
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
using EloBuddy;
using EloBuddy.SDK;
using Simple_Marksmans.Interfaces;
using Simple_Marksmans.Utils;
using Color = System.Drawing.Color;

namespace Simple_Marksmans.Plugins.Jinx
{
    internal class Jinx : ChampionPlugin
    {
        static Jinx()
        {

        }

        protected override void OnDraw()
        {
        }
        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            throw new NotImplementedException();
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            throw new NotImplementedException();
        }

        protected override void CreateMenu()
        {
            throw new NotImplementedException();
        }

        protected override void PermaActive()
        {
            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            Modes.Combo.Execute();
        }

        protected override void HarassMode()
        {
            Modes.Harass.Execute();
        }

        protected override void LaneClear()
        {
            Modes.LaneClear.Execute();
        }

        protected override void JungleClear()
        {
            Modes.JungleClear.Execute();
        }

        protected override void LastHit()
        {
            Modes.LastHit.Execute();
        }

        protected override void Flee()
        {
            Modes.Flee.Execute();
        }
    }
}