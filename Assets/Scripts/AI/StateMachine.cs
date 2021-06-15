using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityAsync;

namespace AI
{
    public class StateMachine
    {

        AIState Current = null;
        LinkedList<AIState> BehaviourQueue = new LinkedList<AIState>();

        public AIState CurrentState { get => Current; }

        public void QueueBehaviour(AIState behaviour)
        {
            BehaviourQueue.AddLast(behaviour);
        }

        public void PerformImmediate(AIState behaviour)
        {
            if (Current != null && !Current.IsStopped)
            {
                Current.BehaviourComplete -= PerformNext;
                Current.Stop();
                BehaviourQueue.AddFirst(Current);
            }
            Current = behaviour;
            behaviour.BehaviourComplete += PerformNext;
            behaviour.Start();
        }

        protected void PerformNext()
        {
            if (!Current.IsStopped)
            {
                throw new Exception("PerformNext called before current behaviour was completed");
            }

            if (BehaviourQueue.Count > 0)
            {
                Current = BehaviourQueue.First.Value;
                BehaviourQueue.RemoveFirst();
                Current.BehaviourComplete += PerformNext;
                Current.Start(); 
            }
        }

        public void SetBehaviour(AIState behaviour)
        {
            BehaviourQueue.Clear();
            if (Current != null)
            {
                Current.BehaviourComplete -= PerformNext;
                Current.Stop(); 
            }
            Current = behaviour;
            Current.BehaviourComplete += PerformNext;
            Current.Start();
        }
        public async void SetBehaviour(AIState behaviour, float delay)
        {
            await Await.Seconds(delay);
            SetBehaviour(behaviour);
        }

        public void Clear()
        {
            BehaviourQueue.Clear();
            if (Current != null)
            {
                Current.BehaviourComplete -= PerformNext;
                Current.Stop();
                Current = null; 
            }
        }
    }
}
