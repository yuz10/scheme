using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Scheme
{
    public enum Type
    {
        Number, String, Symbol, Pair, Null, Bool, Lambda, Fun
    }
    public struct Node
    {
        public Type type;
        public object content;

        public Node(string code)
        {
            this = Parser.Parse(code);
        }
        public Node(double n)
        {
            type = Type.Number;
            content = n;
        }
        public Node(bool b)
        {
            type = Type.Bool;
            content = b;
        }
        public static Node getNull() {return new Node { type = Type.Null }; }
        public override string ToString()
        {
            switch (type)
            {
                case Type.Pair:
                    StringBuilder s = new StringBuilder();
                    s.Append('(');
                    Node n = this;
                    while (n.type == Type.Pair)
                    {
                        s.Append(Funs.car(n).ToString());
                        s.Append(' ');
                        n = Funs.cdr(n);
                    }
                    if (n.type == Type.Null)
                    {
                        s[s.Length - 1] = ')';
                    }
                    else
                    {
                        s.Append(". ");
                        s.Append(n.ToString());
                        s.Append(")");
                    }
                    return s.ToString();
                case Type.Null:
                    return "null";
                case Type.Bool:
                    return ((bool)content) ? "true" : "false";
                case Type.String:
                    return "\"" + content + "\"";
                default:
                    return content.ToString();

            }
        }
    }
    struct Pair
    {
        public Node car;
        public Node cdr;
    }
    static class Tools
    {
        static public int hex2int(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            else if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }
            else if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }
            else
            {
                throw new Exception("wrong ascii code");
            }
        }
        static public bool isBlank(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        static unsafe public bool isNumber(string s)
        {
            try
            {
                double.Parse(s);
                return true;
            }
            catch { return false; }
        }
    }
    public static class Parser
    {
        static char[] escapeChar = { 'a', 'b', 'f', 'n', 'r', 't', 'v', '\\', '\'', '\"' };
        static char[] escapeResult = { '\a', '\b', '\f', '\n', '\r', '\t', '\v', '\\', '\'', '\"' };


        static Node quote(Node n)
        {
            if (n.type == Type.Null || n.type == Type.Bool || n.type == Type.Number || n.type == Type.String)
            {
                return n;
            }
            else
            {
                return Funs.cons(new Node("quote"), Funs.cons(n, Node.getNull()));
            }
        }

        public static unsafe Node Parse(string code)
        {
            fixed (char* ps = code)
            {
                char* c = ps;
                return Parse(ref c);
            }
        }

        static unsafe void skipBlank(ref char* code)
        {
            while (Tools.isBlank(*code))
            {
                code++;
            }
        }
        private static unsafe Node stringParse(ref char*code)
        {
            StringBuilder s = new StringBuilder();
            char* end = code + 1;
            while (true)
            {
                if (*end == '\0')
                {
                    throw new Exception();
                }
                else if (*end == '\\')
                {
                    end++;
                    if (*end == 'x')// \xhh
                    {

                        s.Append((char)(Tools.hex2int(end[1]) << 4 | Tools.hex2int(end[2])));
                        end += 3;
                    }
                    else if (*end >= '0' && *end <= '8')// \ddd
                    {
                        s.Append((char)((end[0] - '0') << 6 | (end[1] - '0') << 3 | (end[2] - '0')));
                        end += 3;
                    }
                    else
                    {
                        for (int i = 0; i < escapeChar.Length; i++)
                        {
                            if (*end == escapeChar[i])
                            {
                                s.Append(escapeResult[i]);
                                break;
                            }
                        }
                        end++;
                    }
                }
                else if (*end == '\"')
                {
                    end++;
                    code = end;
                    return new Node { type = Type.String, content = s.ToString() };
                }
                else
                {
                    s.Append(*end);
                    end++;
                }
            }
        }

        static unsafe Node singleParse(ref char* code)
        {
            if (*code == '\"')//parse a string
            {
                return stringParse(ref code);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                while (*code != '(' && *code != ')' && !Tools.isBlank(*code) && *code != '\0')
                {
                    sb.Append(*code);
                    code++;
                }
                string s = sb.ToString();
                if (Tools.isNumber(s))//parse a number
                {
                    return new Node(double.Parse(s));
                }
                else//parse a symbol
                {
                    if (s == "true" || s == "false")
                    {
                        return new Node(bool.Parse(s));
                    }
                    else if (s == "null")
                    {
                        return Node.getNull();
                    }
                    else
                    {
                        return new Node { type = Type.Symbol, content = s };
                    }
                }
            }
        }
        static unsafe Node Parse(ref char* code)
        {
            skipBlank(ref code);
            if (*code == '(')
            {
                code++;
                List<Node> nList = new List<Node>();
                while (true)
                {
                    skipBlank(ref code);
                    if (*code == '\0')
                    {
                        throw new Exception("List not match");
                    }
                    if (*code == ')')
                    {
                        code++;
                        break;
                    }
                    nList.Add(Parse(ref code));
                }
                Node list;
                if (nList.Count > 2 && Funs.eq0(nList[nList.Count - 2], new Node(".")))
                {
                    list = nList[nList.Count - 1];
                    for (int i = nList.Count - 3; i >= 0; i--)
                    {
                        list = Funs.cons(nList[i], list);
                    }
                }
                else {
                    list = Node.getNull();
                    for (int i = nList.Count - 1; i >= 0; i--)
                    {
                        list = Funs.cons(nList[i], list);
                    }
                }
                return list;
            }
            else if (*code == '\'')//parse a quote
            {
                code++;
                return quote(Parse(ref code));
            }
            else
            {
                return singleParse(ref code);
            }
        }
    }
    [TestClass]
    public partial class UnitTest1
    {
        [TestMethod]
        public void TestStringParser()
        {
            Assert.AreEqual("\"\\\"", new Node("\"\\\\\"").ToString());
            Assert.AreEqual("\"\n\"", new Node("\"\\n\"").ToString());
            Assert.AreEqual("\"\n\"", new Node("\"\n\"").ToString());
            Assert.AreEqual("\"\xff\x2a\"", new Node("\"\\xFf\\x2a\"").ToString());
            Assert.AreEqual("\"\u0092\"", new Node("\"\\222\"").ToString());
        }
        [TestMethod]
        public void TestQuoteParser()
        {
            Assert.AreEqual("4", new Node("'4").ToString());
            Assert.AreEqual("true", new Node("'true").ToString());
            Assert.AreEqual("false", new Node("'false").ToString());
            Assert.AreEqual("null", new Node("'()").ToString());
            Assert.AreEqual("null", new Node("()").ToString());
            Assert.AreEqual("(quote (2 3))", new Node("'(2 3)").ToString());
            Assert.AreEqual("(quote (quote a))", new Node("''a").ToString());
        }
        [TestMethod]
        public void TestListParser()
        {
            Assert.AreEqual("((1 2) 3 (4 5))", new Node("((1 2) 3 (4 5))").ToString());
            Assert.AreEqual("((1 . 2) 3 (4 5))", new Node("((1 . 2) 3 (4 5))").ToString());
            Assert.AreEqual("(1 2 3 . 5)", new Node("(1 2 3 . 5)").ToString());
            Node n = new Node("(1 . 2)");
            Assert.AreEqual(Type.Pair, n.type);
            Assert.IsTrue(Funs.eq0(new Node(1), ((Pair)(n.content)).car));
            Assert.IsTrue(Funs.eq0(new Node(2), ((Pair)(n.content)).cdr));
        }
        [TestMethod]
        public void TestEq()
        {
            Assert.IsTrue(Funs.eq0(new Node(1), new Node("1")));
            Assert.IsTrue(Funs.eq0(new Node("true"), new Node("true")));
            Assert.IsTrue(Funs.eq0(new Node("null"), new Node("null")));
            Assert.IsFalse(Funs.eq0(new Node(2), new Node("1")));
        }
    }
}
