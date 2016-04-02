using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
    class Program
    {
        static unsafe void removeBlank(ref char* code)
        {
            while (*code == ' ' || *code == '\t')
            {
                code++;
            }
        }
        static unsafe public bool isNumber(char* c)
        {
            if (c[0] >= '0' && c[0] <= '9')
                return true;
            if (c[0] == '.')
            {
                if (c[1] >= '0' && c[1] <= '9')
                    return true;
            }
            if (c[0] == '+' || c[0] == '-')
            {
                return isNumber(c + 1);
            }
            return false;
        }
        unsafe static void Main(string[] args)
        {
            string s = "-.";
            fixed(char* c = s)
            {
                char* d = c;
                removeBlank(ref d);
                Console.Write(isNumber(d));
            }
            Console.Read();
        }
    }
}
