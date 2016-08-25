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
using SharpDX;
using Simple_Marksmans.Utils;
using Color = System.Drawing.Color;
using ColorPicker = Simple_Marksmans.Utils.ColorPicker;

namespace Simple_Marksmans.Plugins.Draven
{
    internal class Draven : ChampionPlugin
    {
        public static Spell.Active Q, W;
        public static Spell.Skillshot E, R;
        public static Menu DrawingsMenu, MiscMenu, AxeSettingsMenu;
        private static readonly List<AxeObjectData> AxeObjects = new List<AxeObjectData>();
        private static readonly Text Text;
        private static readonly ColorPicker[] ColorPicker;

        public static bool InterrupterEnabled
        {
            get
            {
                return MiscMenu?["Plugins.Draven.MiscMenu.EnableInterrupter"] != null && MiscMenu["Plugins.Draven.MiscMenu.EnableInterrupter"].Cast<CheckBox>().CurrentValue;
            }
            set
            {
                if (MiscMenu?["Plugins.Draven.MiscMenu.EnableInterrupter"] != null)
                    MiscMenu["Plugins.Draven.MiscMenu.EnableInterrupter"].Cast<CheckBox>().CurrentValue = value;
            }
        }

        public static bool AntiGapcloserEnabled
        {
            get
            {
                return MiscMenu?["Plugins.Draven.MiscMenu.EnableAntiGapcloser"] != null && MiscMenu["Plugins.Draven.MiscMenu.EnableAntiGapcloser"].Cast<CheckBox>().CurrentValue;
            }
            set
            {
                if (MiscMenu?["Plugins.Draven.MiscMenu.EnableAntiGapcloser"] != null)
                    MiscMenu["Plugins.Draven.MiscMenu.EnableAntiGapcloser"].Cast<CheckBox>().CurrentValue = value;
            }
        }

        public static int AxeCatchRange
        {
            get
            {
                return AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.AxeCatchRange"]?.Cast<Slider>().CurrentValue ?? 0;
            }
            set
            {
                if (AxeSettingsMenu?["Plugins.Draven.AxeSettingsMenu.AxeCatchRange"] != null)
                    AxeSettingsMenu["Plugins.Draven.AxeSettingsMenu.AxeCatchRange"].Cast<Slider>().CurrentValue = value;
            }
        }

        public static bool DrawE
        {
            get
            {
                return DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawE"] != null && DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue;
            }
            set
            {
                if (DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawE"] != null)
                    DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawE"].Cast<CheckBox>().CurrentValue = value;
            }
        }

        public static bool DrawAxes
        {
            get
            {
                return DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxes"] != null && DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxes"].Cast<CheckBox>().CurrentValue;
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
                return DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxesTimer"] != null && DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxesTimer"].Cast<CheckBox>().CurrentValue;
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
                return DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxesCatchRange"] != null && DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxesCatchRange"].Cast<CheckBox>().CurrentValue;
            }
            set
            {
                if (DrawingsMenu?["Plugins.Draven.DrawingsMenu.DrawAxesCatchRange"] != null)
                    DrawingsMenu["Plugins.Draven.DrawingsMenu.DrawAxesCatchRange"].Cast<CheckBox>().CurrentValue = value;
            }
        }

        static Draven()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 950, SkillShotType.Linear, 250, 1300, 100)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Skillshot(SpellSlot.R, uint.MaxValue, SkillShotType.Linear, 500, 1900, 160)
            {
                AllowedCollisionCount = int.MaxValue
            };

            ColorPicker = new ColorPicker[3];
            ColorPicker[0] = new ColorPicker("DravenE", new ColorBGRA(114, 171, 160, 255));
            ColorPicker[1] = new ColorPicker("DravenAxeTimer", new ColorBGRA(255, 255, 255, 255));
            ColorPicker[2] = new ColorPicker("DravenCatchRange", new ColorBGRA(231, 237, 160, 255));

            Text = new Text("", new Font("calibri", 15, FontStyle.Regular));

            Game.OnTick += Game_OnTick;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        private static void Game_OnTick(EventArgs args)
        {
            Orbwalker.OverrideOrbwalkPosition += () => Game.CursorPos;

            foreach (var axeObjectData in AxeObjects.Where(x => Game.CursorPos.Distance(x.EndPosition) < AxeCatchRange).OrderBy(x=>x.EndPosition.Distance(Player.Instance)))
            {
                if (Player.Instance.ServerPosition.Distance(axeObjectData.EndPosition) < 90)
                    return;

                if (new Geometry.Polygon.Circle(axeObjectData.EndPosition, 90).IsInside(Player.Instance.ServerPosition) &&
                    !CanPlayerLeaveAxeRangeInDesiredTime(axeObjectData.EndPosition, (axeObjectData.EndTick - Game.Time * 1000) / 1000))
                {
                    return;
                }

                Orbwalker.OverrideOrbwalkPosition += () => axeObjectData.EndPosition;
            }/*

            var closestAxeQ = from x in AxeObjects
                where Game.CursorPos.Distance(x.EndPosition) < AxeCatchRange
                where CanPlayerLeaveAxeRangeInDesiredTime(x.EndPosition, (x.EndTick - Game.Time*1000)/1000)
                orderby x.EndPosition.Distance(Player.Instance.Position)
                select x;

            var closestAxe = closestAxeQ.FirstOrDefault();
            
            if (closestAxe == null || Player.Instance.ServerPosition.Distance(closestAxe.EndPosition) < 90)
                return;

            if (new Geometry.Polygon.Circle(closestAxe.EndPosition, 90).IsInside(Player.Instance.ServerPosition) &&
                !CanPlayerLeaveAxeRangeInDesiredTime(closestAxe.EndPosition, (closestAxe.EndTick - Game.Time*1000)/1000))
            {
                return;
            }

            Orbwalker.OverrideOrbwalkPosition += () => closestAxe.EndPosition;*/
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

            if(!sender.Name.Contains("missile") && !sender.Name.Contains("SRU") && !sender.Name.Contains("minion") && !sender.Name.Contains("turret") && !sender.Name.Contains("tower"))
                Console.WriteLine(sender.Name);

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
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            if (sender.Name.Contains("Q_reticle_self"))
            {
                AxeObjects.Remove(AxeObjects.Find(data => data.NetworkId == sender.NetworkId));
            }
        }

        protected override void OnDraw()
        {
            if(DrawE && E.IsReady())
                Circle.Draw(ColorPicker[0].Color, E.Range, Player.Instance.Position);

            foreach (var axeObjectData in AxeObjects)
            {
                var percentage = 100 * Math.Max(0, (axeObjectData.EndTick - Game.Time * 1000) / 1000) / 1.2f;
                var g = Math.Min(255, 255f / 100f * percentage);
                var r = Math.Min(255, 255 - g);
                
                if (DrawAxes)
                {
                    Circle.Draw(CanPlayerLeaveAxeRangeInDesiredTime(axeObjectData.EndPosition, (axeObjectData.EndTick - Game.Time * 1000) / 1000)
                            ? new ColorBGRA(255, 0, 0, 255)
                            : new ColorBGRA(0,255, 0, 255), 90, axeObjectData.EndPosition);
                        //new ColorBGRA((byte)r, (byte)g, 0, 255), 90, axeObjectData.EndPosition);
                }

                CanPlayerLeaveAxeRangeInDesiredTime(axeObjectData.EndPosition, 0.5f);

                if (!DrawAxesTimer)
                    continue;

                Text.Color = Color.FromArgb(ColorPicker[1].Color.R, ColorPicker[1].Color.G, ColorPicker[1].Color.B);
                Text.X = (int) Drawing.WorldToScreen(axeObjectData.EndPosition).X;
                Text.Y = (int) Drawing.WorldToScreen(axeObjectData.EndPosition).Y + 50;
                Text.TextValue = ((axeObjectData.EndTick - Game.Time*1000)/1000).ToString("F1") + " s";
                Text.Draw();
            }

            if (DrawAxesCatchRange)
                Circle.Draw(ColorPicker[2].Color, AxeCatchRange, Game.CursorPos);
        }

        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (InterrupterEnabled && E.IsReady() && sender.IsValidTarget(E.Range))
            {
                E.Cast(sender);
            }
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (AntiGapcloserEnabled && E.IsReady() && sender.IsValidTarget(E.Range))
            {
                E.Cast(sender);
            }
        }

        protected override void CreateMenu()
        {
            AxeSettingsMenu = MenuManager.Menu.AddSubMenu("Axe Settings");
            AxeSettingsMenu.AddGroupLabel("Axe settings for Draven addon");
            AxeSettingsMenu.AddLabel("Basic settings :");
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxes", new CheckBox("Catch Axes"));
            AxeSettingsMenu.AddSeparator(5);

            AxeSettingsMenu.AddLabel("Catching settings :");
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesWhen", new ComboBox("When should I catch them", 1, "Always", "In Combo"));
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesMode", new ComboBox("Catch mode", 0, "Default", "Brutal"));
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.AxeCatchRange", new Slider("Axe Catch Range", 450, 200, 1000));
            AxeSettingsMenu.AddSeparator(5);

            AxeSettingsMenu.AddLabel("Additional settings :");
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesUnderTower", new CheckBox("Catch Axes that are under enemy tower", false));
            AxeSettingsMenu.Add("Plugins.Draven.AxeSettingsMenu.CatchAxesNearEnemies", new CheckBox("Catch Axes that are near enemies", false));

            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.AddGroupLabel("Drawings settings for Draven addon");
            DrawingsMenu.AddLabel("Stand Aside (E) drawing settings :");
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawE", new CheckBox("Draw E range"));
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawEColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) => ColorPicker[0].Initialize(Color.Aquamarine);
            DrawingsMenu.AddSeparator(5);

            DrawingsMenu.AddLabel("Spinning Axe (Q) drawing settings :");
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxes", new CheckBox("Draw Axes"));
            DrawingsMenu.AddSeparator(1);
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesTimer", new CheckBox("Draw Axes timer"));
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesTimerColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) => ColorPicker[1].Initialize(Color.Aquamarine);
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesCatchRange", new CheckBox("Draw Axe's catch range"));
            DrawingsMenu.Add("Plugins.Draven.DrawingsMenu.DrawAxesCatchRangeColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) => ColorPicker[2].Initialize(Color.Aquamarine);

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Misc settings for Draven addon");
            MiscMenu.Add("Plugins.Draven.MiscMenu.EnableInterrupter", new CheckBox("Enable Interrupter"));
            MiscMenu.Add("Plugins.Draven.MiscMenu.EnableAntiGapcloser", new CheckBox("Enable Anti-Gapcloser"));

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
    }
}