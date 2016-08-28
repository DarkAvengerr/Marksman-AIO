#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Urgot.cs" company="EloBuddy">
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
using Simple_Marksmans.Utils;
using Color = System.Drawing.Color;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Utils;
using SharpDX;
using Simple_Marksmans.Utils.PermaShow;

namespace Simple_Marksmans.Plugins.Urgot
{
    internal class Urgot : ChampionPlugin
    {
        public static Spell.Skillshot Q { get; }
        public static Spell.Skillshot SecoundQ { get; }
        public static Spell.Active W { get; }
        public static Spell.Skillshot E { get; }
        public static Spell.Targeted R { get; }

        private static Menu ComboMenu { get; set; }
        private static Menu HarassMenu { get; set; }
        private static Menu LaneClearMenu { get; set; }
        private static Menu MiscMenu { get; set; }
        private static Menu DrawingsMenu { get; set; }

        private static readonly ColorPicker[] ColorPicker;

        public static List<Obj_AI_Base> CorrosiveDebufTargets { get; }=
            new List<Obj_AI_Base>();

        private static float _lastScanTick;
        private static bool _changingRangeScan;

        public static int FleeRange { get; } = 375;

        public static bool HasSpottedBuff(Obj_AI_Base unit)
        {
            return CorrosiveDebufTargets.Contains(unit);
        }

        public static PermaShow PermaShow { get; }
        public static BoolItemData AutoHarassBoolItemData { get; private set; }

        static Urgot()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 900, SkillShotType.Linear, 250, 1550, 60);
            SecoundQ = new Spell.Skillshot(SpellSlot.Q, 1200, SkillShotType.Linear, 250, 1550, 60)
            {
                AllowedCollisionCount = int.MaxValue
            };

            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 900, SkillShotType.Circular, 250, 1550, 250);
            R = new Spell.Targeted(SpellSlot.R, 500);

            ColorPicker = new ColorPicker[4];

            ColorPicker[0] = new ColorPicker("UrgotQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("UrgotE", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("UrgotR", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[3] = new ColorPicker("UrgotHpBar", new ColorBGRA(255, 134, 0, 255));


            DamageIndicator.Initalize(Color.FromArgb(ColorPicker[3].Color.R, ColorPicker[3].Color.G, ColorPicker[3].Color.B));
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[3].OnColorChange +=
                (a, b) =>
                    DamageIndicator.Color =
                        Color.FromArgb(ColorPicker[3].Color.R, ColorPicker[3].Color.G, ColorPicker[3].Color.B);

            PermaShow = new PermaShow("Urgot PermaShow", new Vector2(200, 200));

            Game.OnTick += Game_OnTick;
            Game.OnPostTick += args =>
            {
                Q.Range = 900;
                Q.AllowedCollisionCount = 0;
            };
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (target is AIHeroClient && Settings.Combo.UseW && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Player.Instance.Mana - 50 + 5*(E.Level - 1) > 220)
            {
                W.Cast();
            }
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (Game.Time*1000 - _lastScanTick < 66)
            {
                return;
            }

            CorrosiveDebufTargets.Clear();

            foreach (var enemy in ObjectManager.Get<Obj_AI_Base>().Where(x=> !x.IsDead && x.Team != Player.Instance.Team && x.HasBuff("urgotcorrosivedebuff")).Where(enemy => !CorrosiveDebufTargets.Contains(enemy)))
            {
                CorrosiveDebufTargets.Add(enemy);
            }
            _lastScanTick = Game.Time*1000;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Urgot.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[1].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[2].Color, R.Range, Player.Instance);
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!R.IsReady() || !Settings.Misc.UseRToInterrupt || !sender.IsValidTarget(R.Range) || sender.IsUnderTurret())
                return;

            if (args.Delay == 0)
            {
                R.Cast(sender);
            } else Core.DelayAction(() => R.Cast(sender), args.Delay);
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (!R.IsReady() || !(args.End.Distance(Player.Instance) < 350) || !Settings.Misc.UseRAgainstGapclosers ||
                !sender.IsValidTarget(R.Range) || sender.IsUnderTurret())
                return;

            if (args.Delay == 0)
            {
                R.Cast(sender);
            }
            else Core.DelayAction(() => R.Cast(sender), args.Delay);
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawInfo)
            {
                return 0;
            }

            var enemy = (AIHeroClient)unit;

            if (enemy == null)
                return 0;
            
            var damage = 0f;
            if (Q.IsReady() && unit.IsValidTarget(Q.Range))
                damage += Player.Instance.GetSpellDamage(unit, SpellSlot.Q);
            if(E.IsReady() && unit.IsValidTarget(Q.Range))
                damage += Player.Instance.GetSpellDamage(unit, SpellSlot.Q);
            if (unit.IsValidTarget(Player.Instance.GetAutoAttackRange()))
                damage += Player.Instance.GetAutoAttackDamage(unit);

            return damage;
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Urgot addon");

            ComboMenu.AddLabel("Acid Hunter (Q) settings :");
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Terror Capacitor (W) settings :");
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Noxian Corrosive Charge (E) settings :");
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Hyper-Kinetic Position Reverser (R) settings :");
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Urgot.ComboMenu.UseRToSwapPosUnderTower", new CheckBox("Try to swap enemy pos under tower"));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Urgot addon");

            HarassMenu.AddLabel("Acid Hunter (Q) settings :");
            HarassMenu.Add("Plugins.Urgot.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.Urgot.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 40, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Noxian Corrosive Charge (E) settings :");
            HarassMenu.Add("Plugins.Urgot.HarassMenu.UseE", new CheckBox("Use E"));
            HarassMenu.Add("Plugins.Urgot.HarassMenu.MinManaE", new Slider("Min mana percentage ({0}%) to use E", 40, 1));
            HarassMenu.AddSeparator(5);
            
            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Urgot addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Acid Hunter (Q) settings :");
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Noxian Corrosive Charge (E) settings :");
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.UseEInLaneClear", new CheckBox("Use E in Lane Clear"));
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.UseEInJungleClear", new CheckBox("Use E in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Urgot.LaneClearMenu.MinManaE", new Slider("Min mana percentage ({0}%) to use E", 50, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Urgot addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Urgot.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Acid Hunter (Q) settings :");
            MiscMenu.Add("Plugins.Urgot.MiscMenu.AutoHarass",
                new KeyBind("Auto harass", true, KeyBind.BindTypes.PressToggle, 'T')).OnValueChange +=
                (a, b) =>
                {
                    if (AutoHarassBoolItemData != null)
                    {
                        AutoHarassBoolItemData.Value = b.NewValue;
                    }
                };
            MiscMenu.AddLabel("Enables Auto harass on enemies with E debuff in Lane Clear and Harass mode !");
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Terror Capacitor (W) settings :");
            MiscMenu.Add("Plugins.Urgot.MiscMenu.WMinDamage", new Slider("Auto W if incoming damage will deal more than {0}% of my hp", 10, 1));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Hyper-Kinetic Position Reverser (R) settings :");
            MiscMenu.Add("Plugins.Urgot.MiscMenu.UseRAgainstGapclosers", new CheckBox("Use R against gapclosers"));
            MiscMenu.Add("Plugins.Urgot.MiscMenu.UseRToInterrupt", new CheckBox("Use R to interrupt dangerous spells"));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Urgot addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Acid Hunter (Q) settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Noxian Corrosive Charge (E) settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawEColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Hyper-Kinetic Position Reverser (R) settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawR", new CheckBox("Draw R range", false));
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Other settings :");
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.DrawInfo", new CheckBox("Draw Infos")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Urgot.DrawingsMenu.InfoColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[3].Initialize(Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddLabel("Draws damage indicator");

            AutoHarassBoolItemData = PermaShow.AddItem("Auto harass", new BoolItemData("Enable Auto harass", Settings.Misc.AutoHarass, 14));
        }

        protected override void PermaActive()
        {
            switch (R.Level)
            {
                case 1:
                {
                    R.Range = 500;
                    break;
                }
                case 2:
                {
                    R.Range = 700;
                    break;
                }
                case 3:
                {
                    R.Range = 850;
                    break;
                }
                default:
                    R.Range = 0;
                    break;
            }

            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            if (!Player.Instance.HasSheenBuff())
            {
                Modes.Combo.Execute();
            }
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
                        return ComboMenu?["Plugins.Urgot.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Urgot.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Urgot.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Urgot.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Urgot.ComboMenu.UseW"] != null &&
                               ComboMenu["Plugins.Urgot.ComboMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Urgot.ComboMenu.UseW"] != null)
                            ComboMenu["Plugins.Urgot.ComboMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Urgot.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Urgot.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Urgot.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Urgot.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Urgot.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Urgot.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Urgot.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Urgot.ComboMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }


                public static bool UseRToSwapPosUnderTower
                {
                    get
                    {
                        return ComboMenu?["Plugins.Urgot.ComboMenu.UseRToSwapPosUnderTower"] != null &&
                               ComboMenu["Plugins.Urgot.ComboMenu.UseRToSwapPosUnderTower"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Urgot.ComboMenu.UseRToSwapPosUnderTower"] != null)
                            ComboMenu["Plugins.Urgot.ComboMenu.UseRToSwapPosUnderTower"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
            }

            internal static class Harass
            {
                public static bool UseQ
                {
                    get
                    {
                        return HarassMenu?["Plugins.Urgot.HarassMenu.UseQ"] != null &&
                               HarassMenu["Plugins.Urgot.HarassMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Urgot.HarassMenu.UseQ"] != null)
                            HarassMenu["Plugins.Urgot.HarassMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Urgot.HarassMenu.MinManaQ"] != null)
                            return HarassMenu["Plugins.Urgot.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Urgot.HarassMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Urgot.HarassMenu.MinManaQ"] != null)
                            HarassMenu["Plugins.Urgot.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return HarassMenu?["Plugins.Urgot.HarassMenu.UseE"] != null &&
                               HarassMenu["Plugins.Urgot.HarassMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Urgot.HarassMenu.UseE"] != null)
                            HarassMenu["Plugins.Urgot.HarassMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaE
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Urgot.HarassMenu.MinManaE"] != null)
                            return HarassMenu["Plugins.Urgot.HarassMenu.MinManaE"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Urgot.HarassMenu.MinManaE menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Urgot.HarassMenu.MinManaE"] != null)
                            HarassMenu["Plugins.Urgot.HarassMenu.MinManaE"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Urgot.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Urgot.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Urgot.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Urgot.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Urgot.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Urgot.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.AllowedEnemies"] != null)
                            return
                                LaneClearMenu["Plugins.Urgot.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Urgot.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Urgot.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }

                public static bool UseQInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Urgot.LaneClearMenu.UseQInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Urgot.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.UseQInLaneClear"] != null)
                            LaneClearMenu["Plugins.Urgot.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseQInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Urgot.LaneClearMenu.UseQInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Urgot.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.UseQInJungleClear"] != null)
                            LaneClearMenu["Plugins.Urgot.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.MinManaQ"] != null)
                            return LaneClearMenu["Plugins.Urgot.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Urgot.LaneClearMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.MinManaQ"] != null)
                            LaneClearMenu["Plugins.Urgot.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseEInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Urgot.LaneClearMenu.UseEInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Urgot.LaneClearMenu.UseEInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.UseEInLaneClear"] != null)
                            LaneClearMenu["Plugins.Urgot.LaneClearMenu.UseEInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseEInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Urgot.LaneClearMenu.UseEInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Urgot.LaneClearMenu.UseEInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.UseEInJungleClear"] != null)
                            LaneClearMenu["Plugins.Urgot.LaneClearMenu.UseEInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaE
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.MinManaE"] != null)
                            return LaneClearMenu["Plugins.Urgot.LaneClearMenu.MinManaE"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Urgot.LaneClearMenu.MinManaE menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Urgot.LaneClearMenu.MinManaE"] != null)
                            LaneClearMenu["Plugins.Urgot.LaneClearMenu.MinManaE"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool EnableKillsteal
                {
                    get
                    {
                        return MiscMenu?["Plugins.Urgot.MiscMenu.EnableKillsteal"] != null &&
                               MiscMenu["Plugins.Urgot.MiscMenu.EnableKillsteal"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Urgot.MiscMenu.EnableKillsteal"] != null)
                            MiscMenu["Plugins.Urgot.MiscMenu.EnableKillsteal"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool AutoHarass
                {
                    get
                    {
                        return MiscMenu?["Plugins.Urgot.MiscMenu.AutoHarass"] != null &&
                               MiscMenu["Plugins.Urgot.MiscMenu.AutoHarass"].Cast<KeyBind>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Urgot.MiscMenu.AutoHarass"] != null)
                            MiscMenu["Plugins.Urgot.MiscMenu.AutoHarass"].Cast<KeyBind>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseRAgainstGapclosers
                {
                    get
                    {
                        return MiscMenu?["Plugins.Urgot.MiscMenu.UseRAgainstGapclosers"] != null &&
                               MiscMenu["Plugins.Urgot.MiscMenu.UseRAgainstGapclosers"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Urgot.MiscMenu.UseRAgainstGapclosers"] != null)
                            MiscMenu["Plugins.Urgot.MiscMenu.UseRAgainstGapclosers"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                public static bool UseRToInterrupt
                {
                    get
                    {
                        return MiscMenu?["Plugins.Urgot.MiscMenu.UseRToInterrupt"] != null &&
                               MiscMenu["Plugins.Urgot.MiscMenu.UseRToInterrupt"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Urgot.MiscMenu.UseRToInterrupt"] != null)
                            MiscMenu["Plugins.Urgot.MiscMenu.UseRToInterrupt"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinDamage
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Urgot.MiscMenu.WMinDamage"] != null)
                            return MiscMenu["Plugins.Urgot.MiscMenu.WMinDamage"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Urgot.MiscMenu.WMinDamage menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Urgot.MiscMenu.WMinDamage"] != null)
                            MiscMenu["Plugins.Urgot.MiscMenu.WMinDamage"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool DrawQ
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawQ"] != null &&
                               DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawQ"] != null)
                            DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawE
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawE"] != null &&
                               DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawE"] != null)
                            DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawR
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawR"] != null &&
                               DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawR"] != null)
                            DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawInfo
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawInfo"] != null &&
                               DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawInfo"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Urgot.DrawingsMenu.DrawInfo"] != null)
                            DrawingsMenu["Plugins.Urgot.DrawingsMenu.DrawInfo"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
            }
        }
    }
}