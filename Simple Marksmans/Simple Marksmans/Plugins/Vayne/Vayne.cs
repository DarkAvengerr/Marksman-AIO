#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Vayne.cs" company="EloBuddy">
// 
//  Marksman AIO
// 
//  Copyright (C) 2016 Krystian Tenerowicz
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see http://www.gnu.org/licenses/. 
//  </copyright>
//  <summary>
// 
//  Email: geroelobuddy@gmail.com
//  PayPal: geroelobuddy@gmail.com
//  </summary>
//  --------------------------------------------------------------------------------------------------------------------
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
using Simple_Marksmans.Utils;
using EloBuddy.SDK.Utils;
using SharpDX;
using Simple_Marksmans.Utils.PermaShow;
using Color = SharpDX.Color;
using Text = EloBuddy.SDK.Rendering.Text;

namespace Simple_Marksmans.Plugins.Vayne
{
    internal class Vayne : ChampionPlugin
    {
        public static Spell.Skillshot Q { get; }
        public static Spell.Active W { get; }
        public static Spell.Targeted E { get; }
        public static Spell.Active R { get; }

        private static Menu ComboMenu { get; set; }
        private static Menu HarassMenu { get; set; }
        private static Menu LaneClearMenu { get; set; }
        private static Menu MiscMenu { get; set; }
        private static Menu DrawingsMenu { get; set; }

        public static PermaShow PermaShow;
        public static BoolItemData SafetyChecksPermaShowItem;
        public static BoolItemData NoAaStealthPermaShowItem;

        public static BuffInstance GetTumbleBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "vaynetumble");

        public static bool HasTumbleBuff
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "vaynetumble");

        public static bool HasSilverDebuff(Obj_AI_Base unit)
            =>
                unit.Buffs.Any(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "vaynesilverdebuff");

        public static BuffInstance GetSilverDebuff(Obj_AI_Base unit)
            =>
                unit.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "vaynesilverdebuff");

        public static bool HasInquisitionBuff
            =>
                Player.Instance.Buffs.Any(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "vayneinquisition");

        public static BuffInstance GetInquisitionBuff
            =>
                Player.Instance.Buffs.FirstOrDefault(
                    b => b.IsActive && b.DisplayName.ToLowerInvariant() == "vayneinquisition");

        private static bool _changingRangeScan;
        private static float _lastQCastTime;
        private static readonly Text Text;
        public static bool IsPostAttack { get; private set; }

        static Vayne()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 300, SkillShotType.Linear);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 650);
            R = new Spell.Active(SpellSlot.R);

            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Game.OnPostTick += args => IsPostAttack = false;

            if (EntityManager.Heroes.Enemies.Any(client => client.Hero == Champion.Rengar))
            {
                GameObject.OnCreate += Obj_AI_Base_OnCreate;
            }
            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));

            PermaShow = new PermaShow("Vayne PermaShow", new Vector2(200, 200));
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            IsPostAttack = true;

            if (!(target is AIHeroClient) || !Settings.Misc.EKs || !target.IsValidTarget(E.Range))
                return;

            var enemy = (AIHeroClient)target;
                
            if (HasSilverDebuff(enemy) && GetSilverDebuff(enemy).Count == 1)
            {
                Core.DelayAction(() =>
                {
                    if (Damage.IsKillableFromSilverEAndAuto(enemy) && enemy.Health > IncomingDamage.GetIncomingDamage(enemy))
                    {
                        Console.WriteLine("[DEBUG] casting e to ks");
                        E.Cast(enemy);
                    }}, 40 + Game.Ping / 2);
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!Settings.Misc.NoAaWhileStealth || !HasInquisitionBuff)
                return;

            if (args.Slot == SpellSlot.Q)
            {
                _lastQCastTime = Game.Time*1000;
            }
        }

        private static void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name != "Rengar_LeapSound.troy" || !E.IsReady() || Player.Instance.IsDead || Settings.Misc.EAntiRengar)
                return;

            foreach (var rengar in EntityManager.Heroes.Enemies.Where(x => x.ChampionName == "Rengar").Where(rengar => rengar.Distance(Player.Instance.Position) < 1000).Where(rengar => rengar.IsValidTarget(E.Range) && E.IsReady()))
            {
                Console.WriteLine("[DEBUG] casting e as anti-rengar");
                E.Cast(rengar);
            }
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (HasInquisitionBuff && Settings.Misc.NoAaWhileStealth && Game.Time * 1000 - _lastQCastTime < Settings.Misc.NoAaDelay)
            {
                if (target is AIHeroClient &&
                    target.Health > Player.Instance.GetAutoAttackDamage((AIHeroClient) target, true)*3)
                {
                    args.Process = false;
                }
            }
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (!E.IsReady() || !sender.IsValidTarget(E.Range))
                return;

            E.Cast(sender);

            Misc.PrintInfoMessage("Interrupting " + sender.ChampionName + "'s " + args.SpellName);

            Console.WriteLine("[DEBUG] OnInterruptible | Champion : {0} | SpellSlot : {1}", sender.ChampionName, args.SpellSlot);
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (E.IsReady() && sender.IsValidTarget(E.Range) && args.End.Distance(Player.Instance) < 500)
            {
                E.Cast(sender);

                Console.WriteLine("[DEBUG] OnGapcloser | Champion : {0} | SpellSlot : {1}", sender.ChampionName, args.SpellSlot);
            }
        }


        public static bool WillEStun(Obj_AI_Base target)
        {
            if (target == null || !IsECastableOnEnemy(target))
                return false;

            var pushDistance = Settings.Misc.PushDistance;
            var eta = target.Distance(Player.Instance) / 2000;
            var position = Prediction.Position.PredictUnitPosition(target, 250 + (int)eta * 1000);

            if (!target.CanMove)
            {
                for (var i = 25; i < pushDistance + 50; i += 50)
                {
                    if (target.ServerPosition.Extend(Player.Instance.ServerPosition, -Math.Min(i, pushDistance)).IsWall())
                    {
                        return true;
                    }
                }
            }

            for (var i = pushDistance; i >= 100; i -= 100)
            {
                var vec = position.Extend(Player.Instance.ServerPosition, -i);

                var left = new Vector2[5];
                var right = new Vector2[5];
                var var = 18 * i / 100;

                for (var x = 0; x < 5; x++)
                {
                    left[x] =
                        position.Extend(
                            vec + (position - vec).Normalized().Rotated((float)(Math.PI / 180) * Math.Max(0, var)) *
                            Math.Abs(i < 200 ? 50 : 45 * x), i);
                    right[x] =
                        position.Extend(
                            vec +
                            (position - vec).Normalized().Rotated((float)(Math.PI / 180) * -Math.Max(0, var)) *
                            Math.Abs(i < 200 ? 50 : 45 * x), i);
                }
                if (left[0].IsWall() && right[0].IsWall() && left[1].IsWall() && right[1].IsWall() &&
                    left[2].IsWall() && right[2].IsWall() && left[3].IsWall() && right[3].IsWall() &&
                    left[4].IsWall() && right[4].IsWall() && vec.IsWall())
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsECastableOnEnemy(Obj_AI_Base unit)
        {
            return E.IsReady() && unit.IsValidTarget(E.Range) && !unit.IsZombie &&
                   !unit.HasBuffOfType(BuffType.Invulnerability) && !unit.HasBuffOfType(BuffType.SpellImmunity) &&
                   !unit.HasBuffOfType(BuffType.SpellShield);
        }


        protected override void OnDraw()
        {
            if (_changingRangeScan)
                Circle.Draw(Color.White,
                    LaneClearMenu["Plugins.Vayne.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue, Player.Instance);

            if (!Settings.Drawings.DrawInfo)
                return;

            foreach (var source in EntityManager.Heroes.Enemies.Where(x => x.IsVisible && x.IsHPBarRendered && x.Position.IsOnScreen() && HasSilverDebuff(x)))
            {
                var hpPosition = source.HPBarPosition;
                hpPosition.Y = hpPosition.Y + 30; // tracker friendly.
                var timeLeft = GetSilverDebuff(source).EndTime - Game.Time;
                var endPos = timeLeft * 0x3e8 / 32;

                var degree = Misc.GetNumberInRangeFromProcent(timeLeft * 1000d / 3000d * 100d, 3, 110);
                var color = new Misc.HsvColor(degree, 1, 1).ColorFromHsv();

                Text.X = (int)(hpPosition.X + endPos);
                Text.Y = (int)hpPosition.Y + 15; // + text size 
                Text.Color = color;
                Text.TextValue = timeLeft.ToString("F1");
                Text.Draw();

                Drawing.DrawLine(hpPosition.X + endPos, hpPosition.Y, hpPosition.X, hpPosition.Y, 1, color);
            }
        }
        

        protected override void CreateMenu()
        {
            ComboMenu = MenuManager.Menu.AddSubMenu("Combo");
            ComboMenu.AddGroupLabel("Combo mode settings for Vayne addon");

            ComboMenu.AddLabel("Tumble (Q) settings :");
            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseQ", new CheckBox("Use Q"));
            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseQOnlyToProcW", new CheckBox("Use Q only to proc W stacks", false));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Condemn (E) settings :");
            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseE", new CheckBox("Use E"));
            ComboMenu.AddSeparator(5);

            ComboMenu.AddLabel("Final Hour (R) settings :");
            ComboMenu.Add("Plugins.Vayne.ComboMenu.UseR", new CheckBox("Use R", false));
            ComboMenu.AddSeparator(5);

            HarassMenu = MenuManager.Menu.AddSubMenu("Harass");
            HarassMenu.AddGroupLabel("Harass mode settings for Vayne addon");

            HarassMenu.AddLabel("Tumble (Q) settings :");
            HarassMenu.Add("Plugins.Vayne.HarassMenu.UseQ", new CheckBox("Use Q", false));
            HarassMenu.Add("Plugins.Vayne.HarassMenu.MinManaToUseQ", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            HarassMenu.AddSeparator(5);

            LaneClearMenu = MenuManager.Menu.AddSubMenu("Clear mode");
            LaneClearMenu.AddGroupLabel("Lane clear / Jungle Clear mode settings for Vayne addon");

            LaneClearMenu.AddLabel("Basic settings :");
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.EnableLCIfNoEn", new CheckBox("Enable lane clear only if no enemies nearby"));
            var scanRange = LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.ScanRange", new Slider("Range to scan for enemies", 1500, 300, 2500));
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
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.AllowedEnemies", new Slider("Allowed enemies amount", 1, 0, 5));
            LaneClearMenu.AddSeparator(5);

            LaneClearMenu.AddLabel("Tumble (Q) settings :");
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.UseQToLaneClear", new CheckBox("Use Q to lane clear"));
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.UseQToJungleClear", new CheckBox("Use Q to jungle clear"));
            LaneClearMenu.Add("Plugins.Vayne.LaneClearMenu.MinMana", new Slider("Min mana percentage ({0}%) to use Q", 80, 1));
            LaneClearMenu.AddSeparator(5);

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Vayne addon");

            MiscMenu.AddLabel("Basic settings :");
            MiscMenu.Add("Plugins.Vayne.MiscMenu.NoAAWhileStealth",
                new KeyBind("Dont AutoAttack while stealth", false, KeyBind.BindTypes.PressToggle, 'T')).OnValueChange
                +=
                (a, b) =>
                {
                    if (NoAaStealthPermaShowItem != null)
                    {
                        NoAaStealthPermaShowItem.Value = b.NewValue;
                    }
                };
            MiscMenu.Add("Plugins.Vayne.MiscMenu.NoAADelay", new Slider("Delay", 1000, 0, 1000));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Additional Condemn (E) settings :");
            MiscMenu.Add("Plugins.Vayne.MiscMenu.EAntiRengar", new CheckBox("Enable Anti-Rengar"));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.Eks", new CheckBox("Use E to killsteal"));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.PushDistance", new Slider("Push distance", 420, 400, 450));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.EMode", new ComboBox("E Mode", 1, "Always", "Only in Combo" ));
            MiscMenu.AddSeparator(5);

            MiscMenu.AddLabel("Additional Tumble (Q) settings :");
            MiscMenu.Add("Plugins.Vayne.MiscMenu.QMode", new ComboBox("Q Mode", 0, "CursorPos", "Auto"));
            MiscMenu.Add("Plugins.Vayne.MiscMenu.QSafetyChecks", new CheckBox("Enable safety checks")).OnValueChange +=
                (a, b) =>
                {
                    if (SafetyChecksPermaShowItem != null)
                    {
                        SafetyChecksPermaShowItem.Value = b.NewValue;
                    }
                };

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawing settings for Vayne addon");
            DrawingsMenu.Add("Plugins.Vayne.DrawingsMenu.DrawInfo", new CheckBox("Draw info"));

            SafetyChecksPermaShowItem = PermaShow.AddItem("Safety Checks", new BoolItemData("Enable safety checks", Settings.Misc.QSafetyChecks, 14));
            NoAaStealthPermaShowItem = PermaShow.AddItem("No Aa While stealth", new BoolItemData("No AA while stealth", Settings.Misc.NoAaWhileStealth, 14));
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
                        return ComboMenu?["Plugins.Vayne.ComboMenu.UseQ"] != null &&
                               ComboMenu["Plugins.Vayne.ComboMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Vayne.ComboMenu.UseQ"] != null)
                            ComboMenu["Plugins.Vayne.ComboMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static bool UseQOnlyToProcW
                {
                    get
                    {
                        return ComboMenu?["Plugins.Vayne.ComboMenu.UseQOnlyToProcW"] != null &&
                               ComboMenu["Plugins.Vayne.ComboMenu.UseQOnlyToProcW"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Vayne.ComboMenu.UseQOnlyToProcW"] != null)
                            ComboMenu["Plugins.Vayne.ComboMenu.UseQOnlyToProcW"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseE
                {
                    get
                    {
                        return ComboMenu?["Plugins.Vayne.ComboMenu.UseE"] != null &&
                               ComboMenu["Plugins.Vayne.ComboMenu.UseE"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Vayne.ComboMenu.UseE"] != null)
                            ComboMenu["Plugins.Vayne.ComboMenu.UseE"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseR
                {
                    get
                    {
                        return ComboMenu?["Plugins.Vayne.ComboMenu.UseR"] != null &&
                               ComboMenu["Plugins.Vayne.ComboMenu.UseR"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (ComboMenu?["Plugins.Vayne.ComboMenu.UseR"] != null)
                            ComboMenu["Plugins.Vayne.ComboMenu.UseR"].Cast<CheckBox>()
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
                        return HarassMenu?["Plugins.Vayne.HarassMenu.UseQ"] != null &&
                               HarassMenu["Plugins.Vayne.HarassMenu.UseQ"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Vayne.HarassMenu.UseQ"] != null)
                            HarassMenu["Plugins.Vayne.HarassMenu.UseQ"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int MinManaToUseQ
                {
                    get
                    {
                        if (HarassMenu?["Plugins.Vayne.HarassMenu.MinManaToUseQ"] != null)
                            return HarassMenu["Plugins.Vayne.HarassMenu.MinManaToUseQ"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Vayne.HarassMenu.MinManaToUseQ menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (HarassMenu?["Plugins.Vayne.HarassMenu.MinManaToUseQ"] != null)
                            HarassMenu["Plugins.Vayne.HarassMenu.MinManaToUseQ"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class LaneClear
            {
                public static bool EnableIfNoEnemies
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Vayne.LaneClearMenu.EnableLCIfNoEn"] != null &&
                               LaneClearMenu["Plugins.Vayne.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Vayne.LaneClearMenu.EnableLCIfNoEn"] != null)
                            LaneClearMenu["Plugins.Vayne.LaneClearMenu.EnableLCIfNoEn"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int ScanRange
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Vayne.LaneClearMenu.ScanRange"] != null)
                            return LaneClearMenu["Plugins.Vayne.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Vayne.LaneClearMenu.ScanRange menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Vayne.LaneClearMenu.ScanRange"] != null)
                            LaneClearMenu["Plugins.Vayne.LaneClearMenu.ScanRange"].Cast<Slider>().CurrentValue = value;
                    }
                }
                
                public static int AllowedEnemies
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Vayne.LaneClearMenu.AllowedEnemies"] != null)
                            return
                                LaneClearMenu["Plugins.Vayne.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Vayne.LaneClearMenu.AllowedEnemies menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Vayne.LaneClearMenu.AllowedEnemies"] != null)
                            LaneClearMenu["Plugins.Vayne.LaneClearMenu.AllowedEnemies"].Cast<Slider>().CurrentValue =
                                value;
                    }
                }

                public static bool UseQToLaneClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Vayne.LaneClearMenu.UseQToLaneClear"] != null &&
                               LaneClearMenu["Plugins.Vayne.LaneClearMenu.UseQToLaneClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Vayne.LaneClearMenu.UseQToLaneClear"] != null)
                            LaneClearMenu["Plugins.Vayne.LaneClearMenu.UseQToLaneClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool UseQToJungleClear
                {
                    get
                    {
                        return LaneClearMenu?["Plugins.Vayne.LaneClearMenu.UseQToJungleClear"] != null &&
                               LaneClearMenu["Plugins.Vayne.LaneClearMenu.UseQToJungleClear"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Vayne.LaneClearMenu.UseQToJungleClear"] != null)
                            LaneClearMenu["Plugins.Vayne.LaneClearMenu.UseQToJungleClear"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
                
                public static int MinMana
                {
                    get
                    {
                        if (LaneClearMenu?["Plugins.Vayne.LaneClearMenu.MinMana"] != null)
                            return LaneClearMenu["Plugins.Vayne.LaneClearMenu.MinMana"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Vayne.LaneClearMenu.MinMana menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (LaneClearMenu?["Plugins.Vayne.LaneClearMenu.MinMana"] != null)
                            LaneClearMenu["Plugins.Vayne.LaneClearMenu.MinMana"].Cast<Slider>().CurrentValue = value;
                    }
                }
            }

            internal static class Misc
            {
                public static bool NoAaWhileStealth
                {
                    get
                    {
                        return MiscMenu?["Plugins.Vayne.MiscMenu.NoAAWhileStealth"] != null &&
                               MiscMenu["Plugins.Vayne.MiscMenu.NoAAWhileStealth"].Cast<KeyBind>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.NoAAWhileStealth"] != null)
                            MiscMenu["Plugins.Vayne.MiscMenu.NoAAWhileStealth"].Cast<KeyBind>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int NoAaDelay
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.NoAADelay"] != null)
                            return MiscMenu["Plugins.Vayne.MiscMenu.NoAADelay"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Vayne.MiscMenu.NoAADelay menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.NoAADelay"] != null)
                            MiscMenu["Plugins.Vayne.MiscMenu.NoAADelay"].Cast<Slider>().CurrentValue = value;
                    }
                }

                public static bool EAntiRengar
                {
                    get
                    {
                        return MiscMenu?["Plugins.Vayne.MiscMenu.EAntiRengar"] != null &&
                               MiscMenu["Plugins.Vayne.MiscMenu.EAntiRengar"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.EAntiRengar"] != null)
                            MiscMenu["Plugins.Vayne.MiscMenu.EAntiRengar"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static bool EKs
                {
                    get
                    {
                        return MiscMenu?["Plugins.Vayne.MiscMenu.Eks"] != null &&
                               MiscMenu["Plugins.Vayne.MiscMenu.Eks"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.Eks"] != null)
                            MiscMenu["Plugins.Vayne.MiscMenu.Eks"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }

                public static int PushDistance
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.PushDistance"] != null)
                            return MiscMenu["Plugins.Vayne.MiscMenu.PushDistance"].Cast<Slider>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Vayne.MiscMenu.PushDistance menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.PushDistance"] != null)
                            MiscMenu["Plugins.Vayne.MiscMenu.PushDistance"].Cast<Slider>().CurrentValue = value;
                    }
                }

                /// <summary>
                /// 0 - Always
                /// 1 - Only in combo
                /// </summary>
                public static int EMode
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.EMode"] != null)
                            return MiscMenu["Plugins.Vayne.MiscMenu.EMode"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Vayne.MiscMenu.EMode menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.EMode"] != null)
                            MiscMenu["Plugins.Vayne.MiscMenu.EMode"].Cast<ComboBox>().CurrentValue = value;
                    }
                }

                /// <summary>
                /// 0 - CursorPos
                /// 1 - Auto
                /// </summary>
                public static int QMode
                {
                    get
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.QMode"] != null)
                            return MiscMenu["Plugins.Vayne.MiscMenu.QMode"].Cast<ComboBox>().CurrentValue;

                        Logger.Error("Couldn't get Plugins.Vayne.MiscMenu.QMode menu item value.");
                        return 0;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.QMode"] != null)
                            MiscMenu["Plugins.Vayne.MiscMenu.QMode"].Cast<ComboBox>().CurrentValue = value;
                    }
                }

                public static bool QSafetyChecks
                {
                    get
                    {
                        return MiscMenu?["Plugins.Vayne.MiscMenu.QSafetyChecks"] != null &&
                               MiscMenu["Plugins.Vayne.MiscMenu.QSafetyChecks"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (MiscMenu?["Plugins.Vayne.MiscMenu.QSafetyChecks"] != null)
                            MiscMenu["Plugins.Vayne.MiscMenu.QSafetyChecks"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
            }

            internal static class Drawings
            {
                public static bool DrawInfo
                {
                    get
                    {
                        return DrawingsMenu?["Plugins.Vayne.DrawingsMenu.DrawInfo"] != null &&
                               DrawingsMenu["Plugins.Vayne.DrawingsMenu.DrawInfo"].Cast<CheckBox>()
                                   .CurrentValue;
                    }
                    set
                    {
                        if (DrawingsMenu?["Plugins.Vayne.DrawingsMenu.DrawInfo"] != null)
                            DrawingsMenu["Plugins.Vayne.DrawingsMenu.DrawInfo"].Cast<CheckBox>()
                                .CurrentValue
                                = value;
                    }
                }
            }
        }

        protected static class Damage
        {
            public static float[] QBonusDamage { get; } = { 0, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f };
            public static int[] WMinimumDamage { get; } = {0, 40, 60, 80, 100, 120};
            public static float[] WPercentageDamage { get; } = {0, 0.06f, 0.075f, 0.09f, 0.105f, 0.12f};
            public static int[] EDamage { get; } = {0, 45, 80, 115, 150, 185};

            public static bool IsKillableFrom3SilverStacks(Obj_AI_Base unit)
            {
                return unit.Health <= GetWDamage(unit);
            }

            public static bool IsKillableFromSilverEAndAuto(Obj_AI_Base unit)
            {
                if (!IsECastableOnEnemy(unit))
                    return false;

                var edmg = Player.Instance.CalculateDamageOnUnit(unit, DamageType.Physical,
                    EDamage[E.Level] + Player.Instance.FlatPhysicalDamageMod / 2);

                if (WillEStun(unit))
                    edmg *= 2;

                var aaDamage = Player.Instance.GetAutoAttackDamage(unit);

                var damage = GetWDamage(unit) + edmg + aaDamage;

                return unit.Health <= damage;
            }

            public static float GetWDamage(Obj_AI_Base unit)
            {
                var damage = Math.Max(WMinimumDamage[W.Level], unit.MaxHealth*WPercentageDamage[W.Level]);

                if (damage > 200 && !(unit is AIHeroClient))
                    damage = 200;

                return Player.Instance.CalculateDamageOnUnit(unit, DamageType.True, damage);
            }
        }
    }
}