#region Licensing
// //  ---------------------------------------------------------------------
// //  <copyright file="Enumerators.cs" company="EloBuddy">
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

namespace Simple_Marksmans.Utils
{
    public enum ItemType
    {
        Defensive,
        Offensive,
        Cleanse,
        Potion
    }

    public enum ItemTargettingType
    {
        Self,
        Unit,
        None
    }

    public enum ItemUsageWhen
    {
        Always,
        AfterAttack,
        ComboMode,
    }

    public enum ItemsEnum
    {
        HealthPotion,
        RefillablePotion,
        HuntersPotion,
        CorruptingPotion,
        ElixirofIron,
        ElixirofSorcery,
        ElixirofWrath,
        Scimitar,
        Quicksilver,
        Ghostblade,
        Cutlass,
        Gunblade,
        BladeOfTheRuinedKing
    }

    public enum ItemIds
    {
        HealthPotion = 2003,
        Biscuit = 2010,
        RefillablePotion = 2031,
        HuntersPotion = 2032,
        CorruptingPotion = 2033,
        ElixirofIron = 2138,
        ElixirofSorcery = 2139,
        ElixirofWrath = 2140,
        Scimitar = 3139,
        Quicksilver = 3140,
        Ghostblade = 3142,
        Cutlass = 3144,
        Gunblade = 3146,
        BladeOfTheRuinedKing = 3153
    }

    public enum GapcloserTypes
    {
        Targeted,
        Skillshot
    }

    [Flags]
    public enum ChampionTrackerFlags
    {
        VisibilityTracker = 1 << 0,
        LongCastTimeTracker = 1 << 1
    }
}