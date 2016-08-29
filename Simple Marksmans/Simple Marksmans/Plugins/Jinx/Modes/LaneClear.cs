#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="LaneClear.cs" company="EloBuddy">
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

using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Simple_Marksmans.Plugins.Jinx.Modes
{
    internal class LaneClear : Jinx
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            if (!Settings.LaneClear.UseQInLaneClear || Player.Instance.IsUnderTurret())
                return;

            var laneMinions =
                EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy,
                    Player.Instance.Position, GetRealRocketLauncherRange() + 100).ToList();

            if (!laneMinions.Any())
                return;

            var rocketsLanuncherMinions = laneMinions.Where(x =>
                x.IsValidTarget(GetRealRocketLauncherRange()) &&
                ((laneMinions.Count(
                    k =>
                        k.Distance(x) <= 150 &&
                        (Prediction.Health.GetPrediction(k, 350) < Player.Instance.GetAutoAttackDamage(k)*1.1f)) > 1) ||
                 (!Player.Instance.IsInAutoAttackRange(x) &&
                  Prediction.Health.GetPrediction(x, 350) < Player.Instance.GetAutoAttackDamage(x, true)))).ToList();
                

            if (HasMinigun)
            {
                if (!(Player.Instance.ManaPercent >= Settings.LaneClear.MinManaQ) || !rocketsLanuncherMinions.Any() || !CanILaneClear())
                    return;

                foreach (var objAiMinion in rocketsLanuncherMinions)
                {
                    Q.Cast();
                    Orbwalker.ForcedTarget = objAiMinion;
                }
            }
            else if(HasRocketLauncher && !rocketsLanuncherMinions.Any())
            {
                if (!IsPreAttack)
                {
                    Q.Cast();
                }
            }
        }
    }
}