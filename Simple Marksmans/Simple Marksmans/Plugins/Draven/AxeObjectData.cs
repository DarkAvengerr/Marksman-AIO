using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using SharpDX;

namespace Simple_Marksmans.Plugins.Draven
{
    internal class AxeObjectData
    {
        public AIHeroClient Owner { get; set; }
        public int NetworkId { get; set; }
        public float StartTick { get; set; }
        public float EndTick { get; set; }
        public Vector3 EndPosition { get; set; }
    }
}
