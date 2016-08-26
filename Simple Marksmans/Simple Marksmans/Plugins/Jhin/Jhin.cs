#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Jhin.cs" company="EloBuddy">
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
using EloBuddy.SDK.Utils;
using SharpDX;
using Simple_Marksmans.Utils;
using EloBuddy.SDK.Rendering;

namespace Simple_Marksmans.Plugins.Jhin
{
    internal class Jhin : ChampionPlugin
    {
        public static Spell.Targeted Q { get; }
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
        private static float _lastRTime;
        private static float _lastETime;
        private static Vector3 _lastEPosition;
        public static float LastLaneClear;

        private static readonly Dictionary<int, Dictionary<float, float>> Damages =
            new Dictionary<int, Dictionary<float, float>>();

        private static readonly Dictionary<int, Dictionary<float, bool>> SpottedBuff =
    new Dictionary<int, Dictionary<float, bool>>();

        public static bool HasSpottedBuff(AIHeroClient unit)
        {
            if (SpottedBuff.ContainsKey(unit.NetworkId) &&
                (Game.Time*1000 - SpottedBuff[unit.NetworkId].Select(x => x.Key).First() < 200))
            {
                return SpottedBuff[unit.NetworkId].FirstOrDefault().Value;
            }

            var buff = unit.Buffs.Any(
                b => b.IsActive && b.Name.ToLowerInvariant() == "jhinespotteddebuff");

            if (!SpottedBuff.ContainsKey(unit.NetworkId))
            {

                SpottedBuff.Add(unit.NetworkId, new Dictionary<float, bool>
                {
                    {
                        Game.Time*1000, buff
                    }
                });
            }
            else
            {
                SpottedBuff[unit.NetworkId]  = new Dictionary<float, bool>
                {
                    {
                        Game.Time*1000, buff
                    }
                };
            }

            return buff;
        }

        public static BuffInstance GetSpottedBuff(AIHeroClient unit)
            =>
                unit.Buffs.FirstOrDefault(
                    b => b.IsActive && b.Name.ToLowerInvariant() == "jhinespotteddebuff");

        public static bool HasReloadingBuff
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.Name.ToLowerInvariant() == "jhinpassivereload");

        public static BuffInstance GetReloadingBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.Name.ToLowerInvariant() == "jhinpassivereload");

        public static bool HasAttackBuff
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.Name.ToLowerInvariant() == "jhinpassiveattackbuff");

        public static BuffInstance GetAttackBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.Name.ToLowerInvariant() == "jhinpassiveattackbuff");


        public static bool IsCastingR { get; private set; }
        public static int GetCurrentShootsRCount { get; private set; }
        public static Vector3 REndPosition { get; private set; }
        public static bool IsPreAttack { get; private set; }

        static Jhin()
        {
            Q = new Spell.Targeted(SpellSlot.Q, 600);
            W = new Spell.Skillshot(SpellSlot.W, 2500, SkillShotType.Linear, 1000, null, 40)
            {
                AllowedCollisionCount = -1
            };
            E = new Spell.Skillshot(SpellSlot.E, 750, SkillShotType.Circular, 750, null, 120);
            R = new Spell.Skillshot(SpellSlot.R, 3500, SkillShotType.Linear, 300, 5000, 80)
            {
                AllowedCollisionCount = -1
            };

            ColorPicker = new ColorPicker[5];

            ColorPicker[0] = new ColorPicker("JhinQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("JhinW", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("JhinE", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[3] = new ColorPicker("JhinR", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[4] = new ColorPicker("JhinHpBar", new ColorBGRA(255, 134, 0, 255));

            Orbwalker.OnPreAttack += (s, a) =>
            {
                IsPreAttack = true;

                if (!HasReloadingBuff)
                    return;

                a.Process = false;
                IsPreAttack = false;
            };
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnPostTick += a => IsPreAttack = false;
            ChampionTracker.Initialize();
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;

            DamageIndicator.Initalize(
                System.Drawing.Color.FromArgb(ColorPicker[4].Color.R, ColorPicker[4].Color.G, ColorPicker[4].Color.B),
                (int) W.Range);
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[4].OnColorChange += (a, b) => { DamageIndicator.Color = System.Drawing.Color.FromArgb(b.Color.A, b.Color.R, b.Color.G, b.Color.B); };
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.E && (Game.Time * 1000 - _lastETime < 3000 && _lastEPosition.Distance(args.EndPosition) < 300))
            {
                args.Process = false;
            } else if (args.Slot == SpellSlot.E)
            {
                _lastETime = Game.Time*1000;
                _lastEPosition = args.EndPosition;
            }

            if (args.Slot == SpellSlot.R && Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name == "JhinRShot" && (Game.Time * 1000 - _lastRTime < Settings.Combo.RDelay+1000))
            {
                args.Process = false;
            }
            else if (args.Slot == SpellSlot.R && Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name == "JhinRShot")
            {
                _lastRTime = Game.Time * 1000;
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if(args.Slot != SpellSlot.R)
                return;

            switch (args.SData.Name.ToLowerInvariant())
            {
                case "jhinr":
                    IsCastingR = true;
                    GetCurrentShootsRCount = 4;
                    REndPosition = Player.Instance.Position.Extend(args.End, R.Range).To3D();
                    break;
                case "jhinrshot":
                    GetCurrentShootsRCount--;
                    break;
                default:
                    return;
            }
        }
        
        public static bool IsInsideRRange(Obj_AI_Base unit)
        {
            return
                new Geometry.Polygon.Sector(Player.Instance.Position, REndPosition, (int) (Math.PI/180*71), R.Range)
                    .IsInside(unit);
        }

        public static bool IsInsideRRange(Vector3 position)
        {
            return
                new Geometry.Polygon.Sector(Player.Instance.Position, REndPosition, (int)(Math.PI / 180 * 71), R.Range)
                    .IsInside(position);
        }

        private static float HandleDamageIndicator(Obj_AI_Base unit)
        {
            if (!Settings.Drawings.DrawInfo)
            {
                return 0;
            }

            var enemy = (AIHeroClient) unit;

            if (enemy == null)
                return 0;

            if (Damages.ContainsKey(unit.NetworkId) &&
                !Damages.Any(x => x.Key == unit.NetworkId && x.Value.Any(k => Game.Time*1000 - k.Key > 200))) //
                return Damages[unit.NetworkId].Values.FirstOrDefault();
            
            var damge = 0f;
            if (!IsCastingR)
            {

                if (R.IsReady() && unit.IsValidTarget(R.Range))
                    damge += GetCurrentShootsRCount == 1
                        ? Damage.GetRDamage(unit, true)
                        : Damage.GetRDamage(unit)*(GetCurrentShootsRCount - 1) + Damage.GetRDamage(unit, true);
                if (Q.IsReady() && unit.IsValidTarget(Q.Range))
                    damge += Damage.GetQDamage(unit);
                if (W.IsReady() && unit.IsValidTarget(W.Range))
                    damge += Damage.GetWDamage(unit);
            }
            else
            {
                if (IsInsideRRange(unit))
                    damge += GetCurrentShootsRCount == 1 ? Damage.GetRDamage(unit, true) : Damage.GetRDamage(unit);
            }

            if (unit.IsValidTarget(Player.Instance.GetAutoAttackRange()))
            {
                damge += HasAttackBuff ? Damage.Get4ThShootDamage(unit) : Player.Instance.GetAutoAttackDamage(unit);
            }
            if (!Damages.ContainsKey(unit.NetworkId))
            {
                Damages.Add(unit.NetworkId, new Dictionary<float, float> {{Game.Time*1000, damge}});
            }
            else
            {
                Damages[unit.NetworkId] = new Dictionary<float, float> {{Game.Time*1000, damge}};
            }
            return damge;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Jhin.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (Settings.Drawings.DrawQ && (!Settings.Drawings.DrawSpellRangesWhenReady || Q.IsReady()))
                Circle.Draw(ColorPicker[0].Color, Q.Range, Player.Instance);
            if (Settings.Drawings.DrawW && (!Settings.Drawings.DrawSpellRangesWhenReady || W.IsReady()))
                Circle.Draw(ColorPicker[1].Color, W.Range, Player.Instance);
            if (Settings.Drawings.DrawE && (!Settings.Drawings.DrawSpellRangesWhenReady || E.IsReady()))
                Circle.Draw(ColorPicker[2].Color, E.Range, Player.Instance);
            if (Settings.Drawings.DrawR && (!Settings.Drawings.DrawSpellRangesWhenReady || R.IsReady()) &&
                Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name == "JhinR")
                Circle.Draw(ColorPicker[3].Color, R.Range, Player.Instance);

            if (!Settings.Drawings.DrawInfo)
                return;

            foreach (var unit in EntityManager.MinionsAndMonsters.EnemyMinions.Where(x=>x.IsValidTarget(Q.Range) && x.Health < Damage.GetQDamage(x)))
            {
                Circle.Draw(Color.Green, 25, unit);
            }
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if(W.IsReady() && Settings.Misc.WAntiGapcloser && args.End.Distance(Player.Instance) < 350)
            {
                if (args.Delay == 0)
                    W.Cast(sender);
                else Core.DelayAction(() => W.Cast(sender), args.Delay);
            }

            if (E.IsReady() && Player.Instance.Mana - 50 > 100 && args.End.Distance(Player.Instance) < 350)
            {
                if (args.Delay == 0)
                    E.Cast(args.End);
                else Core.DelayAction(() => W.Cast(args.End), args.Delay);
            }
        }

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Jhin addon");

            ComboMenu.AddLabel("Dancing Grenade (Q) settings :");
            ComboMenu.Add("Plugins.Jhin.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Deadly Flourish (W) settings :");
            ComboMenu.Add("Plugins.Jhin.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Captive Audience (E) settings :");
            ComboMenu.Add("Plugins.Jhin.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Curtain Call (R) settings :");
            ComboMenu.Add("Plugins.Jhin.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Jhin.ComboMenu.RDelay", new Slider("Delay between shots", 0,  0, 2500));
            ComboMenu.Add("Plugins.Jhin.ComboMenu.EnableFowPrediction", new CheckBox("Enable FoW prediction"));
            ComboMenu.Add("Plugins.Jhin.ComboMenu.RMode", new ComboBox("R mode", 0, "In Combo mode", "KeyBind", "Automatic"));
            ComboMenu.Add("Plugins.Jhin.ComboMenu.RKeybind", new KeyBind("R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Jhin addon");

            HarassMenu.AddLabel("Dancing Grenade (Q) settings :");
            HarassMenu.Add("Plugins.Jhin.HarassMenu.UseQ", new CheckBox("Use Q", false));
            HarassMenu.Add("Plugins.Jhin.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Deadly Flourish (W) settings :");
            HarassMenu.Add("Plugins.Jhin.HarassMenu.UseW", new CheckBox("Use W"));
            HarassMenu.Add("Plugins.Jhin.HarassMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 80, 1));
            HarassMenu.AddSeparator(5);

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Jhin addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Dancing Grenade (Q) settings :");
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear", false));
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Deadly Flourish (W) settings :");
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.UseWInLaneClear", new CheckBox("Use w in Lane Clear"));
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.UseWInJungleClear", new CheckBox("Use W in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Jhin.LaneClearMenu.MinManaW", new Slider("Min mana percentage ({0}%) to use W", 50, 1));

            MenuManager.BuildAntiGapcloserMenu();

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Jhin addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Jhin.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Deadly Flourish (W) settings :");
            MiscMenu.Add("Plugins.Jhin.MiscMenu.WFowPrediction", new CheckBox("Use FoW prediction"));
            MiscMenu.Add("Plugins.Jhin.MiscMenu.WAntiGapcloser", new CheckBox("Cast against gapclosers"));
            MiscMenu.AddSeparator(5);
            MiscMenu.AddLabel("Captive Audience (E) settings :");
            MiscMenu.Add("Plugins.Jhin.MiscMenu.EAntiGapcloser", new CheckBox("Cast against gapclosers"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Jhin addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawSpellRangesWhenReady",
                new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Dancing Grenade (Q) settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Deadly Flourish (W) settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawW", new CheckBox("Draw W range"));
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawWColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Captive Audience (E) settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawEColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Curtain Call (R) settings :");
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[3].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.DrawInfo", new CheckBox("Draw Infos")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Jhin.DrawingsMenu.InfoColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[4].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddLabel("Draws damage indicator and minions killable from Q");
        }

        protected override void PermaActive()
        {
            Orbwalker.DisableAttacking = IsCastingR;
            Orbwalker.DisableMovement = IsCastingR;

            if (IsCastingR)
            {
                if (EntityManager.Heroes.Enemies.Any(x => x.Distance(Player.Instance) < 600))
                {
                    foreach (var enemy in EntityManager.Heroes.Enemies.Where(x => x.Distance(Player.Instance) < 600))
                    {
                        var health = enemy.Health;

                        if (GetCurrentShootsRCount > 1)
                        {
                            for (var i = 1; i <= GetCurrentShootsRCount; i++)
                            {
                                if (i != GetCurrentShootsRCount)
                                {
                                    health -= Damage.GetRDamage(enemy);
                                }
                                else
                                {
                                    health -= Damage.GetRDamage(enemy, true);
                                }
                            }
                        }
                        else
                        {
                            health -= Damage.GetRDamage(enemy, true);
                        }

                        if (enemy.Health - health < 100 && Player.Instance.HealthPercent > 25)
                        {
                            Orbwalker.DisableAttacking = true;
                            Orbwalker.DisableMovement = true;
                        }
                        else
                        {
                            Orbwalker.DisableAttacking = false;
                            Orbwalker.DisableMovement = false;
                        }
                    }
                }
                else
                {
                    Orbwalker.DisableAttacking = IsCastingR;
                    Orbwalker.DisableMovement = IsCastingR;
                }
            }

            if (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name.ToLowerInvariant() == "jhinr" && !R.IsReady() &&
                Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name.ToLowerInvariant() != "jhinrshot")
            {
                IsCastingR = false;
                REndPosition = Vector3.Zero;
            }

            Modes.PermaActive.Execute();
        }

        protected override void ComboMode()
        {
            if (!IsPreAttack && !Player.Instance.HasSheenBuff() && !IsCastingR)
            {
                Modes.Combo.Execute();
            }
        }

        protected override void HarassMode()
        {
            if (!IsPreAttack && !Player.Instance.HasSheenBuff() && !IsCastingR)
            {
                Modes.Harass.Execute();
            }
        }

        protected override void LaneClear()
        {
            if (!IsPreAttack && !IsCastingR)
            {
                Modes.LaneClear.Execute();
            }
        }

        protected override void JungleClear()
        {
            if (!IsPreAttack && !IsCastingR)
            {
                Modes.JungleClear.Execute();
            }
        }

        protected override void LastHit()
        {
            if (!IsCastingR)
            {
                Modes.LastHit.Execute();
            }
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
                        return ComboMenu?["Plugins.Jhin.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Jhin.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Jhin.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jhin.ComboMenu.UseW"] != null &&
                               ComboMenu["Plugins.Jhin.ComboMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.UseW"] != null)
                            ComboMenu["Plugins.Jhin.ComboMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jhin.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Jhin.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Jhin.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jhin.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Jhin.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Jhin.ComboMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int RDelay
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.RDelay"] != null)
                            return ComboMenu["Plugins.Jhin.ComboMenu.RDelay"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jhin.ComboMenu.RDelay menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.RDelay"] != null)
                            ComboMenu["Plugins.Jhin.ComboMenu.RDelay"].Cast<Slider>().CurrentValue = value;
                    }
                }

                /// <summary>
                /// 0 - In Combo mode
                /// 1 - Keybind
                /// 2 - Automatic
                /// </summary>
                public static int RMode
                {
                    get
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.RMode"] != null)
                            return ComboMenu["Plugins.Jhin.ComboMenu.RMode"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jhin.ComboMenu.RMode menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.RMode"] != null)
                            ComboMenu["Plugins.Jhin.ComboMenu.RMode"].Cast<ComboBox>().CurrentValue = value;
                    }
                }
                
                public static bool EnableFowPrediction
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jhin.ComboMenu.EnableFowPrediction"] != null &&
                               ComboMenu["Plugins.Jhin.ComboMenu.EnableFowPrediction"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.EnableFowPrediction"] != null)
                            ComboMenu["Plugins.Jhin.ComboMenu.EnableFowPrediction"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool RKeybind
                {
                    get
                    {
                        return ComboMenu?["Plugins.Jhin.ComboMenu.RKeybind"] != null &&
                               ComboMenu["Plugins.Jhin.ComboMenu.RKeybind"].Cast<KeyBind>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Jhin.ComboMenu.RKeybind"] != null)
                            ComboMenu["Plugins.Jhin.ComboMenu.RKeybind"].Cast<KeyBind>()
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
                        return HarassMenu?["Plugins.Jhin.HarassMenu.UseQ"] != null &&
                               HarassMenu["Plugins.Jhin.HarassMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Jhin.HarassMenu.UseQ"] != null)
                            HarassMenu["Plugins.Jhin.HarassMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Jhin.HarassMenu.MinManaQ"] != null)
                            return HarassMenu["Plugins.Jhin.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jhin.HarassMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Jhin.HarassMenu.MinManaQ"] != null)
                            HarassMenu["Plugins.Jhin.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return HarassMenu?["Plugins.Jhin.HarassMenu.UseW"] != null &&
                               HarassMenu["Plugins.Jhin.HarassMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Jhin.HarassMenu.UseW"] != null)
                            HarassMenu["Plugins.Jhin.HarassMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaW
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Jhin.HarassMenu.MinManaW"] != null)
                            return HarassMenu["Plugins.Jhin.HarassMenu.MinManaW"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jhin.HarassMenu.MinManaW menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Jhin.HarassMenu.MinManaW"] != null)
                            HarassMenu["Plugins.Jhin.HarassMenu.MinManaW"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Jhin.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Jhin.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Jhin.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Jhin.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jhin.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Jhin.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.AllowedEnemies"] != null)
                            return
                                LaneClearMenu["Plugins.Jhin.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jhin.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Jhin.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }

                public static bool UseQInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Jhin.LaneClearMenu.UseQInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Jhin.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.UseQInLaneClear"] != null)
                            LaneClearMenu["Plugins.Jhin.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseQInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Jhin.LaneClearMenu.UseQInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Jhin.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.UseQInJungleClear"] != null)
                            LaneClearMenu["Plugins.Jhin.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.MinManaQ"] != null)
                            return LaneClearMenu["Plugins.Jhin.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jhin.LaneClearMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.MinManaQ"] != null)
                            LaneClearMenu["Plugins.Jhin.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool UseWInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Jhin.LaneClearMenu.UseWInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Jhin.LaneClearMenu.UseWInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.UseWInLaneClear"] != null)
                            LaneClearMenu["Plugins.Jhin.LaneClearMenu.UseWInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseWInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Jhin.LaneClearMenu.UseWInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Jhin.LaneClearMenu.UseWInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.UseWInJungleClear"] != null)
                            LaneClearMenu["Plugins.Jhin.LaneClearMenu.UseWInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaW
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.MinManaW"] != null)
                            return LaneClearMenu["Plugins.Jhin.LaneClearMenu.MinManaW"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Jhin.LaneClearMenu.MinManaW menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Jhin.LaneClearMenu.MinManaW"] != null)
                            LaneClearMenu["Plugins.Jhin.LaneClearMenu.MinManaW"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool EnableKillsteal
                {
                    get
                    {
                        return MiscMenu?["Plugins.Jhin.MiscMenu.EnableKillsteal"] != null &&
                               MiscMenu["Plugins.Jhin.MiscMenu.EnableKillsteal"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Jhin.MiscMenu.EnableKillsteal"] != null)
                            MiscMenu["Plugins.Jhin.MiscMenu.EnableKillsteal"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool WFowPrediction
                {
                    get
                    {
                        return MiscMenu?["Plugins.Jhin.MiscMenu.WFowPrediction"] != null &&
                               MiscMenu["Plugins.Jhin.MiscMenu.WFowPrediction"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Jhin.MiscMenu.WFowPrediction"] != null)
                            MiscMenu["Plugins.Jhin.MiscMenu.WFowPrediction"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool WAntiGapcloser
                {
                    get
                    {
                        return MiscMenu?["Plugins.Jhin.MiscMenu.WAntiGapcloser"] != null &&
                               MiscMenu["Plugins.Jhin.MiscMenu.WAntiGapcloser"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Jhin.MiscMenu.WAntiGapcloser"] != null)
                            MiscMenu["Plugins.Jhin.MiscMenu.WAntiGapcloser"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                public static bool EAntiGapcloser
                {
                    get
                    {
                        return MiscMenu?["Plugins.Jhin.MiscMenu.EAntiGapcloser"] != null &&
                               MiscMenu["Plugins.Jhin.MiscMenu.EAntiGapcloser"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Jhin.MiscMenu.EAntiGapcloser"] != null)
                            MiscMenu["Plugins.Jhin.MiscMenu.EAntiGapcloser"].Cast<CheckBox>()
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
                        return DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool DrawQ
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawQ"] != null &&
                               DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawQ"] != null)
                            DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawW
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawW"] != null &&
                               DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawW"] != null)
                            DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawE
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawE"] != null &&
                               DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawE"] != null)
                            DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawR
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawR"] != null &&
                               DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawR"] != null)
                            DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawInfo
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawInfo"] != null &&
                               DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawInfo"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Jhin.DrawingsMenu.DrawInfo"] != null)
                            DrawingsMenu["Plugins.Jhin.DrawingsMenu.DrawInfo"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
            }
        }

        protected static class Damage
        {
            public static int[] QDamage { get; } = {0, 50, 75, 100, 125, 150};
            public static float[] QDamageTotalAdMod { get; } = {0, 0.3f, 0.35f, 0.4f, 0.45f, 0.5f};
            public static float QDamageBonusApMod { get; } = 0.6f;
            public static int[] WDamageOnChampions { get; } = { 0, 50, 85, 120, 155, 190 };
            public static float WDamageOnChampionsTotalAdMod { get; } = 0.5f;
            public static int[] RMinimumDamage { get; } = {0, 40, 100, 160};
            public static float RMinimumDamageTotalAdMod { get; } = 0.2f;
            public static int[] RMaximumDamage { get; } = { 0, 140, 350, 560 };
            public static float RMaximumDamageTotalAdMod { get; } = 0.7f;

            private static float _lastScanTick;
            private static float _lastAttackDamage;
            private static readonly Dictionary<int, Dictionary<float, float>> QDamages = new Dictionary<int, Dictionary<float, float>>(); 

            public static float GetRDamage(Obj_AI_Base unit, bool isFourthShoot = false)
            {
                var missingHealthAdditionalDamagePercent = 1 + ( 100 - unit.HealthPercent ) * 0.025f;
                var minimumDamage = RMinimumDamage[R.Level] + GetRealAttackDamage() * RMinimumDamageTotalAdMod;
                var maximumDamage = RMaximumDamage[R.Level] + GetRealAttackDamage() * RMaximumDamageTotalAdMod;
                float damage;

                if (!isFourthShoot)
                {
                    damage = Math.Min(minimumDamage * missingHealthAdditionalDamagePercent, maximumDamage);
                }
                else
                {
                    damage = Math.Min(minimumDamage * missingHealthAdditionalDamagePercent, maximumDamage) * (Player.Instance.HasItem(ItemId.Infinity_Edge) ? 2.5f : 2f) * (1 + Player.Instance.FlatCritChanceMod);
                }
                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, damage);
            }

            public static float GetRealAttackDamage()
            {
                if (Game.Time*1000 - _lastScanTick < 10000)
                {
                    return _lastAttackDamage;
                }

                float[] additionalAttackDamage =
                {
                    0, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f, 0.07f, 0.08f, 0.1f, 0.12f, 0.14f,
                    0.16f, 0.18f, 0.2f, 0.24f, 0.28f, 0.32f, 0.36f, 0.4f
                };
                int[] attackSpeedItemsId =
                {
                    3006, 3153, 1042, 2015, 3115, 3046, 3094, 1043, 3085, 3087, 3101, 3078, 3091,
                    3086
                };

                var addicionalAdFromCritChance = 0f;
                var additionalAdFromAttackSpeed = 0f;
                var additionalAttackSpeed = (from i in attackSpeedItemsId
                    where Player.Instance.HasItem(i)
                    select Item.ItemData.FirstOrDefault(x => x.Key == (ItemId) i)
                    into data
                    select data.Value.Stats.PercentAttackSpeedMod).Sum();

                for (var i = 0f; i < 1; i += 0.1f)
                {
                    if (Player.Instance.FlatCritChanceMod >= i && Player.Instance.FlatCritChanceMod < i + 0.1f)
                    {
                        addicionalAdFromCritChance = Player.Instance.TotalAttackDamage*(0.04f*(i*10));
                    }
                    if (additionalAttackSpeed >= i && additionalAttackSpeed < i + 0.1f)
                    {
                        additionalAdFromAttackSpeed = Player.Instance.TotalAttackDamage*(0.025f*(i*10));
                    }
                }
                var totalAd = Player.Instance.TotalAttackDamage +
                              Player.Instance.TotalAttackDamage*additionalAttackDamage[Player.Instance.Level] +
                              additionalAdFromAttackSpeed + addicionalAdFromCritChance;

                _lastScanTick = Game.Time*1000;
                _lastAttackDamage = totalAd;

                return totalAd;
            }

            public static float Get4ThShootDamage(Obj_AI_Base unit)
            {
                var bonusDamage = 0f;
                if (Player.Instance.Level < 6)
                    bonusDamage = 0.15f;
                else if (Player.Instance.Level < 11 && Player.Instance.Level >= 6)
                    bonusDamage = 0.20f;
                else if (Player.Instance.Level >= 11)
                    bonusDamage = 0.25f;

                var damage = GetRealAttackDamage() * 1.75f + (unit.MaxHealth - unit.Health) * bonusDamage;

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, Player.Instance.HasItem(ItemId.Infinity_Edge) ? damage * 1.5f : damage, false, true);
            }

            public static float GetQDamage(Obj_AI_Base unit)
            {
                if (QDamages.ContainsKey(unit.NetworkId) &&
                    Game.Time*1000 - QDamages[unit.NetworkId].Select(x => x.Key).First() < 250)
                {
                    return QDamages[unit.NetworkId].Select(x => x.Value).First();
                }

                var damage = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                    QDamage[Q.Level] + GetRealAttackDamage() * QDamageTotalAdMod[Q.Level] +
                    Player.Instance.FlatMagicDamageMod * QDamageBonusApMod, false, true);

                if (!QDamages.ContainsKey(unit.NetworkId))
                {
                    QDamages.Add(unit.NetworkId, new Dictionary<float, float>
                    {
                        {
                            Game.Time*1000, damage
                        }
                    });
                }
                else
                {
                    QDamages[unit.NetworkId] = new Dictionary<float, float>
                    {
                        {
                            Game.Time*1000, damage
                        }
                    };
                }
                return damage;
            }

            public static float GetWDamage(Obj_AI_Base unit)
            {
                var damage = WDamageOnChampions[W.Level] +
                             GetRealAttackDamage() * WDamageOnChampionsTotalAdMod;

                if (!(unit is AIHeroClient))
                {
                    damage *= 0.75f;
                }
                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, damage, false, true);
            }

            public static bool IsTargetKillableFromW(Obj_AI_Base unit)
            {
                if (!(unit is AIHeroClient))
                {
                    return unit.TotalHealthWithShields() <= GetWDamage(unit);
                }

                var enemy = (AIHeroClient)unit;

                if (enemy.HasSpellShield() || enemy.HasUndyingBuffA())
                    return false;

                if (enemy.ChampionName != "Blitzcrank")
                    return enemy.TotalHealthWithShields(true) < GetWDamage(enemy);

                if (!enemy.HasBuff("BlitzcrankManaBarrierCD") && !enemy.HasBuff("ManaBarrier"))
                {
                    return enemy.TotalHealthWithShields(true) + enemy.Mana / 2 < GetWDamage(enemy);
                }

                return enemy.TotalHealthWithShields(true) < GetWDamage(enemy);
            }
        }
    }
}