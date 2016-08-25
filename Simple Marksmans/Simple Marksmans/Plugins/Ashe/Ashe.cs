#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Ashe.cs" company="EloBuddy">
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
using ColorPicker = Simple_Marksmans.Utils.ColorPicker;

namespace Simple_Marksmans.Plugins.Ashe
{
    internal class Ashe : ChampionPlugin
    {
        public static Spell.Active Q { get; }
        public static Spell.Skillshot W { get; }
        public static Spell.Skillshot E { get; }
        public static Spell.Skillshot R { get; }

        private static Menu ComboMenu { get; set; }
        private static Menu HarassMenu { get; set; }
        private static Menu LaneClearMenu { get; set; }
        private static Menu MiscMenu { get; set; }
        private static Menu DrawingsMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;
        private static bool _changingRangeScan;
        public static bool IsPreAttack { get; private set; }

        static Ashe()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1225, SkillShotType.Cone)
            {
                AllowedCollisionCount = 0,
                CastDelay = 250,
                ConeAngleDegrees = (int) (Math.PI/180*40),
                Speed = 2000,
                Range = 1225,
                Width = 20
            };
            E = new Spell.Skillshot(SpellSlot.E, 30000, SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, 30000, SkillShotType.Linear, 250, 1600, 120);

            ColorPicker = new ColorPicker[2];

            ColorPicker[0] = new ColorPicker("AsheW", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("AsheR", new ColorBGRA(177, 67, 191, 255));

            Orbwalker.OnPreAttack += (a,b) => IsPreAttack = true;
            Game.OnPostTick += a => IsPreAttack = false;

        }
        
        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Ashe.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[0].Color, W.Range, Player.Instance);

            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[1].Color, W.Range, Player.Instance);
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (R.IsReady() && (args.DangerLevel == DangerLevel.Medium || args.DangerLevel == DangerLevel.High) && Player.Instance.Mana > 200 &&
                sender.IsValidTarget(Settings.Misc.MaxInterrupterRange))
            {
                var rPrediction = R.GetPrediction(sender);

                if (rPrediction.HitChance >= HitChance.High || rPrediction.HitChance == HitChance.Collision)
                {
                    if (rPrediction.HitChance == HitChance.Collision)
                    {
                        var polygon = new Geometry.Polygon.Rectangle(Player.Instance.Position,
                            rPrediction.CastPosition, 120);

                        if (!EntityManager.Heroes.Enemies.Any(x => polygon.IsInside(x)))
                        {
                            R.Cast(rPrediction.CastPosition);
                        }
                    }
                    else
                    {
                        R.Cast(rPrediction.CastPosition);
                    }
                }
            }
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (R.IsReady() && args.End.Distance(Player.Instance.Position) < 400)
            {
                R.Cast(sender);
            }
        }

        public static PredictionResult GetWPrediction(Obj_AI_Base unit)
        {
            var poly = new Geometry.Polygon.Sector(Player.Instance.Position, Game.CursorPos,
                (float) (Math.PI/180*40), 950, 9).Points.ToArray();

            for (var i = 1; i < 10; i++)
            {
                var qPred = Prediction.Position.PredictLinearMissile(unit, 1100, 20, 25, 1200, 0,
                    Player.Instance.Position.Extend(poly[i], 20).To3D());

                if (!qPred.CollisionObjects.Any() && qPred.HitChance >= HitChance.High)
                {
                    return qPred;
                }
            }
            return null;
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Ashe addon");

            ComboMenu.AddLabel("Ranger's Focus (Q) settings :");
            ComboMenu.Add("Plugins.Ashe.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Volley (W) settings :");
            ComboMenu.Add("Plugins.Ashe.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Hawkshot (E) settings :");
            ComboMenu.Add("Plugins.Ashe.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Enchanted Crystal Arrow (R) settings :");
            ComboMenu.Add("Plugins.Ashe.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Ashe.ComboMenu.RMinimumRange", new Slider("R minimum range to cast", 350, 100, 700));
            ComboMenu.Add("Plugins.Ashe.ComboMenu.RMaximumRange", new Slider("R maximum range to cast", 1500, 700, 3000));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Ashe addon");

            HarassMenu.AddLabel("Volley (W) settings :");
            HarassMenu.Add("Plugins.Ashe.HarassMenu.UseW", new CheckBox("Use W"));
            HarassMenu.Add("Plugins.Ashe.HarassMenu.MinManaForW", new Slider("Min mana percentage ({0}%) to use W", 60, 1));
            HarassMenu.AddSeparator(5);

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Ashe addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Ranger's Focus (Q) settings :");
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Volley (W) settings :");
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.UseWInLaneClear", new CheckBox("Use W in Lane Clear"));
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.UseWInJungleClear", new CheckBox("Use W in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Ashe.LaneClearMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 80, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Ashe addon");
            MiscMenu.Add("Plugins.Ashe.MiscMenu.MaxInterrupterRange",
                new Slider("Max range to cast R against interruptible spell", 1500, 0, 2500));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawing settings for Ashe addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawW", new CheckBox("Draw W range"));
            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawWColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) => {
                    if (!b.NewValue)
                        return;

                    ColorPicker[0].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };

            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawRColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) => {
                    if (!b.NewValue)
                        return;

                    ColorPicker[1].Initialize(Color.Aquamarine);
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

        protected static class Settings
        {
            internal static class Combo
            {
                public static bool UseQ
                {
                    get
                    {
                        return ComboMenu?["Plugins.Ashe.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Ashe.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Ashe.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Ashe.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool UseW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Ashe.ComboMenu.UseW"] != null &&
                               ComboMenu["Plugins.Ashe.ComboMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Ashe.ComboMenu.UseW"] != null)
                            ComboMenu["Plugins.Ashe.ComboMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Ashe.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Ashe.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Ashe.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Ashe.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Ashe.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Ashe.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Ashe.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Ashe.ComboMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int RMinimumRange
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Ashe.ComboMenu.RMinimumRange"] != null)
                            return ComboMenu["Plugins.Ashe.ComboMenu.RMinimumRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Ashe.ComboMenu.RMinimumRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Ashe.ComboMenu.RMinimumRange"] != null)
                            ComboMenu["Plugins.Ashe.ComboMenu.RMinimumRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int RMaximumRange
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Ashe.ComboMenu.RMaximumRange"] != null)
                            return ComboMenu["Plugins.Ashe.ComboMenu.RMaximumRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Ashe.ComboMenu.RMaximumRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Ashe.ComboMenu.RMaximumRange"] != null)
                            ComboMenu["Plugins.Ashe.ComboMenu.RMaximumRange"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Harass
            {
                public static bool UseW
                {
                    get
                    {
                        return HarassMenu?["Plugins.Ashe.HarassMenu.UseW"] != null &&
                               HarassMenu["Plugins.Ashe.HarassMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Ashe.HarassMenu.UseW"] != null)
                            HarassMenu["Plugins.Ashe.HarassMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaForW
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Ashe.HarassMenu.MinManaForW"] != null)
                            return HarassMenu["Plugins.Ashe.HarassMenu.MinManaForW"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Ashe.HarassMenu.MinManaForW menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Ashe.HarassMenu.MinManaForW"] != null)
                            HarassMenu["Plugins.Ashe.HarassMenu.MinManaForW"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Ashe.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Ashe.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Ashe.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Ashe.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Ashe.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Ashe.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.AllowedEnemies"] != null)
                            return
                                LaneClearMenu["Plugins.Ashe.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Ashe.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Ashe.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }

                public static bool UseQInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Ashe.LaneClearMenu.UseQInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Ashe.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.UseQInLaneClear"] != null)
                            LaneClearMenu["Plugins.Ashe.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool UseQInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Ashe.LaneClearMenu.UseQInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Ashe.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.UseQInJungleClear"] != null)
                            LaneClearMenu["Plugins.Ashe.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.MinManaQ"] != null)
                            return LaneClearMenu["Plugins.Ashe.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Ashe.LaneClearMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.MinManaQ"] != null)
                            LaneClearMenu["Plugins.Ashe.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseWInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Ashe.LaneClearMenu.UseWInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Ashe.LaneClearMenu.UseWInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.UseWInLaneClear"] != null)
                            LaneClearMenu["Plugins.Ashe.LaneClearMenu.UseWInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool UseWInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Ashe.LaneClearMenu.UseWInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Ashe.LaneClearMenu.UseWInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.UseWInJungleClear"] != null)
                            LaneClearMenu["Plugins.Ashe.LaneClearMenu.UseWInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static int MinManaW
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.MinManaW"] != null)
                            return LaneClearMenu["Plugins.Ashe.LaneClearMenu.MinManaW"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Ashe.LaneClearMenu.MinManaW menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Ashe.LaneClearMenu.MinManaW"] != null)
                            LaneClearMenu["Plugins.Ashe.LaneClearMenu.MinManaW"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static int MaxInterrupterRange
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Ashe.MiscMenu.MaxInterrupterRange"] != null)
                            return MiscMenu["Plugins.Ashe.MiscMenu.MaxInterrupterRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Ashe.MiscMenu.MaxInterrupterRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Ashe.MiscMenu.MaxInterrupterRange"] != null)
                            MiscMenu["Plugins.Ashe.MiscMenu.MaxInterrupterRange"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Drawings
            {

                public static bool DrawSpellRangesWhenReady
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Ashe.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Ashe.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Ashe.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Ashe.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool DrawW
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Ashe.DrawingsMenu.DrawW"] != null &&
                               DrawingsMenu["Plugins.Ashe.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Ashe.DrawingsMenu.DrawW"] != null)
                            DrawingsMenu["Plugins.Ashe.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawR
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Ashe.DrawingsMenu.DrawR"] != null &&
                               DrawingsMenu["Plugins.Ashe.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Ashe.DrawingsMenu.DrawR"] != null)
                            DrawingsMenu["Plugins.Ashe.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
            }
        }
    }
}