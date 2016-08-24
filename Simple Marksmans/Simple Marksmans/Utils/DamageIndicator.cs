using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace Simple_Marksmans.Utils
{
    internal static class DamageIndicator
    {
        public static Color Color { get; set; } = Color.Lime;
        public static Color JungleColor { get; set; } = Color.White;
        public static int DrawingRange { get; set; }
        public static bool IncludeJungleMobs { get; set; }

        internal delegate float DamageDelegateH(Obj_AI_Base unit);
        public static DamageDelegateH DamageDelegate { get; set; }

        public static void Initalize(Color color, int drawingRange = 1200)
        {
            Color = color;
            DrawingRange = drawingRange;

            Drawing.OnEndScene += DrawingOnEndScene;
        }

        public static void Initalize(Color color, bool includeJungleMobs, Color jungleColor, int drawingRange = 1200)
        {
            Color = color;
            IncludeJungleMobs = includeJungleMobs;
            DrawingRange = drawingRange;
            JungleColor = jungleColor;

            Drawing.OnEndScene += DrawingOnEndScene;
        }

        private static void DrawingOnEndScene(EventArgs args)
        {
            if (DamageDelegate == null)
                return;

            if (IncludeJungleMobs)
            {
                foreach (
                    var unit in
                        EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, DrawingRange)
                            .Where(x => x.IsValidTarget()
                                        && x.IsHPBarRendered && Drawing.WorldToScreen(x.Position).IsOnScreen()))
                {
                    if (DamageDelegate(unit) <= 0)
                        return;

                    int height;
                    int width;

                    int xOffset;
                    int yOffset;

                    if ((unit.Name.Contains("Blue") || unit.Name.Contains("Red")) && !unit.Name.Contains("Mini"))
                    {
                        height = 9;
                        width = 142;
                        xOffset = -4;
                        yOffset = 7;
                    }
                    else if (unit.Name.Contains("Dragon"))
                    {
                        height = 10;
                        width = 143;
                        xOffset = -4;
                        yOffset = 8;
                    }
                    else if (unit.Name.Contains("Baron"))
                    {
                        height = 12;
                        width = 191;
                        xOffset = -29;
                        yOffset = 6;
                    }
                    else if (unit.Name.Contains("Herald"))
                    {
                        height = 10;
                        width = 142;
                        xOffset = -4;
                        yOffset = 7;
                    }
                    else if ((unit.Name.Contains("Razorbeak") || unit.Name.Contains("Murkwolf")) &&
                             !unit.Name.Contains("Mini"))
                    {
                        width = 74;
                        height = 3;
                        xOffset = 30;
                        yOffset = 7;
                    }
                    else if (unit.Name.Contains("Krug") && !unit.Name.Contains("Mini"))
                    {
                        width = 80;
                        height = 3;
                        xOffset = 27;
                        yOffset = 7;
                    }
                    else if (unit.Name.Contains("Gromp"))
                    {
                        width = 86;
                        height = 3;
                        xOffset = 24;
                        yOffset = 6;
                    }
                    else if (unit.Name.Contains("Crab"))
                    {
                        width = 61;
                        height = 2;
                        xOffset = 36;
                        yOffset = 21;
                    }
                    else if (unit.Name.Contains("RedMini") || unit.Name.Contains("BlueMini") ||
                             unit.Name.Contains("RazorbeakMini"))
                    {
                        height = 2;
                        width = 49;
                        xOffset = 42;
                        yOffset = 6;
                    }
                    else if (unit.Name.Contains("KrugMini") || unit.Name.Contains("MurkwolfMini"))
                    {
                        height = 2;
                        width = 55;
                        xOffset = 39;
                        yOffset = 6;
                    }
                    else
                    {
                        continue;
                    }

                    var damageAfter = Math.Max(0, unit.Health - DamageDelegate(unit)) / unit.MaxHealth;
                    var currentHealth = unit.Health / unit.MaxHealth;

                    var start = unit.HPBarPosition.X + xOffset + (damageAfter * width);
                    var end = unit.HPBarPosition.X + xOffset + (currentHealth * width);

                    Drawing.DrawLine(start, unit.HPBarPosition.Y + yOffset, end, unit.HPBarPosition.Y + yOffset, height, JungleColor);
                }
            }


            foreach (var unit in
                EntityManager.Heroes.Enemies.Where(
                    index => index.IsHPBarRendered && index.IsValidTarget(DrawingRange) && index.Position.IsOnScreen()))
            {
                if (DamageDelegate(unit) <= 0)
                    return;

                const int height = 9;
                const int width = 104;

                var xOffset = 2;
                var yOffset = 9;

                switch (unit.Hero)
                {
                    case Champion.Annie:
                        xOffset = -9;
                        yOffset = -3;
                        break;
                    case Champion.Jhin:
                        xOffset = -9;
                        yOffset = -5;
                        break;
                }

                var damageAfter = Math.Max(0, unit.Health - DamageDelegate(unit)) / unit.MaxHealth;
                var currentHealth = unit.Health / unit.MaxHealth;

                var start = new Vector2(unit.HPBarPosition.X + xOffset + damageAfter * width, unit.HPBarPosition.Y + yOffset);
                var end = new Vector2(unit.HPBarPosition.X + currentHealth * width, unit.HPBarPosition.Y + yOffset);

                Line.DrawLine(Color, height, start, end);
            }
        }
    }
}