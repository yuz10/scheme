using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Scheme.Funs;
using static Scheme.Type;
namespace Scheme
{
    public class Env
    {
        public List<Dictionary<string, Node>> env = new List<Dictionary<string, Node>>();
        public Env()
        {
            env.Add(new Dictionary<string, Node>());
        }
        public Env(Env parent)
        {
            foreach (var i in parent.env)
            {
                env.Add(i);
            }
            env.Add(new Dictionary<string, Node>());
        }
        public void add(string key, Node value)
        {
            env.Last().Add(key, value);
        }
        public Node get(string key)
        {
            for (int i = env.Count - 1; i >= 0; i--)
            {
                if (env[i].ContainsKey(key)) { return env[i][key]; }
            }
            throw new Exception($"no variable {key}");
        }
    }
    public static class Interpreter
    {
 
        public static Node Eval(Node exp, Env env)
        {
            if (exp.type == Null || exp.type == Bool || exp.type == Number || exp.type == Type.String)
            {
                return exp;
            }
            else if (exp.type == Symbol)
            {
                return env.get((string)exp.content);
            }
            else if (exp.type == Type.Pair &&
                (bool)eq(car(exp), Parser.Parse("quote")).content)
            {
                return car(cdr(exp));
            }
            return Node.getNull();
        }

    }
}
