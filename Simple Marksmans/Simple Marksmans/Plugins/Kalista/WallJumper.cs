using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace Simple_Marksmans.Plugins.Kalista
{
    internal static class WallJumper
    {
        private static readonly Dictionary<Vector3, Vector3> WallJumpSpots = new Dictionary<Vector3, Vector3>
        {
            {
                new Vector3(4674.075f, 5862.176f, 51.39587f), new Vector3(4964.955f, 5410.113f, 50.27698f)
            },
            {
                new Vector3(4776.036f, 5681.148f, 50.2323f), new Vector3(4450.556f, 6258.124f, 51.30017f)
            },
            {
                new Vector3(4205.026f, 6230.98f, 52.04443f), new Vector3(3390.128f, 7107.43f, 51.62903f)
            },
            {
                new Vector3(4058.4f, 6407.077f, 52.46643f), new Vector3(4344.324f, 6137.956f, 52.11548f)
            },
            {
                new Vector3(3648.005f, 6740.299f, 52.45801f), new Vector3(3432.113f, 7171.313f, 51.7063f)
            },
            {
                new Vector3(3361.615f, 7455.041f, 51.89197f), new Vector3(3363.352f, 8117.096f, 51.78662f)
            },
            {
                new Vector3(3321.974f, 7708.548f, 52.18164f), new Vector3(3353.69f, 7095.598f, 51.64148f)
            },
            {
                new Vector3(3017.809f, 6767.353f, 51.46631f), new Vector3(2547.598f, 6734.157f, 55.98999f)
            },
            {
                new Vector3(2799.904f, 6726.203f, 56.13196f), new Vector3(3233.671f, 6737.399f, 51.71729f)//
            },
            {
                new Vector3(3019.342f, 6154.791f, 57.04688f), new Vector3(3353.769f, 6354.227f, 52.30249f)
            },
            {
                new Vector3(3183.354f, 6284.209f, 52.05823f), new Vector3(2811.656f, 6101.958f, 57.04346f)
            },
            {
                new Vector3(5985.822f, 5483.679f, 51.78357f), new Vector3(6224.729f, 5154.694f, 48.52795f)
            },
            {
                new Vector3(6088.231f, 5311.382f, 48.66809f), new Vector3(5644.362f, 5734.164f, 51.55969f)
            },
            {
                new Vector3(5958.93f, 4905.675f, 48.56433f), new Vector3(6064.395f, 4395.197f, 48.854f)
            },
            {
                new Vector3(6003.119f, 4699.23f, 48.53394f), new Vector3(5836.034f, 5264.244f, 51.49707f)//
            },
            {
                new Vector3(2073.425f, 9449.21f, 52.81799f), new Vector3(2324.867f, 9086.609f, 51.77649f)
            },
            {
                new Vector3(2192.551f, 9277.479f, 51.77612f), new Vector3(1784.083f, 9769.506f, 52.83789f)
            },
            {
                new Vector3(2596.218f, 9475.452f, 53.19934f), new Vector3(3001.343f, 9483.234f, 50.86426f)
            },
            {
                new Vector3(2782.156f, 9524.927f, 51.70544f), new Vector3(2372.439f, 9541.388f, 54.12097f)
            },
            {
                new Vector3(3260.017f, 9570.996f, 50.75f), new Vector3(3686.57f, 9676.949f, -67.49133f)
            },
            {
                new Vector3(3830.311f, 9291.035f, -39.20325f), new Vector3(4330.704f, 9366.914f, -65.57581f)
            },
            {
                new Vector3(4049.929f, 9337.727f, -67.83044f), new Vector3(3548.787f, 9153.723f, 42.43506f)
            },
            {
                new Vector3(3463.348f, 9573.761f, -11.46021f), new Vector3(3027.57f, 9511.312f, 50.88171f)//
            },
            {
                new Vector3(4678.605f, 8930.429f, -68.85168f), new Vector3(4657.199f, 8345.509f, 42.93982f)
            },
            {
                new Vector3(4669.956f, 8709.591f, -26.17603f), new Vector3(4670.273f, 9154.957f, -67.51306f)
            },
            {
                new Vector3(4964.398f, 9784.654f, -70.81885f), new Vector3(5107.994f, 10348.38f, -71.24084f)
            },
            {
                new Vector3(4349.712f, 10221.11f, -71.2406f), new Vector3(4543.807f, 10568.52f, -71.24072f)
            },
            {
                new Vector3(4990.49f, 10008.76f, -71.2406f), new Vector3(4911.171f, 9536.953f, -67.45691f)
            },
            {
                new Vector3(4478.381f, 10413.06f, -71.24072f), new Vector3(4133.447f, 10008.29f, -71.24048f)
            },
            {
                new Vector3(5450.241f, 10682.51f, -71.2406f), new Vector3(6056.064f, 10726.44f, 55.04712f)
            },
            {
                new Vector3(5756.578f, 10627.04f, 55.50256f), new Vector3(5091.194f, 10523.42f, -71.2406f)
            },
            {
                new Vector3(4654.404f, 12054.92f, 56.48206f), new Vector3(4866.387f, 12496.41f, 56.47717f)
            },
            {
                new Vector3(4808.343f, 12232.11f, 56.47681f), new Vector3(4536.632f, 11772.7f, 56.84839f)
            },
            {
                new Vector3(6597.816f, 11970.13f, 56.47681f), new Vector3(6654.76f, 11584.67f, 53.84241f)
            },
            {
                new Vector3(5037.089f, 12122.35f, 56.47681f), new Vector3(4845.982f, 11712.43f, 56.83057f)
            },
            {
                new Vector3(4921.988f, 11921.41f, 56.63684f), new Vector3(5131.101f, 12412.9f, 56.42578f)
            },
            {
                new Vector3(6562.792f, 11729.48f, 53.84436f), new Vector3(6524.585f, 12614.06f, 55.2002f)
            },
            {
                new Vector3(8112.829f, 9822.483f, 50.63965f), new Vector3(8879.749f, 9838.395f, 50.32629f)
            },
            {
                new Vector3(8348.689f, 9854.147f, 50.38232f), new Vector3(7733.365f, 9923.805f, 51.47546f)
            },
            {
                new Vector3(8876.396f, 9477.259f, 51.44019f), new Vector3(8683.688f, 9867.419f, 50.38428f)
            },
            {
                new Vector3(8768.142f, 9647.292f, 50.38757f), new Vector3(8950.688f, 9282.373f, 52.81299f)
            },
            {
                new Vector3(7237.933f, 8535.534f, 53.06848f), new Vector3(6911.81f, 8232.869f, -64.66357f)
            },
            {
                new Vector3(7062.687f, 8373.167f, -70.64819f), new Vector3(7469.869f, 8734.628f, 52.87256f)
            },
            {
                new Vector3(6873.494f, 8916.587f, 52.87219f), new Vector3(6505.242f, 8619.341f, -71.24048f)
            },
            {
                new Vector3(6658.066f, 8833.806f, -71.24719f), new Vector3(6997.431f, 9236.629f, 52.93079f)
            },
            {
                new Vector3(6516.605f, 9115.799f, 5.47644f), new Vector3(6472.492f, 8783.131f, -71.24048f)
            },
            {
                new Vector3(6486.219f, 8932.186f, -50.38245f), new Vector3(6593.634f, 9636.98f, 53.25244f)
            },
            {
                new Vector3(10191.71f, 9087.334f, 49.85303f), new Vector3(9929.119f, 9643.738f, 51.92957f)
            },
            {
                new Vector3(10079.68f, 9291.497f, 51.9646f), new Vector3(10357.46f, 8853.28f, 53.68933f)
            },
            {
                new Vector3(10656.18f, 8731.279f, 62.88135f), new Vector3(11305.75f, 7722.244f, 52.21777f)
            },
            {
                new Vector3(10798.26f, 8547.337f, 63.08923f), new Vector3(10297.43f, 9050.915f, 49.49707f)
            },
            {
                new Vector3(11239.86f, 8183.693f, 60.09253f), new Vector3(11422.37f, 7669.299f, 52.21594f)
            },
            {
                new Vector3(11316.68f, 7953.193f, 52.21985f), new Vector3(10576.1f, 8966.911f, 56.98792f)
            },
            {
                new Vector3(11792.18f, 8036.237f, 53.35071f), new Vector3(12238.76f, 8009.057f, 52.45483f)
            },
            {
                new Vector3(12035.42f, 8022.847f, 52.50647f), new Vector3(11344.4f, 8019.567f, 52.20801f)
            },
            {
                new Vector3(11615.39f, 8731.227f, 64.79346f), new Vector3(12007.9f, 9157.172f, 51.31812f)
            },
            {
                new Vector3(11761.23f, 8902.433f, 50.30737f), new Vector3(11371.68f, 8622.363f, 62.18396f)
            },
            {
                new Vector3(11461.58f, 7220.586f, 51.72644f), new Vector3(11435.65f, 7900.254f, 52.22717f)
            },
            {
                new Vector3(11345.33f, 7475.27f, 52.20227f), new Vector3(11332.8f, 6884.023f, 51.71301f)
            },
            {
                new Vector3(10943.87f, 7498.245f, 52.20349f), new Vector3(10985.02f, 6940.704f, 51.7229f)
            },
            {
                new Vector3(10989.51f, 7276.3f, 51.72388f), new Vector3(11001.48f, 7824.464f, 52.20337f)
            },
            {
                new Vector3(12685.75f, 5630.602f, 51.64124f), new Vector3(12987.24f, 5297.553f, 51.72949f)
            },
            {
                new Vector3(12805.22f, 5476.07f, 52.39209f), new Vector3(12423.69f, 5878.115f, 57.12878f)
            },
            {
                new Vector3(12271.95f, 5267.301f, 51.72949f), new Vector3(11785.53f, 5273.65f, 53.09851f)
            },
            {
                new Vector3(12023.61f, 5546.923f, 54.08569f), new Vector3(12576f, 5489.254f, 51.9386f)
            },
            {
                new Vector3(12270.39f, 5542.233f, 52.21411f), new Vector3(11741.91f, 5622.261f, 52.41943f)
            },
            {
                new Vector3(12044.07f, 4595.935f, 51.72961f), new Vector3(11388.58f, 4303.719f, -71.2406f)
            },
            {
                new Vector3(11657.52f, 4744.356f, -71.24072f), new Vector3(12110.17f, 5006.434f, 52.04895f)
            },
            {
                new Vector3(11888.61f, 4827.719f, 51.75354f), new Vector3(11382.68f, 4625.833f, -71.24048f)
            },
            {
                new Vector3(11367.42f, 5515.814f, 9.731201f), new Vector3(11784.17f, 5372.018f, 54.12024f)
            },
            {
                new Vector3(11552.44f, 5440.104f, 54.07751f), new Vector3(11100.15f, 5604.056f, -28.15002f)
            },
            {
                new Vector3(10078.97f, 2709.129f, 49.2229f), new Vector3(9992.279f, 3197.125f, 51.94043f)
            },
            {
                new Vector3(10078.25f, 2985.698f, 50.72534f), new Vector3(10024.98f, 2478.564f, 49.22253f)
            },
            {
                new Vector3(8300.578f, 2927.003f, 51.12988f), new Vector3(8174.804f, 3346.193f, 51.64172f)
            },
            {
                new Vector3(8233.854f, 3175.692f, 51.64331f), new Vector3(8382.541f, 2395.067f, 51.10413f)
            },
            {
                new Vector3(9052.554f, 4364.01f, 52.74133f), new Vector3(9483.579f, 4424.337f, -71.2406f)
            },
            {
                new Vector3(9264.467f, 4418.867f, -71.24072f), new Vector3(8749.148f, 4352.381f, 53.22034f)
            },
            {
                new Vector3(4776.829f, 3261.627f, 50.87463f), new Vector3(4359.231f, 3119.195f, 95.74817f)
            }
        };
        
        public static bool Jumping { get; private set; }
        public static Vector3 JumpingSpot { get; private set; }
        public static Vector3 OrbwalkingSpot { get; private set; }
        public static float StartTime { get; private set; }

        public static void Init()
        {
            Game.OnTick += Game_OnTick;
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (!Jumping || !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                return;
            }

            if (!Kalista.Q.IsReady() || StartTime + 1000 < Game.Time*1000 || !Kalista.Settings.Flee.JumpWithQ)
            {
                Orbwalker.OverrideOrbwalkPosition = () => Game.CursorPos;
                Jumping = false;
                JumpingSpot = Vector3.Zero;
                OrbwalkingSpot = Vector3.Zero;
            }

            if (Player.Instance.ServerPosition.Distance(OrbwalkingSpot) < (OrbwalkingSpot == new Vector3(9253.057f, 4442.405f, -71.24084f) ? 160 : Player.Instance.BoundingRadius) || (Player.Instance.Path.LastOrDefault().Distance(Player.Instance) < 10 && Player.Instance.ServerPosition.Distance(OrbwalkingSpot) < 200))
            {
                Player.ForceIssueOrder(GameObjectOrder.Stop, Player.Instance.ServerPosition, true);
                Kalista.Q.Cast(Player.Instance.Position.Extend(JumpingSpot, 400).To3D());
                Player.ForceIssueOrder(GameObjectOrder.MoveTo, JumpingSpot, true);
                Orbwalker.OverrideOrbwalkPosition = () => Game.CursorPos;

                Jumping = false;
                JumpingSpot = Vector3.Zero;
                OrbwalkingSpot = Vector3.Zero;
            }
        }
        
        public static void DrawSpots() 
        {
            foreach (
                var spot in
                    WallJumpSpots.Where(
                        id =>
                            id.Key.Distance(Player.Instance.Position) < 500 ||
                            id.Value.Distance(Player.Instance.Position) < 500))
            {
                Circle.Draw(Color.LimeGreen, Player.Instance.BoundingRadius, spot.Key);
            }
        }

        public static void TryToJump()
        {
            if (!Kalista.Q.IsReady() || Jumping || !Kalista.Settings.Flee.JumpWithQ)
                return;

            var pos = WallJumpSpots.OrderBy(x => x.Key.Distance(Player.Instance.ServerPosition)).FirstOrDefault();
            var oPos = pos.Value;

            if (Player.Instance.ServerPosition.Distance(pos.Key) < (pos.Key == new Vector3(9264.467f, 4418.867f, -71.24072f) ? 150 : 75))
            {
                Orbwalker.OverrideOrbwalkPosition = () => pos.Key == new Vector3(9264.467f, 4418.867f, -71.24072f) ? new Vector3(9247.006f, 4413.333f, -54.87134f) : pos.Key;
                OrbwalkingSpot = pos.Key;
                JumpingSpot = oPos;
                Jumping = true;
                StartTime = Game.Time * 1000;
            }
        }
    }
}