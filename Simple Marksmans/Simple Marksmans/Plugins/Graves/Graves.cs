#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Graves.cs" company="EloBuddy">
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
using Simple_Marksmans.Utils;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Utils;
using SharpDX;
using EloBuddy.SDK.Rendering;

namespace Simple_Marksmans.Plugins.Graves
{
    internal class Graves : ChampionPlugin
    {
        protected static Spell.Skillshot Q { get; }
        protected static Spell.Skillshot W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }
        protected static Spell.Skillshot RCone { get; }

        protected static Menu ComboMenu { get; set; }
        protected static Menu HarassMenu { get; set; }
        protected static Menu LaneClearMenu { get; set; }
        protected static Menu DrawingsMenu { get; set; }
        protected static Menu MiscMenu { get; set; }

        private static ColorPicker[] ColorPicker { get; }

        protected static int[] QMana { get; } = {0, 60, 70, 80, 90, 100};
        protected static int[] WMana { get; } = {0, 70, 75, 80, 85, 90};
        protected static int EMana { get; } = 40;
        protected static int RMana { get; } = 100;

        protected static bool DardochTrick { get; set; }
        protected static AIHeroClient DardochTrickTarget { get; set; }

        private static bool _changingRangeScan;
        private static bool _rcasted;
        private static bool _ecasted;
        private static float _tick;

        protected static bool IsReloading
            => !Player.Instance.Buffs.Any(b => b.IsActive && b.Name.ToLowerInvariant() == "gravesbasicattackammo1");

        protected static int GetAmmoCount
            => IsReloading
                ? 0
                : Player.Instance.Buffs.Any(b => b.IsActive && b.Name.ToLowerInvariant() == "gravesbasicattackammo2")
                    ? 2
                    : 1;

        static Graves()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 950, SkillShotType.Linear, 500, 2500, 50)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Skillshot(SpellSlot.W, 950, SkillShotType.Circular, 250, 1200, 250);
            E = new Spell.Skillshot(SpellSlot.E, 425, SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, 1000, SkillShotType.Linear, 250, 2000, 110)
            {
                AllowedCollisionCount = 0
            };
            RCone = new Spell.Skillshot(SpellSlot.R, 800, SkillShotType.Cone, 250, 2000, 110)
            {
                ConeAngleDegrees = (int) (Math.PI/180*70)
            };

            ColorPicker = new ColorPicker[3];

            ColorPicker[0] = new ColorPicker("GravesQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("GravesR", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("GravesHpBar", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(System.Drawing.Color.FromArgb(ColorPicker[2].Color.R, ColorPicker[2].Color.G,
                ColorPicker[2].Color.B));
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[2].OnColorChange +=
                (a, b) =>
                {
                    DamageIndicator.Color = System.Drawing.Color.FromArgb(b.Color.A, b.Color.R, b.Color.G, b.Color.B);
                };

            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            ChampionTracker.Initialize(ChampionTrackerFlags.LongCastTimeTracker);
            ChampionTracker.OnLongSpellCast += ChampionTracker_OnLongSpellCast;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!DardochTrick)
                return;

            if (args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W)
                args.Process = false;

            if (args.Slot == SpellSlot.R)
            {
                _rcasted = true;
                _ecasted = false;

                _tick = Game.Time * 1000;
            }
            if (args.Slot == SpellSlot.E)
            {
                _ecasted = true;

                DardochTrick = false;
                DardochTrickTarget = null;
                _rcasted = false;
            }
        }

        private static void ChampionTracker_OnLongSpellCast(object sender, OnLongSpellCastEventArgs e)
        {
            if (!W.IsReady() || !(Player.Instance.Mana - WMana[W.Level] > EMana + RMana) || !Settings.Combo.UseW)
                return;

            if (e.IsTeleport)
            {
                Core.DelayAction(() =>
                {
                    if (W.IsReady() && e.EndPosition.Distance(Player.Instance) < W.Range)
                    {
                        W.Cast(e.EndPosition);
                    }
                }, 4000);
            }
            else if (!e.IsTeleport && e.Sender.IsValidTarget(W.Range))
            {
                var wPrediction = W.GetPrediction(e.Sender);
                if (wPrediction.HitChancePercent >= 60)
                {
                    W.Cast(e.Sender);
                }
            }
        }

        protected static IEnumerable<AIHeroClient> GetRSplashHits(Obj_AI_Base targetToCheckFrom)
        {
            var coneApexPoint = targetToCheckFrom.Position;
            var conePolygon = new Geometry.Polygon.Sector(coneApexPoint, (coneApexPoint-Player.Instance.Position).Normalized(), (float)(Math.PI / 180 * 70), RCone.Range);

            return EntityManager.Heroes.Enemies.Where(x => !x.IsDead && !x.HasUndyingBuffA() && new Geometry.Polygon.Circle(x.Position, x.BoundingRadius).Points.Any(p => conePolygon.IsInside(p)));
        }

        protected static IEnumerable<AIHeroClient> GetRSplashHits(Vector3 positionToCheckFrom)
        {
            var conePolygon = new Geometry.Polygon.Sector(positionToCheckFrom, (positionToCheckFrom - Player.Instance.Position).Normalized(), (float)(Math.PI / 180 * 70), RCone.Range);

            return EntityManager.Heroes.Enemies.Where(x => !x.IsDead && !x.HasUndyingBuffA() && new Geometry.Polygon.Circle(x.Position, x.BoundingRadius).Points.Any(p => conePolygon.IsInside(p)));
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawDamageIndicator)
            {
                return 0;
            }

            if (unit.GetType() != typeof (AIHeroClient))
                return 0;
            
            var damage = 0f;

            if (unit.IsValidTarget(Q.Range))
                damage += Damage.GetQDamage(unit, true);
            
            if (unit.IsValidTarget(W.Range))
                damage += Damage.GetWDamage(unit);

            if (unit.IsValidTarget(R.Range))
                damage += Damage.GetRDamage(unit);

            if (Player.Instance.IsInAutoAttackRange(unit))
                damage += Player.Instance.GetAutoAttackDamage(unit);

            return damage;
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (DardochTrick)
                args.Process = false;
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            var hero = R.GetTarget();

            if (hero != null && E.IsReady() && R.IsReady() && (Player.Instance.Mana - EMana - RMana > 0) && hero.CountEnemiesInRange(600) < 3 &&
                !hero.HasUndyingBuffA() &&
                (Player.Instance.HealthPercent > 40) && !hero.Position.IsVectorUnderEnemyTower())
            {
                var damage = Damage.GetQDamage(hero, true) +
                             Damage.GetWDamage(hero) +
                             Damage.GetRDamage(hero, true) +
                             Player.Instance.GetAutoAttackDamage(hero) * 2;

                if (hero.TotalHealthWithShields() > damage && hero.TotalHealthWithShields() > Player.Instance.GetAutoAttackDamage(hero))
                {
                    return;
                }

                DardochTrickTarget = hero;

                Core.DelayAction(() =>
                {
                    if (DardochTrickTarget == null)
                        return;

                    if (DardochTrickTarget.IsDead)
                        return;

                    var t = EntityManager.Heroes.Enemies.FirstOrDefault(x => x.NetworkId == DardochTrickTarget.NetworkId);

                    if (t == null)
                        return;

                    var rPred = R.GetPrediction(t);

                    if (rPred.HitChancePercent < 55)
                        return;

                    DardochTrick = true;

                    R.Cast(rPred.CastPosition);
                }, 250 + Game.Ping / 2);
            }

            if (target.GetType() != typeof(AIHeroClient) || target.IsMe || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))//no idea why it invokes twice
                return;

            if (!E.IsReady() || !Settings.Combo.UseE || Settings.Misc.EUsageMode != 1 || DardochTrick || GetAmmoCount > 1)
                return;
            
            var heroClient = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 425, DamageType.Physical);
            var position = Vector3.Zero;

            switch (Settings.Misc.EMode)
            {
                case 0:
                    if (heroClient != null && Player.Instance.HealthPercent > 50 && heroClient.HealthPercent < 30)
                    {
                        if (!Player.Instance.Position.Extend(Game.CursorPos, 420)
                            .To3D()
                            .IsVectorUnderEnemyTower() &&
                            (!heroClient.IsMelee ||
                             Player.Instance.Position.Extend(Game.CursorPos, 420)
                                 .IsInRange(heroClient, heroClient.GetAutoAttackRange()*1.5f)))
                        {
                            Console.WriteLine("[DEBUG] 1v1 Game.CursorPos");
                            position = Game.CursorPos.Distance(Player.Instance) > E.Range
                                ? Player.Instance.Position.Extend(Game.CursorPos, 420).To3D()
                                : Game.CursorPos;
                        }
                    }
                    else if (heroClient != null)
                    {
                        var closest =
                            EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(1300))
                                .OrderBy(x => x.Distance(Player.Instance)).ToArray()[0];

                        var list =
                            SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 420,
                                1300,
                                heroClient.IsMelee ? heroClient.GetAutoAttackRange()*2 : heroClient.GetAutoAttackRange())
                                .Where(
                                    x =>
                                        !x.Key.To3D().IsVectorUnderEnemyTower() &&
                                        x.Key.IsInRange(Prediction.Position.PredictUnitPosition(closest, 850),
                                            Player.Instance.GetAutoAttackRange() - 50))
                                .Select(source => source.Key)
                                .ToList();

                        if (list.Any())
                        {
                            var paths =
                                EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(1300))
                                    .Select(x => x.Path)
                                    .Count(result => result != null && result.Last().Distance(Player.Instance) < 300);

                            var asc = Misc.SortVectorsByDistance(list, heroClient.Position.To2D())[0].To3D();
                            if (Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange()) == 0 &&
                                !EntityManager.Heroes.Enemies.Where(x => x.Distance(Player.Instance) < 1000).Any(
                                    x => Prediction.Position.PredictUnitPosition(x, 800)
                                        .IsInRange(asc,
                                            x.IsMelee ? x.GetAutoAttackRange()*2 : x.GetAutoAttackRange())))
                            {
                                position = asc;

                                Console.WriteLine("[DEBUG] Paths low sorting Ascending");
                            }
                            else if (Player.Instance.CountEnemiesInRange(1000) <= 2 && (paths == 0 || paths == 1) &&
                                     ((closest.Health < Player.Instance.GetAutoAttackDamage(closest, true)*2) ||
                                      (Orbwalker.LastTarget is AIHeroClient &&
                                       Orbwalker.LastTarget.Health <
                                       Player.Instance.GetAutoAttackDamage(closest, true)*2)))
                            {
                                position = asc;
                            }
                            else
                            {
                                position =
                                    Misc.SortVectorsByDistanceDescending(list, heroClient.Position.To2D())[0].To3D();
                                Console.WriteLine("[DEBUG] Paths high sorting Descending");
                            }
                        }
                        else Console.WriteLine("[DEBUG] 1v1 not found positions...");
                    }

                    if (position != Vector3.Zero && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(900)))
                    {
                        E.Cast(position);
                    }
                    break;
                case 1:
                    var enemies = Player.Instance.CountEnemiesInRange(1300);
                    var pos = Game.CursorPos.Distance(Player.Instance) > E.Range
                        ? Player.Instance.Position.Extend(Game.CursorPos, 420).To3D()
                        : Game.CursorPos;

                    if (pos.IsVectorUnderEnemyTower())
                        return;

                    if (heroClient == null)
                        return;

                    if (enemies == 1 && heroClient.HealthPercent + 15 < Player.Instance.HealthPercent)
                    {
                        if (heroClient.IsMelee &&
                            !pos.IsInRange(Prediction.Position.PredictUnitPosition(heroClient, 850),
                                heroClient.GetAutoAttackRange() + 150))
                        {
                            E.Cast(pos);
                            return;
                        }
                        if (!heroClient.IsMelee)
                        {
                            E.Cast(pos);
                            return;
                        }
                    }
                    else if (enemies == 1 &&
                             !pos.IsInRange(Prediction.Position.PredictUnitPosition(heroClient, 850),
                                 heroClient.GetAutoAttackRange()))
                    {
                        E.Cast(pos);
                        return;
                    }
                    else if (enemies == 2 && Player.Instance.CountAlliesInRange(850) >= 1)
                    {
                        E.Cast(pos);
                        return;
                    }
                    else if (enemies >= 2)
                    {
                        if (
                            !EntityManager.Heroes.Enemies.Any(
                                x =>
                                    pos.IsInRange(Prediction.Position.PredictUnitPosition(x, 850),
                                        x.IsMelee ? x.GetAutoAttackRange() + 150 : x.GetAutoAttackRange())))
                        {
                            E.Cast(pos);
                            return;
                        }
                    }
                    E.Cast(pos);
                    break;
                default:
                    return;
            }
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Graves.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()))
                Circle.Draw(ColorPicker[1].Color, R.Range, Player.Instance);
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
            ComboMenu.AddGroupLabel("Combo mode settings for Graves addon");
                
            ComboMenu.AddLabel("End of the Line	(Q) settings :");
            ComboMenu.Add("Plugins.Graves.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Smoke Screen (W) settings :");
            ComboMenu.Add("Plugins.Graves.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Quickdraw (E) settings :");
            ComboMenu.Add("Plugins.Graves.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Collateral Damage (R) settings :");
            ComboMenu.Add("Plugins.Graves.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Graves.ComboMenu.RMinEnemiesHit", new Slider("Use R only if will hit {0} enemies", 0, 0, 5));
            ComboMenu.AddLabel("If set to 0 this setting will be ignored.");
            ComboMenu.Add("Plugins.Graves.ComboMenu.RKeybind", new KeyBind("R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Graves addon");

            HarassMenu.AddLabel("End of the Line (Q) settings :");
            HarassMenu.Add("Plugins.Graves.HarassMenu.UseQ", new CheckBox("Use Q", false));
            HarassMenu.Add("Plugins.Graves.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Graves addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("End of the Line (Q) settings :");
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.MinMinionsHitQ", new Slider("Min minions hit to use Q", 3, 1, 8));
            LaneClearMenu.AddSeparator(5);
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Graves.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Graves addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Graves.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Quickdraw (E) settings :");
            MiscMenu.Add("Plugins.Graves.MiscMenu.EMode", new ComboBox("E mode", 0, "Auto", "Cursor Pos"));
            MiscMenu.Add("Plugins.Graves.MiscMenu.EUsageMode", new ComboBox("E usage", 1, "Always", "After autoattack only"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Graves addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("End of the Line (Q) settings :");
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Collateral Damage (R) settings :");
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };

            DrawingsMenu.AddLabel("Damage indicator settings :");
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DrawDamageIndicator", new CheckBox("Draw damage indicator")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Graves.DrawingsMenu.DamageIndicatorColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
        }

        protected override void PermaActive()
        {
            if (DardochTrick)
                return;

            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            if (DardochTrick && !_ecasted)
            {
                if (Game.Time*1000 - _tick < 130)
                    return;

                if (DardochTrickTarget == null)
                {
                    Reset();
                    return;
                }

                if (DardochTrickTarget.IsDead)
                {
                    Reset();
                    return;
                }

                var k = EntityManager.Heroes.Enemies.FirstOrDefault(x => x.NetworkId == DardochTrickTarget.NetworkId);

                if (!E.IsReady() || !_rcasted || k == null)
                {
                    Reset();
                    return;
                }

                E.Cast(Player.Instance.Distance(k) > 420 ? Player.Instance.Position.Extend(k, 420).To3D() : k.ServerPosition);
            }
            Modes.Combo.Execute();
        }

        private static void Reset()
        {
            DardochTrick = false;
            DardochTrickTarget = null;
            _rcasted = false;
            _ecasted = true;
            _tick = 0;
        }

        protected override void HarassMode()
        {
            if (DardochTrick)
                return;

            Modes.Harass.Execute();
        }

        protected override void LaneClear()
        {
            if (DardochTrick)
                return;

            Modes.LaneClear.Execute();
        }

        protected override void JungleClear()
        {
            if (DardochTrick)
                return;

            Modes.JungleClear.Execute();
        }

        protected override void LastHit()
        {
            if (DardochTrick)
                return;

            Modes.LastHit.Execute();
        }

        protected override void Flee()
        {
            if (DardochTrick)
                return;

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
                        return ComboMenu?["Plugins.Graves.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Graves.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Graves.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Graves.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Graves.ComboMenu.UseW"] != null &&
                               ComboMenu["Plugins.Graves.ComboMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Graves.ComboMenu.UseW"] != null)
                            ComboMenu["Plugins.Graves.ComboMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Graves.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Graves.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Graves.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Graves.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Graves.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Graves.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Graves.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Graves.ComboMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                public static int RMinEnemiesHit
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Graves.ComboMenu.RMinEnemiesHit"] != null)
                            return ComboMenu["Plugins.Graves.ComboMenu.RMinEnemiesHit"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Graves.ComboMenu.RMinEnemiesHit menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Graves.ComboMenu.RMinEnemiesHit"] != null)
                            ComboMenu["Plugins.Graves.ComboMenu.RMinEnemiesHit"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool RKeybind
                {
                    get
                    {
                        return ComboMenu?["Plugins.Graves.ComboMenu.RKeybind"] != null &&
                               ComboMenu["Plugins.Graves.ComboMenu.RKeybind"].Cast<KeyBind>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Graves.ComboMenu.RKeybind"] != null)
                            ComboMenu["Plugins.Graves.ComboMenu.RKeybind"].Cast<KeyBind>()
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
                        return HarassMenu?["Plugins.Graves.HarassMenu.UseQ"] != null &&
                               HarassMenu["Plugins.Graves.HarassMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Graves.HarassMenu.UseQ"] != null)
                            HarassMenu["Plugins.Graves.HarassMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Graves.HarassMenu.MinManaQ"] != null)
                            return HarassMenu["Plugins.Graves.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Graves.HarassMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Graves.HarassMenu.MinManaQ"] != null)
                            HarassMenu["Plugins.Graves.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Graves.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Graves.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Graves.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Graves.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Graves.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Graves.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.AllowedEnemies"] != null)
                            return
                                LaneClearMenu["Plugins.Graves.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Graves.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Graves.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }

                public static bool UseQInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Graves.LaneClearMenu.UseQInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Graves.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.UseQInLaneClear"] != null)
                            LaneClearMenu["Plugins.Graves.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseQInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Graves.LaneClearMenu.UseQInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Graves.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.UseQInJungleClear"] != null)
                            LaneClearMenu["Plugins.Graves.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinMinionsHitQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.MinMinionsHitQ"] != null)
                            return LaneClearMenu["Plugins.Graves.LaneClearMenu.MinMinionsHitQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Graves.LaneClearMenu.MinMinionsHitQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.MinMinionsHitQ"] != null)
                            LaneClearMenu["Plugins.Graves.LaneClearMenu.MinMinionsHitQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.MinManaQ"] != null)
                            return LaneClearMenu["Plugins.Graves.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Graves.LaneClearMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Graves.LaneClearMenu.MinManaQ"] != null)
                            LaneClearMenu["Plugins.Graves.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool EnableKillsteal
                {
                    get
                    {
                        return MiscMenu?["Plugins.Graves.MiscMenu.EnableKillsteal"] != null &&
                               MiscMenu["Plugins.Graves.MiscMenu.EnableKillsteal"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Graves.MiscMenu.EnableKillsteal"] != null)
                            MiscMenu["Plugins.Graves.MiscMenu.EnableKillsteal"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                /// <summary>
                /// 0 - "Auto"
                /// 1 - "Cursor Pos"
                /// </summary>
                public static int EMode
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Graves.MiscMenu.EMode"] != null)
                            return MiscMenu["Plugins.Graves.MiscMenu.EMode"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Graves.MiscMenu.EMode menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Graves.MiscMenu.EMode"] != null)
                            MiscMenu["Plugins.Graves.MiscMenu.EMode"].Cast<ComboBox>().CurrentValue = value;
                    }
                }

                /// <summary>
                /// 0 - "Always"
                /// 1 - "After autoattack only"
                /// </summary>
                public static int EUsageMode
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Graves.MiscMenu.EUsageMode"] != null)
                            return MiscMenu["Plugins.Graves.MiscMenu.EUsageMode"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Graves.MiscMenu.EUsageMode menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Graves.MiscMenu.EUsageMode"] != null)
                            MiscMenu["Plugins.Graves.MiscMenu.EUsageMode"].Cast<ComboBox>().CurrentValue = value;
                    }
                }
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Graves.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Graves.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Graves.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Graves.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool DrawQ
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Graves.DrawingsMenu.DrawQ"] != null &&
                               DrawingsMenu["Plugins.Graves.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Graves.DrawingsMenu.DrawQ"] != null)
                            DrawingsMenu["Plugins.Graves.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
                public static bool DrawR
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Graves.DrawingsMenu.DrawR"] != null &&
                               DrawingsMenu["Plugins.Graves.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Graves.DrawingsMenu.DrawR"] != null)
                            DrawingsMenu["Plugins.Graves.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawDamageIndicator
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Graves.DrawingsMenu.DrawDamageIndicator"] != null &&
                               DrawingsMenu["Plugins.Graves.DrawingsMenu.DrawDamageIndicator"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Graves.DrawingsMenu.DrawDamageIndicator"] != null)
                            DrawingsMenu["Plugins.Graves.DrawingsMenu.DrawDamageIndicator"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
            }
        }

        protected static class Damage
        {
            public static int[] QDamage { get; } = { 0, 55, 70, 85, 100, 115 };
            public static float QDamageBonusAdMod { get; } = 0.75f;
            public static int[] QExplosionDamage { get; } = { 0, 80, 125, 170, 215, 260 };
            public static float[] QExplosionDamageBonusAdMod { get; } = { 0, 0.4f, 0.6f, 0.8f, 1, 1.2f };

            public static int[] WDamage { get; } = { 0, 60, 110, 160, 210, 260 };
            public static float WDamageApMod { get; } = 0.6f;

            public static int[] RDamage { get; } = { 0, 250, 400, 550 };
            public static float RDamageBonusAdMod { get; } = 1.5f;

            public static float GetQDamage(Obj_AI_Base unit, bool includeExplosionDamage = false)
            {
                var damage = QDamage[Q.Level] + Player.Instance.FlatPhysicalDamageMod*QDamageBonusAdMod;

                var explosionDamage = QExplosionDamage[Q.Level] + Player.Instance.FlatPhysicalDamageMod * QExplosionDamageBonusAdMod[Q.Level];

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                    includeExplosionDamage ? damage + explosionDamage : damage);
            }

            public static float GetWDamage(Obj_AI_Base unit)
            {
                var damage = WDamage[W.Level] + Player.Instance.FlatMagicDamageMod * WDamageApMod;
                
                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Magical, damage);
            }

            public static float GetRDamage(Obj_AI_Base unit, bool includeSplashDamage = false)
            {
                var damage = RDamage[R.Level] + Player.Instance.FlatPhysicalDamageMod * RDamageBonusAdMod;

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, includeSplashDamage ? damage*1.8f : damage);
            }
        }
    }
}