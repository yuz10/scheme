using System;
using System.Collections.Generic;
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
                if (parent == null)
                    return null;
                return parent.get(key);
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
                type = Type.Fun,
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
            addNewFun(env, "begin", (x) => { return x[x.Count - 1]; });
            addNewFun(env, "car", (x) => { Assert.IsTrue(x.Count == 1); return Funs.car(x[0]); });
            addNewFun(env, "cdr", (x) => { Assert.IsTrue(x.Count == 1); return Funs.cdr(x[0]); });
            addNewFun(env, "cons", (x) => { Assert.IsTrue(x.Count == 2); return Funs.cons(x[0], x[1]); });
            addNewFun(env, "eq?", (x) => { Assert.IsTrue(x.Count == 2); return Funs.eq(x[0], x[1]); });
            addNewFun(env, "null?", (x) => { Assert.IsTrue(x.Count == 1); return new Node(x[0].type == Type.Null); });
            Eval("(define apply (lambda (op x) (eval (cons op x))))", env);
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
            if (exp.type == Type.Null || exp.type == Type.Bool || exp.type == Type.Number || exp.type == Type.String)
            {
                return exp;
            }
            else if (exp.type == Type.Symbol)
            {
                Node? res = env.get((string)exp.content);
                if (res == null)
                {
                    throw new Exception((string)exp.content + " not found");
                }
                else return (Node)res;
            }
            else if (exp.type == Type.Pair && Funs.car(exp).type == Type.Symbol)
            {
                Node res, n;
                switch ((string)Funs.car(exp).content)
                {
                    case "quote":
                        return Funs.car(Funs.cdr(exp));
                    case "set!":
                        exp = Funs.cdr(exp);
                        env.set((string)Funs.car(exp).content, Eval(Funs.car(Funs.cdr(exp)), env));
                        return Node.getNull();
                    case "define":
                        if (Funs.car(Funs.cdr(exp)).type != Type.Symbol)
                        {
                            throw new Exception("cannot define " + Funs.car(Funs.cdr(exp)).ToString());
                        }
                        string name = (string)Funs.car(Funs.cdr(exp)).content;
                        if (env.env.ContainsKey(name))
                        {
                            throw new Exception("cannot re-define " + Funs.car(Funs.cdr(exp)).ToString());
                        }
                        res = Eval(Funs.car(Funs.cdr(Funs.cdr(exp))), env);
                        env.add(name, res);
                        return res;
                    case "and":
                        exp = Funs.cdr(exp);
                        n = new Node(true);
                        while (exp.type != Type.Null)
                        {
                            res = Eval(Funs.car(exp), env);
                            if (Funs.eq0(res, new Node(false)))
                                return res;
                            n = res;
                            exp = Funs.cdr(exp);
                        }
                        return n;
                    case "or":
                        exp = Funs.cdr(exp);
                        n = new Node(false);
                        while (exp.type != Type.Null)
                        {
                            res = Eval(Funs.car(exp), env);
                            if (!Funs.eq0(res, new Node(false)))
                                return res;
                            n = res;
                            exp = Funs.cdr(exp);
                        }
                        return n;
                    case "if":
                        res = Eval(Funs.car(Funs.cdr(exp)), env);
                        if (!Funs.eq0(res, new Node(false)))
                            return Eval(Funs.car(Funs.cdr(Funs.cdr(exp))), env);
                        else return Eval(Funs.car(Funs.cdr(Funs.cdr(Funs.cdr(exp)))), env);
                    case "cond":
                        exp = Funs.cdr(exp);
                        while (exp.type != Type.Null)
                        {
                            res = Eval(Funs.car(Funs.car(exp)), env);
                            if (!Funs.eq0(res, new Node(false)))
                                return Eval(Funs.car(Funs.cdr(Funs.car(exp))), env);
                            exp = Funs.cdr(exp);
                        }
                        return Node.getNull();
                    case "lambda":
                        return new Node
                        {
                            type = Type.Lambda,
                            content = new Lambda { env = env, param = Funs.car(Funs.cdr(exp)), exp = Funs.cons(new Node("begin"), Funs.cdr(Funs.cdr(exp))) }
                        };
                    case "eval":
                        return Eval(Eval(Funs.car(Funs.cdr(exp)), env), env);
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
            Node fun = Eval(Funs.car(exp), env);
            exp = Funs.cdr(exp);
            if (fun.type == Type.Lambda)
            {
                Lambda lambda = (Lambda)fun.content;
                Node param = lambda.param;
                Env env1 = new Env(lambda.env);
                while (param.type != Type.Null)
                {
                    if (param.type == Type.Pair)
                    {
                        env1.add((string)Funs.car(param).content, Eval(Funs.car(exp), env));
                        param = Funs.cdr(param);
                        exp = Funs.cdr(exp);
                    }
                    else
                    {
                        env1.add((string)param.content, exp);
                        break;
                    }
                }
                return Eval(lambda.exp, env1);
            }
            else if (fun.type == Type.Fun)
            {
                List<Node> nList = new List<Node>();
                while (exp.type != Type.Null)
                {
                    nList.Add(Eval(Funs.car(exp), env));
                    exp = Funs.cdr(exp);
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
            Assert.AreEqual("4", env.get("y").ToString());
            Assert.AreEqual("2", env.get("x").ToString());
        }
        [TestMethod]
        public void TestEvalFun()
        {
            Assert.AreEqual("3", Interpreter.Eval("(+ 1 2)"));
            Assert.AreEqual("-1", Interpreter.Eval("(- 1 2)"));
            Assert.AreEqual("-1", Interpreter.Eval("(- 1)"));
            Assert.AreEqual("1", Interpreter.Eval("(car '(1 2))"));
            Assert.AreEqual("2", Interpreter.Eval("(cdr '(1 . 2))"));
            Assert.AreEqual("(2 3)", Interpreter.Eval("(cdr '(1 2 3))"));
            Assert.AreEqual("6", Interpreter.Eval("(apply + '(1 2 3))"));
            Assert.AreEqual("true", Interpreter.Eval("(eq? (+ 1 3) 4)"));
            Assert.AreEqual("true", Interpreter.Eval("(eq? 'a 'a)"));
            Assert.AreEqual("true", Interpreter.Eval("(eq? \"123\" \"123\")"));
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
            Interpreter.Eval("(define sum-of-square (lambda x (if (null? x) 0 (+ (* (car x) (car x)) (apply sum-of-square (cdr x))))))", env);
            Assert.AreEqual("14", Interpreter.Eval("(sum-of-square 1 2 3)", env));
            Assert.AreEqual("30", Interpreter.Eval("(sum-of-square 1 2 3 4)", env));
        }
        [TestMethod]
        public void TestEvalEval()
        {
            Assert.AreEqual("3", Interpreter.Eval("(eval '(+ 1 2))"));
            Assert.AreEqual("(+ 1 2)", Interpreter.Eval("(eval ''(+ 1 2))"));
        }
    }
}
