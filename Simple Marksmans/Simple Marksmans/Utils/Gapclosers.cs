using System;
using EloBuddy;
using SharpDX;

namespace Simple_Marksmans.Utils
{
    public class GapCloserEventArgs : EventArgs
    {
        public GameObject Target { get; private set; }
        public Vector3 Start { get; private set; }
        public Vector3 End { get; private set; }
        public SpellSlot SpellSlot { get; private set; }
        public GapcloserTypes GapcloserType { get; private set; }
        public float GameTime { get; private set; }
        public int Delay { get; private set; }
        public int Enemies { get; private set; }
        public int HealthPercent { get; private set; }

        public GapCloserEventArgs(GameObject target, SpellSlot spellSlot, GapcloserTypes gapcloserType, Vector3 start, Vector3 end, int delay, int enemies, int healthPercent, float gameTime)
        {
            Target = target;
            SpellSlot = spellSlot;
            GapcloserType = gapcloserType;
            Start = start;
            End = end;
            Delay = delay;
            Enemies = enemies;
            HealthPercent = healthPercent;
            GameTime = gameTime;
        }
    }
}