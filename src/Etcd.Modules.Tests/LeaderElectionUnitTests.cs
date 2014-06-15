using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etcd.Modules.Tests
{
    [TestClass]
    public class LeaderElectionUnitTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var election = new LeaderElection("frogfish");

            election.OnLeaderShip += () =>
            {
                Console.WriteLine("Time to break some promises");
            };

            election.Run();
            Console.ReadLine();
            election.Leave();
        }
    }
}
