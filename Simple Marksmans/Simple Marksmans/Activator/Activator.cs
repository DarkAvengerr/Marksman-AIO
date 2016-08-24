#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Activator.cs" company="EloBuddy">
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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Simple_Marksmans.Activator.Items;
using Simple_Marksmans.Interfaces;
using Simple_Marksmans.Utils;
using Simple_Marksmans.Utils.PermaShow;

namespace Simple_Marksmans.Activator
{
    internal class Activator
    {
        private static int _lastScanTick;

        public static Menu ActivatorMenu { get; set; }

        public static Menu PotionsAndElixirsMenu { get; set; }
        public static Menu ItemsMenu { get; set; }
        public static Menu CleanseMenu { get; set; }
        public static ItemsCollection Items { get; set; } = new ItemsCollection();

        private static readonly Dictionary<Func<ItemIds, bool>, Action> ObjectInitializer = new Dictionary
            <Func<ItemIds, bool>, Action>
        {
            {itemId => itemId == ItemIds.HealthPotion || itemId == ItemIds.Biscuit, () => Items[ItemsEnum.HealthPotion] = new HealthPotion()},
            {itemId => itemId == ItemIds.RefillablePotion, () => Items[ItemsEnum.RefillablePotion] = new RefillablePotion()},
            {itemId => itemId == ItemIds.HuntersPotion, () => Items[ItemsEnum.HuntersPotion] = new HuntersPotion()},
            {itemId => itemId == ItemIds.CorruptingPotion, () => Items[ItemsEnum.CorruptingPotion] = new CorruptingPotion()},
            {itemId => itemId == ItemIds.ElixirofIron, () => Items[ItemsEnum.ElixirofIron] = new ElixirofIron()},
            {itemId => itemId == ItemIds.ElixirofSorcery, () => Items[ItemsEnum.ElixirofSorcery] = new ElixirofSorcery()},
            {itemId => itemId == ItemIds.ElixirofWrath, () => Items[ItemsEnum.ElixirofWrath] = new ElixirofWrath()},
            {itemId => itemId == ItemIds.Scimitar, () => Items[ItemsEnum.Scimitar] = new Scimitar()},
            {itemId => itemId == ItemIds.Quicksilver, () => Items[ItemsEnum.Quicksilver] = new Quicksilver()},
            {itemId => itemId == ItemIds.Ghostblade, () => Items[ItemsEnum.Ghostblade] = new Ghostblade()},
            {itemId => itemId == ItemIds.Cutlass, () => Items[ItemsEnum.Cutlass] = new Cutlass()},
            {itemId => itemId == ItemIds.Gunblade, () => Items[ItemsEnum.Gunblade] = new Gunblade()},
            {itemId => itemId == ItemIds.BladeOfTheRuinedKing, () => Items[ItemsEnum.BladeOfTheRuinedKing] = new Botrk()}
        };

        private static readonly Dictionary<Func<ItemIds, bool>, Action> ObjectDestroyer = new Dictionary
            <Func<ItemIds, bool>, Action>
        {
            {itemId => itemId == ItemIds.HealthPotion, () => Items[ItemsEnum.HealthPotion]  = null},
            {itemId => itemId == ItemIds.RefillablePotion, () => Items[ItemsEnum.RefillablePotion] = null},
            {itemId => itemId == ItemIds.HuntersPotion, () => Items[ItemsEnum.HuntersPotion] = null},
            {itemId => itemId == ItemIds.CorruptingPotion, () => Items[ItemsEnum.CorruptingPotion] = null},
            {itemId => itemId == ItemIds.ElixirofIron, () => Items[ItemsEnum.ElixirofIron] = null},
            {itemId => itemId == ItemIds.ElixirofSorcery, () => Items[ItemsEnum.ElixirofSorcery] = null},
            {itemId => itemId == ItemIds.ElixirofWrath, () => Items[ItemsEnum.ElixirofWrath] = null},
            {itemId => itemId == ItemIds.Scimitar, () =>Items[ItemsEnum.Scimitar]  = null},
            {itemId => itemId == ItemIds.Quicksilver, () => Items[ItemsEnum.Quicksilver] = null},
            {itemId => itemId == ItemIds.Ghostblade, () =>Items[ItemsEnum.Ghostblade]  = null},
            {itemId => itemId == ItemIds.Cutlass, () => Items[ItemsEnum.Cutlass] = null},
            {itemId => itemId == ItemIds.Gunblade, () => Items[ItemsEnum.Gunblade] = null},
            {itemId => itemId == ItemIds.BladeOfTheRuinedKing, () => Items[ItemsEnum.BladeOfTheRuinedKing] = null}
        };

        public static void InitializeActivator()
        {
            ActivatorMenu = MainMenu.AddMenu("Marksman AIO : Activator", "MarksmanAIOActivator");
            ActivatorMenu.AddGroupLabel("Activator settings : ");
            ActivatorMenu.Add("Activator.Enable", new CheckBox("Enable activator"));
            ScanForItems();
            InitializeMenu();

            Shop.OnBuyItem += Shop_OnBuyItem;
            Shop.OnSellItem += Shop_OnSellItem;
            Game.OnTick += Game_OnTick;
            Obj_AI_Base.OnBuffGain += Obj_AI_Base_OnBuffGain;
            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            AttackableUnit.OnDamage += Obj_AI_Base_OnDamage;
        }
        


        private static void Obj_AI_Base_OnBuffGain(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            if (!sender.IsMe || !MenuManager.MenuValues["Activator.Enable"])
                return;
            
            var menu = MenuManager.MenuValues;
            var buffType = args.Buff.Type == BuffType.Flee ? BuffType.Fear : args.Buff.Type;
            var scimitar = menu["Activator.CleanseMenu.Scimitar"];
            var qss = menu["Activator.CleanseMenu.Quicksilver"];
            var onlyInCombo = menu["Activator.CleanseMenu.OnlyInCombo"];
            var buffDuration = menu["Activator.CleanseMenu.BuffDuration", true]*50;
            var minDelay = menu["Activator.CleanseMenu.MinimumDelay", true];
            var maxDelay = menu["Activator.CleanseMenu.MaximumDelay", true];
            var hpPercentage = menu["Activator.CleanseMenu.QssHP", true];
            IItem item;

            if (CleanseMenu["Activator.CleanseMenu." + buffType] == null)
                return;

            if (scimitar && Items[ItemsEnum.Scimitar] != null && Items[ItemsEnum.Scimitar].ToItem().IsReady())
                item = Items[ItemsEnum.Scimitar];
            else if (qss && Items[ItemsEnum.Quicksilver] != null && Items[ItemsEnum.Quicksilver].ToItem().IsReady())
                item = Items[ItemsEnum.Quicksilver];
            else
                return;
            
            if (!menu["Activator.CleanseMenu." + buffType] ||
                !((args.Buff.EndTime - args.Buff.StartTime)*1000 >= buffDuration) ||
                !(Player.Instance.HealthPercent <= hpPercentage))
                return;

            var random = new Random();

            if (onlyInCombo && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Core.DelayAction(() =>
                {
                    item.UseItem();
                    Misc.PrintInfoMessage("Using <font color=\"#2ED139\">" + item.ItemName +
                                          "</font> to cleanse <font color=\"#D1492E\">" + buffType + "</font>");
                }, random.Next(minDelay, maxDelay));
            else if(!onlyInCombo)
                Core.DelayAction(() =>
                {
                    item.UseItem();
                    Misc.PrintInfoMessage("Using <font color=\"#2ED139\">" + item.ItemName +
                                          "</font> to cleanse <font color=\"#D1492E\">" + buffType + "</font>");
                }, random.Next(minDelay, maxDelay));
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (!MenuManager.MenuValues["Activator.Enable"])
                return;

            if (Game.Time*1000 > _lastScanTick + 10000)
            {
                ScanForItems();
            }
            
            foreach (var enumValues in Enum.GetValues(typeof(ItemsEnum)).Cast<ItemsEnum>())
            {
                if (MenuManager.MenuValues["Activator.PotionsAndElixirsMenu.UsePotions"] && Items[enumValues] != null && Items[enumValues].ItemType == ItemType.Potion &&
                    MenuManager.MenuValues["Activator.PotionsAndElixirsMenu." + Items[enumValues].ItemName])
                {
                    if (Player.Instance.IsRecalling() ||
                        Player.Instance.Position.CountEnemiesInRange(Player.Instance.GetAutoAttackRange() + 250) == 0)
                        return;

                    if (!MenuManager.MenuValues["Activator.PotionsAndElixirsMenu.OnlyIfTakingDamage"] && (int) Player.Instance.HealthPercent <=
                        MenuManager.MenuValues["Activator.PotionsAndElixirsMenu.BelowHealth", true] &&
                        !Items[enumValues].ItemName.Contains("Elixir") && Items[enumValues].ToItem().IsReady() && !Player.Instance.IsUsingHealingPotion())
                    {
                        Items[enumValues].UseItem();
                    }
                }
                if (Items[enumValues] != null && Items[enumValues].ItemType != ItemType.Potion && Items[enumValues].ItemType != ItemType.Cleanse && MenuManager.MenuValues["Activator.ItemsMenu." + Items[enumValues].ItemName])
                {
                    var myMinHp = MenuManager.MenuValues["Activator.ItemsMenu." + Items[enumValues].ItemName + ".MyMinHP", true];
                    var targetsMinHp = MenuManager.MenuValues["Activator.ItemsMenu." + Items[enumValues].ItemName + ".TargetsMinHP", true];
                    var ifEnemiesNear = MenuManager.MenuValues["Activator.ItemsMenu." + Items[enumValues].ItemName + ".IfEnemiesNear", true];
                    var target = TargetSelector.GetTarget(Items[enumValues].Range, DamageType.Physical);

                    if (target == null || ((!MenuManager.MenuValues["Activator.ItemsMenu.OnlyInCombo"] || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)) && MenuManager.MenuValues["Activator.ItemsMenu.OnlyInCombo"]))
                        return;
                    
                    if ((int) Player.Instance.HealthPercent <= myMinHp && (int) target.HealthPercent <= targetsMinHp &&
                        Player.Instance.Position.CountEnemiesInRange(Player.Instance.GetAutoAttackRange() + 250) >=
                        ifEnemiesNear)
                    {
                        if (Items[enumValues].ToItem().IsReady() && Items[enumValues].Range > 0 &&
                            Items[enumValues].ToItem().IsInRange(target))
                        {
                            Items[enumValues].ToItem().Cast(target);
                        }
                        else if (Items[enumValues].ToItem().IsReady() && Items[enumValues].Range <= 0f)
                        {
                            switch (Items[enumValues].ItemTargettingType)
                            {
                                case ItemTargettingType.None:
                                    Items[enumValues].ToItem().Cast();
                                    break;
                                case ItemTargettingType.Unit:
                                    Items[enumValues].ToItem().Cast(target);
                                    break;
                                case ItemTargettingType.Self:
                                    Items[enumValues].ToItem().Cast(Player.Instance);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                }
            }
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!(target is AIHeroClient) || !MenuManager.MenuValues["Activator.Enable"])
                return;

            Items[ItemsEnum.ElixirofIron]?.UseItem();
            Items[ItemsEnum.ElixirofSorcery]?.UseItem();
            Items[ItemsEnum.ElixirofWrath]?.UseItem();
        }

        private static void Obj_AI_Base_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (!MenuManager.MenuValues["Activator.Enable"] || !MenuManager.MenuValues["Activator.PotionsAndElixirsMenu.OnlyIfTakingDamage"] || !args.Target.IsMe || (int) Player.Instance.HealthPercent >= MenuManager.MenuValues["Activator.PotionsAndElixirsMenu.BelowHealth", true] || Player.Instance.IsUsingHealingPotion())
                return;

            if (Items[ItemsEnum.HealthPotion] != null)
                Items[ItemsEnum.HealthPotion].UseItem();
            else if (Items[ItemsEnum.RefillablePotion] != null)
                Items[ItemsEnum.RefillablePotion].UseItem();
            else if (Items[ItemsEnum.HuntersPotion] != null)
                Items[ItemsEnum.HuntersPotion].UseItem();
            else
            {
                Items[ItemsEnum.CorruptingPotion]?.UseItem();
            }
        }

        private static void Shop_OnBuyItem(AIHeroClient sender, ShopActionEventArgs args)
        {
            if (!MenuManager.MenuValues["Activator.Enable"])
                return;

            LoadItem(args.Id);
        }

        private static void Shop_OnSellItem(AIHeroClient sender, ShopActionEventArgs args)
        {
            if (!MenuManager.MenuValues["Activator.Enable"])
                return;

            UnLoadItem(args.Id);
        }

        private static void ScanForItems()
        {
            _lastScanTick = (int) Game.Time*1000;

            foreach (var item in Enum.GetValues(typeof(ItemIds)).Cast<int>())
            {
                var myItem = new EloBuddy.SDK.Item(item);

                if (myItem.IsOwned())
                {
                    LoadItem(item);
                }
                else
                {
                    UnLoadItem(item);
                }
            }
        }

        private static void InitializeMenu()
        {
            PotionsAndElixirsMenu = ActivatorMenu.AddSubMenu("Potions and Elixirs");
            PotionsAndElixirsMenu.AddGroupLabel("Potions and Elixirs : ");
            PotionsAndElixirsMenu.AddLabel("Potions : ");
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.UsePotions", new CheckBox("Use Potions"));
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.OnlyIfTakingDamage", new CheckBox("Use Potions only if taking damage"));
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.BelowHealth", new Slider("Use potions if health is below {0}%", 35));
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.HealthPotion", new CheckBox("Use Health Potion"));
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.RefillablePotion", new CheckBox("Use Refillable Potion"));
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.HuntersPotion", new CheckBox("Use Hunter's Potion"));
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.CorruptingPotion", new CheckBox("Use Corrupting Potion"));
            PotionsAndElixirsMenu.AddSeparator(10);
            PotionsAndElixirsMenu.AddLabel("Elixirs : ");
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.ElixirofIron", new CheckBox("Use Elixir of Iron"));
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.ElixirofSorcery", new CheckBox("Use Elixir of Sorcery"));
            PotionsAndElixirsMenu.Add("Activator.PotionsAndElixirsMenu.ElixirofWrath", new CheckBox("Use Elixir of Wrath"));
            ItemsMenu = ActivatorMenu.AddSubMenu("Items");
            ItemsMenu.AddGroupLabel("Items : ");
            ItemsMenu.AddLabel("Bilgewater Cutlass : ");
            ItemsMenu.Add("Activator.ItemsMenu.Cutlass", new CheckBox("Use Bilgewater Cutlass"));
            ItemsMenu.Add("Activator.ItemsMenu.Cutlass.MyMinHP", new Slider("Only if my health is below {0}%", 100));
            ItemsMenu.Add("Activator.ItemsMenu.Cutlass.TargetsMinHP", new Slider("Only if target's health is below {0}%", 100));
            ItemsMenu.Add("Activator.ItemsMenu.Cutlass.IfEnemiesNear", new Slider("Only if {0} or more enemies are near", 1, 1, 5));
            ItemsMenu.AddSeparator(10);
            ItemsMenu.AddLabel("Blade of the Ruined King : ");
            ItemsMenu.Add("Activator.ItemsMenu.BladeOfTheRuinedKing", new CheckBox("Use Blade of the Ruined King"));
            ItemsMenu.Add("Activator.ItemsMenu.BladeOfTheRuinedKing.MyMinHP", new Slider("Only if my health is below {0}%", 100));
            ItemsMenu.Add("Activator.ItemsMenu.BladeOfTheRuinedKing.TargetsMinHP", new Slider("Only if target's health is below {0}%", 100));
            ItemsMenu.Add("Activator.ItemsMenu.BladeOfTheRuinedKing.IfEnemiesNear", new Slider("Only if {0} or more enemies are near", 1, 1, 5));
            ItemsMenu.AddSeparator(10);
            ItemsMenu.AddLabel("Hextech Gunblade : ");
            ItemsMenu.Add("Activator.ItemsMenu.Gunblade", new CheckBox("Use Hextech Gunblade"));
            ItemsMenu.Add("Activator.ItemsMenu.Gunblade.MyMinHP", new Slider("Only if my health is below {0}%", 100));
            ItemsMenu.Add("Activator.ItemsMenu.Gunblade.TargetsMinHP", new Slider("Only if target's health is below {0}%", 100));
            ItemsMenu.Add("Activator.ItemsMenu.Gunblade.IfEnemiesNear", new Slider("Only if {0} or more enemies are near", 1, 1, 5));
            ItemsMenu.AddSeparator(10);
            ItemsMenu.AddLabel("Youmuu's Ghostblade : ");
            ItemsMenu.Add("Activator.ItemsMenu.Ghostblade", new CheckBox("Use Youmuu's Ghostblade"));
            ItemsMenu.Add("Activator.ItemsMenu.Ghostblade.MyMinHP", new Slider("Only if my health is below {0}%", 100));
            ItemsMenu.Add("Activator.ItemsMenu.Ghostblade.TargetsMinHP", new Slider("Only if target's health is below {0}%", 100));
            ItemsMenu.Add("Activator.ItemsMenu.Ghostblade.IfEnemiesNear", new Slider("Only if {0} or more enemies are near", 1, 1, 5));
            ItemsMenu.AddSeparator(10);
            ItemsMenu.AddLabel("Misc settings : ");
            ItemsMenu.Add("Activator.ItemsMenu.OnlyInCombo", new CheckBox("Use items only in combo mode"));
            CleanseMenu = ActivatorMenu.AddSubMenu("Cleanse");
            CleanseMenu.AddLabel("Cleanse items : ");
            CleanseMenu.Add("Activator.CleanseMenu.Scimitar", new CheckBox("Use Scimitar"));
            CleanseMenu.Add("Activator.CleanseMenu.Quicksilver", new CheckBox("Use Quicksilver"));
            CleanseMenu.AddSeparator(10);
            CleanseMenu.AddLabel("Crowd control settings : ");
            CleanseMenu.Add("Activator.CleanseMenu.Blind", new CheckBox("Cleanse Blind"));
            CleanseMenu.Add("Activator.CleanseMenu.Charm", new CheckBox("Cleanse Charm"));
            CleanseMenu.Add("Activator.CleanseMenu.Fear", new CheckBox("Cleanse Fear"));
            CleanseMenu.Add("Activator.CleanseMenu.Polymorph", new CheckBox("Cleanse Polymorph"));
            CleanseMenu.Add("Activator.CleanseMenu.Silence", new CheckBox("Cleanse Silence"));
            CleanseMenu.Add("Activator.CleanseMenu.Slow", new CheckBox("Cleanse Slow", false));
            CleanseMenu.Add("Activator.CleanseMenu.Snare", new CheckBox("Cleanse Snare"));
            CleanseMenu.Add("Activator.CleanseMenu.Stun", new CheckBox("Cleanse Stun"));
            CleanseMenu.Add("Activator.CleanseMenu.Suppression", new CheckBox("Cleanse Suppression"));
            CleanseMenu.Add("Activator.CleanseMenu.Taunt", new CheckBox("Cleanse Taunt"));
            CleanseMenu.AddSeparator(10);
            CleanseMenu.AddLabel("Misc settings : ");
            CleanseMenu.Add("Activator.CleanseMenu.OnlyInCombo", new CheckBox("Cleanse only in Combo"));
            CleanseMenu.Add("Activator.CleanseMenu.QssHP", new Slider("Cleanse only if my health is below {0}%", 100));

            var buffDuration = CleanseMenu.Add("Activator.CleanseMenu.BuffDuration", new Slider(" ", 15));
            buffDuration.DisplayName = "Cleanse only if buff duration is longer than " + buffDuration.CurrentValue*50 + " milliseconds";
            buffDuration.OnValueChange += delegate(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args) { sender.DisplayName = "Cleanse only if buff duration is higher than " + args.NewValue*50 + " milliseconds"; };

            CleanseMenu.Add("Activator.CleanseMenu.MinimumDelay", new Slider("Minimum delay", 0, 0, 500));
            CleanseMenu.Add("Activator.CleanseMenu.MaximumDelay", new Slider("Maximum delay", 350, 0, 500));
        }

        public static void LoadItem(int id)
        {
            if (!Enum.IsDefined(typeof(ItemIds), id))
                return;

            var output = ObjectInitializer.FirstOrDefault(comparer => comparer.Key((ItemIds) id)).Value;

            output?.Invoke();
        }

        public static void UnLoadItem(int id)
        {
            if (!Enum.IsDefined(typeof(ItemIds), id))
                return;

            var output = ObjectDestroyer.FirstOrDefault(comparer => comparer.Key((ItemIds) id)).Value;

            output?.Invoke();
        }
    }
}