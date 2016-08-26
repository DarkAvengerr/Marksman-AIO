#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Corki.cs" company="EloBuddy">
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
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Utils;
using SharpDX;
using Simple_Marksmans.Utils;
using Simple_Marksmans.Utils.PermaShow;
using Color = System.Drawing.Color;

namespace Simple_Marksmans.Plugins.Corki
{
    internal class Corki : ChampionPlugin
    {
        public static Spell.Skillshot Q { get; }
        public static Spell.Skillshot W { get; }
        public static Spell.Active E { get; }
        public static Spell.Skillshot R { get; }

        public static int[] QMana { get; } = {0, 60, 70, 80, 90, 100};
        public static int WMana { get; } = 100;
        public static int EMana { get; } = 50;
        public static int RMana { get; } = 20;

        private static Menu ComboMenu { get; set; }
        private static Menu HarassMenu { get; set; }
        private static Menu JungleClearMenu { get; set; }
        private static Menu LaneClearMenu { get; set; }
        private static Menu MiscMenu { get; set; }
        private static Menu DrawingsMenu { get; set; }

        private static bool _changingRangeScan;
        
        private static readonly ColorPicker[] ColorPicker;
        public static PermaShow PermaShow;
        public static BoolItemData AutoHarassPermaShowItem;

        public static BuffInstance GetRBigMissileBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "corkimissilebarragecounterbig");

        public static bool HasBigRMissile
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "corkimissilebarragecounterbig");

        public static BuffInstance GetRNormalMissileBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "corkimissilebarragecounternormal");

        public static bool HasNormalRMissile
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "corkimissilebarragecounternormal");

        public static bool HasPackagesBuff
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "corkiloaded");

        public static BuffInstance GetPackagesBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "corkiloaded");

        public static bool HasSheenBuff
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "sheen");

        public static BuffInstance GetSheenBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "sheen");


        static Corki()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 825, SkillShotType.Circular, 300, 1000, 250)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Skillshot(SpellSlot.W, 600, SkillShotType.Linear, 250, 650, 120)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Active(SpellSlot.E, 1000);
            R = new Spell.Skillshot(SpellSlot.R, 1225, SkillShotType.Linear, 250, 1950, 50)
            {
                AllowedCollisionCount = 0
            };

            ColorPicker = new ColorPicker[4];

            ColorPicker[0] = new ColorPicker("CorkiQ", new ColorBGRA(243, 109, 160, 255));
            ColorPicker[1] = new ColorPicker("CorkiW", new ColorBGRA(255, 210, 54, 255));
            ColorPicker[2] = new ColorPicker("CorkiR", new ColorBGRA(1, 109, 160, 255));
            ColorPicker[3] = new ColorPicker("CorkiHpBar", new ColorBGRA(255, 134, 0, 255));
            
            PermaShow = new PermaShow("Corki PermaShow", new Vector2(200, 200));
            DamageIndicator.Initalize(Color.FromArgb(ColorPicker[3].Color.R, ColorPicker[3].Color.G, ColorPicker[3].Color.B), 1300);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[3].OnColorChange += (sender, args) =>
            {
                DamageIndicator.Color = Color.FromArgb(args.Color.R, args.Color.G,
                    args.Color.B);
            };
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawDamageIndicator)
                return 0;

            var enemy = (AIHeroClient) unit;
            return enemy != null ? Damage.GetComboDamage(enemy) : 0f;
        }

        protected override void OnDraw()
        {
            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawW && !HasPackagesBuff &&
                (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[1].Color, W.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[2].Color, R.Range, Player.Instance);

            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Corki.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);
            
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Corki addon");

            ComboMenu.AddLabel("Phosphorus Bomb (Q) settings :");
            ComboMenu.Add("Plugins.Corki.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Gatling Gun (E) settings :");
            ComboMenu.Add("Plugins.Corki.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Valkyrie (W) settings :");
            ComboMenu.Add("Plugins.Corki.ComboMenu.UseW", new CheckBox("Use W", false));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Missile Barrage (R) settings :");
            ComboMenu.Add("Plugins.Corki.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Corki.ComboMenu.MinStacksForR", new Slider("Minimum stacks to use R", 0, 0, 7));
            ComboMenu.AddSeparator(1);
            ComboMenu.Add("Plugins.Corki.ComboMenu.RAllowCollision", new CheckBox("Allow collision on minions", false));
            ComboMenu.AddLabel("Allow collision on minions if damage will be applied on enemy champion.");

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Corki addon");

            HarassMenu.AddLabel("Phosphorus Bomb (Q) settings :");
            HarassMenu.Add("Plugins.Corki.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.Corki.HarassMenu.MinManaToUseQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            HarassMenu.AddSeparator(5);
            
            HarassMenu.AddLabel("Gatling Gun (E) settings :");
            HarassMenu.Add("Plugins.Corki.HarassMenu.UseE", new CheckBox("Use E"));
            HarassMenu.Add("Plugins.Corki.HarassMenu.MinManaToUseE", new Slider("Min mana percentage ({0}%) to use E", 50, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Missile Barrage (R) settings :");
            HarassMenu.Add("Plugins.Corki.HarassMenu.UseR", new CheckBox("Use R"));
            HarassMenu.Add("Plugins.Corki.HarassMenu.MinManaToUseR", new Slider("Min mana percentage ({0}%) to use R", 50, 1));
            HarassMenu.Add("Plugins.Corki.HarassMenu.MinStacksToUseR", new Slider("Minimum stacks to use R", 3, 0, 7));
            HarassMenu.AddSeparator(1);
            HarassMenu.Add("Plugins.Corki.HarassMenu.RAllowCollision", new CheckBox("Allow collision on minions"));
            HarassMenu.AddLabel("Allow collision on minions if damage will be applied on enemy champion.");

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Lane clear");
            LaneClearMenu.AddGroupLabel("Lane clear mode settings for Corki addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
            scanRange.OnValueChange += (a, b) =>
            {
                _changingRangeScan = true;
                Core.DelayAction(() =>
                {
                    if (!scanRange.IsLeftMouseDown && !scanRange.IsMouseInside)
                    {
                        _changingRangeScan = false;
                    }
                }, 2000);
            };
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Phosphorus Bomb (Q) settings :");
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.UseQ", new CheckBox("Use Q"));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinMinionsKilledToUseQ", new Slider("Min minions killed to use Q", 2, 1, 6));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinManaToUseQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Gatling Gun (E) settings :");
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.UseE", new CheckBox("Use E", false));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinManaToUseE", new Slider("Min mana percentage ({0}%) to use E", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Missile Barrage (R) settings :");
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.UseR", new CheckBox("Use R"));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinManaToUseR", new Slider("Min mana percentage ({0}%) to use R", 50, 1));
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.MinStacksToUseR", new Slider("Minimum stacks to use R", 6, 0, 7));
            LaneClearMenu.AddSeparator(1);
            LaneClearMenu.Add("Plugins.Corki.LaneClearMenu.RAllowCollision", new CheckBox("Allow collision on minions"));
            LaneClearMenu.AddLabel("Allow collision on minions if damage will be applied on other minions.");

            JungleClearMenu = MenuManager.Menu.AddSubMenu("Jungle clear");
            JungleClearMenu.AddGroupLabel("Jungle clear mode settings for Corki addon");

            JungleClearMenu.AddLabel("Phosphorus Bomb (Q) settings :");
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.UseQ", new CheckBox("Use Q"));
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.MinManaToUseQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            JungleClearMenu.AddSeparator(5);

            JungleClearMenu.AddLabel("Gatling Gun (E) settings :");
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.UseE", new CheckBox("Use E", false));
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.MinManaToUseE", new Slider("Min mana percentage ({0}%) to use E", 50, 1));
            JungleClearMenu.AddSeparator(5);

            JungleClearMenu.AddLabel("Missile Barrage (R) settings :");
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.UseR", new CheckBox("Use R"));
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.MinManaToUseR", new Slider("Min mana percentage ({0}%) to use R", 50, 1));
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.MinStacksToUseR", new Slider("Minimum stacks to use R", 5, 0, 7));
            JungleClearMenu.AddSeparator(1);
            JungleClearMenu.Add("Plugins.Corki.JungleClearMenu.RAllowCollision", new CheckBox("Allow collision on minions"));
            JungleClearMenu.AddLabel("Allow collision on minions if damage will be applied on other minions.");

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Corki addon");
            MiscMenu.AddLabel("Auto harass settings : ");
            MiscMenu.Add("Plugins.Corki.MiscMenu.AutoHarassEnabled", new KeyBind("Enable auto harass", true, KeyBind.BindTypes.PressToggle, 'T')).OnValueChange +=
                (a, b) =>
                {
                    if (AutoHarassPermaShowItem != null)
                    {
                        AutoHarassPermaShowItem.Value = b.NewValue;
                    }
                };
            MiscMenu.Add("Plugins.Corki.MiscMenu.UseBigBomb", new CheckBox("Use big bomb", false));
            MiscMenu.Add("Plugins.Corki.MiscMenu.MinStacksToUseR", new Slider("Minimum stacks to use R", 3, 0, 7));
            MiscMenu.AddSeparator(5);
            MiscMenu.AddLabel("Auto harass enabled for : ");

            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                MiscMenu.Add("Plugins.Corki.MiscMenu.AutoHarassEnabled."+enemy.Hero, new CheckBox(enemy.Hero.ToString()));
            }

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Corki addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Phosphorus Bomb (Q) drawing settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Valkyrie (W) drawing settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawW", new CheckBox("Draw W range", false));
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawWColor", new CheckBox("Change color", false)).OnValueChange += (a, b) => 
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Missile Barrage (R) drawing settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Damage indicator drawing settings :");
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawDamageIndicator",
                new CheckBox("Draw damage indicator on enemy HP bars"));
            DrawingsMenu.Add("Plugins.Corki.DrawingsMenu.DrawDamageIndicatorColor",
                new CheckBox("Change color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[3].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            
            AutoHarassPermaShowItem = PermaShow.AddItem("Auto Harass", new BoolItemData("Auto Harass", Settings.Misc.AutoHarassEnabled, 14));
        }
        
        public static List<T> GetCollisionObjects<T>(Obj_AI_Base unit) where T : Obj_AI_Base
        {
            try
            {
                var minions =
                    EntityManager.MinionsAndMonsters.CombinedAttackable.Where(
                        obj => obj.Position.Distance(unit) < (HasBigRMissile ? 280 : 130)).ToList();
                var enemies =
                    EntityManager.Heroes.Enemies.Where(
                        obj => obj.Position.Distance(unit) < (HasBigRMissile ? 280 : 130)).ToList();

                if (typeof(T) == typeof(Obj_AI_Base))
                {
                    return (List<T>)Convert.ChangeType(minions.Cast<Obj_AI_Base>().Concat(enemies).ToList(), typeof(List<T>));
                }
                if (typeof (T) == typeof (AIHeroClient))
                {
                    return (List<T>) Convert.ChangeType(enemies, typeof (List<T>));
                }
                if (typeof (T) == typeof (Obj_AI_Minion))
                {
                    return (List<T>) Convert.ChangeType(minions, typeof (List<T>));
                }
                Logger.Error("Error at Corki.cs => GetCollisionObjects => Cannot cast to " + typeof(T));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
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

        internal static class Settings
        {
            internal static class Combo
            {
                public static bool UseQ
                {
                    get
                    {
                        return ComboMenu?["Plugins.Corki.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Corki.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Corki.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Corki.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Corki.ComboMenu.UseW"] != null &&
                               ComboMenu["Plugins.Corki.ComboMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Corki.ComboMenu.UseW"] != null)
                            ComboMenu["Plugins.Corki.ComboMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Corki.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Corki.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Corki.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Corki.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Corki.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Corki.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Corki.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Corki.ComboMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                public static bool RAllowCollision
                {
                    get
                    {
                        return ComboMenu?["Plugins.Corki.ComboMenu.RAllowCollision"] != null &&
                               ComboMenu["Plugins.Corki.ComboMenu.RAllowCollision"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Corki.ComboMenu.RAllowCollision"] != null)
                            ComboMenu["Plugins.Corki.ComboMenu.RAllowCollision"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinStacksForR
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Corki.ComboMenu.MinStacksForR"] != null)
                            return ComboMenu["Plugins.Corki.ComboMenu.MinStacksForR"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.ComboMenu.MinStacksForR menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Corki.ComboMenu.MinStacksForR"] != null)
                            ComboMenu["Plugins.Corki.ComboMenu.MinStacksForR"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Harass
            {
                public static bool UseQ
                {
                    get
                    {
                        return HarassMenu?["Plugins.Corki.HarassMenu.UseQ"] != null &&
                               HarassMenu["Plugins.Corki.HarassMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.UseQ"] != null)
                            HarassMenu["Plugins.Corki.HarassMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaToUseQ
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.MinManaToUseQ"] != null)
                            return HarassMenu["Plugins.Corki.HarassMenu.MinManaToUseQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.HarassMenu.MinManaToUseQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.MinManaToUseQ"] != null)
                            HarassMenu["Plugins.Corki.HarassMenu.MinManaToUseQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return HarassMenu?["Plugins.Corki.HarassMenu.UseE"] != null &&
                               HarassMenu["Plugins.Corki.HarassMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.UseE"] != null)
                            HarassMenu["Plugins.Corki.HarassMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaToUseE
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.MinManaToUseE"] != null)
                            return HarassMenu["Plugins.Corki.HarassMenu.MinManaToUseE"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.HarassMenu.MinManaToUseE menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.MinManaToUseE"] != null)
                            HarassMenu["Plugins.Corki.HarassMenu.MinManaToUseE"].Cast<Slider>().CurrentValue = value;
                    }
                }
                
                public static bool UseR
                {
                    get
                    {
                        return HarassMenu?["Plugins.Corki.HarassMenu.UseR"] != null &&
                               HarassMenu["Plugins.Corki.HarassMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.UseR"] != null)
                            HarassMenu["Plugins.Corki.HarassMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool RAllowCollision
                {
                    get
                    {
                        return HarassMenu?["Plugins.Corki.HarassMenu.RAllowCollision"] != null &&
                               HarassMenu["Plugins.Corki.HarassMenu.RAllowCollision"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.RAllowCollision"] != null)
                            HarassMenu["Plugins.Corki.HarassMenu.RAllowCollision"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaToUseR
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.MinManaToUseR"] != null)
                            return HarassMenu["Plugins.Corki.HarassMenu.MinManaToUseR"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.HarassMenu.MinManaToUseR menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.MinManaToUseR"] != null)
                            HarassMenu["Plugins.Corki.HarassMenu.MinManaToUseR"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int MinStacksToUseR
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.MinStacksToUseR"] != null)
                            return HarassMenu["Plugins.Corki.HarassMenu.MinStacksToUseR"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.HarassMenu.MinStacksToUseR menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Corki.HarassMenu.MinStacksToUseR"] != null)
                            HarassMenu["Plugins.Corki.HarassMenu.MinStacksToUseR"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Corki.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Corki.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Corki.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }
                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.AllowedEnemies"] != null)
                            return LaneClearMenu["Plugins.Corki.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseQ
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Corki.LaneClearMenu.UseQ"] != null &&
                               LaneClearMenu["Plugins.Corki.LaneClearMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.UseQ"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinMinionsKilledToUseQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinMinionsKilledToUseQ"] != null)
                            return LaneClearMenu["Plugins.Corki.LaneClearMenu.MinMinionsKilledToUseQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.LaneClearMenu.MinMinionsKilledToUseQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinMinionsKilledToUseQ"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.MinMinionsKilledToUseQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int MinManaToUseQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinManaToUseQ"] != null)
                            return LaneClearMenu["Plugins.Corki.LaneClearMenu.MinManaToUseQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.LaneClearMenu.MinManaToUseQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinManaToUseQ"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.MinManaToUseQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Corki.LaneClearMenu.UseE"] != null &&
                               LaneClearMenu["Plugins.Corki.LaneClearMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.UseE"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaToUseE
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinManaToUseE"] != null)
                            return LaneClearMenu["Plugins.Corki.LaneClearMenu.MinManaToUseE"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.LaneClearMenu.MinManaToUseE menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinManaToUseE"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.MinManaToUseE"].Cast<Slider>().CurrentValue = value;
                    }
                }
                
                public static bool UseR
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Corki.LaneClearMenu.UseR"] != null &&
                               LaneClearMenu["Plugins.Corki.LaneClearMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.UseR"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool RAllowCollision
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Corki.LaneClearMenu.RAllowCollision"] != null &&
                               LaneClearMenu["Plugins.Corki.LaneClearMenu.RAllowCollision"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.RAllowCollision"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.RAllowCollision"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static int MinManaToUseR
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinManaToUseR"] != null)
                            return LaneClearMenu["Plugins.Corki.LaneClearMenu.MinManaToUseR"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.LaneClearMenu.MinManaToUseR menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinManaToUseR"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.MinManaToUseR"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int MinStacksToUseR
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinStacksToUseR"] != null)
                            return LaneClearMenu["Plugins.Corki.LaneClearMenu.MinStacksToUseR"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.LaneClearMenu.MinStacksToUseR menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Corki.LaneClearMenu.MinStacksToUseR"] != null)
                            LaneClearMenu["Plugins.Corki.LaneClearMenu.MinStacksToUseR"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class JungleClear
            {
                public static bool UseQ
                {
                    get
                    {
                        return JungleClearMenu?["Plugins.Corki.JungleClearMenu.UseQ"] != null &&
                               JungleClearMenu["Plugins.Corki.JungleClearMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.UseQ"] != null)
                            JungleClearMenu["Plugins.Corki.JungleClearMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaToUseQ
                {
                    get
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.MinManaToUseQ"] != null)
                            return JungleClearMenu["Plugins.Corki.JungleClearMenu.MinManaToUseQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.JungleClearMenu.MinManaToUseQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.MinManaToUseQ"] != null)
                            JungleClearMenu["Plugins.Corki.JungleClearMenu.MinManaToUseQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return JungleClearMenu?["Plugins.Corki.JungleClearMenu.UseE"] != null &&
                               JungleClearMenu["Plugins.Corki.JungleClearMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.UseE"] != null)
                            JungleClearMenu["Plugins.Corki.JungleClearMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaToUseE
                {
                    get
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.MinManaToUseE"] != null)
                            return JungleClearMenu["Plugins.Corki.JungleClearMenu.MinManaToUseE"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.JungleClearMenu.MinManaToUseE menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.MinManaToUseE"] != null)
                            JungleClearMenu["Plugins.Corki.JungleClearMenu.MinManaToUseE"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseR
                {
                    get
                    {
                        return JungleClearMenu?["Plugins.Corki.JungleClearMenu.UseR"] != null &&
                               JungleClearMenu["Plugins.Corki.JungleClearMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.UseR"] != null)
                            JungleClearMenu["Plugins.Corki.JungleClearMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool RAllowCollision
                {
                    get
                    {
                        return JungleClearMenu?["Plugins.Corki.JungleClearMenu.RAllowCollision"] != null &&
                               JungleClearMenu["Plugins.Corki.JungleClearMenu.RAllowCollision"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.RAllowCollision"] != null)
                            JungleClearMenu["Plugins.Corki.JungleClearMenu.RAllowCollision"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaToUseR
                {
                    get
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.MinManaToUseR"] != null)
                            return JungleClearMenu["Plugins.Corki.JungleClearMenu.MinManaToUseR"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.JungleClearMenu.MinManaToUseR menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.MinManaToUseR"] != null)
                            JungleClearMenu["Plugins.Corki.JungleClearMenu.MinManaToUseR"].Cast<Slider>().CurrentValue = value;
                    }
                }
                
                public static int MinStacksToUseR
                {
                    get
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.MinStacksToUseR"] != null)
                            return JungleClearMenu["Plugins.Corki.JungleClearMenu.MinStacksToUseR"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.JungleClearMenu.MinStacksToUseR menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Corki.JungleClearMenu.MinStacksToUseR"] != null)
                            JungleClearMenu["Plugins.Corki.JungleClearMenu.MinStacksToUseR"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool AutoHarassEnabled
                {
                    get
                    {
                        return MiscMenu?["Plugins.Corki.MiscMenu.AutoHarassEnabled"] != null &&
                               MiscMenu["Plugins.Corki.MiscMenu.AutoHarassEnabled"].Cast<KeyBind>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Corki.MiscMenu.AutoHarassEnabled"] != null)
                            MiscMenu["Plugins.Corki.MiscMenu.AutoHarassEnabled"].Cast<KeyBind>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseBigBomb
                {
                    get
                    {
                        return MiscMenu?["Plugins.Corki.MiscMenu.UseBigBomb"] != null &&
                               MiscMenu["Plugins.Corki.MiscMenu.UseBigBomb"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Corki.MiscMenu.UseBigBomb"] != null)
                            MiscMenu["Plugins.Corki.MiscMenu.UseBigBomb"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinStacksToUseR
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Corki.MiscMenu.MinStacksToUseR"] != null)
                            return MiscMenu["Plugins.Corki.MiscMenu.MinStacksToUseR"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Corki.MiscMenu.MinStacksToUseR menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Corki.MiscMenu.MinStacksToUseR"] != null)
                            MiscMenu["Plugins.Corki.MiscMenu.MinStacksToUseR"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool IsAutoHarassEnabledFor(AIHeroClient champion)
                {
                    if (MiscMenu?["Plugins.Corki.MiscMenu.AutoHarassEnabled." + champion.Hero] != null)
                    {
                        return MiscMenu["Plugins.Corki.MiscMenu.AutoHarassEnabled." + champion.Hero].Cast<CheckBox>()
                                .CurrentValue;
                    }
                    return false;
                }

                public static bool IsAutoHarassEnabledFor(Champion hero)
                {
                    if (MiscMenu?["Plugins.Corki.MiscMenu.AutoHarassEnabled." + hero] != null)
                    {
                        return MiscMenu["Plugins.Corki.MiscMenu.AutoHarassEnabled." + hero].Cast<CheckBox>()
                                .CurrentValue;
                    }
                    return false;
                }

                public static bool IsAutoHarassEnabledFor(string championName)
                {
                    if (MiscMenu?["Plugins.Corki.MiscMenu.AutoHarassEnabled." + championName] != null)
                    {
                        return MiscMenu["Plugins.Corki.MiscMenu.AutoHarassEnabled." + championName].Cast<CheckBox>()
                                .CurrentValue;
                    }
                    return false;
                }
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool DrawQ
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawQ"] != null &&
                               DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawQ"] != null)
                            DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawW
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawW"] != null &&
                               DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawW"] != null)
                            DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawR
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawR"] != null &&
                               DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawR"] != null)
                            DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawDamageIndicator
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawDamageIndicator"] != null &&
                               DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawDamageIndicator"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Corki.DrawingsMenu.DrawDamageIndicator"] != null)
                            DrawingsMenu["Plugins.Corki.DrawingsMenu.DrawDamageIndicator"].Cast<CheckBox>()
                                .CurrentValue =
                                value;
                    }
                }
            }
        }

        internal static class Damage
        {
            private static float[] QDamage { get; } = {0, 70, 115, 160, 205, 250};
            private static float QDamageBounsAdMod { get; } = 0.5f;
            private static float QDamageTotalApMod { get; } = 0.5f;
            private static float[] EDamage { get; } = {0, 80, 140, 200, 260, 320};
            private static float EDamageBounsAdMod { get; } = 1.6f;
            private static float[] RDamageNormal { get; } = {0, 100, 130, 160};
            private static float[] RDamageNormalTotalAdMod { get; } = {0, 0.2f, 0.5f, 0.8f};
            private static float RDamageNormalTotalApMod { get; } = 0.3f;
            private static float[] RDamageBig { get; } = { 0, 150, 195, 240 };
            private static float[] RDamageBigTotalAdMod { get; } = { 0, 0.3f, 0.75f, 1.2f };
            private static float RDamageBigTotalApMod { get; } = 0.45f;

            public static float GetComboDamage(AIHeroClient enemy, uint autos = 1, uint bombs = 1)
            {
                float damage = 0;

                if (Q.IsReady())
                    damage += GetSpellDamage(enemy, SpellSlot.Q);

                if (Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing] != null && Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Blade_of_the_Ruined_King);

                if (Activator.Activator.Items[ItemsEnum.Cutlass] != null && Activator.Activator.Items[ItemsEnum.Cutlass].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Bilgewater_Cutlass);

                if (Activator.Activator.Items[ItemsEnum.Gunblade] != null && Activator.Activator.Items[ItemsEnum.Gunblade].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Hextech_Gunblade);

                if (E.IsReady())
                    damage += GetSpellDamage(enemy, SpellSlot.R, 2);

                if (R.IsReady())
                    damage += GetSpellDamage(enemy, SpellSlot.R) * bombs;

                damage += Player.Instance.GetAutoAttackDamage(enemy, true) * autos;

                return damage;
            }

            public static float GetSpellDamage(Obj_AI_Base unit, SpellSlot slot, float time = 4)
            {
                if (unit == null)
                    return 0f;

                switch (slot)
                {
                    case SpellSlot.Q:
                    {
                        return GetQDamage(unit);
                    }
                    case SpellSlot.E:
                    {
                        return GetEDamage(unit, time);
                    }
                    case SpellSlot.R:
                    {
                        return GetRDamage(unit);
                    }
                    default:
                        return 0f;
                }
            }

            private static float GetQDamage(Obj_AI_Base unit)
            {
                if (unit == null || !Q.IsReady())
                    return 0f;

                float damage;

                if (!(unit is AIHeroClient))
                {
                    damage = QDamage[Q.Level] + Player.Instance.FlatPhysicalDamageMod*QDamageBounsAdMod +
                             Player.Instance.FlatMagicDamageMod*QDamageTotalApMod;

                    return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);
                }

                var client = (AIHeroClient) unit;
                if (client.HasSpellShield() || client.HasUndyingBuffA())
                    return 0f;

                damage = QDamage[Q.Level] + Player.Instance.FlatPhysicalDamageMod * QDamageBounsAdMod +
                             Player.Instance.FlatMagicDamageMod*QDamageTotalApMod;

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);
            }

            private static float GetEDamage(Obj_AI_Base unit, float time = 4)
            {
                if (unit == null || !E.IsReady() || time < 0.25f || time > 4)
                    return 0f;

                float damage;

                float actualTIme = 0;

                if (!(Math.Abs(time % 0.25f) <= 0))
                {
                    actualTIme = time - time % 0.25f;
                }

                if (!(unit is AIHeroClient))
                {
                    damage = EDamage[Q.Level] / 16 + Player.Instance.FlatPhysicalDamageMod * EDamageBounsAdMod / 16;

                    return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Mixed, damage * (16 / (4 / actualTIme)));
                }

                var client = (AIHeroClient) unit;

                if (client.HasUndyingBuffA())
                    return 0f;
                
                damage = EDamage[Q.Level] / 16 + Player.Instance.FlatPhysicalDamageMod * EDamageBounsAdMod / 16;

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Mixed, damage  * (16 / (4 / actualTIme)));
            }

            private static float GetRDamage(Obj_AI_Base unit)
            {
                if (unit == null || !R.IsReady())
                    return 0f;

                float damage;

                if (!(unit is AIHeroClient))
                {
                    if (HasBigRMissile)
                    {
                        damage = RDamageBig[R.Level] + Player.Instance.TotalAttackDamage * RDamageBigTotalAdMod[R.Level] +
                                 Player.Instance.FlatMagicDamageMod * RDamageBigTotalApMod;
                    }
                    else
                    {
                        damage = RDamageNormal[R.Level] + Player.Instance.TotalAttackDamage * RDamageNormalTotalAdMod[R.Level] +
                                 Player.Instance.FlatMagicDamageMod * RDamageNormalTotalApMod;
                    }

                    return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);
                }

                var client = (AIHeroClient) unit;

                if (client.HasSpellShield() || client.HasUndyingBuffA())
                    return 0f;
                
                if (HasBigRMissile)
                {
                    damage = RDamageBig[R.Level] + Player.Instance.TotalAttackDamage*RDamageBigTotalAdMod[R.Level] +
                             Player.Instance.FlatMagicDamageMod*RDamageBigTotalApMod;
                }
                else
                {
                    damage = RDamageNormal[R.Level] + Player.Instance.TotalAttackDamage*RDamageNormalTotalAdMod[R.Level] +
                             Player.Instance.FlatMagicDamageMod*RDamageNormalTotalApMod;
                }

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);
            }
        }
    }
}