using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management
{
    public class Faction
    {
        public string Name;
        public Dictionary<string, Fleet> Fleets = new Dictionary<string, Fleet>();

        public bool RegisterFleet(string name)
        {
            if(Fleets.ContainsKey(name))
            {
                return false;
            }
            else
            {
                Fleets.Add(name, new Fleet(name));
                return true;
            }
        }

        public Faction(string name)
        {
            Name = name;
        }
    }
}
