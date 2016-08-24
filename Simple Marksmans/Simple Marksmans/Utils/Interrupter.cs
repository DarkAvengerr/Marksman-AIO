using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK.Enumerations;
using SharpDX;

namespace Simple_Marksmans.Utils
{
    internal class Interrupter
    {
        public static readonly List<InterrupterData> InterruptibleList = new List<InterrupterData>
        {
            new InterrupterData {ChampionName = "Caitlyn", SpellName = "Ace in the Hole", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "FiddleSticks", SpellName = "Drain", DangerLevel = DangerLevel.Low, SpellSlot = SpellSlot.W},
            new InterrupterData {ChampionName = "FiddleSticks", SpellName = "Crowstorm", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Galio", SpellName = "Idol of Durand", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Janna", SpellName = "Monsoon", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Jhin", SpellName = "Curtain Call", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Karthus", SpellName = "Requiem", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Katarina", SpellName = "Death Lotus", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Lucian", SpellName = "The Culling", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Malzahar", SpellName = "Nether Grasp", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "MasterYi", SpellName = "Meditate", DangerLevel = DangerLevel.Low, SpellSlot = SpellSlot.W},
            new InterrupterData {ChampionName = "MissFortune", SpellName = "Bullet Time", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Nunu", SpellName = "Absolute Zero", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Pantheon", SpellName = "Heartseeker Strike", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.E},
            new InterrupterData {ChampionName = "Pantheon", SpellName = "Grand Skyfall", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Quinn", SpellName = "Behind Enemy Lines", DangerLevel = DangerLevel.Low, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "RekSai", SpellName = "Void Rush", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Sion", SpellName = "Decimating Smash", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.Q},
            new InterrupterData {ChampionName = "Shen", SpellName = "Stand United", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "TahmKench", SpellName = "Abyssal Voyage", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "TwistedFate", SpellName = "Destiny", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Urgot", SpellName = "Hyper-Kinetic Position Reverser", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Velkoz", SpellName = "Lifeform Disintegration Ray", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Warwick", SpellName = "Infinite Duress", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Xerath", SpellName = "Arcanopulse", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.Q},
            new InterrupterData {ChampionName = "Xerath", SpellName = "Rite of the Arcane", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.R},
            new InterrupterData {ChampionName = "Varus", SpellName = "Piercing Arrow", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.Q},
            new InterrupterData {ChampionName = "Vi", SpellName = "Vault Breaker", DangerLevel = DangerLevel.High, SpellSlot = SpellSlot.Q},
            new InterrupterData {ChampionName = "Vladimir", SpellName = "Tides of Blood", DangerLevel = DangerLevel.Medium, SpellSlot = SpellSlot.E}
        };
    }

    internal class InterrupterData
    {
        public string ChampionName { get; set; }
        public string SpellName { get; set; }
        public DangerLevel DangerLevel { get; set; }
        public SpellSlot SpellSlot { get; set; }
    }

    public class InterrupterEventArgs : EventArgs
    {
        public GameObject Target { get; private set; }
        public Vector3 Start { get; private set; }
        public Vector3 End { get; private set; }
        public SpellSlot SpellSlot { get; private set; }
        public DangerLevel DangerLevel { get; private set; }
        public string SpellName { get; private set; }
        public float GameTime { get; private set; }
        public int Delay { get; private set; }
        public int Enemies { get; private set; }
        public int HealthPercent { get; private set; }

        public InterrupterEventArgs(GameObject target, SpellSlot spellSlot, DangerLevel dangerLevel, string spellName, Vector3 start, Vector3 end, int delay, int enemies, int healthPercent, float gameTime)
        {
            Target = target;
            SpellSlot = spellSlot;
            DangerLevel = dangerLevel;
            SpellName = spellName;
            Start = start;
            End = end;
            Delay = delay;
            Enemies = enemies;
            HealthPercent = healthPercent;
            GameTime = gameTime;
        }
    }
}