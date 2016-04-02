using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheme
{
    class Program
    {
        static void Main(string[] args)
        {
            Node n = Parser.Parse("\"\\n\"");
            Console.Write(n.ToString());
        }
    }
}


