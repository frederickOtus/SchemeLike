using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DerpScheme
{
    class Environment
    {
        //track Name/Value pairs, also has heiracrhy
        private Environment parent;
        private Dictionary<string, SExpression> store;

        public Environment(Environment p) { parent = p; store = new Dictionary<string, SExpression>(); }
        public Environment() { store = new Dictionary<string, SExpression>(); }

        public void addVal(string id, SExpression val)
        {
            if (store.Keys.Contains(id))
                throw new Exception(String.Format("Id {0} already exists", id));

            store[id] = val;
        }

        public void setVal(string id, SExpression val)
        {
            if (store.Keys.Contains(id))
                store[id] = val;

            if(parent == null)
                throw new Exception(String.Format("ID {0} does not exist", id));

            parent.setVal(id, val);
        }

        public void setLocalVal(string id, SExpression val)
        {
            if (store.Keys.Contains(id))
                store[id] = val;
            else
                throw new Exception(String.Format("ID {0} does not exist", id));
        }

        public bool hasParent() { return parent != null; }
        public SExpression lookup(SID id) {
            if (store.Keys.Contains(id.identifier))
                return store[id.identifier];

            if (parent != null)
                return parent.lookup(id);
            else
                throw new Exception(String.Format("ID {0} does not exist", id));
        }
    }

    class DerpInterpreter
    {
        Environment e;
        public DerpParser parser;

        public static void Main()
        {
            DerpInterpreter interp = new DerpInterpreter();

            Console.WriteLine("Welcome to the Derpiter!");
            while (true)
            {
                if (interp.parser.isDone())
                    Console.Write("> ");
                else
                    Console.Write("\t");
                string input = Console.ReadLine();
                if (input == "exit")
                    return;
               string outs = interp.interpret(input);
                if (outs == null)
                    continue;
                Console.WriteLine(outs);
            }
        }

        public DerpInterpreter()  {
            e = new Environment();
            parser = new DerpParser("");

            //create and load primitive functions
            Func plus = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count == 0)
                    new SInt(0);

                int rval = 0;
                foreach (SExpression s in args)
                {
                    SExpression tmp = evaluate(s, e);
                    rval += ((SInt)tmp).val;
                }
                return new SInt(rval);
            };
            Func sub = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count == 0)
                    return new SInt(0);
                int rval = ((SInt)evaluate(args[0], e)).val;

                for(int i = 1; i < args.Count; i++)
                {
                    SExpression tmp = evaluate(args[i], e);
                    rval -= ((SInt)tmp).val;
                }
                return new SInt(rval);
            };
            Func mult = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count == 0)
                    return new SInt(1);
                int rval = 1;
                foreach (SExpression s in args)
                {
                    SExpression tmp = evaluate(s, e);
                    rval *= ((SInt)tmp).val;
                }
                return new SInt(rval);
            };
            Func ifExpr = delegate (List<SExpression> args, Environment e)
            {
                SExpression test = evaluate(args[0], e);

                if (args.Count != 3 || !(test is SBool))
                    throw new Exception("Bad args");
                if (((SBool)test).val)
                    return args[1];
                else
                    return args[2];
            };
            Func let = delegate (List<SExpression> args, Environment e)
            {
                Environment local = new Environment(e);
                SPair nameBindings = (SPair)args[0];
                if (!nameBindings.isProperList())
                    throw new Exception("Can't use improper list in a let");

                List<SExpression> names = nameBindings.flatten();
                for(int i=0; i<names.Count - 1; i++) {
                    String name = ((SID)((SPair)names[i]).getHead()).identifier;
                    SExpression val = ((SPair)((SPair)names[i]).getTail()).getHead();
                    local.addVal(name, evaluate(val, e));
                }
                return evaluate(args[1], local);
            };
            Func define = delegate(List<SExpression> args, Environment e)
            {
                if (e.hasParent())
                    throw new Exception("Define only allowed at global scope");

                SID name = (SID)args[0];
                SExpression rval = evaluate(args[1], e);
                e.addVal(name.identifier, rval);

                return new SNone();
            };
            Func lambda = delegate (List<SExpression> args, Environment e)
            {
                SExpression body = args[1];
                if (args[0] is SID) //If arg 0 is a single SID, that means this func takes a variable # of args, and thus will have a single name for the list of args
                {
                    return new SFunc((SID)args[0], body, e);
                }

                //otherwise, build the list of names and pass it off to the other constructor
                List<SExpression> nameList = ((SPair)args[0]).flatten();
                List<SID> names = new List<SID>();
                for(int i = 0; i <  nameList.Count - 1; i++)
                {
                    names.Add((SID)nameList[i]);
                }

                return new SFunc(names, body, e);
            };
            Func div = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count != 2)
                    throw new Exception(String.Format("Expected 2 args, got {0}", args.Count));
                SExpression a = evaluate(args[0], e);
                SExpression b = evaluate(args[1], e);
                int rval = ((SInt)a).val / ((SInt)b).val;
                return new SInt(rval);
            };
            Func mod = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count != 2)
                    throw new Exception(String.Format("Expected 2 args, got {0}", args.Count));
                SExpression a = evaluate(args[0], e);
                SExpression b = evaluate(args[1], e);
                int rval = ((SInt)a).val % ((SInt)b).val;
                return new SInt(rval);
            };
            Func debug = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count != 1)
                    throw new Exception(String.Format("Expected 1 args, got {0}", args.Count));
                SExpression a = evaluate(args[0], e);
                Console.WriteLine("DB: " + a.DebugString());
                return new SNone();
            };

            Func cons = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count != 2)
                    throw new Exception(String.Format("Expected 2 args, got {0}", args.Count));

                SExpression a = evaluate(args[0], e);
                SExpression b = evaluate(args[1], e);

                return new SPair(a, b);
            };
            Func car = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count != 1)
                    throw new Exception(String.Format("Expected 1 args, got {0}", args.Count));

                SExpression a = evaluate(args[0], e);

                if (a is SPair)
                {
                    SPair al = (SPair)a;
                    return (SExpression)al.getHead().Clone();
                }
                else
                {
                    throw new Exception("car expects a list!");
                }
            };
            Func cdr = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count != 1)
                    throw new Exception(String.Format("Expected 1 args, got {0}", args.Count));

                SExpression a = evaluate(args[0], e);

                if (a is SPair)
                {
                    return (SExpression)((SPair)a).getTail().Clone();
                }
                else
                {
                    throw new Exception("cdr expects a list!");
                }
            };
            Func list = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count == 0)
                    return new SEmptyList();
                List<SExpression> elms = new List<SExpression>();                
                foreach (SExpression s in args)
                {
                    elms.Add(evaluate(s, e));    
                }
                return new SPair(elms, true);
            };
            Func nulllist = delegate (List<SExpression> args, Environment e)
            {
                if (args.Count != 1)
                    throw new Exception(String.Format("Expected 1 args, got {0}", args.Count));

                SExpression a = evaluate(args[0], e);
                return new SBool(a is SEmptyList);
            };

            e.addVal("+", new SPrimitive(plus));
            e.addVal("*", new SPrimitive(mult));
            e.addVal("/", new SPrimitive(div));
            e.addVal("-", new SPrimitive(sub));
            e.addVal("mod", new SPrimitive(mod));
            e.addVal("if", new SPrimitive(ifExpr));
            e.addVal("let", new SPrimitive(let));
            e.addVal("define", new SPrimitive(define));
            e.addVal("lambda", new SPrimitive(lambda));
            e.addVal("debug", new SPrimitive(debug));
            e.addVal("list", new SPrimitive(list));
            e.addVal("cons", new SPrimitive(cons));
            e.addVal("car", new SPrimitive(car));
            e.addVal("cdr", new SPrimitive(cdr));
            e.addVal("null?", new SPrimitive(nulllist));

        }

        public void addPrimative(string name, SPrimitive prim) { e.addVal(name, prim); }

        public static SExpression evaluate(SExpression expr, Environment e)
        {
            if (expr is SID)
            {
                return e.lookup((SID)expr);
            }

            if (!(expr is SPair))
                return expr;

            SPair exprL = (SPair)expr;

            if (!exprL.isProperList())
                throw new Exception("Not a proper list!");

            //if head is a SID, lookup val, insure it is callable, then pass control to it
            List<SExpression> elms = exprL.flatten();
            SExpression head = evaluate(elms[0], e);
            
            if (!(head is SApplicable))
                throw new Exception("SExpression not applicable!");

            //args are going to be body. But because this is a proper list, the last element is going to be a empty list we want to drop
            elms.RemoveAt(0); //drop head
            elms.RemoveAt(elms.Count - 1); // remove empty list at end

            return ((SApplicable)head).apply(elms, e);
        }

        public string interpret(String text)
        {
            try {
                parser.AddTokens(text);
                if (parser.attemptParse())
                {
                    List<SExpression> ptree = parser.flushParseTree();
                    string res = "";
                    foreach (SExpression sxp in ptree)
                        res += DerpInterpreter.evaluate(sxp, e).ToString() + "\n";
                    return res;
                }
                return null;
            }catch(Exception e)
            {
                parser.flushParseTree();
                return "Error: " + e.Message;
            }
        }
    }
}
