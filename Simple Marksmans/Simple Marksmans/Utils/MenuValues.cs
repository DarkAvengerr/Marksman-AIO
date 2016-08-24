#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="MenuValues.cs" company="EloBuddy">
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
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Utils;

namespace Simple_Marksmans.Utils
{
    public class MenuValues
    {
        public bool this[string key]
        {
            get
            {
                var menu = ParseMenuNames.GetMenu(key);
                if (menu?[key] != null)
                {
                    var type = menu[key].GetType();
                    switch (type.ToString())
                    {
                        case "EloBuddy.SDK.Menu.Values.CheckBox":
                            return menu[key].Cast<CheckBox>().CurrentValue;
                        case "EloBuddy.SDK.Menu.Values.KeyBind":
                            return menu[key].Cast<KeyBind>().CurrentValue;
                        default:
                            Logger.Error("Menu item : " + key + " is not oftype bool.");
                            break;
                    }
                }
                Logger.Error("Menu item : " + key + " doesn't exists.");
                return false;
            }
            set
            {
                var menu = ParseMenuNames.GetMenu(key);
                if (menu?[key] != null)
                {
                    var type = menu[key].GetType();
                    switch (type.ToString())
                    {
                        case "EloBuddy.SDK.Menu.Values.CheckBox":
                            menu[key].Cast<CheckBox>().CurrentValue = value;
                            break;
                        case "EloBuddy.SDK.Menu.Values.KeyBind":
                            menu[key].Cast<KeyBind>().CurrentValue = value;
                            break;
                        default:
                            Logger.Error("Menu item : " + key + " is not oftype bool.");
                            break;
                    }
                }
                Logger.Error("Menu item : " + key + " doesn't exists.");
            }
        }

        public int this[string key, bool returnInt = true]
        {
            get
            {
                var menu = ParseMenuNames.GetMenu(key);
                if (menu?[key] != null)
                {
                    var type = menu[key].GetType();
                    switch (type.ToString())
                    {
                        case "EloBuddy.SDK.Menu.Values.ComboBox":
                            return menu[key].Cast<ComboBox>().CurrentValue;
                        case "EloBuddy.SDK.Menu.Values.Slider":
                            return menu[key].Cast<Slider>().CurrentValue;
                        default:
                            Logger.Error("Menu item : " + key + " is not oftype int.");
                            break;
                    }
                }
                Logger.Error("Menu item : " + key + " doesn't exists.");
                return 0;
            }
            set
            {
                var menu = ParseMenuNames.GetMenu(key);
                if (menu?[key] != null)
                {
                    var type = menu[key].GetType();
                    switch (type.ToString())
                    {
                        case "EloBuddy.SDK.Menu.Values.ComboBox":
                            menu[key].Cast<ComboBox>().CurrentValue = value;
                            break;
                        case "EloBuddy.SDK.Menu.Values.Slider":
                            menu[key].Cast<Slider>().CurrentValue = value;
                            break;
                        default:
                            Logger.Error("Menu item : " + key + " is not oftype int.");
                            break;
                    }
                }
                else
                {
                    Logger.Error("Menu item : " + key + " doesn't exists.");
                }
            }
        }
    }

    internal class ParseMenuNames
    {
        public static Menu GetMenu(string uniqueIdentifier)
        {
            var splitted = uniqueIdentifier.Split('.');

            if (splitted[0] == "Activator")
            {
                switch (splitted[1])
                {
                    case "ItemsMenu":
                        return Activator.Activator.ItemsMenu;
                    case "PotionsAndElixirsMenu":
                        return Activator.Activator.PotionsAndElixirsMenu;
                    case "CleanseMenu":
                        return Activator.Activator.CleanseMenu;
                    default:
                        return Activator.Activator.ActivatorMenu;
                }
            } else if (splitted[0] == "MenuManager")
            {
                switch (splitted[1])
                {
                    case "GapcloserMenu":
                        return MenuManager.GapcloserMenu;
                    case "InterrupterMenu":
                        return MenuManager.InterrupterMenu;
                    default:
                        return MenuManager.Menu;
                }
            }
            return null;
        }
    }
}