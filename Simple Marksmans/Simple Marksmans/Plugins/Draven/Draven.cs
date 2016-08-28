#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Draven.cs" company="EloBuddy">
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
using Simple_Marksmans.Utils;
using Color = System.Drawing.Color;
using ColorPicker = Simple_Marksmans.Utils.ColorPicker;
using Font = System.Drawing.Font;

namespace Simple_Marksmans.Plugins.Draven
{
    internal class Draven : ChampionPlugin
    {
        protected static Spell.Active Q { get; }
        protected static Spell.Active W { get; }
        protected static Spell.Skillshot E { get; }
        protected static Spell.Skillshot R { get; }

        protected static Menu ComboMenu { get; set; }
        protected static Menu HarassMenu { get; set; }
        protected static Menu LaneClearMenu { get; set; }
        protected static Menu DrawingsMenu { get; set; }
        protected static Menu MiscMenu { get; set; }
        protected static Menu AxeSettingsMenu { get; set; }

        private static readonly List<AxeObjectData> AxeObjects = new List<AxeObjectData>();
        private static readonly Text Text;
        private static readonly ColorPicker[] ColorPicker;

        protected static float[] WAdditionalMovementSpeed { get; } = {0, 1.4f, 1.45f, 1.5f, 1.55f, 1.6f};

        protected static bool HasSpinningAxeBuff
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "dravenspinningattack");

        protected static BuffInstance GetSpinningAxeBuff
            => Player.Instance.Buffs.FirstOrDefault(x => x.Name.ToLowerInvariant() == "dravenspinningattack");

        protected static bool HasMoveSpeedFuryBuff
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "dravenfury");

        protected static BuffInstance GetMoveSpeedFuryBuff
            => Player.Instance.Buffs.FirstOrDefault(x => x.Name.ToLowerInvariant() == "dravenfury");

        protected static bool HasAttackSpeedFuryBuff
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "dravenfurybuff");

        protected static BuffInstance GetAttackSpeedFuryBuff
            => Player.Instance.Buffs.FirstOrDefault(x => x.Name.ToLowerInvariant() == "dravenfurybuff");

        private static bool _changingRangeScan;
        private static bool _changingkeybindRange;
        private static bool _catching;

        protected static MissileClient DravenRMissile { get; private set; }

        static Draven()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 950, SkillShotType.Linear, 250, 1300, 100)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Skillshot(SpellSlot.R, 30000, SkillShotType.Linear, 300, 1900, 160)
            {
                AllowedCollisionCount = int.MaxValue
            };

            ColorPicker = new ColorPicker[2];
            ColorPicker[0] = new ColorPicker("DravenE", new ColorBGRA(114, 171, 160, 255));
            ColorPicker[1] = new ColorPicker("DravenCatchRange", new ColorBGRA(231, 237, 160, 255));

            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));

            Game.OnTick += Game_OnTick;
            Game.OnPostTick += Game_OnPostTick;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!(target is AIHeroClient) || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            if (Q.IsReady() && GetAxesCount() != 0 && GetAxesCount() < Settings.Combo.MaxAxesAmount)
                Q.Cast();
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                var jungleMinions =
                    EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position,
                        Player.Instance.GetAutoAttackRange()).ToList();

                var laneMinions =
                    EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                        Player.Instance.Position,
                        Player.Instance.GetAutoAttackRange()).ToList();

                if (jungleMinions.Any())
                {
                    if (Settings.LaneClear.UseQInJungleClear && Q.IsReady() && GetAxesCount() == 0 &&
                        Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ)
                    {
                        Q.Cast();
                    }

                    if (Settings.LaneClear.UseWInJungleClear && W.IsReady() && jungleMinions.Count > 1 &&
                        !HasAttackSpeedFuryBuff &&
                        Player.Instance.ManaPercent >= Settings.LaneClear.MinManaW)
                    {
                        W.Cast();
                    }
                    return;
                }
                if (laneMinions.Any() && Modes.LaneClear.CanILaneClear())
                {
                    if (Settings.LaneClear.UseQInLaneClear && Q.IsReady() && GetAxesCount() == 0 &&
                        Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ)
                    {
                        Q.Cast();
                    }

                    if (Settings.LaneClear.UseWInLaneClear && W.IsReady() && laneMinions.Count > 3 &&
                        !HasAttackSpeedFuryBuff &&
                        Player.Instance.ManaPercent >= Settings.LaneClear.MinManaW)
                    {
                        W.Cast();
                    }
                    return;
                }
            }

            if (!(target is AIHeroClient) || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            if (Q.IsReady() && GetAxesCount() == 0)
                Q.Cast();

            if (!W.IsReady() || !Settings.Combo.UseW || HasAttackSpeedFuryBuff || !(Player.Instance.Mana - 40 > 145))
                return;

            var t = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange(), DamageType.Physical);
            if (t != null)
            {
                W.Cast();
            }
        }

        private static void Game_OnPostTick(EventArgs args)
        {
        }

        private static void Game_OnTick(EventArgs args)
        {
            if(!AxeObjects.Any() || !Settings.Axe.CatchAxes || !_catching)
            { 
                Orbwalker.OverrideOrbwalkPosition += () => Game.CursorPos;
            }

            foreach (var axeObjectData in AxeObjects.Where(x => Game.CursorPos.Distance(x.EndPosition) < Settings.Axe.AxeCatchRange && CanPlayerCatchAxe(x)).OrderBy(x => x.EndPosition.Distance(Player.Instance)))
            {
                switch (Settings.Axe.CatchAxesMode)
                {
                    case 1:
                        var isOutside = !new Geometry.Polygon.Circle(Player.Instance.ServerPosition, Player.Instance.BoundingRadius - 15)
                            .Points.Any(x => new Geometry.Polygon.Circle(axeObjectData.EndPosition, 80).IsInside(x));

                        var isInside = new Geometry.Polygon.Circle(Player.Instance.ServerPosition, Player.Instance.BoundingRadius - 30)
                            .Points.Any(x => new Geometry.Polygon.Circle(axeObjectData.EndPosition, 40).IsInside(x));

                        if (isOutside)
                        {
                            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                            {
                                var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 350,
                                    DamageType.Physical);

                                if (target != null &&
                                    target.TotalHealthWithShields() <
                                    Player.Instance.GetAutoAttackDamage(target, true) * 2)
                                {
                                    var pos = Prediction.Position.PredictUnitPosition(target, (int)(GetEta(axeObjectData, Player.Instance.MoveSpeed) * 1000));
                                    if (!axeObjectData.EndPosition.IsInRange(pos, Player.Instance.GetAutoAttackRange()))
                                    {
                                        continue;
                                    }
                                }
                            }

                            if (axeObjectData.EndTick - Game.Time < GetEta(axeObjectData, Player.Instance.MoveSpeed) && !HasMoveSpeedFuryBuff &&
                                GetEta(axeObjectData, Player.Instance.MoveSpeed * WAdditionalMovementSpeed[W.Level]) > axeObjectData.EndTick - Game.Time &&
                                W.IsReady() && Settings.Axe.UseWToCatch)
                            {
                                W.Cast();
                            }

                            Orbwalker.OverrideOrbwalkPosition +=
                                () =>
                                    axeObjectData.EndPosition.Extend(Player.Instance.Position,
                                        40 - Player.Instance.Distance(axeObjectData.EndPosition)).To3D();
                            _catching = true;
                        }
                        else if (isInside &&
                                 !CanPlayerLeaveAxeRangeInDesiredTime(axeObjectData.EndPosition,
                                     (axeObjectData.EndTick / 1000 - Game.Time) - 0.15f))
                        {
                            Orbwalker.OverrideOrbwalkPosition += () => Game.CursorPos;
                            _catching = false;
                        }
                        break;
                    case 0:
                        if (Player.Instance.Distance(axeObjectData.EndPosition) > 250)
                            return;

                        var isOutside2 = !new Geometry.Polygon.Circle(Player.Instance.ServerPosition, Player.Instance.BoundingRadius - 15)
                            .Points.Any(x => new Geometry.Polygon.Circle(axeObjectData.EndPosition, 80).IsInside(x));

                        var isInside2 = new Geometry.Polygon.Circle(Player.Instance.ServerPosition, Player.Instance.BoundingRadius - 30)
                            .Points.Any(x => new Geometry.Polygon.Circle(axeObjectData.EndPosition, 80).IsInside(x));

                        if (isOutside2)
                        {
                            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                            {
                                var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 350,
                                    DamageType.Physical);
                                
                                if (target != null &&
                                    target.TotalHealthWithShields() <
                                    Player.Instance.GetAutoAttackDamage(target, true) * 2)
                                {
                                    var pos = Prediction.Position.PredictUnitPosition(target, (int)(GetEta(axeObjectData, Player.Instance.MoveSpeed)*1000));
                                    if (!axeObjectData.EndPosition.IsInRange(pos, Player.Instance.GetAutoAttackRange()))
                                    {
                                        continue;
                                    }
                                }
                            }

                            if (axeObjectData.EndTick - Game.Time < GetEta(axeObjectData, Player.Instance.MoveSpeed) && !HasMoveSpeedFuryBuff &&
                                GetEta(axeObjectData, Player.Instance.MoveSpeed * WAdditionalMovementSpeed[W.Level]) > axeObjectData.EndTick - Game.Time &&
                                W.IsReady() && Settings.Axe.UseWToCatch)
                            {
                                W.Cast();
                            }

                            Orbwalker.OverrideOrbwalkPosition +=
                                () =>
                                    axeObjectData.EndPosition.Extend(Player.Instance.Position,
                                        40 - Player.Instance.Distance(axeObjectData.EndPosition)).To3D();
                            _catching = true;
                        }
                        else if (isInside2 &&
                                 !CanPlayerLeaveAxeRangeInDesiredTime(axeObjectData.EndPosition,
                                     (axeObjectData.EndTick / 1000 - Game.Time) - 0.15f))
                        {
                            Orbwalker.OverrideOrbwalkPosition += () => Game.CursorPos;
                            _catching = false;
                        }
                        break;
                    default:
                        _catching = false;
                        return;
                }
            }
        }

        private static bool CanPlayerCatchAxe(AxeObjectData axe)
        {
            if (!Settings.Axe.CatchAxes || (Settings.Axe.CatchAxesWhen == 0 && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear)) || (Settings.Axe.CatchAxesWhen == 1 && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)))
            {
                return false;
            }

            if (!Settings.Axe.CatchAxesUnderTower && axe.EndPosition.IsVectorUnderEnemyTower())
                return false;

            return Settings.Axe.CatchAxesNearEnemies || axe.EndPosition.CountEnemiesInRange(550) <= 2;
        }

        private static float GetEta(AxeObjectData axe, float movespeed)
        {
            return Player.Instance.Distance(axe.EndPosition) / movespeed;
        }

        private static bool CanPlayerLeaveAxeRangeInDesiredTime(Vector3 axeCenterPosition, float time)
        {
            var axePolygon = new Geometry.Polygon.Circle(axeCenterPosition, 90);
            var playerPosition = Player.Instance.ServerPosition;
            var playerLastWaypoint = Player.Instance.Path.LastOrDefault();
            var cloestPoint = playerLastWaypoint.To2D().Closest(axePolygon.Points);
            var distanceFromPoint = cloestPoint.Distance(playerPosition);
            var distanceInTime = Player.Instance.MoveSpeed*time;

            return distanceInTime > distanceFromPoint;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            if (sender.Name.Contains("Q_reticle_self"))
            {
                AxeObjects.Add(new AxeObjectData
                {
                    EndPosition = sender.Position,
                    EndTick = Game.Time * 1000 + 1227.1f,
                    NetworkId = sender.NetworkId,
                    Owner = Player.Instance,
                    StartTick = Game.Time * 1000
                });
            }


            var missile = sender as MissileClient;
            if (missile == null || !missile.IsValidMissile())
                return;

            if (missile.SData.Name.ToLowerInvariant() == "dravenr" && missile.SpellCaster.IsMe)
            {
                DravenRMissile = missile;
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            if (sender.Name.Contains("Q_reticle_self"))
            {
                AxeObjects.Remove(AxeObjects.Find(data => data.NetworkId == sender.NetworkId));
            }


            var missile = sender as MissileClient;
            if (missile == null)
                return;

            if (missile.SData.Name.ToLowerInvariant() == "dravenr" && missile.SpellCaster.IsMe)
            {
                DravenRMissile = null;
            }
        }

        protected static int GetAxesCount()
        {
            if (!HasSpinningAxeBuff && AxeObjects == null)
                return 0;

            if (!HasSpinningAxeBuff && AxeObjects?.Count > 0)
                return AxeObjects.Count;

            if (HasSpinningAxeBuff && GetSpinningAxeBuff.Count == 0 && AxeObjects?.Count > 0)
                return AxeObjects.Count;

            if (HasSpinningAxeBuff && GetSpinningAxeBuff.Count > 0 && AxeObjects?.Count == 0)
                return GetSpinningAxeBuff.Count;

            if (HasSpinningAxeBuff && GetSpinningAxeBuff.Count > 0 && AxeObjects?.Count > 0)
                return GetSpinningAxeBuff.Count + AxeObjects.Count;

            return 0;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(SharpDX.Color.White,
                    LaneClearMenu["Plugins.Draven.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (_changingkeybindRange)
                Circle.Draw(SharpDX.Color.White, Settings.Combo.RRangeKeybind, Player.Instance);

            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[0].Color, E.Range, Player.Instance);

            foreach (var axeObjectData in AxeObjects)
            {
                if (Settings.Drawings.DrawAxes)
                {
                    Circle.Draw(
                        new Geometry.Polygon.Circle(Player.Instance.ServerPosition, Player.Instance.BoundingRadius).Points.Any(x => new Geometry.Polygon.Circle(axeObjectData.EndPosition, 80).IsInside(x))
                            ? new ColorBGRA(0, 255, 0, 255)
                            : new ColorBGRA(255, 0, 0, 255), 80, axeObjectData.EndPosition);
                }

                if (!Settings.Drawings.DrawAxesTimer)
                    continue;

                var timeLeft = axeObjectData.EndTick / 1000 - Game.Time;
                var degree = Misc.GetNumberInRangeFromProcent(timeLeft * 1000d / 1227.1 * 100d, 3, 110);
                
                Text.Color = new Misc.HsvColor(degree, 1, 1).ColorFromHsv();
                Text.X = (int) Drawing.WorldToScreen(axeObjectData.EndPosition).X;
                Text.Y = (int) Drawing.WorldToScreen(axeObjectData.EndPosition).Y + 50;
                Text.TextValue = ((axeObjectData.EndTick - Game.Time*1000)/1000).ToString("F1") + " s";
                Text.Draw();
            }

            if (Settings.Drawings.DrawAxesCatchRange)
                Circle.Draw(ColorPicker[1].Color, Settings.Axe.AxeCatchRange, Game.CursorPos);
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!Settings.Misc.EnableInterrupter || !E.IsReady() || !sender.IsValidTarget(E.Range))
                return;

            if (args.Delay == 0)
                E.Cast(sender);
            else Core.DelayAction(() => E.Cast(sender), args.Delay);
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (!Settings.Misc.EnableAntiGapcloser || !(args.End.Distance(Player.Instance) < 350) || !E.IsReady() ||
                !sender.IsValidTarget(E.Range))
                return;

            if(args.Delay == 0)
                E.Cast(sender);
            else Core.DelayAction(() => E.Cast(sender), args.Delay);
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Draven addon");

            ComboMenu.AddLabel("Spinning Axe (Q) settings :");
            ComboMenu.Add("Plugins.Draven.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.Add("Plugins.Draven.ComboMenu.MaxAxesAmount", new Slider("Maximum axes amount", 2, 1, 3));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Blood Rush (W) settings :");
            ComboMenu.Add("Plugins.Draven.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Stand Aside (E) settings :");
            ComboMenu.Add("Plugins.Draven.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Whirling Death (R) settings :");
            ComboMenu.Add("Plugins.Draven.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Draven.ComboMenu.RKeybind",
                new KeyBind("R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));
            ComboMenu.AddLabel("Fires R on best target in range when keybind is active.");
            ComboMenu.AddSeparator(5);
            var keybindRange = ComboMenu.Add("Plugins.Draven.ComboMenu.RRangeKeybind",
                new Slider("Maximum range to enemy to cast R while keybind is active", 1100, 300, 2500));
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

            AxeSettingsMenu = MenuManager.Menu.AddSubMenu("Axe Settings");
            AxeSettingsMenu.AddGroupLabel("Axe settings for Draven addon");
            AxeSettingsMenu.AddLabel("Basic settings :");
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxes", new CheckBox("Catch Axes"));
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.UseWToCatch", new CheckBox("Cast W if axe is uncatchable"));
            AxeSettingsMenu.AddSeparator(5);

            AxeSettingsMenu.AddLabel("Catching settings :");
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesWhen",
                new ComboBox("When should I catch them", 0, "Lane clear and combo", " Only in Combo"));
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesMode",
                new ComboBox("Catch mode", 0, "Default", "Brutal"));
            AxeSettingsMenu.AddSeparator(2);
            AxeSettingsMenu.AddLabel("Default mode only tries to catch axe if distance to from player to axe is less than 250.\nBrutal catches all axes within range of desired catch radius.");
            AxeSettingsMenu.AddSeparator(5);

            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.AxeCatchRange",
                new Slider("Axe Catch Range", 450, 200, 1000));
            AxeSettingsMenu.AddSeparator(5);

            AxeSettingsMenu.AddLabel("Additional settings :");
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesUnderTower",
                new CheckBox("Catch Axes that are under enemy tower", false));
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesNearEnemies",
                new CheckBox("Catch Axes that are near enemies", false));

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Draven addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.EnableLCIfNoEn",
                new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.ScanRange",
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
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.AllowedEnemies",
                new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Spinning Axe (Q) settings :");
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.MinManaQ",
                new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Blood Rush (Q) settings :");
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.UseWInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.UseWInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Draven.LaneClearMenu.MinManaW",
                new Slider("Min mana percentage ({0}%) to use W", 75, 1));

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Draven addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Draven.MiscMenu.EnableInterrupter", new CheckBox("Enable Interrupter"));
            MiscMenu.Add("Plugins.Draven.MiscMenu.EnableAntiGapcloser", new CheckBox("Enable Anti-Gapcloser"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Draven addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Spinning Axe (Q) drawing settings :");
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxes", new CheckBox("Draw Axes"));
            DrawingsMenu.AddSeparator(1);
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesTimer", new CheckBox("Draw Axes timer"));
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesCatchRange", new CheckBox("Draw Axe's catch range"));
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesCatchRangeColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[1].Initialize(Color.Aquamarine);
                    a.CurrentValue = false;
                };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Stand Aside (E) drawing settings :");
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawEColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[0].Initialize(Color.Aquamarine);
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
                        return ComboMenu?["Plugins.Draven.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Draven.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Draven.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Draven.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                public static int MaxAxesAmount
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Draven.ComboMenu.MaxAxesAmount"] != null)
                            return ComboMenu["Plugins.Draven.ComboMenu.MaxAxesAmount"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Draven.ComboMenu.MaxAxesAmount menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Draven.ComboMenu.MaxAxesAmount"] != null)
                            ComboMenu["Plugins.Draven.ComboMenu.MaxAxesAmount"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Draven.ComboMenu.UseW"] != null &&
                               ComboMenu["Plugins.Draven.ComboMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Draven.ComboMenu.UseW"] != null)
                            ComboMenu["Plugins.Draven.ComboMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Draven.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Draven.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Draven.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Draven.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Draven.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Draven.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Draven.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Draven.ComboMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool RKeybind
                {
                    get
                    {
                        return ComboMenu?["Plugins.Draven.ComboMenu.RKeybind"] != null &&
                               ComboMenu["Plugins.Draven.ComboMenu.RKeybind"].Cast<KeyBind>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Draven.ComboMenu.RKeybind"] != null)
                            ComboMenu["Plugins.Draven.ComboMenu.RKeybind"].Cast<KeyBind>()
                                .CurrentValue
                                = value;
                    }
                }
                public static int RRangeKeybind
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Draven.ComboMenu.RRangeKeybind"] != null)
                            return ComboMenu["Plugins.Draven.ComboMenu.RRangeKeybind"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Draven.ComboMenu.RRangeKeybind menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Draven.ComboMenu.RRangeKeybind"] != null)
                            ComboMenu["Plugins.Draven.ComboMenu.RRangeKeybind"].Cast<Slider>().CurrentValue = value;
                    }
                }
                
            }

            internal static class Axe
            {
                public static bool CatchAxes
                {
                    get
                    {
                        return AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxes"] != null &&
                               AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxes"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxes"] != null)
                            AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxes"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseWToCatch
                {
                    get
                    {
                        return AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.UseWToCatch"] != null &&
                               AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.UseWToCatch"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.UseWToCatch"] != null)
                            AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.UseWToCatch"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                /// <summary>
                /// 0 - Lane clear and combo
                /// 1 - Only in Combo
                /// </summary>
                public static int CatchAxesWhen
                {
                    get
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxesWhen"] != null)
                            return AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxesWhen"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Draven.AxeSettingsMenu.CatchAxesWhen menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxesWhen"] != null)
                            AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxesWhen"].Cast<ComboBox>().CurrentValue = value;
                    }
                }

                /// <summary>
                /// 0 - Default
                /// 1 - Brutal
                /// </summary>
                public static int CatchAxesMode
                {
                    get
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxesMode"] != null)
                            return AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxesMode"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Draven.AxeSettingsMenu.CatchAxesMode menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxesMode"] != null)
                            AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxesMode"].Cast<ComboBox>().CurrentValue = value;
                    }
                }

                public static int AxeCatchRange
                {
                    get
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.AxeCatchRange"] != null)
                            return AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.AxeCatchRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Draven.AxeSettingsMenu.AxeCatchRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.AxeCatchRange"] != null)
                            AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.AxeCatchRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool CatchAxesUnderTower
                {
                    get
                    {
                        return AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxesUnderTower"] != null &&
                               AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxesUnderTower"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxesUnderTower"] != null)
                            AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxesUnderTower"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool CatchAxesNearEnemies
                {
                    get
                    {
                        return AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxesNearEnemies"] != null &&
                               AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxesNearEnemies"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.CatchAxesNearEnemies"] != null)
                            AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.CatchAxesNearEnemies"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Draven.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Draven.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Draven.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Draven.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Draven.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Draven.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.AllowedEnemies"] != null)
                            return
                                LaneClearMenu["Plugins.Draven.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Draven.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Draven.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }

                public static bool UseQInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Draven.LaneClearMenu.UseQInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Draven.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.UseQInLaneClear"] != null)
                            LaneClearMenu["Plugins.Draven.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseQInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Draven.LaneClearMenu.UseQInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Draven.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.UseQInJungleClear"] != null)
                            LaneClearMenu["Plugins.Draven.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static int MinManaQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.MinManaQ"] != null)
                            return LaneClearMenu["Plugins.Draven.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Draven.LaneClearMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.MinManaQ"] != null)
                            LaneClearMenu["Plugins.Draven.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseWInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Draven.LaneClearMenu.UseWInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Draven.LaneClearMenu.UseWInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.UseWInLaneClear"] != null)
                            LaneClearMenu["Plugins.Draven.LaneClearMenu.UseWInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseWInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Draven.LaneClearMenu.UseWInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Draven.LaneClearMenu.UseWInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.UseWInJungleClear"] != null)
                            LaneClearMenu["Plugins.Draven.LaneClearMenu.UseWInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaW
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.MinManaW"] != null)
                            return LaneClearMenu["Plugins.Draven.LaneClearMenu.MinManaW"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Draven.LaneClearMenu.MinManaW menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Draven.LaneClearMenu.MinManaW"] != null)
                            LaneClearMenu["Plugins.Draven.LaneClearMenu.MinManaW"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool EnableInterrupter
                {
                    get
                    {
                        return MiscMenu?["Plugins.Draven.MiscMenu.EnableInterrupter"] != null &&
                               MiscMenu["Plugins.Draven.MiscMenu.EnableInterrupter"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Draven.MiscMenu.EnableInterrupter"] != null)
                            MiscMenu["Plugins.Draven.MiscMenu.EnableInterrupter"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool EnableAntiGapcloser
                {
                    get
                    {
                        return MiscMenu?["Plugins.Draven.MiscMenu.EnableAntiGapcloser"] != null &&
                               MiscMenu["Plugins.Draven.MiscMenu.EnableAntiGapcloser"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Draven.MiscMenu.EnableAntiGapcloser"] != null)
                            MiscMenu["Plugins.Draven.MiscMenu.EnableAntiGapcloser"].Cast<CheckBox>()
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
                        return DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool DrawAxes
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxes"] != null &&
                               DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxes"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxes"] != null)
                            DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxes"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawAxesTimer
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxesTimer"] != null &&
                               DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxesTimer"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxesTimer"] != null)
                            DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxesTimer"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawAxesCatchRange
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxesCatchRange"] != null &&
                               DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxesCatchRange"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxesCatchRange"] != null)
                            DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxesCatchRange"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawE
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawE"] != null &&
                               DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawE"] != null)
                            DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
            }
        }
    }
}