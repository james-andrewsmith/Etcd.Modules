using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using etcetera;

namespace Etcd.Modules
{
    public class Program
    {
        static void Main(string[] args)
        {
            var election = new LeaderElection("frogfish");

            election.OnLeaderShip += () =>
            {
                Console.WriteLine("Time to break some promises");
            };
            
            election.Run();
            Console.ReadLine();
            election.Leave();

            return;
            var lock1 = new DistributedLock("james");
            var lock2 = new DistributedLock("ryan");
            System.Threading.Thread.Sleep(5 * 1000);
            lock2.Dispose();
            lock1.Dispose();

            Parallel.For(0, 10, (_) =>
            {
                using (var l = new DistributedLock("test", 30))
                {
                    System.Threading.Thread.Sleep((2000) - (_ * 100));
                }
            });


            var lock3 = new DistributedLock("leah", 10, 30);

            try
            {
                var lock4 = new DistributedLock("leah", 5, 30);
                System.Threading.Thread.Sleep(10000);
                lock4.Dispose();
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }
            lock3.Dispose();



            return;

            // var client = new EtcdClient(new Uri("http://127.0.0.1:4001/"));

            // barrier
            // limit nodes (between 1 and 32 child nodes)
            // watch, and on change blah blah..

            var barrier = new Barrier(name: "snowflake", 
                                      limit: 3, 
                                      ttl: 30, 
                                      value: "test",
                                      accept: (nodeID) =>
                                      {
                                          // setup a snowflake server with this ID
                                          Console.WriteLine("Accept fired with ID: " + nodeID);
                                      });

            // prevent the sever from exiting                
            bool running = true;
            do
            {
                var command = Console.ReadLine();
                running = ProcessCommand(command);
            }
            while (running);

            // clean up
            barrier.Dispose();


            // lock
            // leader election
            
            
            // watch reconnection
            // try watching a 
        }


        private static bool ProcessCommand(string line)
        {
            line = line.ToLower();
            if (line == "quit" || line == "exit" || line == "!q")
                return false;

            // example commands 
            // -> Clear Cache
            // if (line == "next")
            //     Console.WriteLine(Generation.Next());

            return true;
        }
    }
}
