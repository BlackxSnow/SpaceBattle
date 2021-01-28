using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

namespace Pathfinding
{
    public class Cell
    {
        public Vector3 Position;
        public Vector3Int ID;
        public bool IsOccupied;
        List<PathfindingAgent> OccupyingAgents = new List<PathfindingAgent>();

        public void UpdateStatus()
        {
            float radius = PathfindingGrid.Instance.CellSize / 2f;

            Collider[] occupiers = Physics.OverlapBox(Position, new Vector3(radius, radius, radius));
            List<PathfindingAgent> agents = new List<PathfindingAgent>();

            for (int i = 0; i < occupiers.Length; i++)
            {
                if (occupiers[i].TryGetComponent(out PathfindingAgent agent))
                {
                    agents.Add(agent);
                }
            }

            if (occupiers.Length > 0)
            {
                IsOccupied = true;
            }
            else if (IsOccupied)
            {
                IsOccupied = false;
                UpdateNeighbours();
            }

            List<PathfindingAgent> departed = OccupyingAgents.Except(agents).ToList();
            List<PathfindingAgent> arrived = agents.Except(OccupyingAgents).ToList();
            if (departed.Count != 0)
            {
                departed.ForEach(a => a.DeregisterCell(this));
            }
            if (arrived.Count != 0)
            {
                arrived.ForEach(a => a.RegisterCell(this));
            }

            OccupyingAgents = agents;

        }

        public void UpdateNeighbours()
        {
            for (int xoff = -1; xoff < 2; xoff += 1)
            {
                for (int yoff = -1; yoff < 2; yoff += 1)
                {
                    for (int zoff = -1; zoff < 2; zoff += 1)
                    {
                        Vector3Int newID = new Vector3Int(ID.x + xoff, ID.y + yoff, ID.z + zoff);
                        if (newID == ID) continue;
                        PathfindingGrid.Cells?[newID.x, newID.y, newID.z].UpdateStatus();
                    }
                }
            }
        }

        public Cell(Vector3 position, Vector3Int id)
        {
            Position = position;
            ID = id;
            UpdateStatus();
        }
    }

}