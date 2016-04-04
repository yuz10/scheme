using System;
using System.Collections.Generic;
using static Scheme.Funs;
using static Scheme.Type;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Scheme
{
    public class Env
    {
        private Env parent;
        public Dictionary<string, Node> env = new Dictionary<string, Node>();
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
        public void set(string key, Node n)
        {
            if (env.ContainsKey(key))
            {
                env[key] = n;
            }
            else
            {
                if (parent == null)
                    throw new Exception("no variable " + key);
                parent.set(key, n);
            }
        }
    }
    public class Lambda
    {
        public Env env;
        public Node param;
        public Node exp;
    }
    public delegate Node FunDelegate(List<Node>nList);
    public static class Interpreter
    {
        static void addNewFun(Env env, string name, FunDelegate fun)
        {
            env.add(name, new Node
            {
                type = Fun,
                content = fun
            });
        }
        delegate double accumDelegate(double a, double b);
        static FunDelegate accumulateFunc(accumDelegate fun)
        {
            return (nList) =>
            {
                double num = (double)nList[0].content;
                for (int i = 1; i < nList.Count; i++)
                {
                    num = fun(num, (double)nList[i].content);
                }
                return new Node(num);
            };
        }
        public static Env getBaseEnv()
        {
            Env env = new Env();
            addNewFun(env, "+", accumulateFunc((a, b) => a + b));
            addNewFun(env, "*", accumulateFunc((a, b) => a * b));
            addNewFun(env, "/", accumulateFunc((a, b) => a / b));
            addNewFun(env, "-", (nList) =>
            {
                double num = (double)nList[0].content;
                if (nList.Count == 1)
                {
                    num = -num;
                }
                else {
                    for (int i = 1; i < nList.Count; i++)
                    {
                        num = num - (double)nList[i].content;
                    }
                }
                return new Node(num);
            });
            addNewFun(env, ">", (x) => {
                Assert.IsTrue(x.Count == 2);
                return new Node((double)x[0].content > (double)x[1].content);
            });
            addNewFun(env, "<", (x) => {
                Assert.IsTrue(x.Count == 2);
                return new Node((double)x[0].content < (double)x[1].content);
            });
            addNewFun(env, "=", (x) => {
                Assert.IsTrue(x.Count == 2);
                return new Node((double)x[0].content == (double)x[1].content);
            });
            addNewFun(env, ">=", (x) => {
                Assert.IsTrue(x.Count == 2);
                return new Node((double)x[0].content >= (double)x[1].content);
            });
            addNewFun(env, "<=", (x) => {
                Assert.IsTrue(x.Count == 2);
                return new Node((double)x[0].content <= (double)x[1].content);
            });
            addNewFun(env, "car", (x) => { Assert.IsTrue(x.Count == 1); return car(x[0]); });
            addNewFun(env, "cdr", (x) => { Assert.IsTrue(x.Count == 1); return cdr(x[0]); }); 
            addNewFun(env, "cons", (x) => { Assert.IsTrue(x.Count == 2); return cons(x[0], x[1]); }); 
            addNewFun(env, "eq?", (x) => { Assert.IsTrue(x.Count == 2); return eq(x[0], x[1]); });

            return env;
        }
        public static string Eval(string code)
        {
            return Eval(code, getBaseEnv()).ToString();
        }
        public static string Eval(string code, Env env)
        {
            return Eval(new Node(code), env).ToString();
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
                    throw new Exception((string)exp.content + " not found");
                }
                else return (Node)res;
            }
            else if (exp.type == Type.Pair && car(exp).type == Symbol)
            {
                Node res, n;
                switch ((string)car(exp).content)
                {
                    case "quote":
                        return car(cdr(exp));
                    case "set!":
                        exp = cdr(exp);
                        env.set((string)car(exp).content, Eval(car(cdr(exp)), env));
                        return Node.getNull();
                    case "define":
                        if (car(cdr(exp)).type != Symbol)
                        {
                            throw new Exception("cannot define " + car(cdr(exp)).ToString());
                        }
                        string name = (string)car(cdr(exp)).content;
                        if (env.env.ContainsKey(name))
                        {
                            throw new Exception("cannot re-define " + car(cdr(exp)).ToString());
                        }
                        res = Eval(car(cdr(cdr(exp))), env);
                        env.add(name, res);
                        return res;
                    case "and":
                        exp = cdr(exp);
                        n = new Node(true);
                        while (exp.type != Null)
                        {
                            res = Eval(car(exp), env);
                            if (eq0(res, new Node(false)))
                                return res;
                            n = res;
                            exp = cdr(exp);
                        }
                        return n;
                    case "or":
                        exp = cdr(exp);
                        n = new Node(false);
                        while (exp.type != Null)
                        {
                            res = Eval(car(exp), env);
                            if (!eq0(res, new Node(false)))
                                return res;
                            n = res;
                            exp = cdr(exp);
                        }
                        return n;
                    case "if":
                        res = Eval(car(cdr(exp)), env);
                        if (!eq0(res, new Node(false)))
                            return Eval(car(cdr(cdr(exp))), env);
                        else return Eval(car(cdr(cdr(cdr(exp)))), env);
                    case "begin":
                        exp = cdr(exp);
                        Env e = env;
                        n = Node.getNull();
                        while (exp.type != Null)
                        {
                            e = new Env(e);
                            n = Eval(car(exp), e);
                            exp = cdr(exp);
                        }
                        return n;
                    case "cond":
                        exp = cdr(exp);
                        while (exp.type != Null)
                        {
                            res = Eval(car(car(exp)), env);
                            if (!eq0(res, new Node(false)))
                                return Eval(car(cdr(car(exp))), env);
                            exp = cdr(exp);
                        }
                        return Node.getNull();
                    case "lambda":
                        return new Node
                        {
                            type = Type.Lambda,
                            content = new Lambda { env = env, param = car(cdr(exp)), exp = cons(new Node("begin"), cdr(cdr(exp))) }
                        };
                    case "eval":
                        return Eval(Eval(car(cdr(exp)), env), env);
                    default:
                        return Apply(exp, env);
                }
            }
            else if (exp.type == Type.Pair)
            {
                return Apply(exp, env);
            }
            return exp;
        }
        static Node Apply(Node exp, Env env)
        {
            Node fun = Eval(car(exp), env);
            exp = cdr(exp);
            if (fun.type == Type.Lambda)
            {
                Lambda lambda = (Lambda)fun.content;
                Node param = lambda.param;
                Env env1 = new Env(lambda.env);
                while (param.type != Null)
                {
                    if (param.type == Type.Pair)
                    {
                        env1.add((string)car(param).content, Eval(car(exp), env));
                        param = cdr(param);
                        exp = cdr(exp);
                    }
                    else
                    {
                        env1.add((string)param.content, exp);
                        break;
                    }
                }
                return Eval(lambda.exp, env1);
            }
            else if (fun.type == Fun)
            {
                List<Node> nList = new List<Node>();
                while (exp.type != Null)
                {
                    nList.Add(Eval(car(exp), env));
                    exp = cdr(exp);
                }
                return ((FunDelegate)fun.content)(nList);
            }
            else
            {
                throw new Exception(fun.ToString() + " is not a function");
            }
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
            Assert.AreEqual("(4 5)", env.get("x").ToString());
            Assert.AreEqual("5", env.get("y").ToString());
            Env env1 = new Env(env);
            env1.add("y", new Node("6"));
            Assert.AreEqual("6", env1.get("y").ToString());
        }
        [TestMethod]
        public void TestEvalQuote()
        {
            Assert.AreEqual("(quote a)", Interpreter.Eval("''a"));
        }
        [TestMethod]
        public void TestAssignment()
        {
            Env env = Interpreter.getBaseEnv();
            Interpreter.Eval("(define x 1)", env);
            Interpreter.Eval("(set! x '(2 . 3))", env);
            Assert.AreEqual("(2 . 3)", env.get("x").ToString());

        }
        [TestMethod]
        public void TestEvalAndOr()
        {
            Assert.AreEqual("1", Interpreter.Eval("(and 2 1)"));
            Assert.AreEqual("true", Interpreter.Eval("(and)"));
            Assert.AreEqual("false", Interpreter.Eval("(and false 1)"));
            Assert.AreEqual("1", Interpreter.Eval("(or false 1)"));
            Assert.AreEqual("false", Interpreter.Eval("(or)"));
            Assert.AreEqual("2", Interpreter.Eval("(or (quote 2) false)"));
        }
        [TestMethod]
        public void TestEvalIfCond()
        {
            Assert.AreEqual("1", Interpreter.Eval("(if 1 1 2)"));
            Assert.AreEqual("2", Interpreter.Eval("(if false 1 2)"));
            Assert.AreEqual("4", Interpreter.Eval("(cond (false 1) (3 4))"));
        }
        [TestMethod]
        public void TestEvalDefine()
        {
            Env env = Interpreter.getBaseEnv();
            Interpreter.Eval(new Node("(define y (begin (define x 2) (* x x)))"), env);
            Interpreter.Eval(new Node("(define x '(1 2))"), env);
            Assert.AreEqual("4", env.get("y").ToString());
            Assert.AreEqual("(1 2)", env.get("x").ToString());
        }
        [TestMethod]
        public void TestEvalFun()
        {
            Env env = Interpreter.getBaseEnv();
            Assert.AreEqual("3", Interpreter.Eval("(+ 1 2)", env));
            Assert.AreEqual("-1", Interpreter.Eval("(- 1 2)", env));
            Assert.AreEqual("-1", Interpreter.Eval("(- 1)", env));
            Assert.AreEqual("1", Interpreter.Eval("(car '(1 2))", env));
            Assert.AreEqual("2", Interpreter.Eval("(cdr '(1 . 2))", env));
            Assert.AreEqual("(2 3)", Interpreter.Eval("(cdr '(1 2 3))", env));
            Assert.AreEqual("true", Interpreter.Eval("(eq? (+ 1 3) 4)", env));
        }
        [TestMethod]
        public void TestEvalLambda()
        {
            Env env = Interpreter.getBaseEnv();
            Interpreter.Eval(new Node("(define 1+ (lambda (x) (+ 1 x)))"), env);
            Assert.AreEqual("3", Interpreter.Eval("(1+ 2)", env));
            Interpreter.Eval(new Node("(define fact (lambda (x) (if (= x 1) 1 (* x (fact (- x 1))))))"), env);
            Assert.AreEqual("6", Interpreter.Eval("(fact 3)", env));
            Assert.AreEqual("2", Interpreter.Eval("((lambda (x) (+ x 1)) 1)", env));
            Assert.AreEqual("3", Interpreter.Eval("(((lambda (x) (lambda (y) (+ x y))) 1) 2)", env));
            Assert.AreEqual("4", Interpreter.Eval("((lambda (x) (define y 3) (+ x y)) 1)", env));
        }
        [TestMethod]
        public void TestEvalEval()
        {
            Assert.AreEqual("3", Interpreter.Eval("(eval '(+ 1 2))"));
            Assert.AreEqual("(+ 1 2)", Interpreter.Eval("(eval ''(+ 1 2))"));
        }
    }
}
