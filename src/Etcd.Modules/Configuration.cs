using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Etcd.Modules
{
    public sealed class Configuration
    {

        private static Configuration instance;
        public static Configuration Instance
        {
            get { return instance; }
        }

        static Configuration()
        {
            instance = new Configuration();
            // instance.GetFromFile();
            instance.Hostnames = new List<Uri>
            {
                new Uri("http://127.0.0.1:4001")
            };
        }

        public List<Uri> Hostnames { get; private set; }
        

    }
}
