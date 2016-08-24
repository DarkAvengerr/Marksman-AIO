#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Combo.cs" company="EloBuddy">
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
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Sivir.Modes
{
    internal class Combo : Sivir
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (target != null && !target.HasUndyingBuffA() && !target.HasSpellShield())
                {
                    var qPrediction = Q.GetPrediction(target);

                    if(qPrediction.HitChance >= HitChance.Medium && target.TotalHealthWithShields() < Player.Instance.GetAutoAttackDamage(target, true) * 2 + Player.Instance.GetSpellDamage(target, SpellSlot.Q))
                    {
                        Console.WriteLine("[DEBUG] Casting Q on {0} variant 1", target.Hero);
                        Q.Cast(qPrediction.CastPosition);
                    }
                    else if (qPrediction.HitChance >= HitChance.High && Player.Instance.Mana - 60 > 100 && Player.Instance.IsInRange(target, Player.Instance.GetAutoAttackRange()))
                    {
                        Console.WriteLine("[DEBUG] Casting Q on {0} variant 2", target.Hero);
                        Q.Cast(qPrediction.CastPosition);
                    }
                }
            }

            if (!W.IsReady() || !Settings.Combo.UseW || !IsPostAttack)
                return;

            {
                var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange(), DamageType.Physical);

                if (target != null &&
                    target.Health - IncomingDamage.GetIncomingDamage(target) <
                    Player.Instance.GetAutoAttackDamage(target, true))
                {
                    Console.WriteLine("[DEBUG] Casting W on {0} variant 1", target.Hero);
                    W.Cast();
                } else if (target != null && target.Distance(Player.Instance) < Player.Instance.GetAutoAttackRange() - 100)
                {
                    Console.WriteLine("[DEBUG] Casting W on {0} variant 2", target.Hero);
                    W.Cast();
                }
            }
        }
    }
}