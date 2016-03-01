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

        public class MatrixCounter
        {
            public MatrixCounter()
            {
                Count = 0;
            }
            public int Count { get; private set; }
            public bool Add(bool IFNotUsefullDontADD)
            {
                Count++;
                if (Count == 1)
                {
                    return true;
                }
                else
                {
                    if (IFNotUsefullDontADD)
                    {
                        Count--;
                    }
                    return false;
                }
            }
            public override string ToString()
            {
                return Count.ToString();
            }
        }
        public Dictionary<Tuple<Node, Node>, MatrixCounter> MatrixOfTestabilityTDRS = new Dictionary<Tuple<Node, Node>, MatrixCounter>();
        public Dictionary<Tuple<Node, Node>, MatrixCounter> MatrixOfTestabilityMulties = new Dictionary<Tuple<Node, Node>, MatrixCounter>();

        public List<Session> Sessions = new List<Session>();
        public List<Session> AdditionalSessions = new List<Session>();
        public List<Tuple<Node, Node>> NonTestables = new List<Tuple<Node, Node>>();

        public ulong DiagnosisLenght
        {
            get
            {
                ulong len = 0;
                foreach (var sess in Sessions)
                {
                    len += sess.NodeLenghtInFailoure.Max(x => x.Value);
                }
                foreach (var sess in AdditionalSessions)
                {
                    len += sess.NodeLenghtInFailoure.Max(x => x.Value);
                }
                return len;
            }
        }


        public Tuple<string, string> DCbeforeAdding;

        public Tuple<string, string> DCafterAdding;



        Tuple<string, string> CapturedDCResults()
        {
            var TDRS = MatrixOfTestabilityTDRS.Where(x => x.Value.Count == 0);
            var Multies = MatrixOfTestabilityMulties.Where(x => x.Value.Count == 0);

            var tdr = ((double)TDRS.Count() / (double)MatrixOfTestabilityTDRS.Count) * 100.0;
            var multi = ((double)Multies.Count() / (double)MatrixOfTestabilityMulties.Count) * 100.0;
            if (double.IsNaN(tdr))
            {
                tdr = 0;
            }
            if (double.IsNaN(multi))
            {
                multi = 0;
            }


            return new Tuple<string, string>((TDRS.Count() + " of " + MatrixOfTestabilityTDRS.Count + "    " + tdr), (Multies.Count() + " of " + MatrixOfTestabilityMulties.Count + "    " + multi));
        }



        public List<T> NonTesteds<T>()
        {
            var AllSets = NonTestables.Select(x => x.Item1).Concat(NonTestables.Select(x => x.Item2)).OfType<T>().Distinct();
            return AllSets.ToList();
        }

        public ulong DiagnosisSumofLenghtForConf
        {
            get
            {
                ulong sum = 0;
                foreach (var p in Sessions)
                {
                    sum += p.SessionLenght + LongestPath + 2 + 5 + 5;
                }
                foreach (var p in AdditionalSessions)
                {
                    sum += p.SessionLenght + (AdditionalSessions.Count > 0 ? AdditionalSessions.Max(x => x.SessionLenght) : 0) + 2 + 5 + 5;
                }
                return sum;
            }
        }

        public ulong LongestPath
        {
            get
            {
                if (Sessions.Count > 0)
                    return Sessions.Max(x => x.SessionLenght);
                else
                    return 0;
            }
        }

        public ulong sumofLenght
        {
            get
            {
                ulong sum = 0;
                foreach (var p in Sessions)
                {
                    sum += p.SessionLenght + LongestPath + 2 + 5 + 5;
                }
                return sum;
            }
        }

        public ulong SumConfigLenghtDiggingDown
        {
            get
            {
                var temp = Sessions.OrderBy(x => x.SessionLenght).ToList();
                ulong sum = temp[0].SessionLenght;
                for (int i = 1; i < temp.Count; i++)
                {
                    sum += temp[i - 1].SessionLenght + 5 + temp[i].SessionLenght + 5;
                }
                return sum;
            }
        }

        public ulong SumConfigLenghtHollowingUP
        {
            get
            {
                var temp = Sessions.OrderByDescending(x => x.SessionLenght).ToList();
                ulong sum = temp[0].SessionLenght + 5;
                for (int i = 1; i < temp.Count; i++)
                {
                    sum += temp[i].SessionLenght + 5;
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

            Node TDIFirst = AllNodes.Find(x => x.Parent == null && x.Source == null);



            while (AllNodes.All(x => x.IsChecked) == false)
            {
                Session Path = new Session();
                Dictionary<Node, bool> nodes_conf = new Dictionary<Node, bool>();
                Path.SessionLenght = DynamicDigingPathFinder(TDIFirst, Path);
                Sessions.Add(Path);
            }

            MakeMatrix();

            return;
            // On DIAGNOSIS RUN
            AllNodes.OfType<SIB>().AsParallel().ForAll(x => x.Status = NodeStatusindex.OpenF);
            AllNodes.OfType<SCB>().AsParallel().ForAll(x => x.MultiplexSelect = 0);


            foreach (var session in Sessions)
            {
                calculateNodeFailourINSession(session);
            }

            foreach (var sess in Sessions)
            {
                checkMatrixessWithSession(sess, false);
            }

            DCbeforeAdding = CapturedDCResults();

            while (true)
            {
                var Controls = MatrixOfTestabilityTDRS.Where(x => x.Value.Count == 0 && NonTestables.Contains(x.Key) == false);
                if (Controls.Count() == 0)
                {
                    Controls = MatrixOfTestabilityMulties.Where(x => x.Value.Count == 0 && NonTestables.Contains(x.Key) == false);
                }

                if (Controls.Count() == 0)
                {
                    break;
                }

                foreach (var mat in Controls)
                {
                    bool Second = false;
                    while (true)
                    {
                        Node Me = Second ? mat.Key.Item2 : mat.Key.Item1;
                        Session s = Sessions.Find(x => x.Contains(Me));
                        Node parent = s.Where(x => x == Me.Parent).FirstOrDefault();

                        if (parent == null)
                        {
                            if (Second)
                            {
                                NonTestables.Add(mat.Key);
                                break;
                            }
                            Second = true;
                            continue;
                        }

                        Session n = null;
                        if (parent is SIB)
                        {
                            SIB par = parent as SIB;
                            if (par.Status == NodeStatusindex.CloseS)
                            {
                                par.Status = NodeStatusindex.OpenF;
                                n = MakePathSib(s, par);
                                par.Status = NodeStatusindex.CloseS;
                            }
                            else
                            {
                                par.Status = NodeStatusindex.CloseS;
                                n = MakePathSib(s, par);
                                par.Status = NodeStatusindex.OpenF;
                            }
                        }
                        if (parent is SCB)
                        {
                            // TODO with makePathSCB

                            //SIB par = parent as SIB;
                            //if (par.Status == NodeStatusindex.CloseS)
                            //{
                            //    par.Status = NodeStatusindex.OpenF;
                            //    n = MakePath(s, par);
                            //    par.Status = NodeStatusindex.CloseS;
                            //}
                            //else
                            //{
                            //    par.Status = NodeStatusindex.CloseS;
                            //    n = MakePath(s, par);
                            //    par.Status = NodeStatusindex.OpenF;
                            //}
                        }

                        calculateNodeFailourINSession(n);

                        if (checkMatrixessWithSession(n, true))
                        {
                            AdditionalSessions.Add(n);
                            break;
                        }
                        else
                        {
                            if (Second)
                            {
                                NonTestables.Add(mat.Key);
                                break;
                            }
                            Second = true;
                        }
                    }
                }
            }
            DCafterAdding = CapturedDCResults();
        }

        private Session MakePathScb(Session s, SIB parent)
        {
            //TODO
            return null;
            //Session n = new Session();
            //foreach (Node nd in s)
            //{
            //    if ((parent.Children.Contains(nd) && parent.Status == NodeStatusindex.OpenF) || parent.Children.Contains(nd) == false)
            //    {
            //        n.Enqueue(nd);
            //        n.SessionLenght += nd.NodeLenght;
            //    }
            //}
            //return n;
        }

        private Session MakePathSib(Session s, SIB parent)
        {
            Session n = new Session();
            foreach (Node nd in s)
            {
                if ((parent.Children.Contains(nd) && parent.Status == NodeStatusindex.OpenF) || parent.Children.Contains(nd) == false)
                {
                    n.Enqueue(nd);
                    n.SessionLenght += nd.NodeLenght;
                }
            }
            return n;
        }

        private bool checkMatrixessWithSession(Session sess, bool IFNotUsefullDontADD)
        {
            int UsefulSessions = 0;
            foreach (var mat in MatrixOfTestabilityTDRS)
            {
                if (sess.Contains(mat.Key.Item1) ^ sess.Contains(mat.Key.Item2))
                {
                    UsefulSessions += mat.Value.Add(IFNotUsefullDontADD) ? 1 : 0;
                }
            }

            foreach (var mat in MatrixOfTestabilityMulties)
            {
                if (sess.Contains(mat.Key.Item1) ^ sess.Contains(mat.Key.Item2))
                {
                    UsefulSessions += mat.Value.Add(IFNotUsefullDontADD) ? 1 : 0;
                }
                if (sess.Contains(mat.Key.Item1) && sess.Contains(mat.Key.Item2))
                {
                    if (sess.NodeLenghtInFailoure[mat.Key.Item1] != sess.NodeLenghtInFailoure[mat.Key.Item2])
                    {
                        UsefulSessions += mat.Value.Add(IFNotUsefullDontADD) ? 1 : 0;
                    }
                }
            }

            return UsefulSessions > 0;
        }

        void calculateNodeFailourINSession(Session session)
        {
            foreach (var node in session)
            {
                if (session.NodeLenghtInFailoure.ContainsKey(node) == false)
                {
                    if ((node is TDR) == false)
                    {
                        ulong len = CheckNodeInothermode(session, node);
                        session.NodeLenghtInFailoure.Add(node, len);
                    }
                }
            }
        }

        private ulong CheckNodeInothermode(Session session, Node node)
        {
            ulong len = 0;
            foreach (Node nd in session)
            {
                if (nd != node)
                {
                    len += node.NodeLenght;
                    len += nd.NodeLenght;
                }
                else
                {
                    //TODO

                    //if ((nd is TDR) == false)
                    //{
                    //    if (nd.Status == NodeStatusindex.OpenF)
                    //    {
                    //        nd.Status = NodeStatusindex.CloseS;
                    //        len += nd.NodeLenght;
                    //        nd.Status = NodeStatusindex.OpenF;
                    //    }
                    //    else
                    //    {
                    //        nd.Status = NodeStatusindex.OpenF;
                    //        len += nd.NodeLenght;
                    //        nd.Status = NodeStatusindex.CloseS;
                    //    }
                    //}
                }
            }
            return len;
        }

        private void MakeMatrix()
        {
            //  KAFTAR BA KAFTAR
             
            foreach (Node nd1 in AllNodes.Where(x=>x is TDR))
            {
                foreach (Node nd2 in AllNodes.Where(x => x is TDR))
                {
                    if (nd2 != nd1)
                    {
                        if (MatrixOfTestabilityTDRS.Any(x => x.Key.Equals(new Tuple<Node, Node>(nd2, nd1))) == false)
                            MatrixOfTestabilityTDRS.Add(new Tuple<Node, Node>(nd1, nd2), new MatrixCounter());
                    }
                }
            }

            foreach (Node nd1 in AllNodes.Where(x => !(x is TDR)))
            {
                foreach (Node nd2 in AllNodes.Where(x => !(x is TDR)))
                {
                    if (nd2 != nd1)
                    {
                        if (MatrixOfTestabilityMulties.Any(x => x.Key.Equals(new Tuple<Node, Node>(nd2, nd1))) == false)
                            MatrixOfTestabilityMulties.Add(new Tuple<Node, Node>(nd1, nd2), new MatrixCounter());
                    }
                }
            }

            //foreach (var path in Sessions)
            //{
            //    foreach (var key in MatrixOfTestability.Select(x => x.Key))
            //    {
            //        if (path.Contains(key.Item1) == true && path.Contains(key.Item2) == false)
            //        {
            //            MatrixOfTestability[key].Add();
            //        }
            //    }
            //}

            /*
             * dont want to explore all of it
             * just the elements in foulty path
             * 
             * 
             * */

            //foreach (var mat in MatrixOfTestability.Select(x => x.Key))
            //{
            //    //if (mat.Item1 is TDR && mat.Item2 is TDR) // if 
            //    {
            //        MatrixOfTestability[mat].Add();
            //    }
            //}
        }

        UInt64 DynamicDigingPathFinder(Node Node, Session path)
        {
            path.Enqueue(Node);

            UInt64 Len = 0;

            Node next = null;

            //check if all children in checked

            if (Node.IsChildrenChecked) // go to close state
            {
                Node parental = Node;
                while (next == null)
                {
                    next = AllNodes.Find(x => x.Source == (parental));
                    parental = parental.Parent;
                    if (parental == null)
                        break;
                }
                if (Node is SIB)
                {
                    if (Node.Children.Count == 0 && (Node as SIB).CheckedOpen == 0)
                    {
                        (Node as SIB).Status = NodeStatusindex.OpenF;
                    }
                    else
                    {
                        (Node as SIB).Status = NodeStatusindex.CloseS;
                    }
                }
            }
            else //  go to open state
            {
                next = Node.Children.First();
                if (Node is SIB)
                {
                    (Node as SIB).Status = NodeStatusindex.OpenF;
                }
            }

            if (Node is SIB)
            {
                SIB sib = Node as SIB;
                if (sib.Status == NodeStatusindex.OpenF)
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
                int ind = scb.ChildCount - 1;

                do
                {
                    if (scb.checkcounter[ind] == 0)
                    {
                        break;
                    }
                    ind--;
                } while (ind > 0);
                
                scb.checkcounter[ind]++;
                Len += scb.SCLength[ind];

                /*
                if (scb.checkcounter.All(x => x == 0)) //[0] == 0 && scb.checkcounter[1] == 0) //going to the longest path first
                {
                    scb.checkcounter[scb.SCLength.Length - 1]++;
                    Len += scb.SCLength[scb.SCLength.Length - 1] + 1;
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
                */

                scb.IsChecked = scb.checkcounter.All(x => x != 0);// scb.checkcounter[0] > 0 && scb.checkcounter[1] > 0;
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
                    me.Source = (last);
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
