#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="ColorPicker.cs" company="EloBuddy">
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
using SharpDX;

namespace Simple_Marksmans.Utils
{
    internal static class ChampionTracker
    {
        private static float _lastTick;

        private static readonly HashSet<LongCastSpellData> LongCastSpells = new HashSet<LongCastSpellData> 
        {
            new LongCastSpellData(Champion.Blitzcrank, SpellSlot.Q, "RocketGrab"),
            new LongCastSpellData(Champion.Braum, SpellSlot.R, "BraumRWrapper"),
            new LongCastSpellData(Champion.Caitlyn, SpellSlot.R, "CaitlynAceintheHole"),
            new LongCastSpellData(Champion.Caitlyn, SpellSlot.Q, "CaitlynPiltoverPeacemaker"),
            new LongCastSpellData(Champion.Chogath, SpellSlot.Q, "Rupture"),
            new LongCastSpellData(Champion.Ezreal, SpellSlot.R, "EzrealTrueshotBarrage"),
            new LongCastSpellData(Champion.Jhin, SpellSlot.W, "JhinW"),
            new LongCastSpellData(Champion.Jinx, SpellSlot.W, "JinxW"),
            new LongCastSpellData(Champion.Katarina, SpellSlot.R),
            new LongCastSpellData(Champion.Lux, SpellSlot.R, "LuxMaliceCannon"),
            new LongCastSpellData(Champion.Malzahar, SpellSlot.R),
            new LongCastSpellData(Champion.MissFortune, SpellSlot.R),
            new LongCastSpellData(Champion.Nunu, SpellSlot.R),
            new LongCastSpellData(Champion.Shen, SpellSlot.R),
            new LongCastSpellData(Champion.Thresh, SpellSlot.Q, "ThreshQ"),
            new LongCastSpellData(Champion.Urgot, SpellSlot.R),
            new LongCastSpellData(Champion.Velkoz, SpellSlot.R),
            new LongCastSpellData(Champion.Warwick, SpellSlot.R),
            new LongCastSpellData(Champion.Xerath, SpellSlot.R)
        };
        
        public static ChampionTrackerFlags Flags { get; private set; }

        public static void Initialize(ChampionTrackerFlags flags)
        {
            Flags = flags;

            if (Flags.HasFlag(ChampionTrackerFlags.VisibilityTracker))
            {
                foreach (var aiHeroClient in EntityManager.Heroes.Enemies)
                {
                    ChampionVisibility.Add(new VisibilityTracker { Hero = aiHeroClient });
                }
                Game.OnTick += Game_OnTick;
            }
            if (Flags.HasFlag(ChampionTrackerFlags.LongCastTimeTracker))
            {
                Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!Flags.HasFlag(ChampionTrackerFlags.LongCastTimeTracker) || sender.IsMe)
                return;

            var hero = sender as AIHeroClient;

            if (hero == null || sender.IsAlly)
                return;

            if (hero.Hero == Champion.Pantheon && args.SData.Name == "PantheonRJump")
            {
                OnLongSpellCast?.Invoke(null, new OnLongSpellCastEventArgs(hero, args.Start, args.End, args.SData, args.Slot));
            } else if (LongCastSpells.Any(x => x.Hero == hero.Hero && args.Slot == x.SpellSlot))
            {
                OnLongSpellCast?.Invoke(null, new OnLongSpellCastEventArgs(hero, args.Start, args.End, args.SData, args.Slot));
            } else if (args.SData.Name == "SummonerTeleport")
            {
                OnLongSpellCast?.Invoke(null, new OnLongSpellCastEventArgs(hero, args.Start, args.End, args.SData, args.Slot, true));
            }
        }

        public static event EventHandler<OnLongSpellCastEventArgs> OnLongSpellCast;
        
        public static event EventHandler<OnLoseVisibilityEventArgs> OnLoseVisibility;

        private static readonly HashSet<VisibilityTracker> ChampionVisibility = new HashSet<VisibilityTracker>();

        private static void Game_OnTick(EventArgs args)
        {
            if (!Flags.HasFlag(ChampionTrackerFlags.VisibilityTracker))
                return;

            if (Game.Time * 1000 - _lastTick < 25)
                return;

            foreach (var visibilityTracker in ChampionVisibility.Where(x=> Game.Time * 1000 - x.LastVisibleGameTime * 1000 < 1000))
            {
                foreach (var unit in EntityManager.Heroes.Enemies.Where(
                        x => x.Hero == visibilityTracker.Hero.Hero && !visibilityTracker.Hero.IsDead && !visibilityTracker.Hero.IsHPBarRendered))
                {
                    OnLoseVisibility?.Invoke(null, new OnLoseVisibilityEventArgs(unit, visibilityTracker.LastVisibleGameTime, visibilityTracker.LastPosition));
                }
            }

            foreach (var aiHeroClient in EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsHPBarRendered))
            {
                var hero = ChampionVisibility.FirstOrDefault(x => x.Hero.NetworkId == aiHeroClient.NetworkId);

                if (hero != null)
                {
                    hero.LastPosition = aiHeroClient.Position;
                    hero.LastVisibleGameTime = Game.Time;
                    hero.LastPath = aiHeroClient.Path.Last();
                    hero.LastHealth = aiHeroClient.Health;
                    hero.LastHealthPercent = aiHeroClient.HealthPercent;
                }
            }
            _lastTick = Game.Time*1000;
        }

        public static VisibilityTracker GetVisibilityTrackerData(this AIHeroClient unit)
        {
            if (!Flags.HasFlag(ChampionTrackerFlags.VisibilityTracker))
                return null;

            var  hero = ChampionVisibility.FirstOrDefault(u => u.Hero.NetworkId == unit.NetworkId);

            return hero;
        }

        public static bool IsUserInvisibleFor(this AIHeroClient unit, float time)
        {
            if (!Flags.HasFlag(ChampionTrackerFlags.VisibilityTracker))
                return false;

            var hero = ChampionVisibility.FirstOrDefault(x => Game.Time*1000 - x.LastVisibleGameTime*1000 > time && Game.Time * 1000 - x.LastVisibleGameTime * 1000 < 4000 && x.Hero.NetworkId == unit.NetworkId);

            return hero != null && EntityManager.Heroes.Enemies.Any(x => x.NetworkId == hero.Hero.NetworkId && !x.IsDead && !x.IsHPBarRendered);
        }

        public class VisibilityTracker
        {
            public AIHeroClient Hero { get; set; }
            public float LastVisibleGameTime { get; set; }
            public float LastHealth { get; set; }
            public float LastHealthPercent { get; set; }
            public Vector3 LastPosition { get; set; }
            public Vector3 LastPath { get; set; }
        }
    }

    public class OnLoseVisibilityEventArgs : EventArgs
    {
        public AIHeroClient Hero { get; private set; }
        public float LastVisibleGameTime { get; private set; }
        public Vector3 LastPosition { get; private set; }

        public OnLoseVisibilityEventArgs(AIHeroClient hero, float lastVisibleGameTime, Vector3 lastPosition)
        {
            Hero = hero;
            LastVisibleGameTime = lastVisibleGameTime;
            LastPosition = lastPosition;
        }
    }

    public class OnLongSpellCastEventArgs : EventArgs
    {
        public AIHeroClient Sender { get; private set; }
        public Vector3 StartPosition { get; private set; }
        public Vector3 EndPosition { get; private set; }
        public SpellData SData { get; private set; }
        public SpellSlot SpellSlot { get; private set; }
        public bool IsTeleport { get; private set; }

        public OnLongSpellCastEventArgs(AIHeroClient sender, Vector3 startPosition, Vector3 endPosition, SpellData sData, SpellSlot spellSlot, bool isTeleport = false)
        {
            Sender = sender;
            StartPosition = startPosition;
            EndPosition = endPosition;
            SData = sData;
            SpellSlot = spellSlot;
            IsTeleport = isTeleport;
        }
    }

    public class LongCastSpellData
    {
        public Champion Hero { get; }
        public SpellSlot SpellSlot { get; }
        public string SpellName { get; private set; }

        public LongCastSpellData(Champion hero, SpellSlot slot, string spellName = "")
        {
            Hero = hero;
            SpellSlot = slot;
            SpellName = spellName;
        }
    }
}