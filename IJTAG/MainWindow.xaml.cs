using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GraphSharp.Controls;
using QuickGraph;
using System.Xml.Linq;

namespace IJTAG
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WPFExtensions.Controls.ZoomControl zoom = new WPFExtensions.Controls.ZoomControl();
        PocGraphLayout graphLayout = new PocGraphLayout();

        GraphExporter exporter;

        public MainWindow()
        {
            InitializeComponent();

            zoom.Content = graphLayout;
            zoom.Mode = WPFExtensions.Controls.ZoomControlModes.Original;

            GeneralGrid.Children.Add(zoom);

            zoom.ZoomBoxOpacity = 0.5;
            zoom.Background = Brushes.White;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // generic Log Parser
            System.Windows.Forms.FolderBrowserDialog fl = new System.Windows.Forms.FolderBrowserDialog();
            if (fl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.IO.StreamWriter tx = System.IO.File.CreateText(fl.SelectedPath + "\\log.csv");
                StringBuilder st = new StringBuilder();

                st.Append("FileName;");
                st.Append("SIB nodes;");
                st.Append("SCB nodes;");
                st.Append("TDR nodes;");
                st.Append("Max Depth;");
                st.Append("Total TDR Lenght;");
                st.Append("Sessions;");
                st.Append("digging Alg conf time ;");
                st.Append("Hollowing Alg conf time ;");
                st.Append("test time;");
                st.Append("Digging TotalTime;");
                st.Append("Hollowing TotalTime;");
                st.Append("ON DIAGNOSIS;");


                st.Append("TDR DC before new Sessions;");
                st.Append("SCB-SIB DC before new Sessions;");

                st.Append("Added Sessions;");

                st.Append("TDR DC after new Sessions;");
                st.Append("SCB-SIB DC after new Sessions;");

                st.Append("NoneTestable TDRs;");
                st.Append("NoneTestable SIBs;");
                st.Append("NoneTestable SCBs;");
                st.Append("Testability;");
                st.Append("Diagnosis Time;");
                st.Append("Diagnosis Config Time;");

                st.Append("Diagnosis Total Time;");

                st.Append("NonTestet Nodes");

                tx.WriteLine(st.ToString());

                foreach (var file in System.IO.Directory.GetFiles(fl.SelectedPath, "*.xml", System.IO.SearchOption.AllDirectories))
                {
                    exporter = new GraphExporter();

                    System.IO.StreamReader read = new System.IO.StreamReader(file);
                    System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Load(read);
                    System.GC.Collect();

                    exporter.Parse(doc.Element("Gateway"));

                    st.Clear();
                    st.Append(System.IO.Path.GetFileNameWithoutExtension(file) + ";");
                    st.Append(exporter.SIBs + ";");
                    st.Append(exporter.SCBs + ";");
                    st.Append(exporter.TDRs + ";");
                    st.Append((exporter.Dept) + ";");
                    st.Append(exporter.LengthofTDRs.ToString() + ";");
                    st.Append(exporter.Sessions.Count + ";");
                    st.Append(exporter.SumConfigLenghtDiggingDown + ";");
                    st.Append(exporter.SumConfigLenghtHollowingUP + ";");
                    st.Append(exporter.sumofLenght + ";");
                    st.Append((exporter.SumConfigLenghtDiggingDown + exporter.sumofLenght).ToString() + ";");
                    st.Append((exporter.SumConfigLenghtHollowingUP + exporter.sumofLenght).ToString() + ";");
                    st.Append(";");

                    st.Append((exporter.DCbeforeAdding.Item1 + "%;"));
                    st.Append((exporter.DCbeforeAdding.Item2 + "%;"));

                    st.Append(exporter.AdditionalSessions.Count + ";");

                    st.Append((exporter.DCafterAdding.Item1 + "%;"));
                    st.Append((exporter.DCafterAdding.Item2 + "%;"));

                    st.Append(exporter.NonTesteds<TDR>().Count + ";");
                    st.Append(exporter.NonTesteds<SIB>().Count + ";");
                    st.Append(exporter.NonTesteds<SCB>().Count + ";");
                    var perc = (double)(exporter.AllNodes.Count - exporter.NonTesteds<TDR>().Count - exporter.NonTesteds<SIB>().Count - exporter.NonTesteds<SCB>().Count) / (double)exporter.AllNodes.Count;
                    st.Append((perc * 100.0).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%;");
                    st.Append(exporter.DiagnosisLenght + ";");
                    st.Append(exporter.DiagnosisSumofLenghtForConf + ";");

                    st.Append((exporter.DiagnosisLenght * 2 + exporter.DiagnosisSumofLenghtForConf).ToString() + ";");

                    foreach (var nd in exporter.NonTesteds<Node>())
                    {
                        st.Append(nd.ToString() + ";");
                    }

                    tx.WriteLine(st.ToString());
                    Console.WriteLine(st.ToString());
                    tx.Flush();
                }
                tx.Close();
                tx.Dispose();

                MessageBox.Show("finished: " + fl.SelectedPath + "\\log.csv");

            }
        }
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            // File oppener

            exporter = new GraphExporter();
            System.Windows.Forms.OpenFileDialog fl = new System.Windows.Forms.OpenFileDialog();
            fl.Filter = "XML File (*.Xml)|";
            fl.Multiselect = false;

            if (fl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.IO.StreamReader read = new System.IO.StreamReader(fl.FileName);
                System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Load(read);
                exporter.Parse(doc.Element("Gateway"));

                BuildCanvas();
            }
        }

        int XRectSize = 26;
        int distXrect = 16;

        void BuildCanvas()
        {
            PocGraph pocGraph = new PocGraph(false);
            exporter.AllNodes.ForEach(x => pocGraph.AddVertex(x));

            foreach (var node in exporter.AllNodes)
            {
                if (node.Source != null)
                {
                    pocGraph.AddEdge(new PocEdge(node.Source, node));
                }
                if (node.Parent != null && node == node.Parent.Children.Last())
                {
                    pocGraph.AddEdge(new PocEdge(node, node.Parent));
                }
                if (node.Parent != null && node == node.Parent.Children.First())
                {
                    pocGraph.AddEdge(new PocEdge(node.Parent, node));
                }
                if (node.Parent == null && node.Source == null)
                {
                    var tdi = new PocVertex() { NameOnScreen = "TDI" };
                    pocGraph.AddVertex(tdi);
                    pocGraph.AddEdge(new PocEdge(tdi, node));
                }
                if (node == exporter.AllNodes.Last(x => x.Parent == null))
                {
                    var tdi = new PocVertex() { NameOnScreen = "TDO" };
                    pocGraph.AddVertex(tdi);
                    pocGraph.AddEdge(new PocEdge(node, tdi));
                }
            }

            graphLayout.LayoutAlgorithmType = "Tree";
            var param = graphLayout.LayoutParameters as GraphSharp.Algorithms.Layout.Simple.Tree.SimpleTreeLayoutParameters;
            param.Direction = GraphSharp.Algorithms.Layout.LayoutDirection.LeftToRight;
            param.VertexGap = 40;
            param.LayerGap = 40;
            param.WidthPerHeight = 0.5;
            param.SpanningTreeGeneration = GraphSharp.Algorithms.Layout.Simple.Tree.SpanningTreeGeneration.BFS;
            param.Direction = GraphSharp.Algorithms.Layout.LayoutDirection.LeftToRight;
            graphLayout.Graph = pocGraph;

            ComboSessions.Items.Clear();
            foreach (var sess in exporter.Sessions)
            {
                ComboSessions.Items.Add(sess.SessionLenght.ToString());
            }
        }

        private void Paths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            exporter.AllNodes.ForEach(x => x.Border = Brushes.Black);
            if ((sender as ComboBox).SelectedIndex == -1)
                return;

            foreach (var node in exporter.Sessions[(sender as ComboBox).SelectedIndex])
            {
                node.Border = Brushes.Red;
            }

        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            int maxLen = Node.StringToInt(MaxLen.Text);
            int minLen = Node.StringToInt(MinLen.Text);
            int numbOfelems = Node.StringToInt(elements.Text);
            int dept = Node.StringToInt(Dept.Text);

            Builder builder = new Builder(numbOfelems, dept, minLen, maxLen);

            System.Windows.Forms.SaveFileDialog fl = new System.Windows.Forms.SaveFileDialog();
            fl.Filter = "XML File (*.Xml)|";
            if (fl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //System.IO.StreamReader read = new System.IO.StreamReader(fl.FileName);
                //System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument(builder.Create());
                System.Xml.Linq.XDocument doc = new System.Xml.Linq.XDocument(builder.Create());
                doc.Save(fl.FileName + ".xml");

            }
        }

        private void TextBlock_Drop(object sender, DragEventArgs e)
        {
            foreach (var s in (string[])e.Data.GetData(DataFormats.FileDrop, false))
            {
                var dInfo = System.IO.Directory.CreateDirectory(s + "_version2");

                foreach (var file in System.IO.Directory.GetFiles(s, "*.xml", System.IO.SearchOption.AllDirectories))
                {
                    System.IO.StreamReader read = new System.IO.StreamReader(file);
                    System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Load(read);

                    XElement NewElement = FileConverttToversion2(doc.Element("Gateway"));
                    XDocument doc2 = new XDocument(NewElement);
                    doc2.Save(dInfo.FullName + "\\" + System.IO.Path.GetFileNameWithoutExtension(file) + "_v2.xml");
                }
            }
        }

        private XElement FileConverttToversion2(XElement xElement)
        {
            XElement newGateway = new XElement("Gateway");
            newGateway.Add(new XAttribute("Version", 2));

            foreach (XElement element in xElement.Elements())
            {
                newGateway.Add(ConverttoVersion2(element));
            }

            return newGateway;
        }

        private XElement ConverttoVersion2(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "TDR":
                    return element;
                case "SIB":
                    XElement newelement = new XElement(element.Name.LocalName);
                    var ID = element.Attribute("ID").Value;
                    newelement.Add(new XAttribute("ID", ID));
                    var val = element.Attribute("SCLength").Value;
                    XElement tdr = new XElement("TDR");
                    tdr.Add(new XAttribute("ID", ID + ".t"));
                    tdr.Add(new XAttribute("SCLength", val));
                    newelement.Add(tdr);
                    foreach (XElement elem in element.Elements())
                    {
                        newelement.Add(ConverttoVersion2(elem));
                    }
                    return newelement;
                case "SCB":
                    return element;
                default:
                    return null;
            }
        }

        private void GeneralGrid_Drop(object sender, DragEventArgs e)
        {
            var s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            //try
            //{
            if (System.IO.File.Exists(s[0]))
            {
                exporter = new GraphExporter();
                System.IO.StreamReader read = new System.IO.StreamReader(s[0]);
                System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Load(read);
                exporter.Parse(doc.Element("Gateway"));

                StringBuilder st = new StringBuilder();

                foreach (var sess in exporter.Sessions)
                {
                    st.AppendLine("----------");
                    st.Append("TDI -> ");
                    foreach (var sin in sess)
                    {
                        st.Append(sin.ToString() + " -> ");
                    }
                    st.Append("TDO   Len: ");
                    st.Append(sess.SessionLenght);
                    st.AppendLine();
                }

                string path = System.IO.Path.GetDirectoryName(s[0]);
                path += "\\" + System.IO.Path.GetFileNameWithoutExtension(s[0]);

                {
                    System.IO.StreamWriter writer = new System.IO.StreamWriter(path + ".test.txt");
                    writer.Write(st.ToString());
                    writer.Close();
                    writer.Dispose();
                }

                //performing Reset
                exporter.AllNodes.OfType<SIB>().AsParallel().ForAll(x => x.Status = NodeStatusindex.OpenF);
                exporter.AllNodes.OfType<SCB>().AsParallel().ForAll(x => { x.MultiplexSelect = x.ChildCount - 1; });

                StringBuilder sq = new StringBuilder();
                //sq.Append(exporter.Sessions.First().GetSequence(false));
                //sq.AppendLine();

                foreach (Session sess in exporter.Sessions)
                {
                    sq.Append(sess.GetSequence(true));
                    sq.AppendLine();

                    sq.Append(sess.GetSequence(false));
                    sq.AppendLine();
                }

                {
                    System.IO.StreamWriter writer = new System.IO.StreamWriter(path + ".seq.txt");
                    writer.Write(sq.ToString());
                    writer.Close();
                    writer.Dispose();
                }

                BuildCanvas();
            }
//            }
//            catch (Exception ese)
//            {
//#if DEBUG
//                throw ese;
//#endif
//            }
        }
    }
}
