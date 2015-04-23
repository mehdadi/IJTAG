using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IJTAG
{
    public static class extentionStack
    {
        public static Stack<T> Clone<T>(this Stack<T> stack)
        {
            return new Stack<T>(new Stack<T>(stack));
        }
    }

    public class SIB
    {
        public static int StringToInt(string s)
        {
            int d = 0;
            int.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d);
            return d;
        }

        public static uint StringToUInt(string s)
        {
            uint d = 0;
            uint.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d);
            return d;
        }

        public override string ToString()
        {
            return "ID = " + ID + "(" + Level + "," + Column + ")" + Size;
        }

        public SIB(XElement Element, SIB parent)
        {
            this.Parent = parent;
            if (parent != null)
            {
                parent.Children.Add(this);
            }
            this.Type = Element.Name.LocalName;

            foreach (XAttribute att in Element.Attributes())
            {
                switch (att.Name.LocalName)
                {
                    case "ID":
                        ID = att.Value;
                        break;
                    case "InstrumentID":
                        InstrumentID = att.Value;
                        break;
                    case "SCPatterns":
                        SCPattherns = StringToInt(att.Value);
                        break;
                    case "SCLength":
                        SCLength = StringToUInt(att.Value);
                        break;
                    case "WIRLength":
                        WIRLength = StringToInt(att.Value);
                        break;

                }
            }
        }

        public SIB()
        {
            // Fake
        }

        //charactersitics
        public readonly string Type;
        public readonly string InstrumentID;
        public readonly string ID;
        public readonly int SCPattherns;
        public readonly uint SCLength;
        public readonly int WIRLength;
        public readonly XElement RelatedElem;
        
        //Pattern Tested
        public bool neverCheckable { get { return ID == null || Parent.Children.Last() == this; } }
        public bool IsBypassOnly { get { return Children.Count == 0 && SCLength == 0; } }
        public int CheckedOpen = 0;
        public int CheckedClose = 0;
        public bool IsFullyChecked
        {
            get
            {
                //if (IsBypassOnly)
                //{
                //    return CheckedClose > 0;
                //}
                //else
                {
                    return CheckedClose > 0 && CheckedOpen > 0;
                }
            }
        }
        //Relations
        public readonly List<SIB> Children = new List<SIB>();
        public readonly SIB Parent;
        public SIB source;
        //public SIB dest;
        
        #region For Drawing 
        public int Level
        {
            get
            {
                int i = 0;
                SIB temp = this;
                while (temp != null)
                {
                    temp = temp.Parent;
                    i++;
                }
                return i;
            }
        }

        public int Size
        {
            get
            {
                return Math.Max(1, Children.Sum(x => x.Size));
            }
        }

        public int Column
        {
            get
            {
                if (Parent != null)
                {
                    int myindex = Parent.Children.IndexOf(this);
                    int adding = 0;
                    if (myindex != 0)
                        adding = Parent.Children.Take(myindex).Sum(x => x.Size);
                    return Parent.Column + adding;
                }
                else
                    return 0;
            }
        }


        #endregion



        //internal SIB Clone()
        //{
        //    throw new NotImplementedException();
        //}
    }

    public class GraphExporter
    {
        SIB gateway;

        List<SIB> AllSIBs = new List<SIB>();

        List<Stack<SIB>> AllPaths = new List<Stack<SIB>>();
        public List<Tuple<Stack<SIB>, uint>> outputPaths = new List<Tuple<Stack<SIB>, uint>>();

        public long sumofLenght;
        public long ConfigLenght;

        public int Dept { get { return AllSIBs.Max(x => x.Level); } }

        public GraphExporter()
        {
        }

        public void Parse(XElement root)
        {
            gateway = new SIB(root, null);
            AllSIBs.Add(gateway);
            ParallelConstruction(root, gateway);

            SIB TDILast = AllSIBs.Where(x => x.Level == 2).Last();
            TDIFirst = AllSIBs.Where(x => x.Level == 2).First();
            Stack<SIB> first = new Stack<SIB>();
            AllPaths.Add(first);
            RecursivePaterns(TDILast, first);


            AllPaths = AllPaths.OrderByDescending(x => x.Count).ToList();


            outputPaths.Clear();

            foreach (Stack<SIB> path in AllPaths)
            {
                if (AllSIBs.Where(x => x.neverCheckable == false).All(x => x.IsFullyChecked))
                {
                    break;
                }

                outputPaths.Add(new Tuple<Stack<SIB>, uint>(path, ControlPath(path)));
            }

            
            //var totalPathLength = AllPaths.Select(y => new Tuple<Stack<SIB>, int>(y, y.Sum(s => s.SCLength)));//.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);

            //var SibstoCheck = AllSIBs.Select(x => new Tuple<SIB, bool, bool>(x, false, false));

            /*outputPaths.Clear();
            ControlAllPaths();

            sumofLenght = outputPaths.Sum(x => x.Sum(s => s.SCLength));
            ConfigLenght = outputPaths.Sum(x => x.Sum(s => s.SCLength) * x.Max(l => l.Level));
            */
              //outputPaths.

            //var t = AllSIBs.Where(x => x.neverCheckable == false).Where(x => x.IsFullyChecked == false);

            /*
            foreach (var pat in AllPaths)
            {
                var Appliedpath = ApplyPath(pat);

                foreach (var ap in Appliedpath)
                {
                    //if (ap.Item2
                }
            }

            Dictionary<SIB, Tuple<bool, bool>> SibsControl = new Dictionary<SIB, Tuple<bool, bool>>();
            Dictionary<Stack<SIB>, List<Tuple<SIB, bool, bool>>> Path_coverage = new Dictionary<Stack<SIB>, List<Tuple<SIB, bool, bool>>>();
            foreach (var pat in AllPaths)
            {
                var Appliedpath = ApplyPath(pat);
                Path_coverage.Add(pat, Appliedpath);

                foreach (var Ap in Appliedpath)
                {
                    if (SibsControl.ContainsKey(Ap.Item1) == false)
                    {
                        SibsControl.Add(Ap.Item1, new Tuple<bool, bool>(Ap.Item2, Ap.Item3));
                    }
                    else
                    {
                        //if (SibsControl[Ap.Item1].Item1 == false &&

                    }
                }
            }
             * */
        }


        uint ControlPath(Stack<SIB> path)
        {
            uint Len = 0;

            var clon = path.Clone();

            while (clon.Count > 1)
            {
                SIB me = clon.Pop();

                bool isClose;

                if (me.Children.Count > 0 && me.Children.First() == clon.Peek())
                {
                    isClose = false;
                    //me.CheckedOpen++;
                }
                else
                {
                    isClose = true;
                    //me.CheckedClose++;
                }

                if (isClose && me.Children.Count == 0)
                {
                    if (me.CheckedOpen == 0)
                    {
                        isClose = false;
                    }
                }

                if (isClose)
                {
                    me.CheckedClose++;
                    Len += 1;
                }
                else
                {
                    me.CheckedOpen++;
                    Len += 1 + me.SCLength;
                }
            }
            return Len;
        }

        //         SIB,Closed,Opened
        List<Tuple<SIB, bool, bool>> ApplyPath(Stack<SIB> path)
        {
            List<Tuple<SIB, bool, bool>> list = new List<Tuple<SIB, bool, bool>>();

            var clon = path.Clone();
            while (clon.Count > 1)
            {
                SIB me = clon.Pop();
                if (me.IsBypassOnly)
                {
                    list.Add(new Tuple<SIB, bool, bool>(me, true, true));
                }
                else
                {
                    if (me.Children.Count > 0 && me.Children.First() == clon.Peek())
                    {
                        list.Add(new Tuple<SIB, bool, bool>(me, true, false));
                    }
                    else
                    {
                        list.Add(new Tuple<SIB, bool, bool>(me, false, true));
                    }
                }
            }
            return list;
        }

        private void ControlAllPaths()
        {
            foreach (Stack<SIB> path in AllPaths)
            {
                if (AllSIBs.Where(x => x.neverCheckable == false).All(x => x.IsFullyChecked))
                {
                    break;
                }

                //outputPaths.Add(path);

                var clon = path.Clone();
                
                while (clon.Count > 1)
                {
                    SIB me = clon.Pop();
                    if (me.Children.Count > 0 && me.Children.First() == clon.Peek())
                    {
                        me.CheckedOpen++;
                    }
                    else
                    {
                        me.CheckedClose++;
                    }
                }
            }
        }

        SIB TDIFirst;

        

        void RecursivePaterns(SIB Node, Stack<SIB> path)
        {
            path.Push(Node);
            if (Node != TDIFirst)
            {
                if (Node.Children.Count == 0)
                {
                    if (Node.source != null)
                    {
                        RecursivePaterns(Node.source, path);
                    }
                }
                else
                {
                    if (path.Contains(Node.Children.Last()) == false)
                    {
                        var copy = extentionStack.Clone(path);
                        AllPaths.Add(copy);
                        RecursivePaterns(Node.Children.Last(), copy);
                        if (Node.source != null)
                        {
                            RecursivePaterns(Node.source, path);
                        }
                    }
                    else
                    {
                        if (Node.source != null)
                        {
                            RecursivePaterns(Node.source, path);
                        }
                    }
                }

                if (Node.Parent.Children.First() == Node)
                {
                    RecursivePaterns(Node.Parent, path);
                }
            }
        }

        public void ParallelConstruction(XElement root, SIB parent)
        {
            var childs = root.Elements("SIB");
            if (childs.Count() > 0)
            {
                var e = childs.GetEnumerator();
                e.MoveNext();
                var last = new SIB(e.Current, parent);
                AllSIBs.Add(last);
                ParallelConstruction(e.Current, last);

                while (e.MoveNext())
                {
                    SIB me = new SIB(e.Current, parent);
                    AllSIBs.Add(me);
                    me.source = last;
                    ParallelConstruction(e.Current, me);
                    last = me;
                }
            }
        }

        internal List<Stack<SIB>> getAllPaths() 
        {
            return AllPaths.OrderByDescending(x => x.Count).ToList();
        }

        internal void Dispose()
        {
        }

        internal List<SIB> GetAllNodes()
        {
            return AllSIBs;//.Clone();
        }
        
        internal SIB GetGateway()
        {
            return gateway;//.Clone();
        }
    }
}
