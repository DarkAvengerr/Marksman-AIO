#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Kalista.cs" company="EloBuddy">
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
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Utils;
using SharpDX;
using Simple_Marksmans.Plugins.Kalista.Modes;
using Simple_Marksmans.Utils;
using Color = System.Drawing.Color;
using FontStyle = System.Drawing.FontStyle;

namespace Simple_Marksmans.Plugins.Kalista
{
    internal class Kalista : ChampionPlugin
    {
        public static Spell.Skillshot Q { get; }
        public static Spell.Active W { get; private set; }
        public static Spell.Active E { get; }
        public static Spell.Active R { get; }

        private static Menu ComboMenu { get; set; }
        private static Menu HarassMenu { get; set; }
        private static Menu JungleLaneClearMenu { get; set; }
        private static Menu FleeMenu { get; set; }
        private static Menu MiscMenu { get; set; }
        private static Menu DrawingsMenu { get; set; }

        public static AIHeroClient SouldBoundAlliedHero { get; private set; }

        private static readonly Text Text;
        private static readonly ColorPicker[] ColorPicker;

        static Kalista()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1150, SkillShotType.Linear, 250, 2400, 40)
            {
                AllowedCollisionCount = 0
            };
            W = new Spell.Active(SpellSlot.W, 5500);
            E = new Spell.Active(SpellSlot.E, 1000);
            R = new Spell.Active(SpellSlot.R, 1150);

            ColorPicker = new ColorPicker[4];

            ColorPicker[0] = new ColorPicker("KalistaQ", new ColorBGRA(243, 109, 160, 255));
            ColorPicker[1] = new ColorPicker("KalistaE", new ColorBGRA(255, 210, 54, 255));
            ColorPicker[2] = new ColorPicker("KalistaR", new ColorBGRA(1, 109, 160, 255));
            ColorPicker[3] = new ColorPicker("KalistaDamageIndicator", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(Color.FromArgb(ColorPicker[3].Color.A, ColorPicker[3].Color.R, ColorPicker[3].Color.G, ColorPicker[3].Color.B), true, Color.Azure, (int)E.Range);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[3].OnColorChange += (a, b) => { DamageIndicator.Color = Color.FromArgb(b.Color.A, b.Color.R, b.Color.G, b.Color.B);};

            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));

            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Orbwalker.OnUnkillableMinion += Orbwalker_OnUnkillableMinion;
            Game.OnTick += Game_OnTick;

            WallJumper.Init();
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            if (SouldBoundAlliedHero == null)
            {
                var entity = EntityManager.Heroes.Allies.Find(
                    unit => !unit.IsMe &&
                        unit.Buffs.Any(
                            n =>
                                n.Caster.IsMe &&
                                n.DisplayName.ToLowerInvariant() =="kalistapassivecoopstrike"));

                if (entity != null)
                {
                    var allies =
                        (from aiHeroClient in EntityManager.Heroes.Allies
                            where !aiHeroClient.IsMe
                            select aiHeroClient.Hero.ToString()).ToList();

                    MiscMenu["Plugins.Kalista.MiscMenu.SoulBoundHero"].Cast<ComboBox>().CurrentValue = allies.FindIndex(x=>x.Equals(entity.Hero.ToString()));

                    SouldBoundAlliedHero = entity;
                }
            }

            if (SouldBoundAlliedHero == null)
                return;

            if (R.IsReady() && Settings.Misc.SaveAlly && SouldBoundAlliedHero.HealthPercent < 15 && !SouldBoundAlliedHero.IsInShopRange() && IncomingDamage.GetIncomingDamage(SouldBoundAlliedHero) > SouldBoundAlliedHero.Health)
            {
                Misc.PrintInfoMessage("Saving <font color=\"#adff2f\">"+SouldBoundAlliedHero.Hero+"</font> from death.");
                R.Cast();
            }

            if (R.IsReady() && Settings.Misc.BlitzCombo &&
                SouldBoundAlliedHero.Position.Distance(Player.Instance) > Player.Instance.GetAutoAttackRange() &&
                Player.Instance.CountEnemiesInRange(1500) > 0)
            {
                switch (SouldBoundAlliedHero.Hero)
                {
                    case Champion.Blitzcrank:
                    {
                        var enemy =
                            EntityManager.Heroes.Enemies.FirstOrDefault(
                                x =>
                                    x.Buffs.Any(
                                        buff =>
                                            buff.IsActive && buff.Name.ToLowerInvariant() == "rocketgrab2" &&
                                            buff.Caster.NetworkId == SouldBoundAlliedHero.NetworkId));

                        if (enemy != null && enemy.Distance(Player.Instance) > 500)
                        {
                            if (Settings.Misc.BlitzComboKillable && enemy.Health < enemy.GetComboDamage(8))
                            {
                                Misc.PrintInfoMessage("Doing Blitzcrank-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                            else
                            {
                                Misc.PrintInfoMessage("Doing Blitzcrank-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                        }
                    }
                        break;
                    case Champion.TahmKench:
                    {
                        var enemy =
                            EntityManager.Heroes.Enemies.FirstOrDefault(
                                x =>
                                    x.Buffs.Any(
                                        buff =>
                                            buff.IsActive && buff.Name.ToLowerInvariant() == "tahmkenchwdevoured" &&
                                            buff.Caster.NetworkId == SouldBoundAlliedHero.NetworkId));

                        if (enemy != null && enemy.Distance(Player.Instance) > 500)
                        {
                            if (Settings.Misc.BlitzComboKillable && enemy.Health < enemy.GetComboDamage(8))
                            {
                                Misc.PrintInfoMessage("Doing Tahm Kench-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                            else
                            {
                                Misc.PrintInfoMessage("Doing Tahm Kench-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                        }
                    }
                        break;
                    case Champion.Skarner:
                    {
                        var enemy =
                            EntityManager.Heroes.Enemies.FirstOrDefault(
                                x =>
                                    x.Buffs.Any(
                                        buff =>
                                            buff.IsActive && buff.Name.ToLowerInvariant() == "skarnerimpale" &&
                                            buff.Caster.NetworkId == SouldBoundAlliedHero.NetworkId));

                        if (enemy != null && enemy.Distance(Player.Instance) > 500)
                        {
                            if (Settings.Misc.BlitzComboKillable && enemy.Health < enemy.GetComboDamage(8))
                            {
                                Misc.PrintInfoMessage("Doing Skarner-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                            else
                            {
                                Misc.PrintInfoMessage("Doing Skarner-Kalista combo on <font color=\"#ff1493\">" +
                                                      enemy.Hero + "</font>");
                                R.Cast();
                            }
                        }
                    }
                        break;
                }
            }
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!Q.IsReady() || !Settings.Combo.UseQ || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            var hero = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (hero == null || hero.IsDead || hero.HasSpellShield() ||  hero.HasUndyingBuffA())
                return;

            var prediction = Q.GetPrediction(hero);

            if (prediction.HitChancePercent >= 70)
            {
                Q.Cast(prediction.CastPosition);
            }
        }

        private static void Orbwalker_OnUnkillableMinion(Obj_AI_Base target, Orbwalker.UnkillableMinionArgs args)
        {
            if (!E.IsReady() || Player.Instance.ManaPercent < Settings.JungleLaneClear.MinManaForE ||
                !Settings.JungleLaneClear.UseE || !Settings.JungleLaneClear.UseEOnUnkillableMinions ||
                !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) ||
                Player.Instance.ManaPercent < Settings.JungleLaneClear.MinManaForE)
                return;

            var aiMinion = target as Obj_AI_Minion;

            if (aiMinion == null)
                return;

            if (aiMinion.IsTargetKillableByRend())
            {
                E.Cast();
            }
        }

        public static float HandleDamageIndicator(Obj_AI_Base target)
        {
            if (!Settings.Drawings.DrawDamageIndicator || Player.Instance.IsDead)
                return 0f;

            if (!(target is AIHeroClient))
                return target.GetRendDamageOnTarget();

            if(Settings.Drawings.DamageIndicatorMode == 0)
                return target.GetRendDamageOnTarget();

            var hero = (AIHeroClient) target;

            float damage = 0;

            damage += hero.GetRendDamageOnTarget();
            damage += hero.GetComboDamage(0);

            return damage;
        }

        protected override void OnDraw()
        {
            if (Player.Instance.IsDead)
                return;

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[1].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[2].Color, R.Range, Player.Instance);

            if (Settings.Flee.JumpWithQ && Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                WallJumper.DrawSpots();
            }

            if (!Settings.Drawings.DrawDamageIndicator)
                return;

            foreach (var source in EntityManager.Heroes.Enemies.Where(x => x.IsVisible && x.IsHPBarRendered && x.Position.IsOnScreen() && x.HasRendBuff()))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30; // tracker friendly.
                var timeLeft = source.GetRendBuff().EndTime - Game.Time;
                var endPos = timeLeft * 0x3e8 / 0x25;

                var degree = Misc.GetNumberInRangeFromProcent(timeLeft * 1000d / 4000d * 100d, 3, 110);
                var color = new Misc.HsvColor(degree, 1, 1).ColorFromHsv();

                Text.X = (int)(hpPosition.X + endPos);
                Text.Y = (int)hpPosition.Y + 15; // + text size 
                Text.Color = color;
                Text.TextValue = timeLeft.ToString("F1");
                Text.Draw();

                var percentDamage = Math.Min(100, source.GetRendDamageOnTarget() / source.TotalHealthWithShields() * 100);

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
            ComboMenu.AddGroupLabel("Combo mode settings for Kalista addon");

            ComboMenu.AddLabel("Pierce (Q) settings :");
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Rend (E) settings :");
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseE", new CheckBox("Use E to execute"));
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEBeforeDeath", new CheckBox("Use E before death"));
            ComboMenu.AddSeparator(1);
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRange",
                new CheckBox("Use E before enemy leaves the range of E", false));
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRangeS",
                new Slider("Minimum percentage ({0}%) damage dealt", 50, 15, 99));
            ComboMenu.AddLabel(
                "Uses E to before enemy leaves range if Rend can deal desired percentage amount of his health.");
            ComboMenu.AddSeparator(1);

            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEToSlow", new CheckBox("Use E to slow"));
            ComboMenu.Add("Plugins.Kalista.ComboMenu.UseEToSlowMinMinions",
                new Slider("Use E to slow min minions", 2, 1, 6));
            ComboMenu.AddLabel("Uses E to slow enemy when desired amout of minions can be killed using Rend.");
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Additional settings :");
            ComboMenu.Add("Plugins.Kalista.ComboMenu.JumpOnMinions", new CheckBox("Use minions to jump"));
            ComboMenu.AddLabel("Uses minions to jump when enemy is outside Kalista's auto attack range.");
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Kalista addon");
            HarassMenu.AddLabel("Pierce (Q) settings :");
            HarassMenu.Add("Plugins.Kalista.HarassMenu.UseQ", new CheckBox("Use Q"));
            HarassMenu.Add("Plugins.Kalista.HarassMenu.MinManaForQ",
                new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Rend (E) settings :");
            HarassMenu.Add("Plugins.Kalista.HarassMenu.UseE", new CheckBox("Use E"));
            HarassMenu.Add("Plugins.Kalista.HarassMenu.UseEIfManaWillBeRestored",
                new CheckBox("Use E only if mana will be restored"));
            HarassMenu.Add("Plugins.Kalista.HarassMenu.MinManaForE",
                new Slider("Min mana percentage ({0}%) to use E", 50, 1));
            HarassMenu.Add("Plugins.Kalista.HarassMenu.MinStacksForE", new Slider("Min stacks to use E", 3, 2, 12));

            JungleLaneClearMenu = MenuManager.Menu.AddSubMenu("Jungle and Lane clear");
            JungleLaneClearMenu.AddGroupLabel("Jungle and Lane clear mode settings for Kalista addon");

            JungleLaneClearMenu.AddLabel("Pierce (Q) settings :");
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseQ", new CheckBox("Use Q"));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.MinManaForQ",
                new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.MinMinionsForQ",
                new Slider("Min minions killed to use Q", 3, 1, 6));
            JungleLaneClearMenu.AddSeparator(5);

            JungleLaneClearMenu.AddLabel("Rend (E) settings :");
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseE", new CheckBox("Use E"));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseEForUnkillable",
                new CheckBox("Use E to lasthit unkillable minions"));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.MinManaForE",
                new Slider("Min mana percentage ({0}%) to use E", 50, 1));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.MinMinionsForE",
                new Slider("Min minions killed to use E", 3, 1, 6));
            JungleLaneClearMenu.AddSeparator(5);

            JungleLaneClearMenu.AddLabel("Additional settings :");
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseEToStealBuffs",
                new CheckBox("Use E to steal buffs"));
            JungleLaneClearMenu.Add("Plugins.Kalista.JungleLaneClearMenu.UseEToStealDragon",
                new CheckBox("Use E to steal Dragon / Baron"));

            FleeMenu = MenuManager.Menu.AddSubMenu("Flee");
            FleeMenu.AddGroupLabel("Flee mode settings for Kalista addon");
            FleeMenu.Add("Plugins.Kalista.FleeMenu.Jump", new CheckBox("Try to jump over walls using Q"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Kalista addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Pierce (Q) drawing settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawQColor", new CheckBox("Change color", false))
                .OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[0].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Rend (E) drawing settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawEColor", new CheckBox("Change color", false))
                .OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[1].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Fate's Call (R) drawing settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawRColor", new CheckBox("Change color", false))
                .OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[2].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Damage indicator drawing settings :");
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawDamageIndicator",
                new CheckBox("Draw damage indicator on enemy HP bars")).OnValueChange += (a, b) =>
                {
                    if (b.NewValue)
                        DamageIndicator.DamageDelegate = HandleDamageIndicator;
                    else if (!b.NewValue)
                        DamageIndicator.DamageDelegate = null;
                };
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DrawDamageIndicatorColor",
                new CheckBox("Change color", false)).OnValueChange +=
                (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[3].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.Add("Plugins.Kalista.DrawingsMenu.DamageIndicatorMode",
                new ComboBox("Damage indicator mode", 0, "Only E damage", "Combo damage"));


            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Kalista addon");
            MiscMenu.AddLabel("Soulbound settings : ");
            MiscMenu.Add("Plugins.Kalista.MiscMenu.SaveAlly", new CheckBox("Save your Soulbound ally from danger"));
            MiscMenu.AddSeparator(5);
            MiscMenu.Add("Plugins.Kalista.MiscMenu.BlitzCombo", new CheckBox("Enable Blitzcrank combo", false));
            MiscMenu.Add("Plugins.Kalista.MiscMenu.BlitzComboKillable",
                new CheckBox("Blitzcrank combo only if enemy is killable"));
            MiscMenu.AddLabel("Uses R when blitzcrank grabbed someone. Works also with Tahm Kench and Skarner.");
            MiscMenu.AddSeparator(5);

            MiscMenu.Add("Plugins.Kalista.MiscMenu.ReduceEDmg",
                new Slider("Reduce E damage calculations by ({0}%) percent", 5, 1));
            MiscMenu.AddLabel(
                "Reduces calculated Rend damage by desired amount. Might help if Kalista uses E too early.");
            MiscMenu.AddSeparator(5);

            var allies =
                (from aiHeroClient in EntityManager.Heroes.Allies
                    where !aiHeroClient.IsMe
                    select aiHeroClient.Hero.ToString()).ToList();

            var soulBound = MiscMenu.Add("Plugins.Kalista.MiscMenu.SoulBoundHero", new ComboBox("Soulbound : ", allies));
            soulBound.OnValueChange += (a, b) =>
            {
                SouldBoundAlliedHero = EntityManager.Heroes.Allies.Find(x => x.Hero.ToString() == soulBound.DisplayName);
            };
        }

        protected override void PermaActive()
        {
            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            Combo.Execute();
        }

        protected override void HarassMode()
        {
            Harass.Execute();
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
                        return ComboMenu?["Plugins.Kalista.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Kalista.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Kalista.ComboMenu.UseQ"].Cast<CheckBox>().CurrentValue
                                = value;
                    }
                }
                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Kalista.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Kalista.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Kalista.ComboMenu.UseE"].Cast<CheckBox>().CurrentValue
                                = value;
                    }
                }

                public static bool UseEBeforeDeath
                {
                    get
                    {
                        return ComboMenu?["Plugins.Kalista.ComboMenu.UseEBeforeDeath"] != null &&
                               ComboMenu["Plugins.Kalista.ComboMenu.UseEBeforeDeath"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.UseEBeforeDeath"] != null)
                            ComboMenu["Plugins.Kalista.ComboMenu.UseEBeforeDeath"].Cast<CheckBox>().CurrentValue
                                = value;
                    }
                }

                public static bool UseEBeforeEnemyLeavesRange
                {
                    get
                    {
                        return ComboMenu?["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRange"] != null &&
                               ComboMenu["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRange"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRange"] != null)
                            ComboMenu["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRange"].Cast<CheckBox>().CurrentValue
                                = value;
                    }
                }

                public static int MinDamagePercToUseEBeforeEnemyLeavesRange
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRangeS"] != null)
                            return
                                ComboMenu["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRangeS"].Cast<Slider>()
                                   .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRangeS menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRangeS"] != null)
                            ComboMenu["Plugins.Kalista.ComboMenu.UseEBeforeEnemyLeavesRangeS"].Cast<Slider>().CurrentValue
                                = value;
                    }
                }

                public static bool UseEToSlow
                {
                    get
                    {
                        return ComboMenu?["Plugins.Kalista.ComboMenu.UseEToSlow"] != null &&
                               ComboMenu["Plugins.Kalista.ComboMenu.UseEToSlow"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.UseEToSlow"] != null)
                            ComboMenu["Plugins.Kalista.ComboMenu.UseEToSlow"].Cast<CheckBox>().CurrentValue
                                = value;
                    }
                }

                public static int UseEToSlowMinMinions
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.UseEToSlowMinMinions"] != null)
                            return
                                ComboMenu["Plugins.Kalista.ComboMenu.UseEToSlowMinMinions"].Cast<Slider>()
                                   .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.ComboMenu.UseEToSlowMinMinions menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.UseEToSlowMinMinions"] != null)
                            ComboMenu["Plugins.Kalista.ComboMenu.UseEToSlowMinMinions"].Cast<Slider>().CurrentValue
                                = value;
                    }
                }

                public static bool JumpOnMinions
                {
                    get
                    {
                        return ComboMenu?["Plugins.Kalista.ComboMenu.JumpOnMinions"] != null &&
                               ComboMenu["Plugins.Kalista.ComboMenu.JumpOnMinions"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Kalista.ComboMenu.JumpOnMinions"] != null)
                            ComboMenu["Plugins.Kalista.ComboMenu.JumpOnMinions"].Cast<CheckBox>().CurrentValue
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
                        return HarassMenu?["Plugins.Kalista.HarassMenu.UseQ"] != null &&
                               HarassMenu["Plugins.Kalista.HarassMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Kalista.HarassMenu.UseQ"] != null)
                            HarassMenu["Plugins.Kalista.HarassMenu.UseQ"].Cast<CheckBox>().CurrentValue
                                = value;
                    }
                }

                public static int MinManaForQ
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Kalista.HarassMenu.MinManaForQ"] != null)
                            return
                                HarassMenu["Plugins.Kalista.HarassMenu.MinManaForQ"].Cast<Slider>()
                                    .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.HarassMenu.MinManaForQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Kalista.HarassMenu.MinManaForQ"] != null)
                            HarassMenu["Plugins.Kalista.HarassMenu.MinManaForQ"].Cast<Slider>().CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return HarassMenu?["Plugins.Kalista.HarassMenu.UseE"] != null &&
                               HarassMenu["Plugins.Kalista.HarassMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Kalista.HarassMenu.UseE"] != null)
                            HarassMenu["Plugins.Kalista.HarassMenu.UseE"].Cast<CheckBox>().CurrentValue
                                = value;
                    }
                }

                public static bool UseEIfManaWillBeRestored
                {
                    get
                    {
                        return HarassMenu?["Plugins.Kalista.HarassMenu.UseEIfManaWillBeRestored"] != null &&
                               HarassMenu["Plugins.Kalista.HarassMenu.UseEIfManaWillBeRestored"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Kalista.HarassMenu.UseEIfManaWillBeRestored"] != null)
                            HarassMenu["Plugins.Kalista.HarassMenu.UseEIfManaWillBeRestored"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaForE
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Kalista.HarassMenu.MinManaForE"] != null)
                            return
                                HarassMenu["Plugins.Kalista.HarassMenu.MinManaForE"].Cast<Slider>()
                                    .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.HarassMenu.MinManaForE menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Kalista.HarassMenu.MinManaForE"] != null)
                            HarassMenu["Plugins.Kalista.HarassMenu.MinManaForE"].Cast<Slider>().CurrentValue
                                = value;
                    }
                }

                public static int MinStacksForE
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Kalista.HarassMenu.MinStacksForE"] != null)
                            return
                                HarassMenu["Plugins.Kalista.HarassMenu.MinStacksForE"].Cast<Slider>()
                                    .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.HarassMenu.MinStacksForE menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Kalista.HarassMenu.MinStacksForE"] != null)
                            HarassMenu["Plugins.Kalista.HarassMenu.MinStacksForE"].Cast<Slider>().CurrentValue
                                = value;
                    }
                }
            }

            internal static class JungleLaneClear
            {
                public static bool UseQ
                {
                    get
                    {
                        return JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseQ"] != null &&
                               JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseQ"] != null)
                            JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaForQ
                {
                    get
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.MinManaForQ"] != null)
                            return
                                JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.MinManaForQ"].Cast<Slider>()
                                    .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.JungleLaneClearMenu.MinManaForQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.MinManaForQ"] != null)
                            JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.MinManaForQ"].Cast<Slider>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinMinionsForQ
                {
                    get
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForQ"] != null)
                            return
                                JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForQ"].Cast<Slider>()
                                    .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.JungleLaneClearMenu.MinMinionsForQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForQ"] != null)
                            JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForQ"].Cast<Slider>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseE"] != null &&
                               JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseE"] != null)
                            JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseEOnUnkillableMinions
                {
                    get
                    {
                        return JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseEForUnkillable"] != null &&
                               JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseEForUnkillable"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseEForUnkillable"] != null)
                            JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseEForUnkillable"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaForE
                {
                    get
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.MinManaForE"] != null)
                            return
                                JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.MinManaForE"].Cast<Slider>()
                                    .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.JungleLaneClearMenu.MinManaForE menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.MinManaForE"] != null)
                            JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.MinManaForE"].Cast<Slider>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinMinionsForE
                {
                    get
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForE"] != null)
                            return
                                JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForE"].Cast<Slider>()
                                    .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.JungleLaneClearMenu.MinMinionsForE menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForE"] != null)
                            JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.MinMinionsForE"].Cast<Slider>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseEToStealBuffs
                {
                    get
                    {
                        return JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseEToStealBuffs"] != null &&
                               JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseEToStealBuffs"]
                                   .Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseEToStealBuffs"] != null)
                            JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseEToStealBuffs"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseEToStealDragon
                {
                    get
                    {
                        return JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseEToStealDragon"] != null &&
                               JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseEToStealDragon"]
                                   .Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (JungleLaneClearMenu?["Plugins.Kalista.JungleLaneClearMenu.UseEToStealDragon"] != null)
                            JungleLaneClearMenu["Plugins.Kalista.JungleLaneClearMenu.UseEToStealDragon"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
            }

            internal static class Flee
            {
                public static bool JumpWithQ
                {
                    get
                    {
                        return FleeMenu?["Plugins.Kalista.FleeMenu.Jump"] != null &&
                               FleeMenu["Plugins.Kalista.FleeMenu.Jump"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (FleeMenu?["Plugins.Kalista.FleeMenu.Jump"] != null)
                            FleeMenu["Plugins.Kalista.FleeMenu.Jump"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool DrawQ
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawQ"] != null &&
                               DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawQ"] != null)
                            DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawE
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawE"] != null &&
                               DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawE"] != null)
                            DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawR
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawR"] != null &&
                               DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawR"] != null)
                            DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawDamageIndicator
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawDamageIndicator"] != null &&
                               DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawDamageIndicator"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DrawDamageIndicator"] != null)
                            DrawingsMenu["Plugins.Kalista.DrawingsMenu.DrawDamageIndicator"].Cast<CheckBox>()
                                .CurrentValue =
                                value;
                    }
                }

                public static int DamageIndicatorMode
                {
                    get
                    {
                        if (DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DamageIndicatorMode"] != null)
                            return
                                DrawingsMenu["Plugins.Kalista.DrawingsMenu.DamageIndicatorMode"].Cast<ComboBox>()
                                    .CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.DrawingsMenu.DamageIndicatorMode menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Kalista.DrawingsMenu.DamageIndicatorMode"] != null)
                            DrawingsMenu["Plugins.Kalista.DrawingsMenu.DamageIndicatorMode"].Cast<ComboBox>()
                                .CurrentValue =
                                value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool SaveAlly
                {
                    get
                    {
                        return MiscMenu?["Plugins.Kalista.MiscMenu.SaveAlly"] != null &&
                               MiscMenu["Plugins.Kalista.MiscMenu.SaveAlly"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Kalista.MiscMenu.SaveAlly"] != null)
                            MiscMenu["Plugins.Kalista.MiscMenu.SaveAlly"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool BlitzCombo
                {
                    get
                    {
                        return MiscMenu?["Plugins.Kalista.MiscMenu.BlitzCombo"] != null &&
                               MiscMenu["Plugins.Kalista.MiscMenu.BlitzCombo"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Kalista.MiscMenu.BlitzCombo"] != null)
                            MiscMenu["Plugins.Kalista.MiscMenu.BlitzCombo"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool BlitzComboKillable
                {
                    get
                    {
                        return MiscMenu?["Plugins.Kalista.MiscMenu.BlitzComboKillable"] != null &&
                               MiscMenu["Plugins.Kalista.MiscMenu.BlitzComboKillable"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Kalista.MiscMenu.BlitzComboKillable"] != null)
                            MiscMenu["Plugins.Kalista.MiscMenu.BlitzComboKillable"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static int ReduceEDmg
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Kalista.MiscMenu.ReduceEDmg"] != null)
                            return MiscMenu["Plugins.Kalista.MiscMenu.ReduceEDmg"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Kalista.MiscMenu.ReduceEDmg menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Kalista.MiscMenu.ReduceEDmg"] != null)
                            MiscMenu["Plugins.Kalista.MiscMenu.ReduceEDmg"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }
        }
    }
}