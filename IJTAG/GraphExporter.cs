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

    public class GraphExporter
    {
        public List<Node> AllNodes = new List<Node>();

        public List<Queue<Node>> AllPaths = new List<Queue<Node>>();
        public List<Tuple<Queue<Node>, UInt64>> Sessions_leng = new List<Tuple<Queue<Node>, UInt64>>();

        public ulong LongestPath
        {
            get
            {
                if (Sessions_leng.Count > 0)
                    return Sessions_leng.Max(x => x.Item2);
                else
                    return 0;
            }
        }

        public ulong sumofLenght
        {
            get
            {
                ulong sum = 0;
                foreach (var p in Sessions_leng)
                {
                    sum += p.Item2 + LongestPath + 2 + 5 + 5;
                }
                return sum;
            }
        }

        public ulong SumConfigLenghtDiggingDown
        {
            get
            {
                var temp = Sessions_leng.OrderBy(x => x.Item2).ToList();
                ulong sum = temp[0].Item2;
                for (int i = 1; i < temp.Count; i++)
                {
                    sum += temp[i - 1].Item2 + 5 + temp[i].Item2 + 5;
                }
                return sum;
            }
        }

        public ulong SumConfigLenghtHollowingUP
        {
            get
            {
                var temp = Sessions_leng.OrderByDescending(x => x.Item2).ToList();
                ulong sum = temp[0].Item2 + 5;
                for (int i = 1; i < temp.Count; i++)
                {
                    sum += temp[i].Item2 + 5;
                }
                return sum;
            }
        }

        public long LengthofTDRs
        {
            get
            {
                long len = 0;
                len += AllNodes.Where(x => x is TDR).Sum(s => (long)((s as TDR).SCLength));
                len += AllNodes.Where(x => x is SIB).Sum(s => (long)((s as SIB).SCLength));
                len += AllNodes.Where(x => x is SCB).Sum(s => (long)((s as SCB).SCLength.Sum(x => (long)x)));
                return len;
            }
        }

        public int TDRs
        {
            get
            {
                return AllNodes.Count(x => x is TDR) + AllNodes.Count(x => (x is SIB) && (x as SIB).SCLength > 0);
            }
        }
        
        public int SCBs { get { return AllNodes.Count(x => x is SCB); } }
        public int SIBs { get { return AllNodes.Count(x => x is SIB); } }

        public int Dept { get { return AllNodes.Where(x => x is SIB).Max(x => x.Level); } }

        public GraphExporter()
        {
        }

        public void Parse(XElement root)
        {
            ParallelConstruction(root.Elements(), null);

            Node TDIFirst = AllNodes.Find(x => x.Parent == null && x.Sources.Count == 0);

            while (AllNodes.All(x => x.IsChecked) == false)
            {
                Queue<Node> Path = new Queue<Node>();
                UInt64 len = DynamicDigingPathFinder(TDIFirst, Path);
                Sessions_leng.Add(new Tuple<Queue<Node>, ulong>(Path, len));
            }
        }

        UInt64 DynamicDigingPathFinder(Node Node, Queue<Node> path)
        {
            path.Enqueue(Node);

            UInt64 Len = 0;

            Node next = null;

            //check if all children in checked

            bool IsOpen;
            if (Node.IsChildrenChecked) // go to close state
            {
                Node parental = Node;
                while (next == null)
                {
                    next = AllNodes.Find(x => x.Sources.Contains(parental));
                    parental = parental.Parent;
                    if (parental == null)
                        break;
                }
                if (Node.Children.Count == 0 && (Node is SIB) && (Node as SIB).CheckedOpen == 0)
                {
                    IsOpen = true;
                }
                else
                {
                    IsOpen = false;
                }
            }
            else //  go to open state
            {
                next = Node.Children.First();
                IsOpen = true;
            }
            
            if (Node is SIB)
            {
                SIB sib = Node as SIB;
                if (IsOpen)
                {
                    sib.CheckedOpen++;
                    Len += sib.SCLength + 1;
                }
                else
                {
                    sib.CheckedClose++;
                    Len += 1;
                }

                sib.IsChecked = sib.CheckedClose > 0 && sib.CheckedOpen > 0 && sib.IsChildrenChecked;

            }
            else if (Node is SCB)
            {
                SCB scb = Node as SCB;
                
                if (scb.checkcounter[0] == 0 && scb.checkcounter[1] == 0) //going to the longest path first
                {
                    scb.checkcounter[-(scb.ShorterIndex - 1)]++;
                    Len += scb.SCLength[-(scb.ShorterIndex - 1)] + 1;
                }
                else if (scb.checkcounter[0] == 0)
                {
                    scb.checkcounter[0]++;
                    Len += scb.SCLength[0] + 1;
                }
                else if (scb.checkcounter[1] == 0)
                {
                    scb.checkcounter[1]++;
                    Len += scb.SCLength[1] + 1;
                }
                else
                {
                    scb.checkcounter[scb.ShorterIndex]++;
                    Len += scb.SCLength[scb.ShorterIndex] + 1;
                }
                scb.IsChecked = scb.checkcounter[0] > 0 && scb.checkcounter[1] > 0;
            }
            else
            {
                TDR tdr = Node as TDR;
                tdr.IsChecked = true;
                Len += tdr.SCLength;
            }

            if (next != null)
            {
                Len += DynamicDigingPathFinder(next, path);
            }

            return Len;
        }
        
        public void ParallelConstruction(IEnumerable<XElement> childs, Node parent)
        {
            //var childs = root.Elements();
            
            if (childs.Count() > 0)
            {
                var e = childs.GetEnumerator();
                e.MoveNext();
                var last = Node.Create(e.Current, parent);
                AllNodes.Add(last);
                ParallelConstruction(e.Current.Elements(), last);

                while (e.MoveNext())
                {
                    Node me = Node.Create(e.Current, parent);
                    AllNodes.Add(me);
                    me.Sources.Add(last);
                    ParallelConstruction(e.Current.Elements(), me);
                    last = me;
                }
            }
        }

        internal void Dispose()
        {
        }
      
    }
}
