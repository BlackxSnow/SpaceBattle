using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AI.Tactics
{
    /// <summary>
    /// Retreat to the nearest safe station or carrier prioritising repair capable docks. If none are reachable burn away
    /// </summary>
    public class Retreat : AITactic
    {
        protected override void Behaviour(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
