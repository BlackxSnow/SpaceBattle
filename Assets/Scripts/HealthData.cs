using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    public class HealthData
    {
        public float Hull;
        public float MaxHull;
        public float Shield;
        public float MaxShield;

        public bool HasShield { get => Shield > 0; }
        public bool IsDead { get => Hull <= 0; }

        public HealthData(float maxHull, float maxShield)
        {
            Hull = maxHull;
            MaxHull = maxHull;
            Shield = maxShield;
            MaxShield = maxShield;
        }
    }
}
