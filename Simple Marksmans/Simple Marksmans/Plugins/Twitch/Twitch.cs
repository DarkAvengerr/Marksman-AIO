#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Twitch.cs" company="EloBuddy">
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
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Simple_Marksmans.Utils;
using EloBuddy.SDK.Utils;
using Color = SharpDX.Color;

namespace Simple_Marksmans.Plugins.Twitch
{
    internal class Twitch : ChampionPlugin
    {
        public static Spell.Active Q { get; }
        public static Spell.Skillshot W { get; }
        public static Spell.Active E { get; }
        public static Spell.Active R { get; }

        private static Menu ComboMenu { get; set; }
        private static Menu HarassMenu { get; set; }
        private static Menu JungleClearMenu { get; set; }
        private static Menu LaneClearMenu { get; set; }
        private static Menu MiscMenu { get; set; }
        private static Menu DrawingsMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;

        public static bool HasDeadlyVenomBuff(Obj_AI_Base unit) => unit.Buffs.Any(
            b => b.IsActive && b.DisplayName.ToLowerInvariant() == "twitchdeadlyvenom");

        public static BuffInstance GetDeadlyVenomBuff(Obj_AI_Base unit) => unit.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "twitchdeadlyvenom");

        private static readonly Text Text;

        private static bool _changingRangeScan;

        static Twitch()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 950, SkillShotType.Circular, 250, 1400, 100)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Active(SpellSlot.E, 1200);
            R = new Spell.Active(SpellSlot.R, 850);

            ColorPicker = new ColorPicker[4];
            
            ColorPicker[0] = new ColorPicker("TwitchW", new ColorBGRA(243, 109, 160, 255));
            ColorPicker[1] = new ColorPicker("TwitchE", new ColorBGRA(255, 210, 54, 255));
            ColorPicker[2] = new ColorPicker("TwitchR", new ColorBGRA(241, 188, 160, 255));
            ColorPicker[3] = new ColorPicker("TwitchHpBar", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(System.Drawing.Color.FromArgb(ColorPicker[3].Color.R, ColorPicker[3].Color.G, ColorPicker[3].Color.B), (int)E.Range);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[3].OnColorChange += (sender, args) =>
            {
                DamageIndicator.Color = System.Drawing.Color.FromArgb(args.Color.R, args.Color.G,
                    args.Color.B);
            };

            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));

            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Game.OnNotify += Game_OnNotify;
        }

        private static void Game_OnNotify(GameNotifyEventArgs args)
        {
            if (Q.IsReady() && Settings.Combo.UseQAfterKill)
            {
                var rand = new Random();

                Core.DelayAction(() =>
                {
                    if (Player.Instance.CountEnemiesInRange(1500) >= 1 &&
                        args.NetworkId == Player.Instance.NetworkId && args.EventId == GameEventId.OnChampionKill)
                    {
                        Q.Cast();
                    }
                }, rand.Next(100, 300));
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.R)
            {
                if (Activator.Activator.Items[ItemsEnum.Ghostblade] != null)
                {
                    Activator.Activator.Items[ItemsEnum.Ghostblade].UseItem();
                }
            }

            if (args.Slot != SpellSlot.Recall || !Q.IsReady() || !Settings.Misc.StealthRecall || Player.Instance.IsInShopRange())
                return;

            args.Process = false;

            Q.Cast();
            Player.Instance.Spellbook.CastSpell(SpellSlot.Recall);
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (R.IsReady() && Settings.Combo.UseR && target is AIHeroClient)
            {
                if (Player.Instance.CountEnemiesInRange(900) < Settings.Combo.RIfEnemiesHit)
                    return;

                var polygon = new Geometry.Polygon.Rectangle(Player.Instance.Position, Player.Instance.Position.Extend(args.Target, 850).To3D(), 65);

                var intersection = polygon.Points[0].Intersection(polygon.Points[2],
                    polygon.Points[1], polygon.Points[3]);

                var count = EntityManager.Heroes.Enemies.Count(x => !x.IsDead && x.IsValidTarget(850) && polygon.IsInside(x));
                var countv2 = EntityManager.Heroes.Enemies.Count(x => !x.IsDead && x.IsValidTarget(850) && new Geometry.Polygon.Circle(x.Position, x.BoundingRadius).IsInside(intersection.Point));

                if (count >= Settings.Combo.RIfEnemiesHit || countv2 > Settings.Combo.RIfEnemiesHit)
                {
                    Misc.PrintInfoMessage("Casting R because it can hit <font color=\"#ff1493\">" + count + "</font>. enemies" + countv2);
                }
            }
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawDamageIndicator)
            {
                return 0;
            }

            var enemy = (AIHeroClient)unit;

            if (enemy != null)
            {
                
                return Damage.GetEDamage(enemy);
            }
            return 0;
        }

        protected override void OnDraw()
        {
            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[0].Color, W.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[1].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[2].Color, R.Range, Player.Instance);

            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Twitch.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (!Settings.Drawings.DrawDamageIndicator)
                return;
            
            foreach (var source in EntityManager.Heroes.Enemies.Where(x=> x.IsVisible && x.IsHPBarRendered && x.Position.IsOnScreen() && HasDeadlyVenomBuff(x)))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30; // tracker friendly.
                var timeLeft = GetDeadlyVenomBuff(source).EndTime - Game.Time;
                var endPos = timeLeft * 0x3e8 / 0x37;
                
                var degree = Misc.GetNumberInRangeFromProcent(timeLeft * 1000d / 6000d * 100d, 3, 110);
                var color = new Misc.HsvColor(degree, 1, 1).ColorFromHsv();

                Text.X = (int) (hpPosition.X + endPos);
                Text.Y = (int)hpPosition.Y + 15; // + text size 
                Text.Color = color;
                Text.TextValue = timeLeft.ToString("F1");
                Text.Draw();

                var percentDamage = Math.Min(100, Damage.GetEDamage(source) / source.TotalHealthWithShields() * 100);

                Text.X = (int)(hpPosition.X - 50);
                Text.Y = (int)source.HPBarPosition.Y;
                Text.Color = new Misc.HsvColor(Misc.GetNumberInRangeFromProcent(percentDamage, 3, 110), 1, 1).ColorFromHsv();
                Text.TextValue = percentDamage.ToString("F1");
                Text.Draw();

                Drawing.DrawLine(hpPosition.X + endPos, hpPosition.Y, hpPosition.X, hpPosition.Y, 1, color);
            }
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
            ComboMenu.AddGroupLabel("Combo mode settings for Twitch addon");

            ComboMenu.AddLabel("Ambush (Q) settings :");
            ComboMenu.Add("Plugins.Twitch.ComboMenu.UseQ", new CheckBox("Use Q after kill"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Venom Cask (W) settings :");
            ComboMenu.Add("Plugins.Twitch.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Contaminate (E) settings :");
            ComboMenu.Add("Plugins.Twitch.ComboMenu.UseE", new CheckBox("Use E"));
            var mode = ComboMenu.Add("Plugins.Twitch.ComboMenu.UseEIfDmg", new ComboBox("E usage mode", 0, "Percentage", "At stacks", "Only to killsteal"));
            ComboMenu.AddSeparator(10);
            ComboMenu.AddLabel("Percentage : Uses E only if it will deal desired percentage of enemy total hp.\nAt stacks : Uses E only if desired amount of stack are reached on enemy.\nOnly to killsteal : " +
                               "Uses E only to execute enemies.");
            ComboMenu.AddSeparator(10);

            var percentage = ComboMenu.Add("Plugins.Twitch.ComboMenu.EAtStacks",
                new Slider("Use E if will deal ({0}%) percentage of enemy hp.", 30));

            switch (mode.CurrentValue)
            {
                case 0:
                    percentage.DisplayName = "Use E if will deal ({0}%) percentage of enemy hp.";
                    percentage.MinValue = 0;
                    percentage.MaxValue = 100;
                    break;
                case 1:
                    percentage.DisplayName = "Use E at {0} stacks.";
                    percentage.MinValue = 1;
                    percentage.MaxValue = 6;
                    break;
                case 2:
                    percentage.IsVisible = false;
                    break;
            }
            mode.OnValueChange += (a, b) =>
            {
                switch (b.NewValue)
                {
                    case 0:
                        percentage.DisplayName = "Use E if will deal ({0}%) percentage of enemy hp.";
                        percentage.MinValue = 0;
                        percentage.MaxValue = 100;
                        percentage.IsVisible = true;
                        break;
                    case 1:
                        percentage.DisplayName = "Use E at {0} stacks.";
                        percentage.MinValue = 1;
                        percentage.MaxValue = 6;
                        percentage.IsVisible = true;
                        break;
                    case 2:
                        percentage.IsVisible = false;
                        break;
                }
            };
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Rat-Ta-Tat-Tat (R) settings :");
            ComboMenu.Add("Plugins.Twitch.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Twitch.ComboMenu.RIfEnemiesHit", new Slider("Use R if gonna hit {0} enemies", 3, 0, 5));
            ComboMenu.AddSeparator(5);
            ComboMenu.Add("Plugins.Twitch.ComboMenu.RifTargetOutOfRange", new CheckBox("Use R if target is out of range", false));
            ComboMenu.AddLabel("Uses R if target is killabe, but he is not inside basic attack range, and R won't be up in next 2 secs.");

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Twitch addon");

            HarassMenu.AddLabel("Venom Cask (W) settings :");
            HarassMenu.Add("Plugins.Twitch.HarassMenu.UseW", new CheckBox("Use W", false));
            HarassMenu.Add("Plugins.Twitch.HarassMenu.WMinMana", new Slider("Min mana percentage ({0}%) to use W", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Contaminate (E) settings :");
            HarassMenu.Add("Plugins.Twitch.HarassMenu.UseE", new CheckBox("Use E", false));
            HarassMenu.Add("Plugins.Twitch.HarassMenu.TwoEnemiesMin", new CheckBox("Only if will hit 2 or more enemies", false));
            HarassMenu.Add("Plugins.Twitch.HarassMenu.EMinMana", new Slider("Min mana percentage ({0}%) to use E", 80, 1));
            HarassMenu.Add("Plugins.Twitch.HarassMenu.EMinStacks", new Slider("Min stacks to use E", 6, 1, 6));

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Lane clear");
            LaneClearMenu.AddGroupLabel("Lane clear mode settings for Twitch addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Twitch.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Twitch.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Twitch.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Venom Cask (W) settings :");
            LaneClearMenu.Add("Plugins.Twitch.LaneClearMenu.UseW", new CheckBox("Use W", false));
            LaneClearMenu.Add("Plugins.Twitch.LaneClearMenu.WMinMana", new Slider("Min mana percentage ({0}%) to use W", 80, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Contaminate (E) settings :");
            LaneClearMenu.Add("Plugins.Twitch.LaneClearMenu.UseE", new CheckBox("Use E", false));
            LaneClearMenu.Add("Plugins.Twitch.LaneClearMenu.EMinMana", new Slider("Min mana percentage ({0}%) to use E", 80, 1));
            LaneClearMenu.Add("Plugins.Twitch.LaneClearMenu.EMinMinionsHit", new Slider("Min minions hit to use E", 4, 1, 7));

            JungleClearMenu = MenuManager.Menu.AddSubMenu("Jungle clear");
            JungleClearMenu.AddGroupLabel("Jungle clear mode settings for Twitch addon");

            JungleClearMenu.AddLabel("Venom Cask (W) settings :");
            JungleClearMenu.Add("Plugins.Twitch.JungleClearMenu.UseW", new CheckBox("Use W", false));
            JungleClearMenu.Add("Plugins.Twitch.JungleClearMenu.WMinMana", new Slider("Min mana percentage ({0}%) to use W", 80, 1));
            JungleClearMenu.AddSeparator(5);

            JungleClearMenu.AddLabel("Contaminate (E) settings :");
            JungleClearMenu.Add("Plugins.Twitch.JungleClearMenu.UseE", new CheckBox("Use E"));
            JungleClearMenu.Add("Plugins.Twitch.JungleClearMenu.EMinMana", new Slider("Min mana percentage ({0}%) to use E", 30, 1));
            JungleClearMenu.AddLabel("Uses E only on big monsters and buffs");

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Twitch addon");

            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Twitch.MiscMenu.StealthRecall", new CheckBox("Enable steath recall"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Twitch addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Venom Cask (W) drawing settings :");
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawW", new CheckBox("Draw W range", false));
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawWColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Contaminate (E) drawing settings :");
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawEColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Rat-Ta-Tat-Tat (R) drawing settings :");
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Damage indicator drawing settings :");
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawDamageIndicator",
                new CheckBox("Draw damage indicator on enemy HP bars", false)).OnValueChange += (a, b) =>
                {
                    if (b.NewValue)
                        DamageIndicator.DamageDelegate = HandleDamageIndicator;
                    else if(!b.NewValue)
                        DamageIndicator.DamageDelegate = null;
                };
            DrawingsMenu.Add("Plugins.Twitch.DrawingsMenu.DrawDamageIndicatorColor",
                new CheckBox("Change color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[3].Initialize(System.Drawing.Color.Aquamarine);
                    a.CurrentValue = false;
                };
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
                public static bool UseQAfterKill
                {
                    get
                    {
                        return ComboMenu?["Plugins.Twitch.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Twitch.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Twitch.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Twitch.ComboMenu.UseW"] != null &&
                               ComboMenu["Plugins.Twitch.ComboMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.UseW"] != null)
                            ComboMenu["Plugins.Twitch.ComboMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Twitch.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Twitch.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Twitch.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int EMode
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.UseEIfDmg"] != null)
                            return ComboMenu["Plugins.Twitch.ComboMenu.UseEIfDmg"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.ComboMenu.UseEIfDmg menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.UseEIfDmg"] != null)
                            ComboMenu["Plugins.Twitch.ComboMenu.UseEIfDmg"].Cast<ComboBox>().CurrentValue = value;
                    }
                }

                public static int EAt
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.EAtStacks"] != null)
                            return ComboMenu["Plugins.Twitch.ComboMenu.EAtStacks"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.ComboMenu.EAtStacks menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.EAtStacks"] != null)
                            ComboMenu["Plugins.Twitch.ComboMenu.EAtStacks"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Twitch.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Twitch.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Twitch.ComboMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                public static bool RifTargetOutOfRange
                {
                    get
                    {
                        return ComboMenu?["Plugins.Twitch.ComboMenu.RifTargetOutOfRange"] != null &&
                               ComboMenu["Plugins.Twitch.ComboMenu.RifTargetOutOfRange"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.RifTargetOutOfRange"] != null)
                            ComboMenu["Plugins.Twitch.ComboMenu.RifTargetOutOfRange"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int RIfEnemiesHit
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.RIfEnemiesHit"] != null)
                            return ComboMenu["Plugins.Twitch.ComboMenu.RIfEnemiesHit"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.ComboMenu.RIfEnemiesHit menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Twitch.ComboMenu.RIfEnemiesHit"] != null)
                            ComboMenu["Plugins.Twitch.ComboMenu.RIfEnemiesHit"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Harass
            {
                public static bool UseW
                {
                    get
                    {
                        return HarassMenu?["Plugins.Twitch.HarassMenu.UseW"] != null &&
                               HarassMenu["Plugins.Twitch.HarassMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Twitch.HarassMenu.UseW"] != null)
                            HarassMenu["Plugins.Twitch.HarassMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaToUseW
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Twitch.HarassMenu.WMinMana"] != null)
                            return HarassMenu["Plugins.Twitch.HarassMenu.WMinMana"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.HarassMenu.WMinMana menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Twitch.HarassMenu.WMinMana"] != null)
                            HarassMenu["Plugins.Twitch.HarassMenu.WMinMana"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return HarassMenu?["Plugins.Twitch.HarassMenu.UseE"] != null &&
                               HarassMenu["Plugins.Twitch.HarassMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Twitch.HarassMenu.UseE"] != null)
                            HarassMenu["Plugins.Twitch.HarassMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool TwoEnemiesMin
                {
                    get
                    {
                        return HarassMenu?["Plugins.Twitch.HarassMenu.TwoEnemiesMin"] != null &&
                               HarassMenu["Plugins.Twitch.HarassMenu.TwoEnemiesMin"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Twitch.HarassMenu.TwoEnemiesMin"] != null)
                            HarassMenu["Plugins.Twitch.HarassMenu.TwoEnemiesMin"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int EMinMana
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Twitch.HarassMenu.EMinMana"] != null)
                            return HarassMenu["Plugins.Twitch.HarassMenu.EMinMana"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.HarassMenu.EMinMana menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Twitch.HarassMenu.EMinMana"] != null)
                            HarassMenu["Plugins.Twitch.HarassMenu.EMinMana"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int EMinStacks
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Twitch.HarassMenu.EMinStacks"] != null)
                            return HarassMenu["Plugins.Twitch.HarassMenu.EMinStacks"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.HarassMenu.EMinStacks menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Twitch.HarassMenu.EMinStacks"] != null)
                            HarassMenu["Plugins.Twitch.HarassMenu.EMinStacks"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Twitch.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Twitch.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Twitch.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Twitch.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Twitch.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.AllowedEnemies"] != null)
                            return
                                LaneClearMenu["Plugins.Twitch.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Twitch.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Twitch.LaneClearMenu.UseW"] != null &&
                               LaneClearMenu["Plugins.Twitch.LaneClearMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.UseW"] != null)
                            LaneClearMenu["Plugins.Twitch.LaneClearMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int WMinMana
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.WMinMana"] != null)
                            return LaneClearMenu["Plugins.Twitch.LaneClearMenu.WMinMana"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.LaneClearMenu.WMinMana menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.WMinMana"] != null)
                            LaneClearMenu["Plugins.Twitch.LaneClearMenu.WMinMana"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Twitch.LaneClearMenu.UseE"] != null &&
                               LaneClearMenu["Plugins.Twitch.LaneClearMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.UseE"] != null)
                            LaneClearMenu["Plugins.Twitch.LaneClearMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int EMinMana
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.EMinMana"] != null)
                            return LaneClearMenu["Plugins.Twitch.LaneClearMenu.EMinMana"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.LaneClearMenu.EMinMana menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.EMinMana"] != null)
                            LaneClearMenu["Plugins.Twitch.LaneClearMenu.EMinMana"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int EMinMinionsHit
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.EMinMinionsHit"] != null)
                            return
                                LaneClearMenu["Plugins.Twitch.LaneClearMenu.EMinMinionsHit"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.LaneClearMenu.EMinMinionsHit menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Twitch.LaneClearMenu.EMinMinionsHit"] != null)
                            LaneClearMenu["Plugins.Twitch.LaneClearMenu.EMinMinionsHit"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }
            }

            internal static class JungleClear
            {
                public static bool UseW
                {
                    get
                    {
                        return JungleClearMenu?["Plugins.Twitch.JungleClearMenu.UseW"] != null &&
                               JungleClearMenu["Plugins.Twitch.JungleClearMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Twitch.JungleClearMenu.UseW"] != null)
                            JungleClearMenu["Plugins.Twitch.JungleClearMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static int WMinMana
                {
                    get
                    {
                        if (JungleClearMenu?["Plugins.Twitch.JungleClearMenu.WMinMana"] != null)
                            return JungleClearMenu["Plugins.Twitch.JungleClearMenu.WMinMana"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.JungleClearMenu.WMinMana menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Twitch.JungleClearMenu.WMinMana"] != null)
                            JungleClearMenu["Plugins.Twitch.JungleClearMenu.WMinMana"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return JungleClearMenu?["Plugins.Twitch.JungleClearMenu.UseE"] != null &&
                               JungleClearMenu["Plugins.Twitch.JungleClearMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Twitch.JungleClearMenu.UseE"] != null)
                            JungleClearMenu["Plugins.Twitch.JungleClearMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int EMinMana
                {
                    get
                    {
                        if (JungleClearMenu?["Plugins.Twitch.JungleClearMenu.EMinMana"] != null)
                            return JungleClearMenu["Plugins.Twitch.JungleClearMenu.EMinMana"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Twitch.JungleClearMenu.EMinMana menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleClearMenu?["Plugins.Twitch.JungleClearMenu.EMinMana"] != null)
                            JungleClearMenu["Plugins.Twitch.JungleClearMenu.EMinMana"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool StealthRecall
                {
                    get
                    {
                        return MiscMenu?["Plugins.Twitch.MiscMenu.StealthRecall"] != null &&
                               MiscMenu["Plugins.Twitch.MiscMenu.StealthRecall"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Twitch.MiscMenu.StealthRecall"] != null)
                            MiscMenu["Plugins.Twitch.MiscMenu.StealthRecall"].Cast<CheckBox>()
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
                        return DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool DrawW
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawW"] != null &&
                               DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawW"] != null)
                            DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawE
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawE"] != null &&
                               DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawE"] != null)
                            DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
                public static bool DrawR
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawR"] != null &&
                               DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawR"] != null)
                            DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawDamageIndicator
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawDamageIndicator"] != null &&
                               DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawDamageIndicator"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Twitch.DrawingsMenu.DrawDamageIndicator"] != null)
                            DrawingsMenu["Plugins.Twitch.DrawingsMenu.DrawDamageIndicator"].Cast<CheckBox>()
                                .CurrentValue =
                                value;
                    }
                }
            }
        }

        internal static class Damage
        {
            private static float[] EDamage { get; } = { 0, 20, 35, 50, 65, 80 };
            private static float[] EDamagePerStack { get; } = { 0, 15, 20, 25, 30, 35 };
            private static float EDamagePerStackBounsAdMod { get; } = 0.25f;
            private static float EDamagePerStackBounsApMod { get; } = 0.2f;
            public static int[] RBonusAd { get; } = {0, 20, 30, 40};

            public static float GetComboDamage(AIHeroClient enemy, int autos = 0)
            {
                float damage = 0;

                if (Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing] != null &&
                    Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing].ToItem().IsReady())
                {
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Blade_of_the_Ruined_King);
                }

                if (Activator.Activator.Items[ItemsEnum.Cutlass] != null && Activator.Activator.Items[ItemsEnum.Cutlass].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Bilgewater_Cutlass);

                if (Activator.Activator.Items[ItemsEnum.Gunblade] != null && Activator.Activator.Items[ItemsEnum.Gunblade].ToItem().IsReady())
                    damage += Player.Instance.GetItemDamage(enemy, ItemId.Hextech_Gunblade);

                if (E.IsReady())
                    damage += GetEDamage(enemy, true, autos > 0 ? autos : CountEStacks(enemy));
                
                damage += Player.Instance.GetAutoAttackDamage(enemy, true) * autos < 1 ? 1 : autos;

                return damage;
            }

            public static bool CanCastEOnUnit(Obj_AI_Base target)
            {
                if (target == null || !target.IsValidTarget(E.Range) || GetDeadlyVenomBuff(target) == null ||
                    !E.IsReady() || CountEStacks(target) < 1)
                    return false;

                if (!(target is AIHeroClient))
                    return true;

                var heroClient = (AIHeroClient) target;

                return !heroClient.HasUndyingBuffA() && !heroClient.HasSpellShield();
            }

            public static bool IsTargetKillableByE(Obj_AI_Base target)
            {
                if (!CanCastEOnUnit(target))
                    return false;

                if (!(target is AIHeroClient))
                {
                    return GetEDamage(target) > target.TotalHealthWithShields();
                }

                var heroClient = (AIHeroClient) target;

                if (heroClient.HasUndyingBuffA() || heroClient.HasSpellShield())
                {
                    return false;
                }

                if (heroClient.ChampionName != "Blitzcrank")
                    return GetEDamage(heroClient) >= heroClient.TotalHealthWithShields();

                if (!heroClient.HasBuff("BlitzcrankManaBarrierCD") && !heroClient.HasBuff("ManaBarrier"))
                {
                    return GetEDamage(heroClient) > heroClient.TotalHealthWithShields() + heroClient.Mana/2;
                }
                return GetEDamage(heroClient) > heroClient.TotalHealthWithShields();
            }

            private static float GetPassiveDamage(Obj_AI_Base target, int stacks = -1)
            {
                if (!HasDeadlyVenomBuff(target))
                    return 0;

                var damagePerStack = 0;

                if (Player.Instance.Level < 5)
                    damagePerStack = 2;
                else if (Player.Instance.Level < 9)
                    damagePerStack = 3;
                else if (Player.Instance.Level < 13)
                    damagePerStack = 4;
                else if (Player.Instance.Level < 17)
                    damagePerStack = 5;
                else if (Player.Instance.Level >= 17)
                    damagePerStack = 6;

                var time = Math.Max(0, GetDeadlyVenomBuff(target).EndTime - Game.Time);

                return (damagePerStack * (stacks > 0 ? stacks : CountEStacks(target)) * time) - target.HPRegenRate * time;
            }

            public static float GetEDamage(Obj_AI_Base unit, bool includePassive = false, int stacks = 0)
            {
                if (unit == null)
                    return 0;

                var stack = stacks > 0 ? stacks : CountEStacks(unit);

                if (stack == 0)
                    return 0;

                if (!(unit is AIHeroClient))
                {
                    var damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                        EDamage[E.Level] + stack*
                        (Player.Instance.FlatMagicDamageMod*EDamagePerStackBounsApMod +
                         Player.Instance.FlatPhysicalDamageMod*EDamagePerStackBounsAdMod +
                         EDamagePerStack[E.Level]));

                    return damage + (includePassive && HasDeadlyVenomBuff(unit) ? GetPassiveDamage(unit) : 0);
                }

                var client = (AIHeroClient)unit;

                if (client.HasSpellShield() || client.HasUndyingBuffA())
                    return 0;

                float dmg = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                    EDamage[E.Level] + stack*
                    (Player.Instance.FlatMagicDamageMod*EDamagePerStackBounsApMod +
                     Player.Instance.FlatPhysicalDamageMod*EDamagePerStackBounsAdMod +
                     EDamagePerStack[E.Level]), false, true);

                return dmg+ (includePassive && HasDeadlyVenomBuff(unit) ? GetPassiveDamage(unit) : 0);
            }

            public static int CountEStacks(Obj_AI_Base unit)
            {
                if (unit.IsDead || !unit.IsEnemy || unit.Type != GameObjectType.AIHeroClient && unit.Type != GameObjectType.obj_AI_Minion)
                {
                    return 0;
                }

                var index = ObjectManager.Get<Obj_GeneralParticleEmitter>().ToList().Where(e => e.Name.Contains("twitch_poison_counter") &&
                e.Position.Distance(unit.ServerPosition) <= (unit.Type == GameObjectType.obj_AI_Minion ? 65 : 175));

                var stacks = 0;

                foreach (var x in index)
                {
                    switch (x.Name)
                    {
                        case "twitch_poison_counter_01.troy":
                            stacks = 1;
                            break;
                        case "twitch_poison_counter_02.troy":
                            stacks = 2;
                            break;
                        case "twitch_poison_counter_03.troy":
                            stacks = 3;
                            break;
                        case "twitch_poison_counter_04.troy":
                            stacks = 4;
                            break;
                        case "twitch_poison_counter_05.troy":
                            stacks = 5;
                            break;
                        case "twitch_poison_counter_06.troy":
                            stacks = 6;
                            break;
                        default:
                            stacks = 0;
                            break;
                    }
                }
                return stacks;
            }
        }
    }
}