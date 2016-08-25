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

        static ChampionTracker()
        {
            foreach (var aiHeroClient in EntityManager.Heroes.AllHeroes)
            {
                ChampionVisibility.Add(new VisibilityTracker {Hero = aiHeroClient});
            }
            Game.OnTick += Game_OnTick;
        }

        public static event EventHandler<OnLoseVisibilityEventArgs> OnLoseVisibility;

        private static readonly HashSet<VisibilityTracker> ChampionVisibility = new HashSet<VisibilityTracker>();

        private static void Game_OnTick(EventArgs args)
        {
            if(Game.Time * 1000 - _lastTick < 100)
                return;

            foreach (var visibilityTracker in ChampionVisibility.Where(x=> Game.Time * 1000 - x.LastVisibleGameTime * 1000 < 1000))
            {
                foreach (var unit in EntityManager.Heroes.AllHeroes.Where(
                        x => x.Hero == visibilityTracker.Hero.Hero && !visibilityTracker.Hero.IsDead && !visibilityTracker.Hero.IsHPBarRendered))
                {
                    OnLoseVisibility?.Invoke(null, new OnLoseVisibilityEventArgs(unit, visibilityTracker.LastVisibleGameTime, visibilityTracker.LastPosition));
                }
            }

            foreach (var aiHeroClient in EntityManager.Heroes.AllHeroes.Where(x => !x.IsDead && x.IsHPBarRendered))
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
            var  hero = ChampionVisibility.FirstOrDefault(u => u.Hero.NetworkId == unit.NetworkId);

            return hero;
        }

        public static bool IsUserInvisibleFor(this AIHeroClient unit, float time)
        {
            var hero = ChampionVisibility.FirstOrDefault(x => Game.Time*1000 - x.LastVisibleGameTime*1000 > time && Game.Time * 1000 - x.LastVisibleGameTime * 1000 < 4000 && x.Hero.NetworkId == unit.NetworkId);

            return hero != null && EntityManager.Heroes.AllHeroes.Any(x => x.NetworkId == hero.Hero.NetworkId && !x.IsDead && !x.IsHPBarRendered);
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
}