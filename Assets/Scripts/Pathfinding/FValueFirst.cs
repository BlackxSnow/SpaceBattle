using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pathfinding
{
    public class FValueFirst : Comparer<PathfindingAgent.Node>
    {
        public override int Compare(PathfindingAgent.Node x, PathfindingAgent.Node y)
        {
            int result = x.F.CompareTo(y.F);

            if (result == 0)
            {
                result = y.G.CompareTo(x.G);
            }
            if (result == 0)
            {
                result = x.cell.ID.x.CompareTo(y.cell.ID.x);
            }
            if (result == 0)
            {
                result = x.cell.ID.y.CompareTo(y.cell.ID.y);
            }
            if (result == 0)
            {
                result = x.cell.ID.z.CompareTo(y.cell.ID.z);
            }

            return result;
        }
    }
}
