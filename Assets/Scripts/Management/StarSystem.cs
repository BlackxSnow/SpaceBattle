using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Management
{
    public class StarSystem
    {
        public string SystemName;

        public List<Ship> Ships { get; private set; } = new List<Ship>();

        public void RegisterShip(Ship ship)
        {
            Ships.Add(ship);
        }
        public void DeregisterShip(Ship ship)
        {
            Ships.Remove(ship);
        }
    }
}
