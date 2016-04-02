using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Scheme.Funs;
using static Scheme.Type;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Scheme
{
    public enum Type
    {
        Number, String, Symbol, Pair, Null, Bool
    }
    public struct Node
    {
        public Type type;
        public object content;
        public static Node getNull() => new Node { type = Null };
        public override string ToString()
        {
            if (type == Type.Pair)
            {
                List<char> s = new List<char>();
                s.Add('(');
                Node n = this;
                while (n.type == Type.Pair)
                {
                    s.AddRange(car(n).ToString());
                    s.Add(' ');
                    n = cdr(n);
                }
                if (n.type == Null)
                {
                    s[s.Count - 1] = ')';
                }
                else
                {
                    s.AddRange(". " + n.ToString() + ")");
                }
                return new string(s.ToArray());
            }
            else if(type==Type.String)
            {
                return "\"" + content + "\"";
            }
            else
            {
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

        static unsafe public bool isNumber(char* c)
        {
            List<char> charList = new List<char>();
            while(*c != '(' && *c != ')' && !isBlank(*c) && *c != '\0')
            {
                charList.Add(*c);
                c++;
            }
            try
            {
                double.Parse(new string(charList.ToArray()));
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
            if (n.type == Null || n.type == Bool || n.type == Number || n.type == Type.String)
            {
                return n;
            }
            else
            {
                return cons(
                    new Node { type = Symbol, content = "quote" },
                    cons(
                        n,
                        Node.getNull()
                        ));
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
        private static unsafe string stringParse(ref char*code)
        {
            List<char> charList = new List<char>();
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

                        charList.Add((char)(Tools.hex2int(end[1]) << 4 | Tools.hex2int(end[2])));
                        end += 3;
                    }
                    else if (*end >= '0' && *end <= '8')// \ddd
                    {
                        charList.Add((char)((end[0] - '0') << 6 | (end[1] - '0') << 3 | (end[2] - '0')));
                        end += 3;
                    }
                    else
                    {
                        for (int i = 0; i < escapeChar.Length; i++)
                        {
                            if (*end == escapeChar[i])
                            {
                                charList.Add(escapeResult[i]);
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
                    return new string(charList.ToArray());
                }
                else
                {
                    charList.Add(*end);
                    end++;
                }
            }
        }

        static unsafe Node singleParse(ref char* code)
        {
            Node node=new Node();
            if (*code == '\"')//parse a string
            {
                node.type = Type.String;
                node.content = stringParse(ref code);
            }
            else
            {
                char* end = code;
                while (*end != '(' && *end != ')' && !Tools.isBlank(*end) && *end != '\0')
                {
                    end++;
                }
                char[] arr = new char[end - code];
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = *(code + i);
                }
                string s = new string(arr);
                if (Tools.isNumber(code))//parse a number
                {
                    node.type = Number;
                    node.content = double.Parse(s);
                }
                else//parse a symbol
                {
                    if (s == "true" || s == "false")
                    {
                        node.type = Bool;
                        node.content = bool.Parse(s);
                    }
                    else if (s == "null")
                    {
                        node.type = Null;
                    }
                    else {
                        node.type = Symbol;
                        node.content = s;
                    }
                }
                code = end;
            }
            return node;
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
                    if (*code == ')')
                    {
                        code++;
                        break;
                    }
                    nList.Add(Parse(ref code));
                }
                Node list;
                if (nList[nList.Count - 2].type == Symbol && (string)(nList[nList.Count - 2].content) == ".")
                {
                    list = nList[nList.Count - 1];
                    for (int i = nList.Count - 3; i >= 0; i--)
                    {
                        list = cons(nList[i], list);
                    }
                }
                else {
                    list = Node.getNull();
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
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.AreEqual(Parser.Parse("\"\\\\\"").ToString(), "\"\\\"");
            Assert.AreEqual(Parser.Parse("\"\\n\"").ToString(), "\"\n\"");
            Assert.AreEqual(Parser.Parse("\"\n\"").ToString(), "\"\n\"");
            Assert.AreEqual(Parser.Parse("\"\\x2f\"").ToString(), "\"\x2f\"");
            Assert.AreEqual(Parser.Parse("\"\\222\"").ToString(), "\"\u0092\"");
        }
    }
}
