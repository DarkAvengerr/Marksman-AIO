#region Licensing
//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Item.cs" company="EloBuddy">
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
using System.Collections;
using System.Collections.Generic;
using Simple_Marksmans.Interfaces;
using Simple_Marksmans.Utils;

namespace Simple_Marksmans.Activator
{
    internal class Item : IItem
    {
        public string ItemName { get; set; }
        public ItemIds ItemId { get; set; }
        public float Range { get; set; }
        public ItemType ItemType { get; set; }
        public ItemTargettingType ItemTargettingType { get; set; }
        public ItemUsageWhen ItemUsageWhen { get; set; }
    }
}
