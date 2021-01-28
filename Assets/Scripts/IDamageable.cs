using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data;

namespace Interfaces
{

    public interface IDamageable
    {
        HealthData Health { get; set; }

        void Damage(float amount, string damageType);
    }
}
