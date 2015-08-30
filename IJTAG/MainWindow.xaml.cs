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
                    st.Append(exporter.Sessions_leng.Count + ";");
                    st.Append(exporter.SumConfigLenghtDiggingDown + ";");
                    st.Append(exporter.SumConfigLenghtHollowingUP + ";");
                    st.Append(exporter.sumofLenght + ";");
                    st.Append((exporter.SumConfigLenghtDiggingDown + exporter.sumofLenght).ToString() + ";");
                    st.Append((exporter.SumConfigLenghtHollowingUP + exporter.sumofLenght).ToString() + ";");
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
                
                // making paths in console trace.
                //StringBuilder st = new StringBuilder();
                //foreach (var s in exporter.Sessions_leng.Select(x => x.Item1))
                //{
                //    st.AppendLine("SeSSION-------------");
                //    while (s.Count > 0)
                //    {
                //        var elem = s.Dequeue();
                //        st.AppendLine(elem.type.ToString() + " " + elem.ID.ToString());
                //    }
                //}
                //System.Diagnostics.Trace.Write(st.ToString());

            }
            BuildCanvas();
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
                if (node == exporter.AllNodes.Last(x=>x.Parent == null))
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
            graphLayout.Graph = pocGraph;

            ComboSessions.Items.Clear();
            foreach (var sess in exporter.Sessions_leng)
            {
                ComboSessions.Items.Add(sess.Item2.ToString());
            }
        }

        private void Paths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            exporter.AllNodes.ForEach(x => x.Border = Brushes.Black);
            if ((sender as ComboBox).SelectedIndex == -1)
                return;

            foreach (var node in exporter.Sessions_leng[(sender as ComboBox).SelectedIndex].Item1)
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
    }
}
