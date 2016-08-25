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
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Simple_Marksmans.Utils;
using Simple_Marksmans.Utils.PermaShow;
using Color = System.Drawing.Color;
using ColorPicker = Simple_Marksmans.Utils.ColorPicker;

namespace Simple_Marksmans.Plugins.Ashe
{
    internal class Ashe : ChampionPlugin
    {
        public static Spell.Active Q;
        public static Spell.Skillshot W, E, R;
        public static Menu DrawingsMenu, MiscMenu;
        public static PermaShow PermaShow;
        public static BoolItemData AutoHarassPermaShowItem;
        private static readonly ColorPicker ColorPicker;

        public static bool DrawQ
        {
            get {
                return DrawingsMenu?["Plugins.Ashe.DrawingsMenu.DrawW"] != null && DrawingsMenu["Plugins.Ashe.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue;
            }
            set
            {
                if (DrawingsMenu?["Plugins.Ashe.DrawingsMenu.DrawW"] != null)
                    DrawingsMenu["Plugins.Ashe.DrawingsMenu.DrawW"].Cast<CheckBox>().CurrentValue = value;
            }
        }

        public static bool InterrupterEnabled
        {
            get
            {
                return MiscMenu?["Plugins.Ashe.MiscMenu.EnableInterrupter"] != null && MiscMenu["Plugins.Ashe.MiscMenu.EnableInterrupter"].Cast<CheckBox>().CurrentValue;
            }
            set
            {
                if (MiscMenu?["Plugins.Ashe.MiscMenu.EnableInterrupter"] != null)
                    MiscMenu["Plugins.Ashe.MiscMenu.EnableInterrupter"].Cast<CheckBox>().CurrentValue = value;
            }
        }

        public static bool AntiGapcloserEnabled
        {
            get
            {
                return MiscMenu?["Plugins.Ashe.MiscMenu.EnableAntiGapcloser"] != null && MiscMenu["Plugins.Ashe.MiscMenu.EnableAntiGapcloser"].Cast<CheckBox>().CurrentValue;
            }
            set
            {
                if (MiscMenu?["Plugins.Ashe.MiscMenu.EnableAntiGapcloser"] != null)
                    MiscMenu["Plugins.Ashe.MiscMenu.EnableAntiGapcloser"].Cast<CheckBox>().CurrentValue = value;
            }
        }

        public static int MaxInterrupterRange
        {
            get {
                return MiscMenu?["Plugins.Ashe.MiscMenu.MaxInterrupterRange"]?.Cast<Slider>().CurrentValue ?? 0;
            }
            set
            {
                if (MiscMenu?["Plugins.Ashe.MiscMenu.MaxInterrupterRange"] != null)
                    MiscMenu["Plugins.Ashe.MiscMenu.MaxInterrupterRange"].Cast<Slider>().CurrentValue = value;
            }
        }

        static Ashe()
        {
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 1225, SkillShotType.Cone, 250, 2000, 20);
            E = new Spell.Skillshot(SpellSlot.E, uint.MaxValue, SkillShotType.Linear);
            R = new Spell.Skillshot(SpellSlot.R, uint.MaxValue, SkillShotType.Linear, 250, 1600, 120);
            
            ColorPicker = new ColorPicker("AsheW", new ColorBGRA(1, 109, 160, 255));

            PermaShow = new PermaShow("Ashe Permashow", new Vector2(200, 200));
            AutoHarassPermaShowItem = PermaShow.AddItem("Auto Harass", new BoolItemData("Auto Harass", true, 14));
        }

        protected override void OnDraw()
        {
            if(DrawQ)
                Circle.Draw(ColorPicker.Color, W.Range, Player.Instance);
        }
        
        protected override void OnInterruptible(AIHeroClient sender, InterrupterEventArgs args)
        {
            if (InterrupterEnabled && R.IsReady() && Player.Instance.Mana > 200 &&
                sender.IsValidTarget(MaxInterrupterRange))
            {
                R.Cast(sender);
            }
        }

        protected override void OnGapcloser(AIHeroClient sender, GapCloserEventArgs args)
        {
            if (AntiGapcloserEnabled && R.IsReady() && args.End.DistanceSquared(Player.Instance.Position) < 400)
            {
                R.Cast(sender);
            }
        }

        protected override void CreateMenu()
        {
            DrawingsMenu = MenuManager.Menu.AddSubMenu("Drawings");
            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawW", new CheckBox("Draw W range"));
            DrawingsMenu.Add("Plugins.Ashe.DrawingsMenu.DrawWColor",
                new CheckBox("Change Color", false)).OnValueChange += (a, b) => ColorPicker.Initialize(Color.Aquamarine);

            MiscMenu = MenuManager.Menu.AddSubMenu("Misc");

            MiscMenu.Add("Plugins.Ashe.MiscMenu.EnableInterrupter", new CheckBox("Enable Interrupter"));
            MiscMenu.Add("Plugins.Ashe.MiscMenu.EnableAntiGapcloser", new CheckBox("Enable Anti-Gapcloser"));
            MiscMenu.Add("Plugins.Ashe.MiscMenu.MaxInterrupterRange",
                new Slider("Max range to cast R against interruptable spell", 1500, 0, 2500));

            MiscMenu.Add("Plugins.Ashe.MiscMenu.EnableAutoHarass", new CheckBox("Enable AutoHarass")).OnValueChange +=
                (sender, args) => AutoHarassPermaShowItem.Value = args.NewValue;

            MenuManager.BuildAntiGapcloserMenu();
            MenuManager.BuildInterrupterMenu();
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