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

namespace IJTAG
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        GraphExporter exporter;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fl = new System.Windows.Forms.FolderBrowserDialog();
            if (fl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.IO.StreamWriter tx = System.IO.File.CreateText(fl.SelectedPath + "\\log.csv");
                StringBuilder st = new StringBuilder();

                st.Append("FileName;");
                st.Append("number of nodes;");
                st.Append("SCB nodes;");
                st.Append("SIB nodes;");
                st.Append("TDR nodes;");
                st.Append("Depth of nodes;");
                st.Append("Paths to test;");
                st.Append("length of full test;");
                st.Append("length of configuration;");

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
                    st.Append((exporter.AllNodes.Count) + ";");
                    st.Append(exporter.AllNodes.Count(x => x is SCB) + ";");
                    st.Append(exporter.AllNodes.Count(x => x is SIB) + ";");
                    st.Append(exporter.AllNodes.Count(x => x is TDR) + ";");
                    st.Append((exporter.Dept) + ";");
                    st.Append(exporter.PathsChecked + ";");
                    st.Append(exporter.sumofLenght + ";");
                    st.Append(exporter.ConfigLenght + ";");
                    tx.WriteLine(st.ToString());
                    tx.Flush();
                }
                tx.Close();
                tx.Dispose();

                MessageBox.Show("finished: " + fl.SelectedPath + "\\log.csv");

            }
        }
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            exporter = new GraphExporter();
            System.Windows.Forms.OpenFileDialog fl = new System.Windows.Forms.OpenFileDialog();
            fl.Filter = "XML File (*.Xml)|";
            fl.Multiselect = false;

            if (fl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //try
                {
                    System.IO.StreamReader read = new System.IO.StreamReader(fl.FileName);
                    System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Load(read);
                    exporter.Parse(doc.Element("Gateway"));

                    //Console.WriteLine(fl.FileName + "had " + exporter.getAllPaths().Count + " Paths with lenght of " + exporter.sumofLenght + " in " + exporter.outputPaths.Count); 
                }
                //catch
                //{
                //}
            }

            //var AllPathes = exporter.getAllPaths();
            //PathsCount.Text = AllPathes.Count.ToString();

            //foreach (var pat in AllPathes)
            //{
            //    combo.Items.Add(new ComboBoxItem() { Content = pat.Count, Tag = pat });
            //}

            BuildCanvas();
        }

        int XRectSize = 26;
        int distXrect = 16;

        void BuildCanvas()
        {
            /*
            Canvas.Children.Clear();
            Canvas.RowDefinitions.Clear();
            Canvas.ColumnDefinitions.Clear();

            var Allsibs = exporter.GetAllNodes();
            SIB gateway = exporter.GetGateway();
            int MaxLevel = Allsibs.Max(x => x.Level);

            for (int i = 0; i < gateway.Size; i++)
            {
                Canvas.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            }

            for (int i = 0; i < MaxLevel; i++)
            {
                Canvas.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            }

            for (int i = 1; i <= MaxLevel; i++)
            {
                var thisLevel = Allsibs.FindAll(x => x.Level == i);
                for (int j = 0; j < thisLevel.Count; j++)
                {
                    SIB sib = thisLevel[j];

                    #region tooltip
                    Grid tooltipgrid = new Grid();
                    tooltipgrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    tooltipgrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    tooltipgrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    tooltipgrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    tooltipgrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    tooltipgrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                    tooltipgrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                    tooltipgrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });

                    TextBlock tb1 = new TextBlock() { Text = typeof(sib).Name + " " + sib.ID, FontWeight = FontWeights.Bold };
                    Grid.SetColumn(tb1, 0); Grid.SetRow(tb1, 0); Grid.SetColumnSpan(tb1, 2); tooltipgrid.Children.Add(tb1);
                    TextBlock tb2 = new TextBlock() { Text = "SC Length:  " };
                    Grid.SetColumn(tb2, 0); Grid.SetRow(tb2, 1); tooltipgrid.Children.Add(tb2);
                    TextBlock tb3 = new TextBlock() { Text = sib.SCLength.ToString() };
                    Grid.SetColumn(tb3, 1); Grid.SetRow(tb3, 1); tooltipgrid.Children.Add(tb3);
                    TextBlock tb4 = new TextBlock() { Text = "With All Paths Open Config checked: "  };
                    Grid.SetColumn(tb4, 0); Grid.SetRow(tb4, 2); tooltipgrid.Children.Add(tb4);
                    TextBlock tb5 = new TextBlock() { Text = sib.CheckedOpen.ToString() };
                    Grid.SetColumn(tb5, 1); Grid.SetRow(tb5, 2); tooltipgrid.Children.Add(tb5);
                    TextBlock tb6 = new TextBlock() { Text = "With All Paths Close Config checked: " };
                    Grid.SetColumn(tb6, 0); Grid.SetRow(tb6, 3); tooltipgrid.Children.Add(tb6);
                    TextBlock tb7 = new TextBlock() { Text = sib.CheckedClose.ToString() };
                    Grid.SetColumn(tb7, 1); Grid.SetRow(tb7, 3); tooltipgrid.Children.Add(tb7);
                    #endregion

                    Border brd = new Border() { MinWidth = 16, ToolTip = tooltipgrid, Background = Brushes.White, BorderBrush = Brushes.Black, BorderThickness = new Thickness(2), Margin = new Thickness(distXrect), HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch, VerticalAlignment = System.Windows.VerticalAlignment.Center };
                    allborders.Add(sib, brd);
                    TextBlock txt = new TextBlock() { Text = sib.ID, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(2) };
                    if (sib.ID == null)
                    {
                        txt.Text = "Gateway";
                    }
                    brd.Child = txt;
                    Grid.SetColumn(brd, sib.Column);
                    Grid.SetRow(brd, sib.Level - 1);
                    Grid.SetColumnSpan(brd, sib.Size);
                    Canvas.Children.Add(brd);

                    if (i != 1 && (sib.Parent.Children.First() == sib || sib.Parent.Children.Last() == sib))
                    {
                        int uniqeChild = 0;
                        if (sib.Parent.Children.Count == 1)
                        {
                            uniqeChild = 7;
                            Path arrow = new Path() { Stroke = Brushes.Black, StrokeThickness = 1, FlowDirection = FlowDirection.LeftToRight, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                            GeometryGroup geomGroup = new GeometryGroup();
                            LineGeometry line1 = new LineGeometry() { StartPoint = new Point(4 - uniqeChild, 15), EndPoint = new Point(0 - uniqeChild, 20) };
                            LineGeometry line2 = new LineGeometry() { StartPoint = new Point(0 - uniqeChild, 0), EndPoint = new Point(0 - uniqeChild, 20) };
                            LineGeometry line3 = new LineGeometry() { StartPoint = new Point(-4 - uniqeChild, 15), EndPoint = new Point(0 - uniqeChild, 20) };
                            geomGroup.Children.Add(line1);
                            geomGroup.Children.Add(line2);
                            geomGroup.Children.Add(line3);
                            arrow.Data = geomGroup;

                            Grid.SetColumn(arrow, sib.Column);
                            Grid.SetRow(arrow, sib.Level - 2);
                            Grid.SetRowSpan(arrow, 2);
                            Grid.SetColumnSpan(arrow, sib.Size);
                            Canvas.Children.Add(arrow);
                        }

                        {
                            bool up = sib.Parent.Children.Last() == sib;
                            Path arrow = new Path() { Stroke = Brushes.Black, StrokeThickness = 1, FlowDirection = FlowDirection.LeftToRight, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment =  HorizontalAlignment.Center };
                            GeometryGroup geomGroup = new GeometryGroup();
                            LineGeometry line1 = new LineGeometry() { StartPoint = new Point(4 + uniqeChild, up ? 5 : 15), EndPoint = new Point(0 + uniqeChild, up ? 0 : 20) };
                            LineGeometry line2 = new LineGeometry() { StartPoint = new Point(0 + uniqeChild, 0), EndPoint = new Point(0 + uniqeChild, 20) };
                            LineGeometry line3 = new LineGeometry() { StartPoint = new Point(-4 + uniqeChild, up ? 5 : 15), EndPoint = new Point(0 + uniqeChild, up ? 0 : 20) };
                            geomGroup.Children.Add(line1);
                            geomGroup.Children.Add(line2);
                            geomGroup.Children.Add(line3);
                            arrow.Data = geomGroup;

                            Grid.SetColumn(arrow, sib.Column);
                            Grid.SetRow(arrow, sib.Level - 2);
                            Grid.SetRowSpan(arrow, 2);
                            Grid.SetColumnSpan(arrow, sib.Size);
                            Canvas.Children.Add(arrow);
                        }
                    }

                    if (j != thisLevel.Count - 1)
                    {
                        if (thisLevel[j].Parent == thisLevel[j + 1].Parent)
                        {
                            Path arrow = new Path() { Stroke = Brushes.Black, StrokeThickness = 1, FlowDirection = FlowDirection.LeftToRight, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center};
                            GeometryGroup geomGroup = new GeometryGroup();
                            LineGeometry line1 = new LineGeometry() { StartPoint = new Point(13, 7), EndPoint = new Point(20, 10) };
                            LineGeometry line2 = new LineGeometry() { StartPoint = new Point(0, 10), EndPoint = new Point(20, 10) };
                            LineGeometry line3 = new LineGeometry() { StartPoint = new Point(13, 13), EndPoint = new Point(20, 10) };
                            geomGroup.Children.Add(line1);
                            geomGroup.Children.Add(line2);
                            geomGroup.Children.Add(line3);
                            arrow.Data = geomGroup;

                            Grid.SetRow(arrow, sib.Level - 1);
                            Grid.SetColumn(arrow, sib.Column + sib.Size - 1);
                            Grid.SetColumnSpan(arrow, 2);
                            Grid.SetZIndex(arrow, 0);
                            Canvas.Children.Add(arrow);
                        }
                    }
                }
            }
             * */
        }

        Dictionary<SIB, Border> allborders = new Dictionary<SIB, Border>();

        private void Paths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //foreach (var brd in allborders.Values)
            //{
            //    brd.BorderBrush = Brushes.Black;
            //}

            //foreach (SIB sib in ((combo.SelectedItem as ComboBoxItem).Tag as Stack<SIB>))
            //{
            //    allborders[sib].BorderBrush = Brushes.Red;
            //}
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
