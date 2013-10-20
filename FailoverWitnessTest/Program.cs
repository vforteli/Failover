using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Flexinets.Failover;

namespace FailoverWitnessTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ;
            var partners = new List<IPEndPoint>
            {
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 50710),
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 50711)
            };

            var witness = new FailoverWitness(new IPEndPoint(IPAddress.Any, 50700), partners);
            witness.Start();


            Console.ReadLine();
        }
    }
}
