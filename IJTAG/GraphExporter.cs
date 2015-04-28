using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IJTAG
{
    public static class extentionQueue
    {
        public static Queue<T> Clone<T>(this Queue<T> Queue)
        {
            return new Queue<T>(new Queue<T>(Queue));
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

        List<Queue<SIB>> AllPaths = new List<Queue<SIB>>();
        public List<Tuple<Queue<SIB>, UInt64>> outputPaths = new List<Tuple<Queue<SIB>, UInt64>>();

        public ulong sumofLenght;
        public ulong ConfigLenght;

        public int Dept { get { return AllSIBs.Max(x => x.Level); } }

        public GraphExporter()
        {
        }

        public void Parse(XElement root)
        {
            gateway = new SIB(root, null);
            AllSIBs.Add(gateway);
            ParallelConstruction(root, gateway);

            SIB TDIFirst = AllSIBs.Where(x => x.Level == 2).First();
            Queue<SIB> first = new Queue<SIB>();
            AllPaths.Add(first);
            RecursivePaterns2(TDIFirst, first);

            AllPaths = AllPaths.OrderByDescending(x => x.Count).ToList();

            outputPaths.Clear();

            foreach (Queue<SIB> path in AllPaths)
            {
                if (AllSIBs.Where(x => x.neverCheckable == false).All(x => x.IsFullyChecked))
                {
                    break;
                }

                //ProvideSIBPossibleCheckWithPath(path.Clone());

                //         SIB, IsOpen
                List<SIB> sibsForcedOpen = sibswithForcedOpen(path.Clone());

                //if (AllSIBs.Where(x => x.ID != null).Where(x => x.IsFullyChecked == false).Any(x => path.Contains(x) == false || sibsForcedOpen.Contains(x)))
                //{
                //    continue;
                //}

                Dictionary<SIB, List<bool>> sibsTocheck = path.ToDictionary(x => x, y => new List<bool>() { true, false });

                foreach (var s in sibsForcedOpen)
                {
                    sibsTocheck[s].RemoveAt(1);
                }

                outputPaths.Add(new Tuple<Queue<SIB>, UInt64>(path, ControlPath(path, sibsTocheck)));

            }

            if (AllSIBs.Where(x => x.neverCheckable == false).All(x => x.IsFullyChecked))
            {
                foreach (var t in outputPaths)
                {
                    sumofLenght += t.Item2;
                    ConfigLenght += (t.Item2 * (ulong)t.Item1.Max(x => x.Level - 1));
                }
            }
        }

        private List<SIB> sibswithForcedOpen(Queue<SIB> path)
        {
            List<SIB> res = new List<SIB>();

            while (path.Count > 1)
            {
                SIB me = path.Dequeue();
                if (me.Children.Count > 0 && me.Children.First() == path.Peek())
                {
                    res.Add(me);
                }
            }
            return res;
        }

        void RecursivePaterns2(SIB Node, Queue<SIB> path)
        {
            path.Enqueue(Node);

            if (Node.Level == 1)
            {
                //end of Path
                return;
            }

            SIB parental = Node;
            SIB dest = null;
            while (dest == null)
            {
                dest = AllSIBs.Find(x => x.source == parental);
                parental = parental.Parent;
                if (parental == null)
                    break;
            }

            //if (dest != null && Node.Level != 1)//akharin ghadam
            {
                if (Node.Children.Count > 0)
                {
                    var copy = extentionQueue.Clone(path);
                    AllPaths.Add(copy);
                    RecursivePaterns2(Node.Children.First(), copy); ;
                }
                if (dest != null)
                {
                    RecursivePaterns2(dest, path);
                }
            }
        }

        UInt64 ControlPath(Queue<SIB> path, Dictionary<SIB, List<bool>> sibstocheck)
        {
            UInt64 SumOfLength = 0;
            while (sibstocheck.Values.SelectMany(x => x).Count() > 0)
            {
                var copy = path.Clone();

                UInt64 Length = 0;
                while (copy.Count > 0)
                {
                    SIB me = copy.Dequeue();

                    if (sibstocheck[me].Count > 0)
                    {
                        if (sibstocheck[me][0])
                        {
                            me.CheckedOpen++;
                            Length += me.SCLength + 1;
                        }
                        else
                        {
                            me.CheckedClose++;
                            Length += 1;
                        }
                        sibstocheck[me].RemoveAt(0);
                    }
                    else
                    {
                        if (me.Children.First() == copy.Peek())
                        {
                            me.CheckedOpen++;
                            Length += me.SCLength + 1;
                        }
                        else
                        {
                            me.CheckedClose++;
                            Length += 1;
                        }
                    }

                }

                SumOfLength += Length;
            }
            return SumOfLength;
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

        internal List<Queue<SIB>> getAllPaths() 
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
