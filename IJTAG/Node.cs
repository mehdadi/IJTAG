using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IJTAG
{
    public abstract class Node : PocVertex
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
            return type.ToString() + " " + ID.ToString();
        }

        public int Level 
        {
            get
            {
                int i = 0;
                Node temp = this;
                while (temp != null)
                {
                    temp = temp.Parent;
                    i++;
                }
                return i;
            }
        }

        public XElement RelatedElem;
        public Node Parent;
        public Node Source = null;
        //public List<Node> Destinations = new List<Node>();
        public List<Node> Children = new List<Node>();
        public bool IsChecked;
        public string ID { get; set; }
        public NodeType type;
        public bool IsChildrenChecked { get { return Children.Count == 0 || Children.All(x => x.IsChecked); } }

        public ulong NodeLenght
        {
            get
            {
                switch (this.type)
                {
                    case NodeType.SCB:
                        return (this as SCB).SCLength[(this as SCB).MultiplexSelect];
                    case NodeType.TDR:
                        return (this as TDR).SCLength;
                    case NodeType.SIB:
                        if ((this as SIB).Status == NodeStatusindex.CloseS) return 1;
                        else
                        {
                            return (this as SIB).GetChildrenLenght();
                        }
                    default:
                        return 0;
                }
            }
        }

        public static Node Create(XElement elem, Node parent)
        {
            Node node;
            switch (elem.Name.LocalName)
            {
                case "SIB":
                    node = new SIB(elem);
                    node.type = NodeType.SIB;
                    break;
                case "SCB":
                    node = new SCB(elem);
                    node.type = NodeType.SCB;
                    break;
                case "TDR":
                    node = new TDR(elem);
                    node.type = NodeType.TDR;
                    break;
                default :
                    return null;
            }
            node.Parent = parent;
            node.RelatedElem = elem;
            node.IsChecked = false;
            if (parent != null)
            {
                parent.Children.Add(node);
            }
            
            return node;
        }

    }

    public enum NodeStatusindex : int
    {
        OpenF = 1,
        CloseS = 0
    }

    public enum NodeType : int
    {
        SIB = 1,
        TDR = 2,
        SCB = 3,
    }

    public class Session : Queue<Node>
    {
        public ulong SessionLenght = 0;

        public Dictionary<Node, ulong> NodeLenghtInFailoure = new Dictionary<Node, ulong>();

        public NodeStatusindex? SibStatusInThis (SIB sib)
        {
            SIB conserning = this.Where(x => x == sib).FirstOrDefault() as SIB;
            if (conserning != null)
            {
                if (conserning.Children.Count > 0 &&  this.Contains(conserning.Children[0]))
                {
                    return NodeStatusindex.OpenF;
                }
                else
                {
                    return NodeStatusindex.CloseS;
                }
            }
            return null;
        }
        //internal int? ScbStatusInThis(SCB sCB)
        //{
        //    SCB conserning = this.Where(x => x == sCB).FirstOrDefault() as SCB;
        //    if (conserning != null)
        //    {
        //    }
        //    else
        //    {
        //    }
        //}

        public Session()
        {

        }

        public override string ToString()
        {
            return SessionLenght.ToString();
        }

        public string GetSequence( bool update = true)
        {
            StringBuilder st = new StringBuilder();
            Stack<Node> seq = new Stack<Node>();
            foreach (Node n in this)
            {
                seq.Push(n);
            }
            st.Append(SessionLenght);
            st.Append('$');
            Node prevNode = seq.Peek();
            while (seq.Count > 0)
            {
                Node n = seq.Pop();
                switch (n.type)
                {
                    case NodeType.TDR:
                        TDR tdr = (n as TDR);
                        if (tdr.selector != null)
                        {
                            int mu = (this.Where(x => x.ID == tdr.selector).FirstOrDefault() as SCB).MultiplexSelect;
                            string val = Convert.ToString(mu, 2);
                            st.Append('0', (int)tdr.SCLength - val.Length);
                            st.Append(val);
                        }
                        else st.Append('X', (int)tdr.SCLength);
                        break;
                    case NodeType.SCB:
                        SCB scb = n as SCB;
                        st.Append('X', (int)scb.SCLength[scb.MultiplexSelect + (update ? 0 : +1)]);
                        if (update)
                        {
                            if (scb.MultiplexSelect != 0)
                                scb.MultiplexSelect--;
                        }
                        break;
                    case NodeType.SIB:
                        SIB sib = n as SIB;
                        if (update)
                        {
                            if (sib.Children.Contains(prevNode) == false)
                            {
                                sib.Status = NodeStatusindex.CloseS;
                            }
                        }
                        st.Append((int)sib.Status);
                        break;
                }
                prevNode = n;
            }
            st.Append('$');
            return st.ToString();
        }


    }

    public class TDR : Node
    {
        public ulong SCLength;
        public readonly SIB Parent;
        public string selector = null;

        public TDR(XElement Element)
        {
            foreach (XAttribute att in Element.Attributes())
            {
                switch (att.Name.LocalName)
                {
                    case "ID":
                        ID = att.Value;
                        break;
                    case "SCLength":
                        SCLength = StringToUInt(att.Value);
                        break;
                    case "selector":
                        selector = att.Value;
                        break;

                }
            }
            NameOnScreen = "TDR " + ID;
        }
    }

    public class SCB : Node
    {
        public int ChildCount;
        public UInt64[] SCLength;
        public int[] checkcounter;

        public int ShorterIndex
        {
            get { return 0; }//{ if (SCLength[0] < SCLength[1]) return SCLength; else return 1; }

        }
        public readonly SIB Parent;

        public int MultiplexSelect = 0;
        //public int UpdateMultiplexSelect { get; private set; }

        //public void updateSelect()
        //{
        //    UpdateMultiplexSelect = MultiplexSelect;
        //}

        public SCB(XElement Element)
        {

            string[] sclens = null;
            foreach (XAttribute att in Element.Attributes())
            {
                switch (att.Name.LocalName)
                {
                    case "ID":
                        ID = att.Value;
                        break;
                    case "ChildNodes":
                        ChildCount = StringToInt(att.Value);
                        break;
                    case "SCLengths":
                        sclens = att.Value.Split(',');
                        break;
                    case "SCLengthA":
                        if (sclens == null)
                            sclens = new string[2];
                        ChildCount = 2;
                        sclens[0] = att.Value;
                        break;
                    case "SCLengthB":
                        if (sclens == null)
                            sclens = new string[2];
                        ChildCount = 2;
                        sclens[1] = att.Value;
                        break;
                }
            }
            SCLength = new UInt64[ChildCount];
            checkcounter = new int[ChildCount];

            for (int i = 0; i < ChildCount; i++)
            {
                SCLength[i] = StringToUInt(sclens[i]);
            }
            SCLength = SCLength.OrderBy(x => x).ToArray();

            NameOnScreen = "SCB " + ID;
        }
    }

    public class SIB : Node
    {
        public SIB(XElement Element)
        {
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
            NameOnScreen = "SIB " + ID;
        }

        public SIB()
        {
            // Fake
        }

        //charactersitics
        public readonly string InstrumentID;
        public readonly int SCPattherns;
        public readonly uint SCLength;
        public readonly int WIRLength;
        public readonly XElement RelatedElem;

        //Pattern Tested
        public bool neverCheckable { get { return ID == null || Parent.Children.Last() == this; } }
        public bool IsBypassOnly { get { return Children.Count == 0 && SCLength == 0; } }
        public int CheckedOpen = 0;
        public int CheckedClose = 0;
        public NodeStatusindex Status;
        public bool IsFullyChecked
        {
            get
            {
                return CheckedClose > 0 && CheckedOpen > 0;
            }
        }

        internal ulong GetChildrenLenght()
        {
            ulong ln = this.SCLength;
            foreach (var child in Children)
            {
                ln += child.NodeLenght;
            }
            return ln;
        }
    }
}
