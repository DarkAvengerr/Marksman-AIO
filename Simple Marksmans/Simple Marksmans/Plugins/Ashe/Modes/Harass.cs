﻿#region Licensing
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

using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

namespace Simple_Marksmans.Plugins.Ashe.Modes
{
    internal class Harass : Ashe
    {
        public static void Execute()
        {
            if (!W.IsReady() || !Settings.Harass.UseW || !(Player.Instance.ManaPercent > Settings.Harass.MinManaForW))
                return;

            var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);

            if (target == null)
                return;

            var wPrediction = GetWPrediction(target);

            if (wPrediction != null && wPrediction.HitChance >= HitChance.High)
            {
                W.Cast(wPrediction.CastPosition);
            }
        }
    }
}
