using System;
using System.Linq;

namespace Scheme
{
    class Program
    {
        static void Main(string[] args)
        {
            Env env = SObject.getBaseEnv();
            string code = "";
            Console.Write(">");
            while (true)
            {
                code += Console.ReadLine();
                if (code.Last() == '\t')
                {
                    try
                    {
                    Console.Write(new SObject(code).eval(env));
                    }
                    catch (Exception e)
                    {
                        Console.Write("error: " + e.Message);
                    }
                    Console.Write("\n>");
                    code = "";
                }
            }
        }
    }
}


