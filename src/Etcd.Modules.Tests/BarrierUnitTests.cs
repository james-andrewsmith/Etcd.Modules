using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etcd.Modules.Tests
{
    [TestClass]
    public class BarrierUnitTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var barrier = new Barrier(name: "snowflake",
                                      limit: 3,
                                      ttl: 30,
                                      value: "test",
                                      accept: (nodeID) =>
                                      {
                                          // setup a snowflake server with this ID
                                          Console.WriteLine("Accept fired with ID: " + nodeID);
                                      });

        }
    }
}
