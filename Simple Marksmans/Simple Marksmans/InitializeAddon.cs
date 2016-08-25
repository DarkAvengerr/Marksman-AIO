#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="InitializeAddon.cs" company="EloBuddy">
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
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using SharpDX;
using Simple_Marksmans.Interfaces;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans
{
    internal static class InitializeAddon
    {
        internal static IHeroAddon PluginInstance { get; private set; }

        private static readonly Dictionary<InterrupterEventArgs, AIHeroClient> InterruptibleSpellsFound = new Dictionary<InterrupterEventArgs, AIHeroClient>(); 

        public static bool Initialize()
        {
            LoadPlugin();

            if (PluginInstance == null)
            {
                Misc.PrintInfoMessage("<b><font color=\"#5ED43D\">" + Player.Instance.ChampionName + "</font></b> is not yet supported.");
                return false;
            }
            
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            return true;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe || Player.Instance.IsDead)
                return;

            var enemy = sender as AIHeroClient;

            if (enemy == null || !enemy.IsEnemy)
                return;

            var menu = MenuManager.MenuValues;

            if (MenuManager.InterruptibleSpellsFound > 0 && menu["MenuManager.InterrupterMenu.Enabled"])
            {
                if (Utils.Interrupter.InterruptibleList.Exists(e => e.ChampionName == enemy.ChampionName) && ((menu["MenuManager.InterrupterMenu.OnlyInCombo"] && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) || !menu["MenuManager.InterrupterMenu.OnlyInCombo"]))
                {
                    foreach (var interruptibleSpell in 
                            Utils.Interrupter.InterruptibleList.Where(x => x.ChampionName == enemy.ChampionName && x.SpellSlot == args.Slot))
                    {
                        var hp = menu["MenuManager.InterrupterMenu." + enemy.ChampionName + "." + interruptibleSpell.SpellSlot + ".Hp", true];
                        var enemies =menu["MenuManager.InterrupterMenu." + enemy.ChampionName + "." + interruptibleSpell.SpellSlot +".Enemies", true];

                        if (menu["MenuManager.InterrupterMenu." + enemy.ChampionName + "." + interruptibleSpell.SpellSlot +".Enabled"] &&
                            Player.Instance.HealthPercent <= hp &&
                            Player.Instance.CountEnemiesInRange(MenuManager.GapcloserScanRange) <= enemies)
                        {
                            InterruptibleSpellsFound.Add(new InterrupterEventArgs(args.Target, args.Slot, interruptibleSpell.DangerLevel, interruptibleSpell.SpellName, args.Start, args.End,
                                menu["MenuManager.InterrupterMenu." + enemy.ChampionName + "." + interruptibleSpell.SpellSlot + ".Delay",true],
                                enemies, hp, Game.Time * 1000), enemy);
                        }
                    }
                }
            }

            if (MenuManager.GapclosersFound == 0)
                return;

            if (!menu["MenuManager.GapcloserMenu.Enabled"] ||
                (menu["MenuManager.GapcloserMenu.OnlyInCombo"] && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) || !Gapcloser.GapCloserList.Exists(e => e.ChampName == enemy.ChampionName))
                return;

            foreach (
                var gapcloser in
                    Gapcloser.GapCloserList.Where(x => x.ChampName == enemy.ChampionName && x.SpellSlot == args.Slot))
            {
                var hp =
                    menu["MenuManager.GapcloserMenu." + enemy.ChampionName + "." + gapcloser.SpellSlot + ".Hp", true];
                var enemies =
                    menu[
                        "MenuManager.GapcloserMenu." + enemy.ChampionName + "." + gapcloser.SpellSlot + ".Enemies", true
                        ];

                if (menu["MenuManager.GapcloserMenu." + enemy.ChampionName + "." + gapcloser.SpellSlot + ".Enabled"] &&
                    Player.Instance.HealthPercent <= hp &&
                    Player.Instance.CountEnemiesInRange(MenuManager.GapcloserScanRange) <= enemies)
                {
                    PluginInstance.OnGapcloser(enemy,
                        new GapCloserEventArgs(args.Target, args.Slot,
                            args.Target == null ? GapcloserTypes.Skillshot : GapcloserTypes.Targeted,
                            args.Start, args.End,
                            menu[
                                "MenuManager.GapcloserMenu." + enemy.ChampionName + "." + gapcloser.SpellSlot + ".Delay",
                                true], enemies, hp, Game.Time*1000));

                }
            }
        }

        public static void LoadPlugin()
        {
            var typeName = "Simple_Marksmans.Plugins." + Player.Instance.ChampionName + "." + Player.Instance.ChampionName;

            var type = Type.GetType(typeName);

            if (type == null)
                return;

            Console.WriteLine("[DEBUG] Getting saved colorpicker data");
            var colorFileContent = FileHandler.ReadDataFile(FileHandler.ColorFileName);

            Bootstrap.SavedColorPickerData = colorFileContent != null ? colorFileContent.ToObject<Dictionary<string, ColorBGRA>>() : new Dictionary<string, ColorBGRA>();

            //var constructorInfo = type.GetConstructor(new Type[] {});

            //_plugin = (IHeroAddon) constructorInfo?.Invoke(new object[] {});

            Console.WriteLine("[DEBUG] Creating activators instance");
            PluginInstance = (IHeroAddon)System.Activator.CreateInstance(type);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            PluginInstance.OnDraw();
        }

        private static void Game_OnTick(EventArgs args)
        {
            foreach (var index in InterruptibleSpellsFound.Where(e=>(int)e.Key.GameTime + 9000 <= (int)Game.Time * 1000 || (!e.Value.Spellbook.IsChanneling && !e.Value.Spellbook.IsCharging && !e.Value.Spellbook.IsCastingSpell)).ToList())
            {
                InterruptibleSpellsFound.Remove(index.Key);
            }

            foreach (var interruptibleSpell in InterruptibleSpellsFound)
            {
                PluginInstance.OnInterruptible(interruptibleSpell.Value, interruptibleSpell.Key);
            }
            
            PluginInstance.PermaActive();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                PluginInstance.ComboMode();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                PluginInstance.HarassMode();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                PluginInstance.JungleClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                PluginInstance.LaneClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                PluginInstance.Flee();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                PluginInstance.LastHit();
            }
        }
    }
}