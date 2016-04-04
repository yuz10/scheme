using System;
using static Scheme.Type;

namespace Scheme
{
    public static class Funs
    {
        public static Node cons(Node car, Node cdr)
        {
            return new Node
            {
                type = Type.Pair,
                content = new Pair { car = car, cdr = cdr }
            };
        }
        public static Node car(Node pair)
        {
            if (pair.type == Type.Pair)
            {
                return ((Pair)pair.content).car;
            }
            else {
                throw new Exception($"car of not a pair");
            }
        }
        public static Node cdr(Node pair)
        {
            if (pair.type == Type.Pair)
            {
                return ((Pair)pair.content).cdr;
            }
            else {
                throw new Exception($"cdr of not a pair");
            }
        }
        public static Node eq(Node a, Node b)
        {
            return eq0(a, b) ? new Node(true) : new Node(false);
        }
        public static bool eq0(Node a, Node b)
        {
            if (a.type == Null && b.type == Null)
                return true;
            return a.type == b.type && a.content.Equals(b.content);
        }
    }
}
