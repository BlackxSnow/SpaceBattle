using System;
using System.Threading;
using System.Threading.Tasks;

namespace AI
{
    public abstract class AIState
    {
        CancellationTokenSource BehaviourTokenSource;
        public event Action BehaviourComplete;
        public TaskCompletionSource<bool> CompletionSource = new TaskCompletionSource<bool>();
        public bool IsStopped = false;

        public virtual bool Start()
        {
            if (BehaviourTokenSource == null || BehaviourTokenSource.IsCancellationRequested)
            {
                BehaviourTokenSource = new CancellationTokenSource();
                Behaviour(BehaviourTokenSource.Token);
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual bool Stop()
        {
            if (BehaviourTokenSource != null && !BehaviourTokenSource.IsCancellationRequested)
            {
                BehaviourTokenSource.Cancel();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected abstract void Behaviour(CancellationToken token);

        protected virtual void Complete()
        {
            IsStopped = true;
            if (BehaviourComplete != null)
            {
                BehaviourComplete.Invoke();
            }
            CompletionSource.SetResult(true);
        }
    }
}
