using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using etcetera;

namespace Etcd.Modules
{
    
    /// <summary>
    /// A specific use of locks, several machines indefinately compete
    /// to hold a lock and once they have it run a special set of code
    /// </summary>
    public sealed class LeaderElection
    {
        // leader election has a callback (when the leader is removed)

        #region // Constructor //
        public LeaderElection(string name, int ttl = 180)
        {
            _name = name;
            _ttl = ttl;
            _source = new CancellationTokenSource();
        }
        #endregion

        #region // Properties //
        private int _ttl;
        private string _name;
        private CancellationTokenSource _source;
        private DistributedLock _lock;
        public event Action OnLeaderShip;
        #endregion

        /// <summary>
        /// Run in an election, we need to be able to cancel at any time
        /// </summary>
        public void Run()
        {
            Task.Delay(1)
                .ContinueWith(Campaign, _source.Token);            
        }

        private void Campaign(Task t)
        {
            if (!t.IsCanceled)
            {
                try
                {
                    _lock = new DistributedLock("election-" + _name, 1800, 900);
                    
                    // we are the leader!
                    OnLeaderShip();
                }
                catch (DistributedLockException)
                {
                    // when we can't get the lock, just start again...
                    Task.Delay(1)
                        .ContinueWith(Campaign, _source.Token);
                }
            }
        }

        /// <summary>
        /// We've had enough of politics, get out now
        /// </summary>
        public void Leave()
        {
            if (_source != null) _source.Cancel();
            if (_lock != null) _lock.Dispose();                       
        }
    }
}
