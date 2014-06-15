using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using etcetera;

namespace Etcd.Modules
{
    // Barrier Logic
    // 
    // 1. Create the root
    // 2. Get a node id
    //    -> if unavailable wait on the root
    //    -> efficiently deal with TTL
    // 3. Create barrier child with TTL
    // 4. As long as barrier is active update the TTL

    public sealed class Barrier : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">eg: Snowflake</param>
        /// <param name="limit">eg: 32</param>
        /// <param name="ttl">minimum of 10, we update with a 10 second buffer</param>
        /// <param name="data">The data to add to the barrier</param>
        /// <param name="accept">Callback when accepted</param>
        public Barrier(string name, int limit, int ttl, string value = "", Action<int> accept = null)
        {
            _ttl = ttl;
            _name = name;
            _limit = limit;
            _value = value;
            _accept = accept;

            _handle = new ManualResetEventSlim(false);
            // _watcher = new BarrierWatcher(this.Callback);
            _root = "barrier";

            // get the client
            _client = new EtcdClient(Configuration.Instance.Hostnames[0]    );

            // create the root node if it doesn't exist
            var response = _client.CreateDir(_root + "/" + _name);           
                        
            // find node
            FindNode();
        }

        #region // Properties //

        public int NodeID
        {
            get { return _nodeID; }
        }

        private EtcdClient _client;
        private ManualResetEventSlim _handle;
        private int _nodeID = -1;
        private readonly Action<int> _accept;

        private readonly string _root;
        private readonly int _ttl;
        private readonly int _limit;
        private readonly string _value;
        private readonly string _name;

        #endregion

        private void Callback(EtcdResponse response)
        {
            _handle.Set();            
        }

        private void FindNode()
        {
            // first look at the nodes in play
            // -> if 32 exist, wait
            // -> if less, then attempt to create that node
            // -> if 'ZooKeeperNet.KeeperException.NodeExistsException' then 
            //    return to top of loop and try again

            while (_nodeID == -1)
            {
                var list = _client.Get(_root + "/" + _name, true, true, true)
                                  .Node
                                  .Nodes;

                if (list == null) list = new List<Node>();

                for (var i = 0; i < _limit; i++)
                {
                    string node = _root + "/" + _name + "/" + i.ToString().PadLeft(2, '0');
                    if (list.Find(_ => _.Key == node) == null)
                    {
                        var response = _client.Set(node, _value, _ttl, false);

                        // if key already exists then try the next value
                        if (response.ErrorCode == 105) continue;    

                        Console.WriteLine("Created Node: " + _root + "/" + node + " with value: " + _value);
                        _nodeID = i;

                        // start heartbeat
                        Task.Delay(new TimeSpan(0, 0, _ttl - 10))
                            .ContinueWith(_ => HeartBeat());

                        if (_accept != null)
                            _accept(_nodeID);

                        return;
                    }
                }

                Console.WriteLine("No slot available, wait until children change and try again");
                _client.Watch(_root + "/" + _name, Callback, true);
                _handle.Wait();
                _handle.Reset();
            }
        }

        private string GetNodePath()
        {
            return _root + "/" + _name + "/" + _nodeID.ToString().PadLeft(2, '0');
        }

        /// <summary>
        /// Update the TTL for the current node, at this stage this 
        /// is a naive call without any of the "last state" information
        /// </summary>
        private void HeartBeat()
        {
            _client.Set(GetNodePath(), _value, _ttl, true);

            // queue next heartbeat
            Task.Delay(new TimeSpan(0, 0, _ttl - 10))
                .ContinueWith(_ => HeartBeat());
        }

        /// <summary>
        /// When called with leave the barier
        /// </summary>
        public void Dispose()
        {
            _client.Delete(GetNodePath());
        }
    }
}
