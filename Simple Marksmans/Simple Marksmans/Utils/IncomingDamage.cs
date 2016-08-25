#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="IncomingDamage.cs" company="EloBuddy">
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
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Spells;

namespace Simple_Marksmans.Utils
{
    internal class IncomingDamage
    {
        private static readonly Dictionary<int, IncomingDamageArgs> IncomingDamages = new Dictionary<int, IncomingDamageArgs>();
        private static readonly List<int> Champions = new List<int>();// don't check for every champion

        static IncomingDamage()
        {
            Game.OnTick += Game_OnTick;

            Obj_AI_Base.OnBasicAttack += Obj_AI_Base_OnBasicAttack;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var heroSender = sender;
            var target = args.Target as AIHeroClient;

            if (heroSender == null || heroSender is Obj_AI_Turret || target == null || Champions.All(x => target.NetworkId != x) || !args.IsAutoAttack() || target.Team == heroSender.Team)
                return;

            if (IncomingDamages.ContainsKey(target.NetworkId))
            {
                IncomingDamages[target.NetworkId].Damage += heroSender.GetAutoAttackDamage(target, true);
            }
            else
            {
                IncomingDamages.Add(target.NetworkId, new IncomingDamageArgs
                {
                    Sender = heroSender,
                    Target = target,
                    Tick = (int) Game.Time*1000,
                    Damage = heroSender.GetAutoAttackDamage(target, true),
                    IsTargetted = true
                });
            }
            Console.WriteLine("[DEBUG] AutoAttack [" + target.Hero + "] " + heroSender.GetAutoAttackDamage(target, true));
        }

        private static void Obj_AI_Base_OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            var turret = sender as Obj_AI_Turret;
            var target = args.Target as AIHeroClient;

            if (turret == null || target == null || Champions.All(x => target.NetworkId != x) || target.Team == turret.Team)
                return;

            if (IncomingDamages.ContainsKey(target.NetworkId))
            {
                IncomingDamages[target.NetworkId].Damage += turret.GetAutoAttackDamage(target);
            }
            else
            {
                IncomingDamages.Add(target.NetworkId, new IncomingDamageArgs
                {
                    Sender = turret,
                    Target = target,
                    IsTurretShot = true,
                    Tick = (int) Game.Time*1000,
                    IsTargetted = false,
                    IsSkillShot = false,
                    Damage = turret.GetAutoAttackDamage(target)
                });
            }
            Console.WriteLine("[DEBUG] Targetted [" + target.Hero + "] " + turret.GetAutoAttackDamage(target));
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Player.Instance.IsDead || sender.IsMe)
                return;

            var heroSender = sender as AIHeroClient;
            var target = args.Target as AIHeroClient;

            if (heroSender != null && target != null && Champions.Exists(x => target.NetworkId == x) && target.Team != heroSender.Team)
            {
                if (IncomingDamages.ContainsKey(target.NetworkId))
                {
                    IncomingDamages[target.NetworkId].Damage += heroSender.GetSpellDamage(target, args.Slot);
                }
                else
                {
                    IncomingDamages.Add(target.NetworkId, new IncomingDamageArgs
                    {
                        Sender = heroSender,
                        Target = target,
                        Tick = (int) Game.Time*1000,
                        Damage = heroSender.CalculateDamageOnUnit(target, DamageType.Mixed,
                        heroSender.GetSpellDamage(target, args.Slot)),
                        IsTargetted = true
                    });
                }
                Console.WriteLine("[DEBUG] Targetted [" + target.Hero + "] " + heroSender.GetSpellDamage(target, args.Slot));
            }
            if (heroSender == null || target != null)
                return;

            if (args.SData.TargettingType == SpellDataTargetType.LocationAoe)
            {
                {
                    var polygon = new Geometry.Polygon.Circle(args.End, args.SData.CastRadius);
                    var polygon2 = new Geometry.Polygon.Circle(args.End, args.SData.CastRadiusSecondary);

                    foreach (var hero in EntityManager.Heroes.AllHeroes.Where(ally => Champions.Exists(x => ally.NetworkId == x) && polygon.IsInside(ally) || polygon2.IsInside(ally) && ally.Team != heroSender.Team))
                    {
                        if (IncomingDamages.ContainsKey(hero.NetworkId))
                        {
                            IncomingDamages[hero.NetworkId].Damage += heroSender.GetSpellDamage(hero, heroSender.GetSpellSlotFromName(args.SData.Name));
                        }
                        else
                        {
                            IncomingDamages.Add(hero.NetworkId, new IncomingDamageArgs
                            {
                                Sender = heroSender,
                                Target = hero,
                                IsSkillShot = true,
                                Damage = heroSender.CalculateDamageOnUnit(hero, DamageType.Mixed,
                                    heroSender.GetSpellDamage(hero, heroSender.GetSpellSlotFromName(args.SData.Name))),
                                Tick = (int) Game.Time*1000,
                                IsTargetted = false,
                                IsTurretShot = false
                            });
                            Console.WriteLine("[DEBUG] Skillshot [" + hero.Hero + "] " + heroSender.GetSpellDamage(hero, heroSender.GetSpellSlotFromName(args.SData.Name)));
                        }
                    }
                }
            }
            else if (args.SData.TargettingType == SpellDataTargetType.Location ||
                     args.SData.TargettingType == SpellDataTargetType.Location2 ||
                     args.SData.TargettingType == SpellDataTargetType.Location3 ||
                     args.SData.TargettingType == SpellDataTargetType.LocationVector ||
                     args.SData.TargettingType == SpellDataTargetType.LocationVector ||
                     args.SData.TargettingType == SpellDataTargetType.LocationVector)
            {
                var range = SpellDatabase.GetSpellInfoList(heroSender).FirstOrDefault();
                var polygon = new Geometry.Polygon.Rectangle(args.Start.To2D(),
                    args.Start.Extend(args.End, range?.Range ?? 1), args.SData.LineWidth);

                foreach (
                    var hero in
                        EntityManager.Heroes.AllHeroes.Where(ally => Champions.Exists(x => ally.NetworkId == x) && polygon.IsInside(ally) && ally.Team != heroSender.Team))
                {
                    if (IncomingDamages.ContainsKey(hero.NetworkId))
                    {
                        IncomingDamages[hero.NetworkId].Damage += heroSender.GetSpellDamage(hero, heroSender.GetSpellSlotFromName(args.SData.Name));
                    }
                    else
                    {
                        IncomingDamages.Add(hero.NetworkId, new IncomingDamageArgs
                        {
                            Sender = heroSender,
                            Target = hero,
                            IsSkillShot = true,
                            Tick = (int) Game.Time*1000,
                            Damage = heroSender.GetSpellDamage(hero, heroSender.GetSpellSlotFromName(args.SData.Name)),
                            IsTargetted = false,
                            IsTurretShot = false
                        });
                    }

                    Console.WriteLine("[DEBUG] Skillshot ["+hero.Hero+"] " + heroSender.GetSpellDamage(hero, heroSender.GetSpellSlotFromName(args.SData.Name)));
                }
            }
        }

        public static float GetIncomingDamage(AIHeroClient hero)
        {
            if (!Champions.Contains(hero.NetworkId))
            {
                Champions.Add(hero.NetworkId);
                Core.DelayAction(() => Champions.RemoveAll(x => x == hero.NetworkId), 2000);
            }

            if (!IncomingDamages.ContainsKey(hero.NetworkId))
                return 0f;
            
            return IncomingDamages.FirstOrDefault(x => x.Key == hero.NetworkId).Value != null ? IncomingDamages[hero.NetworkId].Damage : 0f;
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (IncomingDamages.Any(x => Game.Time * 1000 > x.Value.Tick + 400))
                IncomingDamages.Remove(IncomingDamages.FirstOrDefault(x => Game.Time * 1000 > x.Value.Tick + 500).Key);
        }

        private class IncomingDamageArgs
        {
            public Obj_AI_Base Sender { get; set; }
            public AIHeroClient Target { get; set; }
            public int Tick { get; set; }
            public float Damage { get; set; }
            public bool IsTurretShot { get; set; }
            public bool IsTargetted { get; set; }
            public bool IsSkillShot { get; set; }
        }
    }
}