using System;
using System.Collections.Generic;
using static Scheme.Funs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Scheme
{
    public class Env
    {
        public Env parent;
        public Dictionary<string, SObject> env = new Dictionary<string, SObject>();
        public Env()
        {
            parent = null;
            env = new Dictionary<string, SObject>();
        }
        public Env(Env parent)
        {
            this.parent = parent;
            env = new Dictionary<string, SObject>();
        }
        public void add(string key, SObject value)
        {
            env.Add(key, value);
        }
        public SObject get(string key)
        {
            if (env.ContainsKey(key))
            {
                return env[key];
            }
            else
            {
                if (parent == null)
                    throw new Exception("no variable " + key);
                return parent.get(key);
            }
        }
        public void set(string key, SObject n)
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
        public SObject param;
        public SObject exp;
    }
    public delegate SObject FunDelegate(List<SObject> nList);
    public partial struct SObject
    {

        static void addNewFun(Env env, string name, FunDelegate fun)
        {
            env.add(name, new SObject
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
                return new SObject(num);
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
                return new SObject(num);
            });
            addNewFun(env, ">", (x) =>
            {
                Assert.IsTrue(x.Count == 2);
                return new SObject((double)x[0].content > (double)x[1].content);
            });
            addNewFun(env, "<", (x) =>
            {
                Assert.IsTrue(x.Count == 2);
                return new SObject((double)x[0].content < (double)x[1].content);
            });
            addNewFun(env, "=", (x) =>
            {
                Assert.IsTrue(x.Count == 2);
                return new SObject((double)x[0].content == (double)x[1].content);
            });
            addNewFun(env, ">=", (x) =>
            {
                Assert.IsTrue(x.Count == 2);
                return new SObject((double)x[0].content >= (double)x[1].content);
            });
            addNewFun(env, "<=", (x) =>
            {
                Assert.IsTrue(x.Count == 2);
                return new SObject((double)x[0].content <= (double)x[1].content);
            });
            addNewFun(env, "begin", (x) => { return x[x.Count - 1]; });
            addNewFun(env, "car", (x) => { Assert.IsTrue(x.Count == 1); return car(x[0]); });
            addNewFun(env, "cdr", (x) => { Assert.IsTrue(x.Count == 1); return cdr(x[0]); });
            addNewFun(env, "cons", (x) => { Assert.IsTrue(x.Count == 2); return cons(x[0], x[1]); });
            addNewFun(env, "eq?", (x) => { Assert.IsTrue(x.Count == 2); return eq(x[0], x[1]); });
            addNewFun(env, "null?", (x) => { Assert.IsTrue(x.Count == 1); return new SObject(x[0].type == Type.Null); });
            new SObject("(define apply (lambda (op x) (eval (cons op x))))").eval(env);
            new SObject("(define u-map (lambda (op x) (if (null? x) null (cons (op (car x)) (u-map op (cdr x))))))").eval(env);
            new SObject(@"(define map (lambda (fn p . q) 
                (if (null? p) null 
                    (cons (apply fn (cons (car p) (u-map car q))) 
                        (apply map (cons fn (cons (cdr p) (u-map cdr q))))))))").eval(env);
            return env;
        }
        public SObject eval(Env env)
        {
            try
            {
                if (this.type == Type.Symbol)
                {
                    return env.get((string)content);
                }
                else if (this.type == Type.Pair && car(this).type == Type.Symbol)
                {
                    SObject res, n;
                    SObject d = cdr(this);
                    switch ((string)car(this).content)
                    {
                        case "quote":
                            return car(d);
                        case "set!":
                            env.set((string)car(d).content, car(cdr(d)).eval(env));
                            return SObject.getNull();
                        case "define":
                            if (car(d).type != Type.Symbol)
                            {
                                throw new Exception("cannot define " + car(d).ToString());
                            }
                            string name = (string)car(d).content;
                            if (env.env.ContainsKey(name))
                            {
                                throw new Exception("cannot re-define " + car(d).ToString());
                            }
                            res = car(cdr(d)).eval(env);
                            env.add(name, res);
                            return res;
                        case "and":
                            n = new SObject(true);
                            while (d.type != Type.Null)
                            {
                                res = car(d).eval(env);
                                if (eq0(res, new SObject(false)))
                                    return res;
                                n = res;
                                d = cdr(d);
                            }
                            return n;
                        case "or":
                            n = new SObject(false);
                            while (d.type != Type.Null)
                            {
                                res = car(d).eval(env);
                                if (!eq0(res, new SObject(false)))
                                    return res;
                                n = res;
                                d = cdr(d);
                            }
                            return n;
                        case "if":
                            res = car(d).eval(env);
                            if (!eq0(res, new SObject(false)))
                                return car(cdr(d)).eval(env);
                            else return car(cdr(cdr(d))).eval(env);
                        case "cond":
                            while (d.type != Type.Null)
                            {
                                res = car(car(d)).eval(env);
                                if (!eq0(res, new SObject(false)))
                                    return car(cdr(car(d))).eval(env);
                                d = cdr(d);
                            }
                            return SObject.getNull();
                        case "lambda":
                            SObject exp = cdr(d);
                            if (cdr(exp).type != Type.Null)
                            {
                                exp = cons(new SObject("begin"), exp);
                            }
                            else
                            {
                                exp = car(exp);
                            }
                            return new SObject
                            {
                                type = Type.Lambda,
                                content = new Lambda { env = env, param = car(d), exp = exp }
                            };
                        case "eval":
                            return car(d).eval(env).eval(env);
                        default:
                            return this.apply(env);
                    }
                }
                else if (this.type == Type.Pair)
                {
                    return this.apply(env);
                }
                return this;
            }
            catch (Exception e)
            {
                string s = "";
                if (env.parent != null)
                {
                    s = " WHERE";
                    foreach (var item in env.env)
                    {
                        s += " " + item.Key + "=" + item.Value;
                    }
                }
                throw new Exception(e.Message + "\nIN " + this.ToString() + s);
            }
        }
        SObject apply(Env env)
        {
            SObject fun = car(this).eval(env);
            SObject d = cdr(this);
            if (fun.type == Type.Lambda)
            {
                Lambda lambda = (Lambda)fun.content;
                SObject param = lambda.param;
                Env env1 = new Env(lambda.env);

                while (param.type != Type.Null)
                {
                    if (param.type == Type.Pair)
                    {
                        env1.add((string)car(param).content, car(d).eval(env));
                        param = cdr(param);
                        d = cdr(d);
                    }
                    else
                    {
                        List<SObject> evalExp = new List<SObject>();
                        while (d.type != Type.Null)
                        {
                            evalExp.Add(car(d).eval(env));
                            d = cdr(d);
                        }
                        d = getNull();
                        for (int i = evalExp.Count - 1; i >= 0; i--)
                        {
                            d = cons(evalExp[i], d);
                        }
                        env1.add((string)param.content, d);
                        break;
                    }
                }
                return lambda.exp.eval(env1);
            }
            else if (fun.type == Type.Fun)
            {
                List<SObject> nList = new List<SObject>();
                while (d.type != Type.Null)
                {
                    nList.Add(car(d).eval(env));
                    d = cdr(d);
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
            env.add("x", new SObject("(4 5)"));
            env.add("y", new SObject("5"));
            Assert.AreEqual("(4 5)", env.get("x").ToString());
            Assert.AreEqual("5", env.get("y").ToString());
            Env env1 = new Env(env);
            env1.add("y", new SObject("6"));
            Assert.AreEqual("6", env1.get("y").ToString());
        }
        [TestMethod]
        public void TestAssignment()
        {
            Env env = SObject.getBaseEnv();
            new SObject("(define x 1)").eval(env);
            new SObject("(set! x '(2 . 3))").eval(env);
            Assert.AreEqual("(2 . 3)", env.get("x").ToString());
        }
        [TestMethod]
        public void TestEvalAndOr()
        {
            Env env = SObject.getBaseEnv();
            Assert.AreEqual("1", new SObject("(and 2 1)").eval(env).ToString());
            Assert.AreEqual("true", new SObject("(and)").eval(env).ToString());
            Assert.AreEqual("false", new SObject("(and false 1)").eval(env).ToString());
            Assert.AreEqual("1", new SObject("(or false 1)").eval(env).ToString());
            Assert.AreEqual("false", new SObject("(or)").eval(env).ToString());
            Assert.AreEqual("2", new SObject("(or (quote 2) false)").eval(env).ToString());
        }
        [TestMethod]
        public void TestEvalIfCond()
        {
            Env env = SObject.getBaseEnv();
            Assert.AreEqual("1", new SObject("(if 1 1 2)").eval(env).ToString());
            Assert.AreEqual("2", new SObject("(if false 1 2)").eval(env).ToString());
            Assert.AreEqual("4", new SObject("(cond (false 1) (3 4))").eval(env).ToString());
        }
        [TestMethod]
        public void TestEvalDefine()
        {
            Env env = SObject.getBaseEnv();
            new SObject("(define y (begin (define x 2) (* x x)))").eval(env);
            Assert.AreEqual("4", env.get("y").ToString());
            Assert.AreEqual("2", env.get("x").ToString());
        }
        [TestMethod]
        public void TestEvalFun()
        {
            Env env = SObject.getBaseEnv();
            Assert.AreEqual("3", new SObject("(+ 1 2)").eval(env).ToString());
            Assert.AreEqual("-1", new SObject("(- 1 2)").eval(env).ToString());
            Assert.AreEqual("-1", new SObject("(- 1)").eval(env).ToString());
            Assert.AreEqual("1", new SObject("(car '(1 2))").eval(env).ToString());
            Assert.AreEqual("2", new SObject("(cdr '(1 . 2))").eval(env).ToString());
            Assert.AreEqual("(2 3)", new SObject("(cdr '(1 2 3))").eval(env).ToString());
            Assert.AreEqual("8", new SObject("(apply + '((+ 1 2) 2 3))").eval(env).ToString());
            Assert.AreEqual("true", new SObject("(eq? (+ 1 3) 4)").eval(env).ToString());
            Assert.AreEqual("true", new SObject("(eq? 'a 'a)").eval(env).ToString());
            Assert.AreEqual("true", new SObject("(eq? \"123\" \"123\")").eval(env).ToString());
            Assert.AreEqual("(2 3)", new SObject("(u-map (lambda (x) (+ 1 x)) '(1 2))").eval(env).ToString());
            Assert.AreEqual("(1 2)", new SObject("(u-map - (u-map - '(1 2)))").eval(env).ToString());
            Assert.AreEqual("(-1 -2 -3)", new SObject("(map - '(1 2 3)))").eval(env).ToString());
        }
        [TestMethod]
        public void TestEvalLambda()
        {
            Env env = SObject.getBaseEnv();
            new SObject("(define 1+ (lambda (x) (+ 1 x)))").eval(env);
            Assert.AreEqual("3", new SObject("(1+ 2)").eval(env).ToString());
            new SObject("(define fact (lambda (x) (if (= x 1) 1 (* x (fact (- x 1))))))").eval(env);
            Assert.AreEqual("6", new SObject("(fact 3)").eval(env).ToString());
            Assert.AreEqual("2", new SObject("((lambda (x) (+ x 1)) 1)").eval(env).ToString());
            Assert.AreEqual("3", new SObject("(((lambda (x) (lambda (y) (+ x y))) 1) 2)").eval(env).ToString());
            Assert.AreEqual("4", new SObject("((lambda (x) (define y 3) (+ x y)) 1)").eval(env).ToString());
            new SObject("(define sum-of-square (lambda x (if (null? x) 0 (+ (* (car x) (car x)) (apply sum-of-square (cdr x))))))").eval(env);
            Assert.AreEqual("30", new SObject("(sum-of-square 1 2 3 4)").eval(env).ToString());
            Assert.AreEqual("29", new SObject("(sum-of-square (+ 1 3) 2 3)").eval(env).ToString());
        }
        [TestMethod]
        public void TestEvalEval()
        {
            Env env = SObject.getBaseEnv();
            Assert.AreEqual("3", new SObject("(eval '(+ 1 2))").eval(env).ToString());
            Assert.AreEqual("(+ 1 2)", new SObject("(eval ''(+ 1 2))").eval(env).ToString());
        }
    }
}
