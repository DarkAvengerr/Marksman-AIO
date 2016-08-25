#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Damage.cs" company="EloBuddy">
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
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Plugins.Kalista
{
    internal static class Damage
    {
        private static readonly int[] EDamage = { 0, 20, 30, 40, 50, 60 };
        private const float EDamageMod = 0.6f;
        private static readonly int[] EDamagePerSpear = { 0, 10, 14, 19, 25, 32 };
        private static readonly float[] EDamagePerSpearMod = { 0, 0.2f, 0.225f, 0.25f, 0.275f, 0.3f };

        public static float GetComboDamage(this AIHeroClient enemy, int stacks)
        {
            float damage = 0;

            if (Kalista.Q.IsReady())
                damage += Player.Instance.GetSpellDamage(enemy, SpellSlot.Q);

            if (Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing] != null && Activator.Activator.Items[ItemsEnum.BladeOfTheRuinedKing].ToItem().IsReady())
                damage += Player.Instance.GetItemDamage(enemy, ItemId.Blade_of_the_Ruined_King);

            if (Activator.Activator.Items[ItemsEnum.Cutlass] != null && Activator.Activator.Items[ItemsEnum.Cutlass].ToItem().IsReady())
                damage += Player.Instance.GetItemDamage(enemy, ItemId.Bilgewater_Cutlass);

            if (Activator.Activator.Items[ItemsEnum.Gunblade] != null && Activator.Activator.Items[ItemsEnum.Gunblade].ToItem().IsReady())
                damage += Player.Instance.GetItemDamage(enemy, ItemId.Hextech_Gunblade);

            if (Kalista.E.IsReady())
                damage += enemy.GetRendDamageOnTarget(stacks);

            damage += Player.Instance.GetAutoAttackDamage(enemy, true) * stacks;

            return damage;
        }

        public static bool CanCastEOnUnit(this Obj_AI_Base target)
        {
            if (target == null || !target.IsValidTarget(Kalista.E.Range) || target.GetRendBuff() == null ||
                !Kalista.E.IsReady() || target.GetRendBuff().Count < 1)
                return false;

            if (!(target is AIHeroClient))
                return true;

            var heroClient = (AIHeroClient) target;

            return !heroClient.HasUndyingBuffA() && !heroClient.HasSpellShield();
        }

        public static bool IsTargetKillableByRend(this Obj_AI_Base target)
        {
            if (target == null || !target.IsValidTarget(Kalista.E.Range) || target.GetRendBuff() == null ||
                !Kalista.E.IsReady() || target.GetRendBuff().Count < 1)
                return false;

            if (!(target is AIHeroClient))
            {
                return target.GetRendDamageOnTarget() > target.TotalHealthWithShields();
            }

            var heroClient = (AIHeroClient) target;

            if (heroClient.HasUndyingBuffA() || heroClient.HasSpellShield())
            {
                return false;
            }

            if (heroClient.ChampionName != "Blitzcrank")
                return heroClient.GetRendDamageOnTarget() >= heroClient.TotalHealthWithShields();

            if (!heroClient.HasBuff("BlitzcrankManaBarrierCD") && !heroClient.HasBuff("ManaBarrier"))
            {
                return heroClient.GetRendDamageOnTarget() > heroClient.TotalHealthWithShields() + heroClient.Mana / 2;
            }
            return heroClient.GetRendDamageOnTarget() > heroClient.TotalHealthWithShields();
        }

        public static float GetRendDamageOnTarget(this Obj_AI_Base target)
        {
            if (!CanCastEOnUnit(target))
                return 0f;

            var damageReduction = 100 - Kalista.Settings.Misc.ReduceEDmg;
            var damage = EDamage[Kalista.E.Level] + Player.Instance.TotalAttackDamage * EDamageMod +
                         (target.GetRendBuff().Count > 1
                             ? (EDamagePerSpear[Kalista.E.Level] +
                                Player.Instance.TotalAttackDamage * EDamagePerSpearMod[Kalista.E.Level]) *
                               (target.GetRendBuff().Count - 1)
                             : 0);

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, damage * damageReduction / 100);
        }

        public static float GetRendDamageOnTarget(this Obj_AI_Base target, int stacks)
        {
            if (target == null || stacks < 1)
                return 0f;

            var damageReduction = 100 - Kalista.Settings.Misc.ReduceEDmg;

            var damage = EDamage[Kalista.E.Level] + Player.Instance.TotalAttackDamage * EDamageMod +
                         (stacks > 1
                             ? (EDamagePerSpear[Kalista.E.Level] +
                                Player.Instance.TotalAttackDamage * EDamagePerSpearMod[Kalista.E.Level]) *
                               (stacks - 1)
                             : 0);

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, damage * damageReduction / 100);
        }

        public static BuffInstance GetRendBuff(this Obj_AI_Base target)
        {
            return
                target.Buffs.Find(
                    b => b.Caster.IsMe && b.IsValid && b.DisplayName.ToLowerInvariant() == "kalistaexpungemarker");
        }

        public static bool HasRendBuff(this Obj_AI_Base target)
        {
            return target.GetRendBuff() != null;
        }
    }
}
