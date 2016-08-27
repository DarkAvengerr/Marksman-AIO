#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Lucian.cs" company="EloBuddy">
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

namespace Simple_Marksmans.Plugins.Lucian
{
    internal class Lucian : ChampionPlugin
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

        public static bool HasPassiveBuff
            => Player.Instance.Buffs.Any(x => x.Name.ToLowerInvariant() == "lucianpassivebuff");

        public static BuffInstance GetPassiveBuff
            => Player.Instance.Buffs.FirstOrDefault(x => x.Name.ToLowerInvariant() == "lucianpassivebuff");

        private static readonly ColorPicker[] ColorPicker;
        private static bool _changingRangeScan;

        protected static bool IsPreAttack { get; private set; }
        protected static bool IsCastingR { get; set; }

        static Lucian()
        {
            Q = new Spell.Targeted(SpellSlot.Q, 750);
            W = new Spell.Skillshot(SpellSlot.W, 1000, SkillShotType.Circular, 250, 1500, 80);
            E = new Spell.Skillshot(SpellSlot.E, 420, SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, 1150, SkillShotType.Linear, 250, 2000, 110);

            ColorPicker = new ColorPicker[3];

            ColorPicker[0] = new ColorPicker("LucianQ", new ColorBGRA(10, 106, 138, 255));
            ColorPicker[1] = new ColorPicker("LucianR", new ColorBGRA(177, 67, 191, 255));
            ColorPicker[2] = new ColorPicker("LucianHpBar", new ColorBGRA(255, 134, 0, 255));

            DamageIndicator.Initalize(
                System.Drawing.Color.FromArgb(ColorPicker[2].Color.R, ColorPicker[2].Color.G, ColorPicker[2].Color.B));
            DamageIndicator.DamageDelegate = HandleDamageIndicator;

            ColorPicker[2].OnColorChange += (a, b) => { DamageIndicator.Color = System.Drawing.Color.FromArgb(b.Color.A, b.Color.R, b.Color.G, b.Color.B); };

            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Game.OnPostTick += args => IsPreAttack = false;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (Player.Instance.Spellbook.IsCastingSpell)
                args.Process = false;// stupid q bug stops occuring
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
                return;

            if(args.Slot == SpellSlot.Q || args.Slot == SpellSlot.W || args.Slot== SpellSlot.E)
                Orbwalker.ResetAutoAttack();

            if (args.Slot == SpellSlot.R)
            {
                Activator.Activator.Items[ItemsEnum.Ghostblade].UseItem();
            }
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                return;

            if (Q.IsReady() && !IsCastingR && Settings.Combo.UseQ && !HasPassiveBuff && !Player.Instance.HasSheenBuff())
            {
                var aiHeroClient = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                var target2 = TargetSelector.GetTarget(900, DamageType.Physical);

                if (aiHeroClient != null && aiHeroClient.IsValidTarget(Q.Range) &&
                    ((Player.Instance.Mana - 50 + 5*(Q.Level - 1) > 40 - 10*(E.Level - 1) + 100) ||
                     Player.Instance.GetSpellDamage(aiHeroClient, SpellSlot.Q) > aiHeroClient.TotalHealthWithShields()))
                {
                    Q.Cast(aiHeroClient);
                    return;
                }
                if (Settings.Combo.ExtendQOnMinions && target2 != null &&
                    ((Player.Instance.Mana - 50 + 5*(Q.Level - 1) > 40 - 10*(E.Level - 1) + 100) ||
                     Player.Instance.GetSpellDamage(target2, SpellSlot.Q) > target2.TotalHealthWithShields()))
                {
                    foreach (
                        var entity in
                            from entity in
                                EntityManager.MinionsAndMonsters.CombinedAttackable.Where(
                                    x => x.IsValidTarget(Q.Range))
                            let pos =
                                Player.Instance.Position.Extend(entity, 900 - Player.Instance.Distance(entity))
                            let targetpos = Prediction.Position.PredictUnitPosition(target2, 250)
                            let rect = new Geometry.Polygon.Rectangle(entity.Position.To2D(), pos, 10)
                            where
                                new Geometry.Polygon.Circle(targetpos, target2.BoundingRadius).Points.Any(
                                    rect.IsInside)
                            select entity)
                    {
                        Q.Cast(entity);
                        return;
                    }
                }
            }

            if (W.IsReady() && !IsCastingR && Settings.Combo.UseW && !HasPassiveBuff && !Player.Instance.HasSheenBuff())
            {
                var aiHeroClient = TargetSelector.GetTarget(W.Range, DamageType.Physical);

                if (aiHeroClient != null &&
                    ((Player.Instance.Mana - 50 > 100) ||
                     Player.Instance.GetSpellDamage(aiHeroClient, SpellSlot.W) > aiHeroClient.TotalHealthWithShields()))
                {
                    if (Settings.Combo.IgnoreCollisionW)
                    {
                        W.Cast(aiHeroClient);
                        return;
                    }
                    var wPrediction = W.GetPrediction(aiHeroClient);
                    if (wPrediction.HitChance == HitChance.Medium)
                    {
                        W.Cast(wPrediction.CastPosition);
                        return;
                    }
                }
            }

            if (!E.IsReady() || IsCastingR || !Settings.Combo.UseE || HasPassiveBuff || Player.Instance.HasSheenBuff())
                return;

            var heroClient = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 420, DamageType.Physical);
            var position = Vector3.Zero;

            if (Settings.Misc.EMode == 0)
            {
                if (heroClient != null && Player.Instance.HealthPercent > 50 && heroClient.HealthPercent < 30)
                {
                    if (!Player.Instance.Position.Extend(Game.CursorPos, 420)
                        .To3D()
                        .IsVectorUnderEnemyTower() &&
                        (!heroClient.IsMelee ||
                         Player.Instance.Position.Extend(Game.CursorPos, 420)
                             .IsInRange(heroClient, heroClient.GetAutoAttackRange() * 1.5f)))
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
                            heroClient.IsMelee ? heroClient.GetAutoAttackRange() * 2 : heroClient.GetAutoAttackRange())
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
                                        x.IsMelee ? x.GetAutoAttackRange() * 2 : x.GetAutoAttackRange())))
                        {
                            position = asc;

                            Console.WriteLine("[DEBUG] Paths low sorting Ascending");
                        }
                        else if (Player.Instance.CountEnemiesInRange(1000) <= 2 && (paths == 0 || paths == 1) &&
                                 ((closest.Health < Player.Instance.GetAutoAttackDamage(closest, true) * 2) ||
                                  (Orbwalker.LastTarget is AIHeroClient &&
                                   Orbwalker.LastTarget.Health < Player.Instance.GetAutoAttackDamage(closest, true) * 2)))
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
                    E.Cast(position);
            }
            else if (Settings.Misc.EMode == 1)
            {
                var enemies = Player.Instance.CountEnemiesInRange(1300);
                var pos = Game.CursorPos.Distance(Player.Instance) > E.Range
                            ? Player.Instance.Position.Extend(Game.CursorPos, 420).To3D()
                            : Game.CursorPos;

                if (!pos.IsVectorUnderEnemyTower())
                {
                    if (heroClient != null)
                    {
                        if (enemies == 1 && heroClient.HealthPercent + 15 < Player.Instance.HealthPercent)
                        {
                            if (heroClient.IsMelee && !pos.IsInRange(Prediction.Position.PredictUnitPosition(heroClient, 850), heroClient.GetAutoAttackRange() + 150))
                            {
                                E.Cast(pos);
                            }
                            else if (!heroClient.IsMelee)
                            {
                                E.Cast(pos);
                            }
                        }
                        else if (enemies == 1 && !pos.IsInRange(Prediction.Position.PredictUnitPosition(heroClient, 850), heroClient.GetAutoAttackRange()))
                        {
                            E.Cast(pos);
                        }
                        else if (enemies == 2 && Player.Instance.CountAlliesInRange(850) >= 1)
                        {
                            E.Cast(pos);
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
                            }
                        }
                    }
                }
            }
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

            if (unit.Distance(Player.Instance) < 900)
                damage += Player.Instance.GetSpellDamage(unit, SpellSlot.Q);

            if (unit.Distance(Player.Instance) < W.Range)
                damage += Player.Instance.GetSpellDamage(unit, SpellSlot.W);

            return damage;
        }

        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Lucian.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

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
            ComboMenu.AddGroupLabel("Combo mode settings for Lucian addon");

            ComboMenu.AddLabel("Piercing Light (Q) settings :");
            ComboMenu.Add("Plugins.Lucian.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.Add("Plugins.Lucian.ComboMenu.ExtendQOnMinions", new CheckBox("Try to extend Q on minions"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Ardent Blaze (W) settings :");
            ComboMenu.Add("Plugins.Lucian.ComboMenu.UseW", new CheckBox("Use W"));
            ComboMenu.Add("Plugins.Lucian.ComboMenu.IgnoreCollisionW", new CheckBox("Ignore collision"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Relentless Pursuit (E) settings :");
            ComboMenu.Add("Plugins.Lucian.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("The Culling (R) settings :");
            ComboMenu.Add("Plugins.Lucian.ComboMenu.UseR", new CheckBox("Use R"));
            ComboMenu.Add("Plugins.Lucian.ComboMenu.RKeybind", new KeyBind("R keybind", false, KeyBind.BindTypes.HoldActive, 'T'));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Lucian addon");

            HarassMenu.AddLabel("Piercing Light (Q) settings :");
            HarassMenu.Add("Plugins.Lucian.HarassMenu.UseQ", new KeyBind("Enable auto harass", false, KeyBind.BindTypes.PressToggle, 'A'));
            HarassMenu.Add("Plugins.Lucian.HarassMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            HarassMenu.AddSeparator(5);

            HarassMenu.AddLabel("Auto harass enabled for :");
            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                HarassMenu.Add("Plugins.Lucian.HarassMenu.UseQ."+enemy.Hero, new CheckBox(enemy.ChampionName == "MonkeyKing" ? "Wukong" : enemy.ChampionName));
            }

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear");
            LaneClearMenu.AddGroupLabel("Lane clear settings for Lucian addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Piercing Light (Q) settings :");
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.UseQInLaneClear", new CheckBox("Use Q in Lane Clear"));
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.MinMinionsHitQ", new Slider("Min minions hit to use Q", 3, 1, 8));
            LaneClearMenu.AddSeparator(5);
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.UseQInJungleClear", new CheckBox("Use Q in Jungle Clear"));
            LaneClearMenu.Add("Plugins.Lucian.LaneClearMenu.MinManaQ", new Slider("Min mana percentage ({0}%) to use Q", 50, 1));

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Lucian addon");
            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Lucian.MiscMenu.EnableKillsteal", new CheckBox("Enable Killsteal"));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Relentless Pursuit (E) settings :");
            MiscMenu.Add("Plugins.Lucian.MiscMenu.EMode", new ComboBox("E mode", 0, "Auto", "Cursor Pos"));
            MiscMenu.Add("Plugins.Lucian.MiscMenu.EUsageMode", new ComboBox("E usage", 0, "Always", "After autoattack only"));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Lucian addon");

            DrawingsMenu.AddLabel("Basic settings :");
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawSpellRangesWhenReady", new CheckBox("Draw spell ranges only when they are ready"));
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Piercing Light (Q) settings :");
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawQ", new CheckBox("Draw Q range"));
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawQColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("The Culling (R) settings :");
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawR", new CheckBox("Draw R range"));
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawRColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.DrawInfo", new CheckBox("Draw Infos")).OnValueChange += (a, b) =>
            {
                if (b.NewValue)
                    DamageIndicator.DamageDelegate = HandleDamageIndicator;
                else if (!b.NewValue)
                    DamageIndicator.DamageDelegate = null;
            };
            DrawingsMenu.Add("Plugins.Lucian.DrawingsMenu.InfoColor", new CheckBox("Change color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            DrawingsMenu.AddLabel("Draws damage indicator");

        }

        protected override void PermaActive()
        {
            if (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name.ToLowerInvariant() == "lucianrdisable")
                IsCastingR = true;

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
                        return ComboMenu?["Plugins.Lucian.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Lucian.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Lucian.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Lucian.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool ExtendQOnMinions
                {
                    get
                    {
                        return ComboMenu?["Plugins.Lucian.ComboMenu.ExtendQOnMinions"] != null &&
                               ComboMenu["Plugins.Lucian.ComboMenu.ExtendQOnMinions"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Lucian.ComboMenu.ExtendQOnMinions"] != null)
                            ComboMenu["Plugins.Lucian.ComboMenu.ExtendQOnMinions"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Lucian.ComboMenu.UseW"] != null &&
                               ComboMenu["Plugins.Lucian.ComboMenu.UseW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Lucian.ComboMenu.UseW"] != null)
                            ComboMenu["Plugins.Lucian.ComboMenu.UseW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool IgnoreCollisionW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Lucian.ComboMenu.IgnoreCollisionW"] != null &&
                               ComboMenu["Plugins.Lucian.ComboMenu.IgnoreCollisionW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Lucian.ComboMenu.IgnoreCollisionW"] != null)
                            ComboMenu["Plugins.Lucian.ComboMenu.IgnoreCollisionW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Lucian.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Lucian.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Lucian.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Lucian.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Lucian.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Lucian.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Lucian.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Lucian.ComboMenu.UseR"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool RKeybind
                {
                    get
                    {
                        return ComboMenu?["Plugins.Lucian.ComboMenu.RKeybind"] != null &&
                               ComboMenu["Plugins.Lucian.ComboMenu.RKeybind"].Cast<KeyBind>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Lucian.ComboMenu.RKeybind"] != null)
                            ComboMenu["Plugins.Lucian.ComboMenu.RKeybind"].Cast<KeyBind>()
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
                        return HarassMenu?["Plugins.Lucian.HarassMenu.UseQ"] != null &&
                               HarassMenu["Plugins.Lucian.HarassMenu.UseQ"].Cast<KeyBind>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Lucian.HarassMenu.UseQ"] != null)
                            HarassMenu["Plugins.Lucian.HarassMenu.UseQ"].Cast<KeyBind>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Lucian.HarassMenu.MinManaQ"] != null)
                            return HarassMenu["Plugins.Lucian.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Lucian.HarassMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Lucian.HarassMenu.MinManaQ"] != null)
                            HarassMenu["Plugins.Lucian.HarassMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool IsAutoHarassEnabledFor(AIHeroClient unit)
                {
                    return HarassMenu?["Plugins.Lucian.HarassMenu.UseQ." + unit.Hero] != null &&
                           HarassMenu["Plugins.Lucian.HarassMenu.UseQ." + unit.Hero].Cast<CheckBox>()
                               .CurrentValue;
                }

                public static bool IsAutoHarassEnabledFor(string championName)
                {
                    return HarassMenu?["Plugins.Lucian.HarassMenu.UseQ." + championName] != null &&
                           HarassMenu["Plugins.Lucian.HarassMenu.UseQ." + championName].Cast<CheckBox>()
                               .CurrentValue;
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Lucian.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Lucian.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Lucian.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Lucian.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Lucian.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Lucian.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.AllowedEnemies"] != null)
                            return
                                LaneClearMenu["Plugins.Lucian.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Lucian.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Lucian.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }

                public static bool UseQInLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Lucian.LaneClearMenu.UseQInLaneClear"] != null &&
                               LaneClearMenu["Plugins.Lucian.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.UseQInLaneClear"] != null)
                            LaneClearMenu["Plugins.Lucian.LaneClearMenu.UseQInLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseQInJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Lucian.LaneClearMenu.UseQInJungleClear"] != null &&
                               LaneClearMenu["Plugins.Lucian.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.UseQInJungleClear"] != null)
                            LaneClearMenu["Plugins.Lucian.LaneClearMenu.UseQInJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinMinionsHitQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.MinMinionsHitQ"] != null)
                            return LaneClearMenu["Plugins.Lucian.LaneClearMenu.MinMinionsHitQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Lucian.LaneClearMenu.MinMinionsHitQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.MinMinionsHitQ"] != null)
                            LaneClearMenu["Plugins.Lucian.LaneClearMenu.MinMinionsHitQ"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static int MinManaQ
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.MinManaQ"] != null)
                            return LaneClearMenu["Plugins.Lucian.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Lucian.LaneClearMenu.MinManaQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Lucian.LaneClearMenu.MinManaQ"] != null)
                            LaneClearMenu["Plugins.Lucian.LaneClearMenu.MinManaQ"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool EnableKillsteal
                {
                    get
                    {
                        return MiscMenu?["Plugins.Lucian.MiscMenu.EnableKillsteal"] != null &&
                               MiscMenu["Plugins.Lucian.MiscMenu.EnableKillsteal"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Lucian.MiscMenu.EnableKillsteal"] != null)
                            MiscMenu["Plugins.Lucian.MiscMenu.EnableKillsteal"].Cast<CheckBox>()
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
                        if (MiscMenu?["Plugins.Lucian.MiscMenu.EMode"] != null)
                            return MiscMenu["Plugins.Lucian.MiscMenu.EMode"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Lucian.MiscMenu.EMode menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Lucian.MiscMenu.EMode"] != null)
                            MiscMenu["Plugins.Lucian.MiscMenu.EMode"].Cast<ComboBox>().CurrentValue = value;
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
                        if (MiscMenu?["Plugins.Lucian.MiscMenu.EUsageMode"] != null)
                            return MiscMenu["Plugins.Lucian.MiscMenu.EUsageMode"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Lucian.MiscMenu.EUsageMode menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Lucian.MiscMenu.EUsageMode"] != null)
                            MiscMenu["Plugins.Lucian.MiscMenu.EUsageMode"].Cast<ComboBox>().CurrentValue = value;
                    }
                }
            }

            internal static class Drawings
            {
                public static bool DrawSpellRangesWhenReady
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Lucian.DrawingsMenu.DrawSpellRangesWhenReady"] != null &&
                               DrawingsMenu["Plugins.Lucian.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Lucian.DrawingsMenu.DrawSpellRangesWhenReady"] != null)
                            DrawingsMenu["Plugins.Lucian.DrawingsMenu.DrawSpellRangesWhenReady"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool DrawQ
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Lucian.DrawingsMenu.DrawQ"] != null &&
                               DrawingsMenu["Plugins.Lucian.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Lucian.DrawingsMenu.DrawQ"] != null)
                            DrawingsMenu["Plugins.Lucian.DrawingsMenu.DrawQ"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
                public static bool DrawR
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Lucian.DrawingsMenu.DrawR"] != null &&
                               DrawingsMenu["Plugins.Lucian.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Lucian.DrawingsMenu.DrawR"] != null)
                            DrawingsMenu["Plugins.Lucian.DrawingsMenu.DrawR"].Cast<CheckBox>().CurrentValue = value;
                    }
                }

                public static bool DrawInfo
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Lucian.DrawingsMenu.DrawInfo"] != null &&
                               DrawingsMenu["Plugins.Lucian.DrawingsMenu.DrawInfo"].Cast<CheckBox>().CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Lucian.DrawingsMenu.DrawInfo"] != null)
                            DrawingsMenu["Plugins.Lucian.DrawingsMenu.DrawInfo"].Cast<CheckBox>().CurrentValue = value;
                    }
                }
            }
        }

        protected class Damage
        {
            public static float GetSingleRShotDamage(AIHeroClient unit)
            {
                int[] qDamages = {0, 20, 35, 50};

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical, qDamages[R.Level] + (Player.Instance.FlatPhysicalDamageMod * 0.2f + Player.Instance.FlatMagicDamageMod * 0.1f));
            }
        }
    }
}