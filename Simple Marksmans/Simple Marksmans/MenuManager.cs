#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="MenuManager.cs" company="EloBuddy">
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
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans
{
    public static class MenuManager
    {
        public static Menu Menu { get; set; }
        public static Menu GapcloserMenu { get; set; }
        public static Menu InterrupterMenu { get; set; }
        public static int GapclosersFound { get; private set; }
        public static int InterruptibleSpellsFound { get; private set; }
        public static int GapcloserScanRange { get; set; } = 1250;

        public static MenuValues MenuValues { get; set; } = new MenuValues();

        public static void CreateMenu()
        {
            Menu = MainMenu.AddMenu("Marksman AIO", "MarksmanAIO");
            Menu.AddGroupLabel("Welcome back, Buddy !");
            Menu.AddSeparator(5);
            Menu.AddLabel("This addon comes in handy for anyone who wants to have\nall marksmans plugins in just one addon. This AIO comes also with beautiful drawings\nand an activator. I just " +
                          "hope you will have fun. Good luck !");
            Menu.AddSeparator(40);
            Menu.AddLabel("Marksman AIO is currently in early beta phase.\nIf you experienced any bugs please report them in the forum thread.");

            //BuildAntiGapcloserMenu();
            //BuildInterrupterMenu();

            InitializeAddon.PluginInstance.CreateMenu();
        }

        public static void BuildInterrupterMenu()
        {
            if (
                !EntityManager.Heroes.Enemies.Any(
                    x => Utils.Interrupter.InterruptibleList.Exists(e => e.ChampionName == x.ChampionName)))
            {
                return;
            }

            InterrupterMenu = Menu.AddSubMenu("Interrupter");
            InterrupterMenu.AddGroupLabel("Global settings");
            InterrupterMenu.Add("MenuManager.InterrupterMenu.Enabled", new CheckBox("Interrupter Enabled"));
            InterrupterMenu.Add("MenuManager.InterrupterMenu.OnlyInCombo",
                new CheckBox("Active only in Combo mode", false));
            InterrupterMenu.AddSeparator(15);

            foreach (
                var enemy in
                    EntityManager.Heroes.Enemies.Where(
                        x => Utils.Interrupter.InterruptibleList.Exists(e => e.ChampionName == x.ChampionName))
                )
            {
                var interruptibleSpells = Utils.Interrupter.InterruptibleList.FindAll(e => e.ChampionName == enemy.ChampionName);

                if (interruptibleSpells.Count <= 0)
                    continue;

                InterrupterMenu.AddGroupLabel(enemy.ChampionName);

                foreach (var interruptibleSpell in interruptibleSpells)
                {
                    int healthPercent;

                    switch (interruptibleSpell.DangerLevel)
                    {
                        case DangerLevel.High:
                            healthPercent = 100;
                            break;
                        case DangerLevel.Medium:
                            healthPercent = 75;
                            break;
                        case DangerLevel.Low:
                            healthPercent = 50;
                            break;
                        default:
                            healthPercent = 0;
                            break;
                    }

                    InterrupterMenu.AddLabel("[" + interruptibleSpell.SpellSlot + "] " + interruptibleSpell.SpellName + " | Danger Level : " + interruptibleSpell.DangerLevel);
                    InterrupterMenu.Add("MenuManager.InterrupterMenu." + enemy.ChampionName + "." + interruptibleSpell.SpellSlot + ".Delay", new Slider("Delay", 0, 0, 500));
                    InterrupterMenu.Add("MenuManager.InterrupterMenu." + enemy.ChampionName + "." + interruptibleSpell.SpellSlot + ".Hp", new Slider("Only if I'm below under {0} % of my HP", healthPercent));
                    InterrupterMenu.Add("MenuManager.InterrupterMenu." + enemy.ChampionName + "." + interruptibleSpell.SpellSlot + ".Enemies", new Slider("Only if {0} or less enemies are near", 5, 1, 5));
                    InterrupterMenu.Add("MenuManager.InterrupterMenu." + enemy.ChampionName + "." + interruptibleSpell.SpellSlot + ".Enabled", new CheckBox("Enabled"));

                    InterruptibleSpellsFound++;
                }
            }
        }

        public static void BuildAntiGapcloserMenu()
        {
            if (!EntityManager.Heroes.Enemies.Any(x => Gapcloser.GapCloserList.Exists(e => e.ChampName == x.ChampionName)))
            {
                return;
            }

            GapcloserMenu = Menu.AddSubMenu("Anti-Gapcloser");
            GapcloserMenu.AddGroupLabel("Global settings");
            GapcloserMenu.Add("MenuManager.GapcloserMenu.Enabled", new CheckBox("Anti-Gapcloser Enabled"));
            GapcloserMenu.Add("MenuManager.GapcloserMenu.OnlyInCombo", new CheckBox("Active only in Combo mode", false));
            GapcloserMenu.AddSeparator(15);

            foreach (var enemy in
                EntityManager.Heroes.Enemies.Where(x => Gapcloser.GapCloserList.Exists(e => e.ChampName == x.ChampionName)))
            {
                var gapclosers = Gapcloser.GapCloserList.FindAll(e => e.ChampName == enemy.ChampionName);

                if (gapclosers.Count <= 0)
                    continue;

                GapcloserMenu.AddGroupLabel(enemy.ChampionName);

                foreach (var gapcloser in gapclosers)
                {
                    GapcloserMenu.AddLabel("[" + gapcloser.SpellSlot + "] " + gapcloser.SpellName);
                    GapcloserMenu.Add("MenuManager.GapcloserMenu." + enemy.ChampionName + "." + gapcloser.SpellSlot + ".Delay", new Slider("Delay", 0, 0, 500));
                    GapcloserMenu.Add("MenuManager.GapcloserMenu." + enemy.ChampionName + "." + gapcloser.SpellSlot + ".Hp", new Slider("Only if I'm below under {0} % of my HP", 100));
                    GapcloserMenu.Add("MenuManager.GapcloserMenu." + enemy.ChampionName + "." + gapcloser.SpellSlot + ".Enemies", new Slider("Only if {0} or less enemies are near", 5, 1, 5));
                    GapcloserMenu.Add("MenuManager.GapcloserMenu." + enemy.ChampionName + "." + gapcloser.SpellSlot + ".Enabled", new CheckBox("Enabled"));

                    GapclosersFound++;
                }
            }
        }


        //    public static T Get<T>(Menu type, string uniqueIdentifier, ItemTypes itemtype, bool getSelectedText = false)
        //    {
        //        if (type[uniqueIdentifier] == null)
        //        {
        //            Logger.Error("[Error] Menu item : " + uniqueIdentifier + " doesn't exists.");
        //            return (T)Convert.ChangeType(false, typeof(T));
        //        }
        //        switch (itemtype)
        //        {
        //            case ItemTypes.ComboBox:
        //                if (getSelectedText)
        //                    return (T)Convert.ChangeType(type[uniqueIdentifier].Cast<ComboBox>().SelectedText, typeof(T));

        //                return (T)Convert.ChangeType(type[uniqueIdentifier].Cast<ComboBox>().CurrentValue, typeof(T));
        //            case ItemTypes.CheckBox:
        //                return (T)Convert.ChangeType(type[uniqueIdentifier].Cast<CheckBox>().CurrentValue, typeof(T));
        //            case ItemTypes.KeyBind:
        //                return (T)Convert.ChangeType(type[uniqueIdentifier].Cast<KeyBind>().CurrentValue, typeof(T));
        //            case ItemTypes.Slider:
        //                return (T)Convert.ChangeType(type[uniqueIdentifier].Cast<Slider>().CurrentValue, typeof(T));
        //            default:
        //                throw new ArgumentOutOfRangeException(nameof(itemtype), itemtype, null);
        //        }
        //    }

        //    public static void Set<T>(Menu type, string uniqueIdentifier, ItemTypes itemtype, T value,
        //        Tuple<uint, uint> keys = null)
        //    {
        //        if (type[uniqueIdentifier] == null)
        //        {
        //            Logger.Error("[Error] Menu item : " + uniqueIdentifier + " doesn't exists.");
        //            return;
        //        }
        //        switch (itemtype)
        //        {
        //            case ItemTypes.ComboBox:
        //                type[uniqueIdentifier].Cast<ComboBox>().CurrentValue = Convert.ToInt32(value);
        //                break;
        //            case ItemTypes.CheckBox:
        //                type[uniqueIdentifier].Cast<CheckBox>().CurrentValue = Convert.ToBoolean(value);
        //                break;
        //            case ItemTypes.KeyBind:
        //                if (keys != null)
        //                    type[uniqueIdentifier].Cast<KeyBind>().Keys = keys;
        //                else
        //                    type[uniqueIdentifier].Cast<KeyBind>().CurrentValue = Convert.ToBoolean(value);
        //                break;
        //            case ItemTypes.Slider:
        //                type[uniqueIdentifier].Cast<Slider>().CurrentValue = Convert.ToInt32(value);
        //                break;
        //            default:
        //                throw new ArgumentOutOfRangeException(nameof(itemtype), itemtype, null);
        //        }
        //    }
    }
}