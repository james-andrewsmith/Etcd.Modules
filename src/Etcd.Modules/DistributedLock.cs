using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using etcetera;

namespace Etcd.Modules
{
    public sealed class DistributedLockException : Exception
    {
        public DistributedLockException(string message) : base(message) { }
    }
    
    public sealed class DistributedLock : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timeout">Seconds we will try to get the lock for.</param>
        /// <param name="ttl">Seconds until lock times out. Governs how frequently we extend the lock as well. Minimum of 10 seconds.</param>
        public DistributedLock(string name, int timeout = 10, int ttl = 30)
        {

            if (string.IsNullOrEmpty(name))
                throw new Exception("Distributed Lock Requires a Name");

            _ttl = ttl;
            if (_ttl < 10) throw new Exception("Minimum TTL is 10 seconds");

            _timeout = DateTime.UtcNow.AddSeconds(timeout);
            _client = new EtcdClient(Configuration.Instance.Hostnames[0]);
            _name = name;
            _handle = new ManualResetEventSlim(false);

            // get the unique ID of this locking instance
            _guid = Guid.NewGuid();

            // 1. try to create the node
            //    -> if already exists set up a watch to lock after it
            //       is given back.
            //       watch should also have the ID of the modifyIndex so 
            //       it will catch up if something happens in between

            // 2. extend the lock while it's active automatically
            //    by updating the TTL
            //    

            // this will attempt to get the lock and re-call itself
            // if there are multiple threads/servers attempting to 
            // get the same lock
            GetLock();

            // use the reset event to block this thread
            _handle.Wait(timeout * 1000);
            _handle.Dispose();

            if (_index == 0)
            {
                // we didn't get the lock
                throw new DistributedLockException("Could not aquire lock after retries");
            }
        }

        private void GetLock()
        {
            var response = _client.Set("lock/" + _name, _guid.ToString(), _ttl, false);
            if (response.ErrorCode == null)
            {
                _index = (int)response.Node.ModifiedIndex;
            }
            
            if (_index > 0)
            {
                // we did get the lock, queue up a TTL 
                _handle.Set();
                _source = new CancellationTokenSource();
                Task.Delay((_ttl - 9) * 1000, _source.Token)
                    .ContinueWith(Extend);
            }
            else if (DateTime.UtcNow < _timeout)
            {
                // set up a wait with a timeout of the timeout
                Console.WriteLine("Setting up watch with timeout of: " + (int)(_timeout - DateTime.UtcNow).TotalSeconds);
                _client.Watch("lock/" + _name, 
                              WatchHandler, 
                              false, 
                              (int)(_timeout - DateTime.UtcNow).TotalMilliseconds);
            }           
        }

        private void WatchHandler(EtcdResponse response)
        {
            Console.WriteLine("Watch Handler");
            GetLock();
        }

        #region // Properties //    
        private ManualResetEventSlim _handle;
        private DateTime _timeout;
        private int _index;
        private string _name;
        private Guid _guid;
        private int _ttl;
        private EtcdClient _client;
        private CancellationTokenSource _source;
        #endregion

        /// <summary>
        /// Increase the TTL on this lock while work is still occuring
        /// </summary>
        /// <param name="t"></param>
        private void Extend(Task t)
        {
            if (!t.IsCanceled)
            {
                Console.WriteLine("Extending lock");
                var response = _client.Set("lock/" + _name, _guid.ToString(), _ttl, true, _guid.ToString(), _index);
                if (response.ErrorCode == null)
                {
                    _index = (int)response.Node.ModifiedIndex;

                    // queue a re-extend
                    Task.Delay((_ttl - 9) * 1000, _source.Token)
                        .ContinueWith(Extend);
                }
            }         
        }

        public void Dispose()
        {          
            if (_source != null) _source.Cancel();
            _client.Delete("lock/" + _name, _guid.ToString(), _index);
        }
    }
}
