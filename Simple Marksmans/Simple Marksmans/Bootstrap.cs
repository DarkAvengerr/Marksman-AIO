#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Bootstrap.cs" company="EloBuddy">
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
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans
{
    internal class Bootstrap
    {
        public static bool MenuLoaded { get; set; }

        public static Dictionary<string, ColorBGRA> SavedColorPickerData { get; set; }

        public static void Initialize()
        {
            Console.WriteLine("[DEBUG] Initializing addon");

            var pluginInitialized = InitializeAddon.Initialize();

            if (!pluginInitialized)
                return;

            Core.DelayAction(
                () =>
                {
                    Console.WriteLine("[DEBUG] Creating Menu");
                    MenuManager.CreateMenu();
                    Console.WriteLine("[DEBUG] Creating plugin instance");
                    Activator.Activator.InitializeActivator();
                    MenuLoaded = true;

                    Misc.PrintInfoMessage("<b><font color=\"#5ED43D\">" + Player.Instance.ChampionName +
                                          "</font></b> loaded successfully.");
                    Console.WriteLine("[DEBUG] Marksman AIO  fully loaded");
                }, 250);
        }
    }
}