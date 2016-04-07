using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheme
{
    static class Funs
    {
        public static SObject cons(SObject car, SObject cdr)
        {
            return new SObject
            {
                type = Type.Pair,
                content = new Pair { car = car, cdr = cdr }
            };
        }
        public static SObject car(SObject pair)
        {
            if (pair.type == Type.Pair)
            {
                return ((Pair)pair.content).car;
            }
            else
            {
                throw new Exception("car of not a pair");
            }
        }
        public static SObject cdr(SObject pair)
        {
            if (pair.type == Type.Pair)
            {
                return ((Pair)pair.content).cdr;
            }
            else
            {
                throw new Exception("cdr of not a pair");
            }
        }
        public static SObject eq(SObject a, SObject b)
        {
            return eq0(a, b) ? new SObject(true) : new SObject(false);
        }
        public static bool eq0(SObject a, SObject b)
        {
            if (a.type == Type.Null && b.type == Type.Null)
                return true;
            return a.type == b.type && a.content.Equals(b.content);
        }
    }
}
