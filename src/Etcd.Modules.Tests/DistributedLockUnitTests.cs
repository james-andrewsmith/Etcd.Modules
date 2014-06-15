using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etcd.Modules.Tests
{
    [TestClass]
    public class DistributedLockUnitTests
    {
        [TestMethod]
        public void BasicLocking()
        {
            var lock1 = new DistributedLock("test00");
            var lock2 = new DistributedLock("test01");
            System.Threading.Thread.Sleep(5 * 1000);
            lock2.Dispose();
            lock1.Dispose();
        }

        [TestMethod]
        public void HighContention()
        {
            Parallel.For(0, 10, (_) =>
            {
                using (var l = new DistributedLock("test", 30))
                {
                    System.Threading.Thread.Sleep((2000) - (_ * 100));
                }
            });

        }

        [TestMethod]
        [ExpectedException(typeof(DistributedLockException))]
        public void ThrowExceptionOnTimeout()
        {
            var lock3 = new DistributedLock("testtimeout", 10, 30);
            var lock4 = new DistributedLock("testtimeout", 5, 30);
            System.Threading.Thread.Sleep(10000);
            lock4.Dispose();
            lock3.Dispose();
        }
    }
}
