using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Scheme.Funs;
using static Scheme.Type;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Scheme
{
    public class Env
    {
        private Env parent;
        private Dictionary<string, Node> env = new Dictionary<string, Node>();
        public Env()
        {
            parent = null;
            env = new Dictionary<string, Node>();
        }
        public Env(Env parent)
        {
            this.parent = parent;
            env = new Dictionary<string, Node>();
        }
        public void add(string key, Node value)
        {
            env.Add(key, value);
        }
        public Node? get(string key)
        {
            if (env.ContainsKey(key))
            {
                return env[key];
            }
            else
            {
                return parent?.get(key);
            }
        }
    }
    public class Lambda
    {
        Env env;
        Node exp;
    }
    public static class Interpreter
    {
        public static string Eval(string code)
        {
            return Eval(new Node(code), new Env()).ToString();
        }
        public static Node Eval(Node exp, Env env)
        {
            if (exp.type == Null || exp.type == Bool || exp.type == Number || exp.type == Type.String)
            {
                return exp;
            }
            else if (exp.type == Symbol)
            {
                Node? res = env.get((string)exp.content);
                if (res == null)
                {
                    throw new Exception("var not found");
                }
                else return (Node)res;
            }
            else if (exp.type == Type.Pair && car(exp).type == Symbol)
            {
                bool b;
                Node res,e ;
                switch ((string)car(exp).content)
                {
                    case "quote":
                        return car(cdr(exp));
                    case "define":
                        res = Eval(car(cdr(cdr(exp))), env);
                        env.add((string)car(cdr(exp)).content, res);
                        return res;
                    case "and":
                        exp = cdr(exp);
                        e = new Node("true");
                        while (exp.type != Null)
                        {
                            res = Eval(car(exp), env);
                            if (eq0(res, new Node("false")))
                                return res;
                            e = res;
                            exp = cdr(exp);
                        }
                        return e;
                    case "or":
                        exp = cdr(exp);
                        e = new Node("false");
                        while (exp.type != Null)
                        {
                            res = Eval(car(exp), env);
                            if (!eq0(res, new Node("false")))
                                return res;
                            e = res;
                            exp = cdr(exp);
                        }
                        return e;
                    case "if":
                        b = (bool)Eval(car(cdr(exp)), env).content;
                        if (b)
                            return Eval(car(cdr(cdr(exp))), env);
                        else return Eval(car(cdr(cdr(cdr(exp)))), env);
                    case "cond":
                        exp = cdr(exp);
                        while (exp.type != Null)
                        {
                            bool b2 = (bool)Eval(car(car(exp)), env).content;
                            if (b2)
                                return Eval(car(cdr(car(exp))), env);
                            exp = cdr(exp);
                        }
                        return Node.getNull();


                }

            }

            return Node.getNull();
        }

    }
    public partial class UnitTest1
    {
        [TestMethod]
        public void TestEnv()
        {
            Env env = new Env();
            env.add("x", new Node("(4 5)"));
            env.add("y", new Node("5"));
            Assert.IsTrue(eq0(new Node("(4 5)"), (Node)env.get("x")));
            Assert.IsTrue(eq0(new Node("5"), (Node)env.get("y")));
            Env env1 = new Env(env);
            env1.add("y", new Node("6"));
            Assert.IsTrue(eq0(new Node("6"), (Node)env1.get("y")));
        }
        [TestMethod]
        public void TestEvalAndOr()
        {
            Env env = new Env();
            Assert.AreEqual("1", Interpreter.Eval("(and 2 1)"));
            Assert.AreEqual("true", Interpreter.Eval("(and)"));
            Assert.AreEqual("false", Interpreter.Eval("(and false 1)"));
            Assert.AreEqual("1", Interpreter.Eval("(or false 1)"));
            Assert.AreEqual("false", Interpreter.Eval("(or)"));
            Assert.AreEqual("1", Interpreter.Eval("(or 1 false)"));
        }
    }
}
