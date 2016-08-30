#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Jinx.cs" company="EloBuddy">
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
using Color = System.Drawing.Color;

namespace Simple_Marksmans.Plugins.Jinx
{
    internal class Jinx : ChampionPlugin
    {
        protected static Spell.Active Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        protected static Menu ComboMenu { get; set; }
        protected static Menu HarassMenu { get; set; }
        protected static Menu LaneClearMenu { get; set; }
        protected static Menu DrawingsMenu { get; set; }
        protected static Menu MiscMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;

        protected static int GetFirecanonStacks() =>
            !HasItemFirecanon ? 0 : Player.Instance.Buffs.Find(x => x.Name.ToLowerInvariant() == "itemstatikshankcharge").Count;

        protected static bool HasFirecanonStackedUp
            => Player.Instance.Buffs.Any(x => HasItemFirecanon && x.Name.ToLowerInvariant() == "itemstatikshankcharge" && x.Count == 100);

        protected static bool HasItemFirecanon
            => Player.Instance.InventoryItems.Any(x=>x.Id == ItemId.Rapid_Firecannon);

        protected static bool HasMinigun
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "jinxqicon");

        protected static int GetMinigunStacks
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "jinxqramp") ? Player.Instance.Buffs.Find(x => x.Name.ToLowerInvariant() == "jinxqramp").Count : 0;

        protected static bool HasRocketLauncher
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "jinxq");

        protected static float GetRealRocketLauncherRange()
        {
            var qRange = 700 + 25*(Q.Level - 1);
            var additionalRange = HasFirecanonStackedUp ? Math.Min(qRange*0.35f, 150) : 0;
            return qRange + additionalRange;
        }

        protected static float GetRealMinigunRange() => HasFirecanonStackedUp ? Math.Min(625 * 1.35f, 700 + 150) : 625;

        private static bool _changingRangeScan;
        private static bool _changingkeybindRange;

        protected static bool IsPreAttack { get; private set; }

        static Jinx()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1500, SkillShotType.Linear, 700, 3200, 60)
            {
                AllowedCollisionCount = -1
            };
            E = new Spell.Skillshot(SpellSlot.E, 900, SkillShotType.Circular, 1100, 1300, 100);
            R = new Spell.Skillshot(SpellSlot.R, 30000, SkillShotType.Linear, 1000, 1500, 130)
            {
                AllowedCollisionCount = -1
            };

            ColorPicker = new ColorPicker[2];

            ColorPicker[0] = new ColorPicker("JinxQ", new ColorBGRA(114, 171, 160, 255));
            ColorPicker[1] = new ColorPicker("JinxW", new ColorBGRA(255, 21, 95, 255));

            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Game.OnPostTick += args => IsPreAttack = false;

            ChampionTracker.Initialize(ChampionTrackerFlags.LongCastTimeTracker);
            ChampionTracker.OnLongSpellCast += ChampionTracker_OnLongSpellCast;
        }
        
        private static void ChampionTracker_OnLongSpellCast(object sender, OnLongSpellCastEventArgs e)
        {
            if (!E.IsReady() || !Settings.Combo.AutoE)
                return;

            if (e.IsTeleport)
            {
                Core.DelayAction(() =>
                {
                    if (E.IsReady() && e.EndPosition.Distance(Player.Instance) <= E.Range)
                    {
                        E.Cast(e.EndPosition);
                    }
                }, 3500);
            }
            else if(e.Sender.IsValidTarget(E.Range))
            {
                E.Cast(e.Sender.ServerPosition);
            }
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            IsPreAttack = true;

            if (Orbwalker.ForcedTarget != null &&
                !Orbwalker.ForcedTarget.IsValidTarget(Player.Instance.GetAutoAttackRange()))
                args.Process = false;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Jinx.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (_changingkeybindRange)
                Circle.Draw(SharpDX.Color.White, Settings.Combo.RRangeKeybind, Player.Instance);

            if (Settings.Drawings.DrawRocketsRange)
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);

            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[1].Color, W.Range, Player.Instance);

        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!Settings.Misc.EnableInterrupter || !(args.End.Distance(Player.Instance) < 350) || !E.IsReady() ||
                !sender.IsValidTarget(E.Range))
                return;

            if (args.Delay == 0)
            {
                E.Cast(E.GetPrediction(sender).CastPosition);
            }
            else Core.DelayAction(() => E.Cast(E.GetPrediction(sender).CastPosition), args.Delay);

        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (!Settings.Misc.EnableAntiGapcloser || !(args.End.Distance(Player.Instance) < 350) || !E.IsReady() ||
                !sender.IsValidTarget(E.Range))
                return;
            
            if (args.Delay == 0)
                E.Cast(args.End);
            else Core.DelayAction(() => E.Cast(args.End), args.Delay);
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Jinx addon");

            ComboMenu.AddLabel("Switcheroo! (Q) settings :");
            ComboMenu.Add("Plugins.Jinx.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Zap! (W) settings :");
            ComboMenu.Add("Plugins.Jinx.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.Add("Plugins.Jinx.ComboMenu.WMinDistanceToTarget", new Slider("Minimum distance to target to cast", 800, 0, 1500));
            ComboMenu.AddLabel("Cast W only if distance from player to target i higher than desired value.");
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Flame Chompers! (E) settings :");
            ComboMenu.Add("Plugins.Jinx.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.Add("Plugins.Jinx.ComboMenu.AutoE", new CheckBox("Automated E usage on certain spells"));
            ComboMenu.AddLabel("Automated E usage fires traps on enemy champions that are Teleporting or are in Zhonyas.\nIt also searchs for spells with long cast time " +
                               "like Caitlyn's R or Malzahar's R");
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Super Mega Death Rocket! (R) settings :");
            ComboMenu.Add("Plugins.Jinx.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Jinx.ComboMenu.RKeybind", new KeyBind("R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));
            ComboMenu.AddLabel("Fires R on best target in range when keybind is active.");
            ComboMenu.AddSeparator(5);
            var keybindRange = ComboMenu.Add("Plugins.Jinx.ComboMenu.RRangeKeybind",
                new Slider("Maximum range to enemy to cast R while keybind is active", 1100, 300, 5000));
            keybindRange.OnValueChange += (a, b) =>
            {
                _changingkeybindRange = true;
                Core.DelayAction(() =>
                {
                    if (!keybindRange.IsLeftMouseDown && !keybindRange.IsMouseInside)
                    {
                        _changingkeybindRange = false;
                    }
                }, 2000);
            };

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Jinx addon");

            HarassMenu.AddLabel("Switcheroo! (Q) settings :");
            HarassMenu.Add("Plugins.Jinx.HarassMenu.UseQ", new CheckBox("Use Q", false));
            HarassMenu.Add("Plugins.Jinx.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Zap! (W) settings :");
            HarassMenu.Add("Plugins.Jinx.HarassMenu.UseW", new CheckBox("Auto harass with W"));
            HarassMenu.AddLabel("Enables auto harass on enemy champions.");
            HarassMenu.Add("Plugins.Jinx.HarassMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 50, 1));
            HarassMenu.AddSeparator(5);
            HarassMenu.AddLabel("W harass enabled for :");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                HarassMenu.Add("Plugins.Jinx.HarassMenu.UseW." + enemy.Hero, new CheckBox(enemy.ChampionName == "MonkeyKing" ? "Wukong" : enemy.ChampionName));
            }

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Jinx addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.EnableLCIfNoEn",
                new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.ScanRange",
                new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Switcheroo! (Q) settings :");
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Jinx.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Jinx addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Jinx.MiscMenu.EnableInterrupter", new CheckBox("Cast E against interruptible spells", false));
            MiscMenu.Add("Plugins.Jinx.MiscMenu.EnableAntiGapcloser", new CheckBox("Cast E against gapclosers"));
            MiscMenu.Add("Plugins.Jinx.MiscMenu.WKillsteal", new CheckBox("Cast W to killsteal"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Jinx addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Switcheroo! (Q) drawing settings :");
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawRocketsRange", new CheckBox("Draw Q rockets range"));
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawRocketsRangeColor", new CheckBox("Change Color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[0].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Zap! (W) drawing settings :");
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawW", new CheckBox("Draw W range"));
            DrawingsMenu.Add("Plugins.Jinx.DrawingsMenu.DrawWColor", new CheckBox("Change Color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);
        }

        protected override void PermaActive()
        {
            Q.Range = (uint)GetRealRocketLauncherRange();

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

        protected static class Settings
        {
            internal static class Combo
            {
                public static bool UseQ
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jinx.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Jinx.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Jinx.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                public static bool UseW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jinx.ComboMenu.UseW"] != null &&
                               ComboMenu["Plugins.Jinx.ComboMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.UseW"] != null)
                            ComboMenu["Plugins.Jinx.ComboMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int WMinDistanceToTarget
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.WMinDistanceToTarget"] != null)
                            return ComboMenu["Plugins.Jinx.ComboMenu.WMinDistanceToTarget"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jinx.ComboMenu.WMinDistanceToTarget menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.WMinDistanceToTarget"] != null)
                            ComboMenu["Plugins.Jinx.ComboMenu.WMinDistanceToTarget"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jinx.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Jinx.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Jinx.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool AutoE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jinx.ComboMenu.AutoE"] != null &&
                               ComboMenu["Plugins.Jinx.ComboMenu.AutoE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.AutoE"] != null)
                            ComboMenu["Plugins.Jinx.ComboMenu.AutoE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jinx.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Jinx.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Jinx.ComboMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool RKeybind
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jinx.ComboMenu.RKeybind"] != null &&
                               ComboMenu["Plugins.Jinx.ComboMenu.RKeybind"].Cast<KeyBind>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.RKeybind"] != null)
                            ComboMenu["Plugins.Jinx.ComboMenu.RKeybind"].Cast<KeyBind>()
                                .CurrentValue
                                = value;
                    }
                }
                public static int RRangeKeybind
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.RRangeKeybind"] != null)
                            return ComboMenu["Plugins.Jinx.ComboMenu.RRangeKeybind"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jinx.ComboMenu.RRangeKeybind menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jinx.ComboMenu.RRangeKeybind"] != null)
                            ComboMenu["Plugins.Jinx.ComboMenu.RRangeKeybind"].Cast<Slider>().CurrentValue = value;
                    }
                }

            }

            internal static class Harass
            {
                public static bool UseQ
                {
                    get
                    {
                        return HarassMenu?["Plugins.Jinx.HarassMenu.UseQ"] != null &&
                               HarassMenu["Plugins.Jinx.HarassMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Jinx.HarassMenu.UseQ"] != null)
                            HarassMenu["Plugins.Jinx.HarassMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Jinx.HarassMenu.MinManaQ"] != null)
                            return HarassMenu["Plugins.Jinx.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jinx.HarassMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Jinx.HarassMenu.MinManaQ"] != null)
                            HarassMenu["Plugins.Jinx.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return HarassMenu?["Plugins.Jinx.HarassMenu.UseW"] != null &&
                               HarassMenu["Plugins.Jinx.HarassMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Jinx.HarassMenu.UseW"] != null)
                            HarassMenu["Plugins.Jinx.HarassMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static int MinManaW
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Jinx.HarassMenu.MinManaW"] != null)
                            return HarassMenu["Plugins.Jinx.HarassMenu.MinManaW"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jinx.HarassMenu.MinManaW menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Jinx.HarassMenu.MinManaW"] != null)
                            HarassMenu["Plugins.Jinx.HarassMenu.MinManaW"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool IsWHarassEnabledFor(AIHeroClient unit)
                {
                    return HarassMenu?["Plugins.Jinx.HarassMenu.UseW." + unit.Hero] != null &&
                           HarassMenu["Plugins.Jinx.HarassMenu.UseW." + unit.Hero].Cast<CheckBox>()
                               .CurrentValue;
                }

                public static bool IsWHarassEnabledFor(string championName)
                {
                    return HarassMenu?["Plugins.Jinx.HarassMenu.UseW." + championName] != null &&
                           HarassMenu["Plugins.Jinx.HarassMenu.UseW." + championName].Cast<CheckBox>()
                               .CurrentValue;
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Jinx.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Jinx.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jinx.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Jinx.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Jinx.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Jinx.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jinx.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jinx.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Jinx.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Jinx.LaneClearMenu.AllowedEnemies"] != null)
                            return
                                LaneClearMenu["Plugins.Jinx.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jinx.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jinx.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Jinx.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }
                
                public static bool UseQInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Jinx.LaneClearMenu.UseQInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Jinx.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jinx.LaneClearMenu.UseQInLaneClear"] != null)
                            LaneClearMenu["Plugins.Jinx.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool UseQInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Jinx.LaneClearMenu.UseQInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Jinx.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jinx.LaneClearMenu.UseQInJungleClear"] != null)
                            LaneClearMenu["Plugins.Jinx.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Jinx.LaneClearMenu.MinManaQ"] != null)
                            return LaneClearMenu["Plugins.Jinx.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jinx.LaneClearMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jinx.LaneClearMenu.MinManaQ"] != null)
                            LaneClearMenu["Plugins.Jinx.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool EnableInterrupter
                {
                    get
                    {
                        return MiscMenu?["Plugins.Jinx.MiscMenu.EnableInterrupter"] != null &&
                               MiscMenu["Plugins.Jinx.MiscMenu.EnableInterrupter"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Jinx.MiscMenu.EnableInterrupter"] != null)
                            MiscMenu["Plugins.Jinx.MiscMenu.EnableInterrupter"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool EnableAntiGapcloser
                {
                    get
                    {
                        return MiscMenu?["Plugins.Jinx.MiscMenu.EnableAntiGapcloser"] != null &&
                               MiscMenu["Plugins.Jinx.MiscMenu.EnableAntiGapcloser"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Jinx.MiscMenu.EnableAntiGapcloser"] != null)
                            MiscMenu["Plugins.Jinx.MiscMenu.EnableAntiGapcloser"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool WKillsteal
                {
                    get
                    {
                        return MiscMenu?["Plugins.Jinx.MiscMenu.WKillsteal"] != null &&
                               MiscMenu["Plugins.Jinx.MiscMenu.WKillsteal"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Jinx.MiscMenu.WKillsteal"] != null)
                            MiscMenu["Plugins.Jinx.MiscMenu.WKillsteal"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Jinx.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Jinx.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Jinx.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Jinx.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool DrawRocketsRange
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Jinx.DrawingsMenu.DrawRocketsRange"] != null &&
                               DrawingsMenu["Plugins.Jinx.DrawingsMenu.DrawRocketsRange"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Jinx.DrawingsMenu.DrawRocketsRange"] != null)
                            DrawingsMenu["Plugins.Jinx.DrawingsMenu.DrawRocketsRange"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawW
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Jinx.DrawingsMenu.DrawW"] != null &&
                               DrawingsMenu["Plugins.Jinx.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Jinx.DrawingsMenu.DrawW"] != null)
                            DrawingsMenu["Plugins.Jinx.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
            }
        }

        protected static class Damage
        {
            public static int[] RMinimalDamage { get; } = {0, 25, 35, 45};
            public static float RBonusAdDamageMod { get; } = 0.15f;
            public static float[] RMissingHealthBonusDamage { get; } = {0, 0.25f, 0.3f, 0.35f};

            public static float GetRDamage(Obj_AI_Base target)
            {
                var distance = Player.Instance.Distance(target) > 1500 ? 1499 : Player.Instance.Distance(target);
                distance = distance < 100 ? 100 : distance;

                var baseDamage = Misc.GetNumberInRangeFromProcent(Misc.GetProcentFromNumberRange(distance, 100, 1505),
                    RMinimalDamage[R.Level],
                    RMinimalDamage[R.Level]*10);
                var bonusAd = Misc.GetNumberInRangeFromProcent(Misc.GetProcentFromNumberRange(distance, 100, 1505),
                    RBonusAdDamageMod,
                    RBonusAdDamageMod*10);
                var percentDamage = (target.MaxHealth - target.Health)*RMissingHealthBonusDamage[R.Level];


                return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                    (float) (baseDamage + percentDamage + Player.Instance.FlatPhysicalDamageMod*bonusAd));
            }
        }
    }
}