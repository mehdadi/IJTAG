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
        public List<Tuple<Queue<Node>, Tuple<UInt64, int>>> outputPaths = new List<Tuple<Queue<Node>, Tuple<UInt64, int>>>();

        public ulong sumofLenght;
        public ulong ConfigLenght;
        public int PathsChecked;

        public int Dept { get { return AllNodes.Max(x => x.Level); } }

        public GraphExporter()
        {
        }

        public void Parse(XElement root)
        {
            ParallelConstruction(root.Elements(), null);

            Node TDIFirst = AllNodes.Find(x => x.Parent == null && x.Sources.Count == 0);
            Queue<Node> first = new Queue<Node>();
            AllPaths.Add(first);
            RecursivePaterns3(TDIFirst, first);
            
            AllPaths = AllPaths.OrderByDescending(x => x.Count).ToList();

            outputPaths.Clear();

            foreach (Queue<Node> path in AllPaths)
            {
                if (AllNodes.All(x => x.IsChecked))
                {
                    break;
                }

                //ProvideSIBPossibleCheckWithPath(path.Clone());

                //         SIB, IsOpen
                List<Node> sibsForcedOpen = sibswithForcedOpen(new Queue<Node>(path.ToList()));

                //if (AllSIBs.Where(x => x.ID != null).Where(x => x.IsFullyChecked == false).Any(x => path.Contains(x) == false || sibsForcedOpen.Contains(x)))
                //{
                //    continue;
                //}

                Dictionary<Node, List<bool>> sibsTocheck = path.ToDictionary(x => x, y => new List<bool>() { true, false });

                foreach (var ch in path.ToList().Where(x => (x is SIB) && (x as SIB).CheckedOpen > 0))
                {
                    sibsTocheck[ch].Remove(true);
                }

                foreach (var s in sibsForcedOpen)
                {
                    sibsTocheck[s].Remove(false);
                }

                outputPaths.Add(new Tuple<Queue<Node>, Tuple<UInt64, int>>(path, ControlPath(path, sibsTocheck)));

            }

            if (AllNodes.All(x => x.IsChecked))
            {
                foreach (var t in outputPaths)
                {
                    sumofLenght += t.Item2.Item1;
                    ConfigLenght += (t.Item2.Item1 * (ulong)t.Item1.Max(x => x.Level - 1));
                    PathsChecked += t.Item2.Item2;
                }
            }
        }

        private List<Node> sibswithForcedOpen(Queue<Node> path)
        {
            List<Node> res = new List<Node>();

            while (path.Count > 1)
            {
                Node me = path.Dequeue();
                if (me.Children.Count > 0 && me.Children.First() == path.Peek())
                {
                    res.Add(me);
                }
            }
            return res;
        }
        
        void RecursivePaterns3(Node Node, Queue<Node> path)
        {


        }
        
        void RecursivePaterns2(Node Node, Queue<Node> path)
        {
            path.Enqueue(Node);

            //if (Node.Level == 1)
            //{
            //    //end of Path
            //    return;
            //}

            Node parental = Node;
            Node dest = null;
            while (dest == null)
            {
                dest = AllNodes.Find(x => x.Sources.Contains(parental));
                parental = parental.Parent;
                if (parental == null)
                    break;
            }

            //if (dest != null && Node.Level != 1)//akharin ghadam
            {
                if (Node.Children.Count > 0)
                {
                    var copy = new Queue<Node>(path.ToList());
                    AllPaths.Add(copy);
                    RecursivePaterns2(Node.Children.First(), copy); ;
                }
                if (dest != null)
                {
                    RecursivePaterns2(dest, path);
                }
            }
        }

        Tuple<UInt64, int> ControlPath(Queue<Node> path, Dictionary<Node, List<bool>> Nodestocheck)
        {
            UInt64 SumOfLength = 0;
            int countTimes = 0;
            while (Nodestocheck.Values.SelectMany(x => x).Count() > 0)
            {
                var copy = path.Clone();
                countTimes++;

                UInt64 Length = 0;
                while (copy.Count > 0)
                {
                    Node me = copy.Dequeue();
                    if (me is SIB)
                    {
                        SIB sib = me as SIB;

                        if (Nodestocheck[me].Count > 0)
                        {
                            if (Nodestocheck[me][0])
                            {
                                sib.CheckedOpen++;
                                Length += sib.SCLength + 1;
                            }
                            else
                            {
                                sib.CheckedClose++;
                                Length += 1;
                            }
                        }
                        else
                        {
                            if (me.Children.Count > 0 && copy.Count > 0 && me.Children.First() == copy.Peek())
                            {
                                sib.CheckedOpen++;
                                Length += sib.SCLength + 1;
                            }
                            else
                            {
                                sib.CheckedClose++;
                                Length += 1;
                            }
                        }
                        if (sib.CheckedClose > 0 && sib.CheckedOpen > 0)
                        {
                            me.IsChecked = true;
                        }
                    }
                    else if (me is SCB)
                    {
                        SCB scb = me as SCB;
                        if (Nodestocheck[me].Count > 0)
                        {
                            if (Nodestocheck[me][0])
                            {
                                scb.checkcounter[0]++;
                                Length += scb.SCLength[0] + 1;
                            }
                            else
                            {
                                scb.checkcounter[1]++;
                                Length += scb.SCLength[1] + 1;
                            }
                        }
                        else
                        {
                            scb.checkcounter[scb.ShorterIndex]++;
                            Length += scb.SCLength[scb.ShorterIndex];
                        }
                        if (scb.checkcounter[0] > 0 && scb.checkcounter[1] > 0)
                        {
                            me.IsChecked = true;
                        }
                    }
                    else
                    {
                        Length += (me as TDR).SCLength;
                        me.IsChecked = true;
                    }
                    if (Nodestocheck[me].Count > 0)
                    {
                        Nodestocheck[me].RemoveAt(0);
                    }
                }

                SumOfLength += Length;
            }
            return new Tuple<UInt64, int>(SumOfLength, countTimes);
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
