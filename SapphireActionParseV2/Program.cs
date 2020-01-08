using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SapphireActionParseV2
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.ParseAll();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
