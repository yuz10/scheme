using System;
using System.Collections.Generic;
using System.Text;
using static Scheme.Funs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Scheme
{
    public enum Type
    {
        Number, String, Symbol, Pair, Null, Bool, Lambda, Fun
    }
    public partial struct SObject
    {
        public Type type;
        public object content;

        public SObject(string code)
        {
            this = Parser.Parse(code);
        }
        public SObject(double n)
        {
            type = Type.Number;
            content = n;
        }
        public SObject(bool b)
        {
            type = Type.Bool;
            content = b;
        }
        public static SObject getNull() { return new SObject { type = Type.Null }; }
        public override string ToString()
        {
            switch (type)
            {
                case Type.Pair:
                    StringBuilder s = new StringBuilder();
                    s.Append('(');
                    SObject n = this;
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
        public SObject car;
        public SObject cdr;
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


        static SObject quote(SObject n)
        {
            if (n.type == Type.Null || n.type == Type.Bool || n.type == Type.Number || n.type == Type.String)
            {
                return n;
            }
            else
            {
                return cons(new SObject("quote"), cons(n, SObject.getNull()));
            }
        }

        public static unsafe SObject Parse(string code)
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
        private static unsafe SObject stringParse(ref char*code)
        {
            StringBuilder s = new StringBuilder();
            code++;
            while (true)
            {
                if (*code == '\0')
                {
                    throw new Exception();
                }
                else if (*code == '\\')
                {
                    code++;
                    if (*code == 'x')// \xhh
                    {

                        s.Append((char)(Tools.hex2int(code[1]) << 4 | Tools.hex2int(code[2])));
                        code += 3;
                    }
                    else if (*code >= '0' && *code <= '8')// \ddd
                    {
                        s.Append((char)((code[0] - '0') << 6 | (code[1] - '0') << 3 | (code[2] - '0')));
                        code += 3;
                    }
                    else
                    {
                        for (int i = 0; i < escapeChar.Length; i++)
                        {
                            if (*code == escapeChar[i])
                            {
                                s.Append(escapeResult[i]);
                                break;
                            }
                        }
                        code++;
                    }
                }
                else if (*code == '\"')
                {
                    code++;
                    return new SObject { type = Type.String, content = s.ToString() };
                }
                else
                {
                    s.Append(*code);
                    code++;
                }
            }
        }

        static unsafe SObject singleParse(ref char* code)
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
                    return new SObject(double.Parse(s));
                }
                else//parse a symbol
                {
                    if (s == "true" || s == "false")
                    {
                        return new SObject(bool.Parse(s));
                    }
                    else if (s == "null")
                    {
                        return SObject.getNull();
                    }
                    else
                    {
                        return new SObject { type = Type.Symbol, content = s };
                    }
                }
            }
        }
        static unsafe SObject Parse(ref char* code)
        {
            skipBlank(ref code);
            if (*code == '\0')
            {
                return SObject.getNull();
            }
            else if (*code == '(')
            {
                code++;
                List<SObject> nList = new List<SObject>();
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
                SObject list;
                if (nList.Count > 2 && eq0(nList[nList.Count - 2], new SObject(".")))
                {
                    list = nList[nList.Count - 1];
                    for (int i = nList.Count - 3; i >= 0; i--)
                    {
                        list = cons(nList[i], list);
                    }
                }
                else {
                    list = SObject.getNull();
                    for (int i = nList.Count - 1; i >= 0; i--)
                    {
                        list = cons(nList[i], list);
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
            Assert.AreEqual("\"\\\"", new SObject("\"\\\\\"").ToString());
            Assert.AreEqual("\"\n\"", new SObject("\"\\n\"").ToString());
            Assert.AreEqual("\"\n\"", new SObject("\"\n\"").ToString());
            Assert.AreEqual("\"\xff\x2a\"", new SObject("\"\\xFf\\x2a\"").ToString());
            Assert.AreEqual("\"\u0092\"", new SObject("\"\\222\"").ToString());
        }
        [TestMethod]
        public void TestQuoteParser()
        {
            Assert.AreEqual("4", new SObject("'4").ToString());
            Assert.AreEqual("true", new SObject("'true").ToString());
            Assert.AreEqual("false", new SObject("'false").ToString());
            Assert.AreEqual("null", new SObject("'()").ToString());
            Assert.AreEqual("null", new SObject("()").ToString());
            Assert.AreEqual("(quote (2 3))", new SObject("'(2 3)").ToString());
            Assert.AreEqual("(quote (quote a))", new SObject("''a").ToString());
        }
        [TestMethod]
        public void TestListParser()
        {
            Assert.AreEqual("((1 2) 3 (4 5))", new SObject("((1 2) 3 (4 5))").ToString());
            Assert.AreEqual("((1 . 2) 3 (4 5))", new SObject("((1 . 2) 3 (4 5))").ToString());
            Assert.AreEqual("(1 2 3 . 5)", new SObject("(1 2 3 . 5)").ToString());
            SObject n = new SObject("(1 . 2)");
            Assert.AreEqual(Type.Pair, n.type);
            Assert.IsTrue(eq0(new SObject(1), ((Pair)(n.content)).car));
            Assert.IsTrue(eq0(new SObject(2), ((Pair)(n.content)).cdr));
        }
        [TestMethod]
        public void TestEq()
        {
            Assert.IsTrue(eq0(new SObject(1), new SObject("1")));
            Assert.IsTrue(eq0(new SObject("true"), new SObject("true")));
            Assert.IsTrue(eq0(new SObject("null"), new SObject("null")));
            Assert.IsFalse(eq0(new SObject(2), new SObject("1")));
        }
    }
}
