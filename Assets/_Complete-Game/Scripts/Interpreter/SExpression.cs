using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DerpScheme
{
    /*
        TODO:
            -Not tracking original tokens, needed for stack traces?
    */

    delegate SExpression Func(List<SExpression> args, Environment e);// Define type 'Func', anonymous function that operates on a list of args in an environment and produces an SExpression

    abstract class SExpression : ICloneable
    {
        public Token originalToken;

        public abstract object Clone();
        abstract public string DebugString();
    }
    abstract class SAtomic : SExpression { }
    abstract class SApplicable : SAtomic
    {
        abstract public SExpression apply(List<SExpression> args, Environment e);
    }

    class SPrimitive : SApplicable // SPrimitive is a function defined in C# by the interpreter
    {
        Func f;
        public SPrimitive(Func d) { f = d; }

        override public SExpression apply(List<SExpression> args, Environment e)
        {
            return f(args, e);
        }

        public override string ToString() { return "#primative"; }
        public override string DebugString() { return "<PRIMATIVE>";  }

        public override object Clone()
        {
            return new SPrimitive(f);
        }
    }

    class SFunc : SApplicable //SFuncs are lambda functions
    {
        private SID arglist;
        private Environment env; //private environment for sweet, sweet closures
        private List<SID> names; //names bound in definition
        private SExpression body; //body of execution

        public SFunc(List<SID> names, SExpression body, Environment e)
        {
            //our starting environment is the env we were defined in
            arglist = null;
            env = new Environment(e);
            foreach(SID id in names)
            {
                env.addVal(id.identifier, new SNone()); //add bound variables to environment with default None value
            }
            this.names = names;
            this.body = body;
        }

        public SFunc(SID arglst, SExpression body, Environment e)
        {
            arglist = arglst;
            names = null;

            //our starting environment is the env we were defined in
            env = new Environment(e);
            env.addVal(arglist.identifier, new SNone()); //add bound variables to environment with default None value
            
            this.body = body;
        }

        override public SExpression apply(List<SExpression> args, Environment e)
        {
            //There are two styles of arguments:
            //      -names for all args
            //      -single name, all args are a list

            //if arglist is null, verify number of args, eval them, and add them to the environment
            if (arglist == null)
            {
                if (args.Count != names.Count)
                    throw new Exception("Incorrect number of args");
                for (int i = 0; i < args.Count; i++)
                {
                    //evaluate all of the args and bind them to my local env according to their cooresponding values
                    env.setLocalVal(names[i].identifier, DerpScheme.DerpInterpreter.evaluate(args[i], e));
                }
            }
            else //otherwise, convert our args to an SPair and bind them to environment
            {
                SPair argSList = new SPair(args, true);
                env.setLocalVal(arglist.identifier, argSList);
            }

            //finally actually execute body
            return DerpScheme.DerpInterpreter.evaluate(body, env);
        }

        public override string ToString() { return "#procedure"; }
        public override string DebugString() {
            string rval = "<FUNC: ";
            if (arglist == null) //multiple arg names
            {
                for(int i = 0; i < names.Count; i++)
                {
                    if (i == 0)
                        rval += names[0].identifier;
                    else
                        rval += names[i].identifier;
                }
            }
            else
            {
                rval += "(" + arglist.identifier + ")";
            }

            rval += " | " + body.DebugString();
            return rval + ">";
        }

        public override object Clone()
        {
            if(arglist == null)
                return new SFunc(names, body, env);
            return new SFunc(arglist, body, env);
        }
    }

    class SID : SExpression {
        public string identifier;
        public SID(string id) { identifier = id; }

        public override string ToString()
        {
            return identifier;
        }

        public override string  DebugString()
        {
            return "<ID: " + identifier + ">";
        }

        public override object Clone()
        {
            return new SID(identifier);
        }
    }

    class SSymbol : SAtomic {
        public string name;
        public SSymbol(string val) { name = val; }

        public override string ToString()
        {
            return "'" + name;
        }

        public override string DebugString()
        {
            return "<Sym: " + name + ">";
        }

        public override object Clone()
        {
            return new SSymbol(name);
        }
    }

    class SInt : SAtomic {
        public int val;
        public SInt(int val) { this.val = val; }
        public override string ToString()
        {
            return val.ToString();
        }

        public override string DebugString()
        {
            return "<INT: " + val.ToString() + ">";
        }

        public override object Clone()
        {
            return new SInt(val);
        }
    }

    class SBool : SAtomic
    {
        public bool val;
        public SBool (bool v) { val = v; }
        public override string ToString()
        {
            return val ? "#t" : "#f";
        }

        public override string DebugString()
        {
            string t = val ? "TRUE" : "FALSE";
            return "<BOOL: " + t + ">";
        }

        public override object Clone()
        {
            return new SBool(val);
        }
    }

    class SPair : SExpression
    {
        //Lists are really just a stack. We'll represent this as a list in which we only add and remove elements from the end of/
        //  In scheme, you maniupulate lists by pushing and popping from the head. Thus, it's hella easier add / remove from end of list

        private SExpression head;
        private SExpression tail;

        public SPair(List<SExpression> elms, bool makeProper = false) {
            if (makeProper)
                elms.Add(new SEmptyList());
            if (elms.Count == 0)
                throw new Exception("Error no elms to create chain of pairs");
            if (elms.Count == 1)
            {
                head = elms[0];
                tail = new SEmptyList();
                return;
            }

            if(elms.Count == 2)
            {
                head = elms[0];
                tail = elms[1];
                return;
            }

            SPair root = new SPair(elms[elms.Count - 2], elms[elms.Count - 1]);
            for(int i = elms.Count - 3; i >  0; i--)
            {
                root = new SPair(elms[i], root);
            }
            head = elms[0];
            tail = root;
        }
        
        public SPair(SExpression head, SExpression tail) { this.head = head; this.tail = tail; }

        public List<SExpression> flatten()
        {
            List<SExpression> elms = new List<SExpression>();
            elms.Add(head);
            if (tail is SPair)
            {
                elms = elms.Concat(((SPair)tail).flatten()).ToList();
            }
            else
            {
                elms.Add(tail);
            }
            return elms;
        }

        public SExpression getHead() { return head; }
        public void setHead(SExpression expr) { head = expr; }
        public SExpression getTail() { return tail; }

        public bool isProperList()
        {
            if (tail is SPair)
                return ((SPair)tail).isProperList();
            return tail is SEmptyList;
        }

        public override string ToString()
        {
            string hstring = head.ToString();
            string tstring = tail.ToString();

            if (head is SEmptyList || head is SPair)
                hstring = hstring.Substring(1, hstring.Length - 1);

            if (tail is SPair)
                tstring = tstring.Substring(2,tstring.Length - 3);

            if (tail is SEmptyList)
                return "'(" + hstring + ")";

            if (!(tail is SPair))
                return "'(" + hstring + " . " + tstring + ")";

            return "'(" + hstring + " " + tstring + ")";
        }
        public override string DebugString()
        { 
            return "<PAIR: " + head.DebugString() + ", " + tail.DebugString() + ">";
        }

        public override object Clone()
        {
            return new SPair((SExpression)head.Clone(), (SExpression)tail.Clone());
        }
    }

    class SEmptyList : SExpression
    {
        public override object Clone()
        {
            return new SEmptyList();
        }
        public override string DebugString()
        {
            return "<EMPTY LIST>";
        }
        public override string ToString()
        {
            return "'()";
        }
    }

    class SNone : SExpression
    {
        public override string ToString()
        {
            return "";
        }

        public override string DebugString()
        {
            return "<NONE>";
        }

        public override object Clone()
        {
            return new SNone();
        }
    }
}
