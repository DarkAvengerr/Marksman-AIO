#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ObjAiBaseExtensions.cs" company="EloBuddy">
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

using System.Linq;
using EloBuddy;
using EloBuddy.SDK;

namespace Simple_Marksmans.Utils
{
    internal static class ObjAiBaseExtensions
    {
        public static bool IsUsingHealingPotion(this Obj_AI_Base unit)
        {
            return unit.HasBuff("ItemMiniRegenPotion") || unit.HasBuff("ItemCrystalFlask") ||
                   unit.HasBuff("ItemCrystalFlaskJungle") || unit.HasBuff("ItemDarkCrystalFlask") || unit.HasBuff("Health Potion");
        }

        public static bool HasUndyingBuffA(this AIHeroClient target)
        {
            if (target.Buffs.Any(b => b.IsValid &&
                                      (b.Name == "ChronoShift" || b.Name == "FioraW" || b.Name == "TaricR" || b.Name == "BardRStasis" ||
                                       b.Name == "JudicatorIntervention" || b.Name == "UndyingRage" || (b.Name == "kindredrnodeathbuff" && target.HealthPercent <= 10))))
            {
                return true;
            }

            if (target.ChampionName != "Poppy")
                return target.IsInvulnerable;
            
            return EntityManager.Heroes.Allies.Any(
                o => !o.IsMe && o.Buffs.Any(b => b.Caster.NetworkId == target.NetworkId && b.IsValid &&
                                                 b.DisplayName == "PoppyDITarget")) || target.IsInvulnerable;
        }
        /*
        public static float GetDamageReduction(this AIHeroClient target)
        {
            return 0f;
        }
        */
        public static bool HasSpellShield(this Obj_AI_Base target)
        {
            return target.HasBuffOfType(BuffType.SpellShield) || target.HasBuffOfType(BuffType.SpellImmunity);
        }

        public static float TotalHealthWithShields(this Obj_AI_Base target, bool includeMagicShields = false)
        {
            return target.Health + target.AllShield + target.AttackShield + (includeMagicShields ? target.MagicShield : 0);
        }

        public static bool HasSheenBuff(this AIHeroClient unit)
        {
            return unit.Buffs.Any(b => b.IsActive && b.DisplayName.ToLowerInvariant() == "sheen");
        }
    }
}