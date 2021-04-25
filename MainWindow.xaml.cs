using Microsoft.Win32;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals.Axes;
using SciChart.Charting.Visuals.PointMarkers;
using SciChart.Core.Extensions;
using SciChart.Data.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SciChart.Charting.Visuals;
using SciChart.Charting3D;
using SciChart.Charting3D.Model;
using Utilities;

namespace CVSDataViewer_V2
{
    public partial class MainWindow : Window
    {
        public DataClass Data;   

        private DispatcherTimer _dispatcherTimer;
        private InitVarsClass2 _initialization;
        private List<string> _fileNames;
        private List<string> _prefixWords;

        private readonly Stopwatch _stopWatch = new Stopwatch();
        private List<GroupDisplayClass> _groups;
        private string _groupIniFileName;
        private GroupDisplayClass _selectedGroup;

        private int _startIndex;
        private bool _readStarted;

        //--------------------------------------------------------------------------------
        //
        //
        public MainWindow()
        {
            SciChartSurface.SetRuntimeLicenseKey(
                "un2xZxxZHPFO3LUKDR+qykKA/N3Z4JdNtW7SPPbpDEYWvZNyI3qO1dCjnRpPg99bKnkB7WJy58q57Qu2HeK3FOHsyu6CGXn9Va95+V4kXRvKy57vBpG6MRV4azRmrsK6/GIUcRATavx6f1rZkCQPVhv5X2rjUO/6+/Q8BP6eoF+HWbvoLQCU6dVsto5SqgUAjzVtp+Y4OpMA2UD0vO7YAAXn7pqhnKaSpX+b/P9QUXbWXNe8XGnN1wGdCKObPPtyqULIDHK39Ih3pxIH/YAB8xBjy3zUq/bBtxhKuHioDt9LNM/8ehSM/9NSGjrlACXCMp7J/otGFISWCvtwWfQoSn1vSyfIA64aLMSd8nnjtNKeS/ciWIB5zTPyz3L+lMj4o32icMgLRv275u3Ed4PQ/ha64EPipt9/emEcd0SIUZxsJUQi8KwhG8fVoHPYfowu0puLJAYLHRckc/2b017LbNmIG3yVOzNo36g4a1Quj1pSXJr8bYEJ5g==");

            InitializeComponent();

            Chart1.EventMouseMoved += UserChart_EventMouseMoved;
            Chart1.EnableHighlight = false;

        }

        //--------------------------------------------------------------------------------
        //
        //
        private void UserChart_EventMouseMoved(object sender, object e)
        {
            if (Data.AllRecords.Count == 0)
                return;

            //
            // mousePoint is the mouse point from the Plot
            //
            if (!(e is MouseMoveDataClass mouseMove))
                return;

            if (Data.AllRecords.Count > 0)
                LabelTime.Content = mouseMove.Date.ToLongDateString() + "  " + mouseMove.Date.ToLongTimeString();

            double.TryParse(TextBoxDeadZoneVelocity.Text, out double velocity);
            double.TryParse(TextBoxChamberWidth.Text, out double width);
            double.TryParse(TextBoxChamberHeight.Text, out double height);
            MachineState?.PlotIt(mouseMove.MousePoint, velocity, width, height, RadioButtonFlow.IsChecked == true);
        }

        //-------------------------------------------------------------------
        //
        //
        private void DeleteSelectedPlot(object sender, object e)
        {
            //
            // Delete the selected plot from the group but make a copy of them
            //   to remove
            //
            var plots = Chart1.SelectedPlots;
            List<PlotInfoClass> selectedPlots = new List<PlotInfoClass>();

            foreach (var plot in plots)
            {
                foreach (var group in _groups)
                {
                    var rec = group.Records.FirstOrDefault(T => T.Name.Equals(plot.DataSeries.SeriesName));
                    if (rec != null)
                    {
                        group.Records.Remove(rec);
                        selectedPlots.Add(rec.PlotInfo);
                        UpdateGroupItemsListBox(group);
                        break;
                    }
                }
            }

            //
            // Remove the plots
            //
            foreach (var p in selectedPlots)
                Chart1.RemovePlot(p);

            WriteGroupInitFile();
        }

        //-------------------------------------------------------------------
        //
        //
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            //
            // If we are building the data, update quickly and put a single line
            //   out at a time. This looks better than just a block of text arriving
            //   at once.
            //
            if (Data.Working)
            {
                _readStarted = true;
                _dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
                int lastIndex = _startIndex + 1;
                if (lastIndex < Data.UserOut.Count)
                {
                    for (int i = _startIndex; i < lastIndex; i++)
                    {
                        UserOut.AddString(Data.UserOut[i].Line, Data.UserOut[i].LineColor,
                            Data.UserOut[i].Indent,
                            Data.UserOut[i].Bold);
                    }

                    _startIndex = lastIndex;
                }
            }
            else if (_readStarted)
            {
                //
                // Finish out any thing that has
                //
                for (int i = _startIndex; i < Data.UserOut.Count; i++)
                    UserOut.AddString(Data.UserOut[i].Line, Data.UserOut[i].LineColor,
                        Data.UserOut[i].Indent, Data.UserOut[i].Bold);

                _startIndex = 0;
                _readStarted = false;

                FinishReading();
            }
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileInfo fileInfo = new FileInfo(assembly.Location);
            DateTime lastModified = fileInfo.LastWriteTime;

            var initFile = "c:\\GG\\InitFiles\\CSVDataViewer_V2.xml";

            _initialization = new InitVarsClass2(this, initFile);
            if (_initialization.Error.Length > 0)
            {
                UserOut.AddString("Error - could not read init file", Colors.Red);
            }
            else
            {
                Title = initFile + "                    " + assembly.GetName().Name +
                        "-> BuildRaw " +
                        lastModified.ToLongDateString() + " " + lastModified.ToLongTimeString();
            }

            Data = new DataClass();

            //
            // Create the system timer, set it to 100ms and the right call back.
            //
            _dispatcherTimer = new DispatcherTimer
            {
                IsEnabled = true,
                Interval = new TimeSpan(0, 0, 0, 0, 100)
            };
            _dispatcherTimer.Tick += DispatcherTimer_Tick;

            Chart1.SetBackgroundColor(Brushes.Black);
            Chart2.SetBackgroundColor(Brushes.Black);
            Chart2.ShowChartLegend(false);
            Chart2.ShowXAxis(false);
            Chart2.ShowYAxis(false);
            Chart2.ClearPlots();

            UserOut.SetBorderColor(Brushes.White);

            StackPanelGroups.Children.Clear();
            _groups = new List<GroupDisplayClass>();

            Chart1.ShowChartLegend(false);

            //
            // get the prefix words. If it's not there, then init it
            //   with the basics below.
            //
            _prefixWords = new List<string>();
            var listOfListOfStrings = _initialization.GetStringList();
            if (listOfListOfStrings.Count == 0)
            {
                _prefixWords.Add("HMI_");
                _prefixWords.Add("U1");
                _prefixWords.Add("U2");
                _prefixWords.Add("U3");
                _prefixWords.Add("Buffle_");
            }
            else
            {
                foreach (var word in listOfListOfStrings[0])
                {
                    if (word.Length > 0)
                        _prefixWords.Add(word);
                }
            }

            if (listOfListOfStrings.Count == 2)
            {
                _fileNames = listOfListOfStrings[1];

                UpdateUserOutFiles();
            }

            TextBlockPrefixs.Text = "";
            foreach (var word in _prefixWords)
                TextBlockPrefixs.Text = TextBlockPrefixs.Text + word + "\n";

            TabControlUserOut.SelectedIndex = 1;

            ((TabItem)TabControlMainPlot.Items[1]).Visibility = Visibility.Hidden;
            ((TabItem)TabControlMainPlot.Items[2]).Visibility = Visibility.Hidden;
            LabelTime.Content = "";
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //
            // Get the prefixes from the textbox.
            //
            UpdatePrefixes();

            _initialization.ClearStringList();
            _initialization.AddStringList(_prefixWords);
            _initialization.AddStringList(_fileNames);
            _initialization.WriteToFile();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void CurveFitSelectedPlot(object sender, RoutedEventArgs e)
        {
            //
            // Find the plot that is active.
            //
            if (Chart1.CurrentPlotInfo != null)
            {
                RecordClass sourceRecord = Data.AllRecords.Find(T => T.PlotInfo.Equals(Chart1.CurrentPlotInfo));
                if (sourceRecord == null)
                    return;

                RecordClass rec = new RecordClass("CurveFit_" + sourceRecord.Name);
                sourceRecord.PlotInfo.CloneInfo(rec.PlotInfo);

                rec.PlotInfo.StrokeThickness = 4;
                rec.PlotInfo.DashArray = new double[] {2, 4};
                rec.PlotInfo.PointMarker = CheckBoxShowPoints.IsChecked == true
                    ? new EllipsePointMarker() {Fill = Colors.White, Height = 8, Width = 8}
                    : null;

                double[] y = sourceRecord.Measures.Select(T => T.Value).ToArray();
                double[] x = new double[y.Length];

                for (int i = 0; i < x.Length; i++)
                    x[i] = i;

                int.TryParse(TextBoxCurveFitPoly.Text, out int polyNum);
                var pf = new PolyFit(x, y, polyNum);
                var fitted = pf.Fit(x);

                for (int i = 0; i < fitted.Length; i++)
                {
                    rec.AddEvent(sourceRecord.Measures[i].Date, fitted[i]);
                }

                rec.BuildData(CheckBoxShitTimeToZero.IsChecked == true, 0);
                Chart1.AddPlot(rec.PlotInfo);
            }
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void UpdateUserOutFiles()
        {
            UserOutFiles.ClearDisplay();

            foreach (var file in _fileNames)
            {
                if (File.Exists(file))
                    UserOutFiles.AddString("File \"" + file + "\" ready", Colors.Blue, 0, true);
            }

            UserOut.AddString("");
            TabControlUserOut.SelectedItem = 1;
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void ReadAndProcessFiles()
        {
            _stopWatch.Reset();
            _stopWatch.Start();

            if (_fileNames == null)
            {
                _fileNames = new List<string>
                {
                    TextBoxFileName.Text
                };
            }

            UpdateUserOutFiles();
            TabControlUserOut.SelectedIndex = 0;

            UserOut.ClearDisplay();

            //
            // Read the data with the right input columns.
            //
            int.TryParse(TextBoxTimeCol.Text, out int timeCol);
            int.TryParse(TextBoxValueCol.Text, out int valCol);
            double.TryParse(TextBoxLabQuestTimeShift.Text, out double labquestHoursOffset);

            UpdatePrefixes();

            if (Data.ReadFilesBackground(_fileNames, _prefixWords,
                timeCol - 1, valCol - 1,
                CheckBoxShitTimeToZero.IsChecked == true,
                CheckBoxRemoveCommonPrefix.IsChecked == true,
                labquestHoursOffset) == false)
            {
                foreach (var error in Data.UserOut)
                {
                    UserOut.AddString(error.Line, error.LineColor,
                        error.Indent,
                        error.Bold);
                }

                Data.UserOut.Clear();
            }

        }

        //---------------------------------------------------------------
        //
        //
        private void FinishReading()
        {
            //
            // build and show the records in the list box
            //
            ListBoxAllRecords.Items.Clear();
            foreach (var record in Data.AllRecords)
            {
                ListBoxAllRecords.Items.Add(record.DisplayLabel);
                record.DisplayLabel.Width =
                    (ListBoxAllRecords.ActualWidth == 0) ? 400 : ListBoxAllRecords.ActualWidth;
                record.DisplayLabel.MouseEnter += DisplayLabel_MouseEnter;
                record.DisplayLabel.MouseLeave += DisplayLabel_MouseLeave;
            }

            ReadGroupInitFile(_fileNames[0]);
            PlotGroups();
            ButtonClearSearch_Click(this, new RoutedEventArgs());

            Make3DMesh2();

        }

        //---------------------------------------------------------------
        //
        //
        private void Make3DMesh1()
        {
            var group = _groups.FirstOrDefault(T =>
                T.GroupName.Contains("3D") ||
                T.GroupName.Contains("3d"));

            if (group == null)
            {
                Chart3D1.RenderableSeries[0].DataSeries?.Clear();
                return;
            }

            if (group.Records.Count == 0)
                return;


            if (CheckBoxSwitch3DAxis.IsChecked == false)
            {
                int xSize = group.Records[0].Measures.Count;
                int zSize = group.Records.Count;

                var meshDataSeries = new UniformGridDataSeries3D<double>(xSize, zSize)
                {
                    StepX = 1,
                    StepZ = 1,
                    SeriesName = "3DPlot",
                };

                Chart3D1.XAxis.VisibleRange = new DoubleRange(0, xSize);
                Chart3D1.YAxis.VisibleRange = new DoubleRange(0, 1.0);
                Chart3D1.ZAxis.VisibleRange = new DoubleRange(0, zSize);

                int zCount = 0;
                foreach (var rec in group.Records)
                {
                    int xCount = 0;
                    foreach (var measure in rec.Measures)
                    {
                        meshDataSeries[zCount, xCount++] = measure.Value / group.MaxScale;
                    }

                    zCount++;
                }

                surfaceMeshRenderableSeries.DataSeries = meshDataSeries;
            }
            else
            {
                int xSize = group.Records.Count;
                int zSize = group.Records[0].Measures.Count;

                var meshDataSeries = new UniformGridDataSeries3D<double>(xSize, zSize)
                {
                    StepX = 1,
                    StepZ = 1,
                    SeriesName = "3DPlot",
                };

                Chart3D1.XAxis.VisibleRange = new DoubleRange(0, xSize);
                Chart3D1.YAxis.VisibleRange = new DoubleRange(0, 1.0);
                Chart3D1.ZAxis.VisibleRange = new DoubleRange(0, zSize);

                int xCount = 0;
                foreach (var rec in _groups[0].Records)
                {
                    int zCount = 0;
                    foreach (var measure in rec.Measures)
                    {
                        meshDataSeries[zCount++, xCount] = measure.Value / group.MaxScale;
                    }

                    xCount++;
                    surfaceMeshRenderableSeries.DataSeries = meshDataSeries;
                }
            }
        }

        //---------------------------------------------------------------
        //
        //
        private void Make3DMesh2()
        {
            var group = _groups.FirstOrDefault(T =>
                T.GroupName.Contains("3D") ||
                T.GroupName.Contains("3d"));

            if (group == null)
            {
                Chart3D1.RenderableSeries[0].DataSeries?.Clear();
                return;
            }

            if (group.Records.Count == 0)
                return;


            if (CheckBoxSwitch3DAxis.IsChecked == false)
            {
                int xSize = group.Records[0].Measures.Count;
                int zSize = group.Records.Count;

                var meshDataSeries = new UniformGridDataSeries3D<double>(xSize, zSize)
                {
                    StepX = 1,
                    StepZ = 1,
                    SeriesName = "3DPlot",
                };

                Chart3D1.XAxis.VisibleRange = new DoubleRange(0, xSize);
                Chart3D1.YAxis.VisibleRange = new DoubleRange(0, 1.0);
                Chart3D1.ZAxis.VisibleRange = new DoubleRange(0, zSize);


                int zCount = 0;
                foreach (var rec in group.Records)
                {
                    int xCount = 0;
                    foreach (var measure in rec.Measures)
                    {
                        meshDataSeries[zCount, xCount++] = measure.Value / group.MaxScale;
                    }

                    zCount++;
                }

                surfaceMeshRenderableSeries.DataSeries = meshDataSeries;


                //
                // Bubble
                //
                Chart3D2.XAxis.VisibleRange = new DoubleRange(0, xSize);
                Chart3D2.YAxis.VisibleRange = new DoubleRange(0, group.MaxScale);
                Chart3D2.ZAxis.VisibleRange = new DoubleRange(0, zSize);

                var xyzDataSeries3D = new XyzDataSeries3D<double>();
                zCount = 0;
                foreach (var rec in group.Records)
                {
                    int xCount = 0;
                    foreach (var measure in rec.Measures)
                    {
                        double yVal = measure.Value;
                        double yScaledVal = measure.Value / group.MaxScale;
                        Color color;

                        if (yScaledVal < 0.75)
                            color = Color.FromArgb(0xFF, (byte) (yScaledVal * 255.0), 0x4F, 0x8F);
                        else
                        {
                            int green = (int) (0x4f + (.75 - yScaledVal) * 255);
                            int blue = (int) (0x8f + (.75 - yScaledVal) * 255);
                            if (green < 0)
                                green = 0;
                            if (blue < 0)
                                blue = 0;

                            color = Color.FromArgb(0xFF, (byte) ((yVal / group.MaxScale) * 255.0), (byte) green,
                                (byte) blue);
                        }


                        xyzDataSeries3D.Append(xCount++, yVal, zCount, new PointMetadata3D(color, (float) 1));
                    }

                    zCount++;
                }

                ScatterSeries3D.Shininess = 15;
                ScatterSeries3D.SpecularColor = Colors.White;
                ScatterSeries3D.SpecularStrength = 1;

                ScatterSeries3D.DataSeries = xyzDataSeries3D;
            }
            else
            {
                int xSize = group.Records.Count;
                int zSize = group.Records[0].Measures.Count;

                var meshDataSeries = new UniformGridDataSeries3D<double>(xSize, zSize)
                {
                    StepX = 1,
                    StepZ = 1,
                    SeriesName = "3DPlot",
                };

                Chart3D1.XAxis.VisibleRange = new DoubleRange(0, xSize);
                Chart3D1.YAxis.VisibleRange = new DoubleRange(0, 1.0);
                Chart3D1.ZAxis.VisibleRange = new DoubleRange(0, zSize);

                int xCount = 0;
                foreach (var rec in _groups[0].Records)
                {
                    int zCount = 0;
                    foreach (var measure in rec.Measures)
                    {
                        meshDataSeries[zCount++, xCount] = measure.Value / group.MaxScale;
                    }

                    xCount++;
                    surfaceMeshRenderableSeries.DataSeries = meshDataSeries;
                }
            }
        }

        //---------------------------------------------------------------
        //
        //
        private void DisplayLabel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!(sender is Label l))
                return;

            RecordClass rec = (RecordClass) l.Tag;
            Chart1.UnSelectPlot(rec.PlotInfo);
        }

        //---------------------------------------------------------------
        //
        //
        private void DisplayLabel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!(sender is Label l))
                return;

            RecordClass rec = (RecordClass) l.Tag;
            Chart1.SelectPlot(rec.PlotInfo);

            PlotInfoClass pi = rec.PlotInfo.Clone();
            pi.MinValue = pi.MinDataValue;
            pi.MaxValue = pi.MaxDataValue;

            Chart2.ClearPlots();
            Chart2.AddPlot(pi);
            Chart2.ZoomFullChart(60, true);
        }

        //---------------------------------------------------------------
        //
        //
        private void ReadGroupInitFile(string fileName)
        {
            StackPanelGroups.Children.Clear();
            _groups = new List<GroupDisplayClass>();

            foreach (var rec in Data.AllRecords)
                rec.Used = false;

            string baseNameIni = fileName.Replace(".csv", ".ini");
            string baseNameMini = fileName.Replace(".csv", ".mini");

            List<string> originalLines = null;

            if (_fileNames.Count == 1)
            {
                _groupIniFileName = baseNameIni;

                if (File.Exists(baseNameIni))
                    originalLines = File.ReadAllLines(baseNameIni).ToList();
            }
            else
            {
                _groupIniFileName = baseNameMini;

                if (File.Exists(baseNameMini))
                    originalLines = File.ReadAllLines(baseNameMini).ToList();
            }

            if (originalLines == null)
                return;

            //
            // GroupName, Color, AxisTitle, MaxScale, DisplaySide, Record1, Record2, ...
            // GroupName, Color, AxisTitle, MaxScale, DisplaySide, Record1, Record2, ...
            // GroupName, Color, AxisTitle, MaxScale, DisplaySide, Record1, Record2, ...
            //

            //
            // Build the groups
            //
            try
            {
                foreach (string groupString in originalLines)
                {
                    var words = groupString.Split(',');
                    Color c = new Color
                    {
                        A = byte.Parse(words[1]),
                        R = byte.Parse(words[2]),
                        G = byte.Parse(words[3]),
                        B = byte.Parse(words[4])
                    };

                    string axisName = words[5];
                    double minScale = double.Parse(words[6]);
                    double maxScale = double.Parse(words[7]);
                    string axisSide = words[8];

                    var gd = MakeGroup(words[0], c, axisName,
                        axisSide.Equals("Right") ? AxisAlignment.Right : AxisAlignment.Left, minScale, maxScale);

                    for (int i = 8; i < words.Length; i++)
                    {
                        var rec = Data.AllRecords.Find(T => T.Name.Equals(words[i]));
                        if (rec == null)
                            continue;

                        if (rec.Used == false)
                        {
                            gd.AddRecord(rec);
                            rec.Used = true;
                        }
                        else
                        {
                            gd.RemoveRecord(rec);
                            rec.Used = false;
                        }
                    }

                    UpdateGroupItemsListBox(gd);
                }

                UserOut.AddString("");
                UserOut.AddString("Read \"" + fileName + "\" init file", Colors.Black, 0, true);
                UserOut.AddString("");
            }
            catch
            {
                UserOut.AddString("Failed to read \"" + fileName + "\" init file", Colors.Red, 0, true);
            }
        }

        //---------------------------------------------------------------
        //
        //
        private void WriteGroupInitFile()
        {
            if (_groupIniFileName == null)
                return;
            //
            // GroupName, Color, AxisTitle, MaxScale, DisplaySide, Record1, Record2, ...
            // GroupName, Color, AxisTitle, MaxScale, DisplaySide, Record1, Record2, ...
            // GroupName, Color, AxisTitle, MaxScale, DisplaySide, Record1, Record2, ...
            //

            //
            // Write out the _groups into an ini file with the same name as the
            //   data file but with a .ini extension
            //
            List<string> initStrings = new List<string>();
            foreach (GroupDisplayClass gd in _groups)
            {
                if (gd.ColorPicker1.SelectedColor != null)
                {
                    string val = gd.GroupName + "," +
                                 gd.ColorPicker1.SelectedColor.Value.A + "," +
                                 gd.ColorPicker1.SelectedColor.Value.R + "," +
                                 gd.ColorPicker1.SelectedColor.Value.G + "," +
                                 gd.ColorPicker1.SelectedColor.Value.B + "," +
                                 gd.AxisName + "," +
                                 gd.MinScale.ToString("0") + "," +
                                 gd.MaxScale.ToString("0") + "," +
                                 gd.AxisPosition.ToString() + ",";

                    foreach (var r in gd.Records)
                    {
                        val = val + r.Name + ",";
                    }

                    initStrings.Add(val);
                }
            }

            using (StreamWriter outputFile = new StreamWriter(_groupIniFileName))
            {
                foreach (var line in initStrings)
                {
                    outputFile.WriteLine(line);
                }
            }
        }

        //--------------------------------------------------------------------------------
        //
        //
        public void UpdatePrefixes()
        {
            //
            // Get the prefixes from the textbox
            //
            _prefixWords.Clear();
            var words = TextBlockPrefixs.Text.Split('\n');
            foreach (var word in words)
            {
                if (word.Length <= 0) continue;

                string w = word.Replace("\n", "").Replace("\r", "");
                _prefixWords.Add(w);
            }

            TextBlockPrefixs.Text = "";
            foreach (var word in _prefixWords)
                TextBlockPrefixs.Text = TextBlockPrefixs.Text + word + "\n";
        }

        //--------------------------------------------------------------------------------
        //
        //
        public void SortListOfLabelsByContent(List<Label> labels)
        {
            foreach (Label l in labels)
            {
                string name = l.Content.ToString();
                Regex regexObj = new Regex(@"([\d]+)|([a-zA-Z_]+)");
                Match matchResults = regexObj.Match(name);
                string newName = "";
                while (matchResults.Success)
                {
                    if (Regex.IsMatch(matchResults.Value, @"\d"))
                    {
                        int.TryParse(matchResults.Value, out int val);
                        newName = newName + val.ToString("000");
                    }
                    else
                        newName = newName + matchResults.Value;

                    matchResults = matchResults.NextMatch();
                }

                l.Content = newName;
            }

            labels = labels.OrderBy(T => (T.Name)).ToList();

            //
            // Fix the numbers back
            //
            foreach (Label l in labels)
            {
                string name = l.Content.ToString();

                Regex regexObj = new Regex(@"([\d]+)|([a-zA-Z_]+)");
                Match matchResults = regexObj.Match(name);
                string newName = "";
                while (matchResults.Success)
                {
                    if (Regex.IsMatch(matchResults.Value, @"\d"))
                    {
                        int.TryParse(matchResults.Value, out int val);
                        newName = newName + val.ToString();
                    }
                    else
                        newName = newName + matchResults.Value;

                    matchResults = matchResults.NextMatch();
                }

                l.Content = newName;
            }
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void UpdateGroupItemsListBox(GroupDisplayClass gd)
        {
            //
            // Go through each record and change the name such that
            //    numbers all have at least 3 digits.
            //
            foreach (var rec in gd.Records)
            {
                Regex regexObj = new Regex(@"([\d]+)|([a-zA-Z_]+)");
                Match matchResults = regexObj.Match(rec.Name);
                string newName = "";
                while (matchResults.Success)
                {
                    if (Regex.IsMatch(matchResults.Value, @"\d"))
                    {
                        int.TryParse(matchResults.Value, out int val);
                        newName = newName + val.ToString("000");
                    }
                    else
                        newName = newName + matchResults.Value;

                    matchResults = matchResults.NextMatch();
                }

                rec.Name = newName;
            }

            //
            // Sort them so they look right when displayed.
            //
            gd.Records = gd.Records.OrderBy(T => (T.Name)).ToList();

            //
            // Fix the numbers back
            //
            foreach (var rec in gd.Records)
            {
                Regex regexObj = new Regex(@"([\d]+)|([a-zA-Z_]+)");
                Match matchResults = regexObj.Match(rec.Name);
                string newName = "";
                while (matchResults.Success)
                {
                    if (Regex.IsMatch(matchResults.Value, @"\d"))
                    {
                        int.TryParse(matchResults.Value, out int val);
                        newName = newName + val.ToString();
                    }
                    else
                        newName = newName + matchResults.Value;

                    matchResults = matchResults.NextMatch();
                }

                rec.Name = newName;
            }

            ListBoxGroupItems.Items.Clear();
            foreach (var rec in gd.Records)
            {
                Label l = new Label()
                {
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    FontSize = 12,
                    Height = 16,
                    //Background = Brushes.Gainsboro,
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    Content = "_" + rec.Name, // this is done becuase the first _ is not shown.
                    Padding = new Thickness(0, 0, 0, 0),
                    Tag = rec,
                };
                ListBoxGroupItems.Items.Add(l);
                l.MouseEnter += GroupListBoxItemMouseEnter;
                l.MouseLeave += GroupListBoxItemMouseLeave;
            }

            ListBoxGroupItems.Foreground = Brushes.Black;
            if (gd.ColorPicker1.SelectedColor != null)
                ListBoxGroupItems.BorderBrush = new SolidColorBrush((Color) gd.ColorPicker1.SelectedColor);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void GroupListBoxItemMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Chart1.UnSelectAllPlots();
            RecordClass rec = (RecordClass) ((Label) sender)?.Tag;
            if (rec == null)
                return;

            Chart1.UnSelectPlot(rec.PlotInfo);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void GroupListBoxItemMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Chart1.UnSelectAllPlots();

            RecordClass rec = (RecordClass) ((Label) sender)?.Tag;
            if (rec == null)
                return;

            Chart1.SelectPlot(rec.PlotInfo);
        }

        //--------------------------------------------------------------------------------
        //
        //
        public void PlotGroups()
        {
            Chart1.ClearPlots();
            Chart1.ShowSlope(CheckBoxShowSlope.IsChecked == true);
            int.TryParse(TextBoxSlopeRange.Text, out int range);
            Chart1.SetSlopeRange(range);

            //
            // IFF there is a group with a name that has "3D" in it - then show the
            //    3D tabs. If not, hide them.
            //
            if (_groups.Exists(T => T.GroupName.Contains("3D")))
            {
                ((TabItem) TabControlMainPlot.Items[1]).Visibility = Visibility.Visible;
                ((TabItem) TabControlMainPlot.Items[2]).Visibility = Visibility.Visible;
            }
            else
            {
                ((TabItem) TabControlMainPlot.Items[1]).Visibility = Visibility.Hidden;
                ((TabItem) TabControlMainPlot.Items[2]).Visibility = Visibility.Hidden;
            }

            //
            // make all axis that have the same name have the same maxScale.
            //
            for (int i = 0; i < _groups.Count - 1; i++)
            {
                for (int j = i + 1; j < _groups.Count; j++)
                    if (_groups[i].AxisName.Equals(_groups[j].AxisName))
                    {
                        _groups[j].MinScale = _groups[i].MinScale;
                        _groups[j].MaxScale = _groups[i].MaxScale;
                    }
            }

            foreach (var group in _groups)
            {
                foreach (var record in group.Records)
                {
                    record.PlotInfo.AxisName = group.GroupName;
                    record.PlotInfo.YAxisID = group.AxisName;
                    record.PlotInfo.IsVisible = group.ButtonShow.Content.ToString().Contains("Hide");

                    record.PlotInfo.Alignment = group.AxisPosition;
                    if (group.ColorPicker1.SelectedColor != null)
                        record.PlotInfo.SeriesColor = (Color) group.ColorPicker1.SelectedColor;

                    record.PlotInfo.MinScale = group.MinScale;
                    record.PlotInfo.MaxScale = group.MaxScale;
                    record.Used = true;
                    record.PlotInfo.Tag = group;

                    //
                    // Show points if checked.
                    //
                    record.PlotInfo.PointMarker = CheckBoxShowPoints.IsChecked == true
                        ? new EllipsePointMarker() {Fill = Colors.White, Height = 6, Width = 6}
                        : null;

                    Chart1.AddPlot(record.PlotInfo);
                }

                //
                // Make the average plot if checked.
                //
                if (group.CheckboxCreateAverage.IsChecked == true && group.Records.Count > 1)
                {
                    PlotInfoClass averagedPlot = group.Records[0].PlotInfo.Clone();
                    averagedPlot.DashArray = new double[] {1, 5};
                    averagedPlot.StrokeThickness = 3;
                    averagedPlot.IsVisible = true;
                    int len = group.Records[0].Measures.Count;

                    averagedPlot.DataSeries = new XyDataSeries<TimeSpan, double>
                    {
                        SeriesName = group.Name + "_AveragePlot",
                    };

                    for (int i = 0; i < len; i++)
                    {
                        double val = 0;
                        foreach (var record in group.Records)
                        {
                            val = record.DataSeries.YValues[i] + val;
                        }

                        val = val / group.Records.Count;
                        averagedPlot.DataSeries.Append(group.Records[0].DataSeries.XValues[i], val);
                    }

                    Chart1.AddPlot(averagedPlot);
                }

                //
                // Make the Summed Plot if checked.
                //
                if (group.CheckboxCreateSum.IsChecked == true)
                {
                    PlotInfoClass summedPlot = group.Records[0].PlotInfo.Clone();
                    summedPlot.DashArray = new double[] {1, 5};
                    summedPlot.IsVisible = true;
                    summedPlot.StrokeThickness = 3;
                    int len = group.Records[0].Measures.Count;

                    summedPlot.DataSeries = new XyDataSeries<TimeSpan, double>
                    {
                        SeriesName = group.Name + "_SummedPlot",
                    };

                    for (int i = 0; i < len; i++)
                    {
                        double val = 0;
                        foreach (var p in group.Records)
                        {
                            val = p.DataSeries.YValues[i] + val;
                        }

                        summedPlot.DataSeries.Append(group.Records[0].DataSeries.XValues[i], val);
                    }

                    Chart1.AddPlot(summedPlot);
                }
            }

            //
            // Update the colors in the listbox
            //
            foreach (Label lab in ListBoxAllRecords.Items)
            {
                RecordClass record = (RecordClass) lab.Tag;
                lab.Foreground = Brushes.Black;

                foreach (var group in _groups)
                {
                    if (group.Records.Exists(T => T == record))
                    {
                        lab.Foreground = new SolidColorBrush(group.PlotColor);
                    }
                }
            }

            UpdateChartMenu();

            //
            // Show it.
            //
            Chart1.RePlotAll();
            Chart1.ZoomFullChart(120, false, 3);
            MachineState.ChartLegend.ShowLegend = CheckBoxShowLegend.IsChecked == true;

            //
            // Make the flows!
            //
            MachineState.Initialize(Data.AllRecords, TextBoxValueFlowKey1.Text, TextBoxValueFlowKey2.Text, TextBoxTemperatureKey1.Text);
            UserOut.AddString("Processing Gas Flows ", Colors.Blue, 0, true);
            UserOut.AddString(
                "Using Key1 \"" + TextBoxValueFlowKey1.Text + "\" and Key2 \"" + TextBoxValueFlowKey2.Text + "\"",
                Colors.Blue, 3, false);
            
            if (MachineState.Valid)
            {
                UserOut.AddString("There were " + MachineState.Gases.Count + " Gas Flows Used", Colors.Blue, 3, false);
                UserOut.AddString("Machine State Complete", Colors.Blue, 0, true);
            }
            else
            {
                UserOut.AddString("No Gas Flows were found", Colors.Red, 3, true);
                UserOut.AddString("Machine State is not plotted", Colors.Red, 3, true);
                UserOut.AddString("Machine State Complete", Colors.Red, 0, true);
            }
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void UpdateChartMenu()
        {
            //
            // Make a menu for the chart.
            //
            ContextMenu pMenu = new ContextMenu();
            MenuItem item1 = new MenuItem
            {
                Header = "CurveFit"
            };
            item1.Click += CurveFitSelectedPlot;
            pMenu.Items.Add(item1);

            MenuItem item2 = new MenuItem
            {
                Header = "Delete"
            };
            item2.Click += DeleteSelectedPlot;
            pMenu.Items.Add(item2);

            foreach (var group in _groups)
            {
                MenuItem menu = new MenuItem
                {
                    Header = "Move To: \"" + group.GroupName + "\"",
                };

                menu.Click += MenuFromChartMoveToNewGroup;
                pMenu.Items.Add(menu);
            }

            Chart1.ContextMenu = pMenu;
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void ButtonDoIt_Click(object sender, RoutedEventArgs e)
        {
            ReadAndProcessFiles();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void ButtonInit_Click(object sender, RoutedEventArgs e)
        {
            string initFile = _initialization.FilePath + "\\" + _initialization.FileName;
            if (File.Exists(initFile))
                File.Delete(initFile);

            Process.GetCurrentProcess().Kill();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void ButtonGetFiles_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                FileName = "",
                DefaultExt = ".CSV",
                Filter = "CSV documents (.csv)|*.csv",
                InitialDirectory = TextBoxFileName.Text,
                Multiselect = true,
            };

            if (openFileDialog.ShowDialog() == true)
            {
                //
                // Get the fileNames and execute!
                //
                TextBoxFileName.Text = openFileDialog.FileName;
                _fileNames = openFileDialog.FileNames.ToList();
                ReadAndProcessFiles();
            }
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void ButtonWriteFiles_Click(object sender, RoutedEventArgs e)
        {
            if (_fileNames == null)
                return;

            //
            // Write out all the data
            //
            string resultsDir = Path.GetDirectoryName(_fileNames[0]) + "\\Results";
            if (Directory.Exists(resultsDir) == false)
                Directory.CreateDirectory(resultsDir);

            if (CheckBoxCreateUniqueFileForEachGroup.IsChecked == true)
            {
                foreach (var group in _groups)
                {
                    string outFile = resultsDir + "\\" + group.GroupName + ".csv";
                    Data.WriteListOfRecords(outFile, group.Records, CheckBoxReverseTags.IsChecked == true);
                    UserOut.AddString("File \"" + outFile + "\" written", Colors.Blue, 0, true);
                }
            }
            else
            {
                string outFile = resultsDir + "\\All.csv";
                Data.WriteAll(outFile, CheckBoxReverseTags.IsChecked == true);
                UserOut.AddString("File \"" + outFile + "\" written", Colors.Blue, 0, true);
            }
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void ButtonMakeGroup_Click(object sender, RoutedEventArgs e)
        {
            var gd = MakeGroup("Group" + _groups.Count, Colors.Blue,
                "Default", AxisAlignment.Right, 0, 1000);

            AddSelectedRecordsToGroup(gd);
            UpdateGroupItemsListBox(gd);
            ListBoxAllRecords.UnselectAll();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private GroupDisplayClass MakeGroup(string name, Color c, string axisName, AxisAlignment axisAlignment,
            double minScale, double maxScale)
        {
            GroupDisplayClass gd = new GroupDisplayClass
            {
                GroupName = name,
                AxisName = axisName,
                PlotColor = c,
                AxisPosition = axisAlignment,
                MinScale = minScale,
                MaxScale = maxScale,
            };

            gd.MouseEnter += GroupDisplayEventMouseEnter;
            gd.MouseLeave += GroupDisplayEventMouseLeave;
            gd.EventSomethingChanged += GroupDisplayEventSomethingChanged;
            gd.EventShow += GroupDisplayEventShow;
            gd.EventHide += GroupDisplayEventHide;
            gd.EventAddRecords += GroupDisplayEventAddRecord;
            gd.EventDelete += GroupDisplayEventDelete;

            StackPanelGroups.Children.Add(gd);
            _groups.Add(gd);
            return (gd);
        }

        //--------------------------------------------------------------------------------
        //
        //
        public void AddSelectedRecordsToGroup(GroupDisplayClass gd)
        {
            foreach (Label lab in ListBoxAllRecords.SelectedItems)
            {
                RecordClass newRecord = (RecordClass) lab.Tag;

                if (gd.Records.Exists(T => T == newRecord))
                {
                    newRecord.Used = false;
                    gd.RemoveRecord(newRecord);
                    lab.Foreground = Brushes.Black;
                }
                else
                {
                    if (newRecord.Used == false)
                    {
                        newRecord.Used = true;
                        gd.AddRecord(newRecord);
                        lab.Foreground = new SolidColorBrush(gd.PlotColor);
                    }
                }
            }

            ListBoxAllRecords.UnselectAll();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void ButtonReplotAll_Click(object sender, RoutedEventArgs e)
        {
            WriteGroupInitFile();
            PlotGroups();
            ButtonReplotAll.BorderBrush = Brushes.Black;
            ButtonReplotAll.BorderThickness = new Thickness(1);

            Make3DMesh2();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void CheckBoxShowSlope_Click(object sender, RoutedEventArgs e)
        {
            PlotGroups();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void CheckBoxShowPoints_Click(object sender, RoutedEventArgs e)
        {
            PlotGroups();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void GroupDisplayEventAddRecord(object sender, object e)
        {
            GroupDisplayClass gd = sender as GroupDisplayClass;
            if (gd == null)
                return;

            AddSelectedRecordsToGroup(gd);
            UpdateGroupItemsListBox(gd);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void GroupDisplayEventShow(object sender, object e)
        {
            if (!(sender is GroupDisplayClass gd)) return;
            foreach (var rec in gd.Records)
                rec.PlotInfo.IsVisible = true;

            Chart1.RePlotAll();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void GroupDisplayEventHide(object sender, object e)
        {
            if (!(sender is GroupDisplayClass gd)) return;

            foreach (var rec in gd.Records)
                rec.PlotInfo.IsVisible = false;

            Chart1.RePlotAll();
        }

        //---------------------------------------------------------------
        //
        //
        private void GroupDisplayEventDelete(object sender, object e)
        {
            GroupDisplayClass gd = sender as GroupDisplayClass;
            if (gd == null) return;

            foreach (var record in gd.Records)
                record.Used = false;

            _groups.Remove(gd);
            WriteGroupInitFile();
            ReadGroupInitFile(_fileNames[0]);
            PlotGroups();
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void GroupDisplayEventSomethingChanged(object sender, object e)
        {
            ButtonReplotAll.BorderBrush = Brushes.Red;
            ButtonReplotAll.BorderThickness = new Thickness(4);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void GroupDisplayEventMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //LabelGroupPlotItems.Content = "";
            //RectangleGroupColor.Visibility = Visibility.Hidden;
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void GroupDisplayEventMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_selectedGroup != null)
                _selectedGroup.BorderMain.BorderBrush = Brushes.Black;

            GroupDisplayClass gd = sender as GroupDisplayClass;
            if (gd == null)
                return;

            gd.BorderMain.BorderBrush = Brushes.Red;
            _selectedGroup = gd;

            UpdateGroupItemsListBox(gd);

            if (gd.ColorPicker1.SelectedColor == null) return;

            RectangleGroupColor.Visibility = Visibility.Visible;
            RectangleGroupColor.Fill = new SolidColorBrush((Color) gd.ColorPicker1.SelectedColor);
            LabelGroupPlotItems.Content = gd.GroupName;
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void ListBoxGroupItems_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_selectedGroup == null || ListBoxGroupItems.SelectedItem == null)
                return;

            RecordClass rec = (RecordClass) ((Label) ListBoxGroupItems.SelectedItem).Tag;
            rec.Used = false;

            _selectedGroup.RemoveRecord(rec);
            ListBoxGroupItems.Items.Remove(ListBoxGroupItems.SelectedItem);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void TextBoxSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ListBoxAllRecords.Items.Clear();
            var allMatches = Data.AllRecords.FindAll(T => T.Name.Contains(TextBoxSearch.Text));

            foreach (var v in allMatches)
                ListBoxAllRecords.Items.Add(v.DisplayLabel);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void ButtonClearSearch_Click(object sender, RoutedEventArgs e)
        {
            TextBoxSearch.Text = "";
            ListBoxAllRecords.Items.Clear();
            foreach (var v in Data.AllRecords)
                ListBoxAllRecords.Items.Add(v.DisplayLabel);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void MenuFromChartMoveToNewGroup(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
                return;

            //
            // Find the plot that is active.
            //
            if (Chart1.CurrentPlotInfo != null)
            {
                RecordClass sourceRecord = Data.AllRecords.Find(T => T.PlotInfo.Equals(Chart1.CurrentPlotInfo));
                if (sourceRecord == null)
                    return;

                //
                // Remove the record from the old group
                //
                GroupDisplayClass group = (GroupDisplayClass) sourceRecord.PlotInfo.Tag;
                group.RemoveRecord(sourceRecord);

                //
                // find the new group name
                //
                string targetName = menuItem.Header.ToString().Substring(menuItem.Header.ToString().IndexOf('"'));
                targetName = targetName.Replace("\"", "");

                GroupDisplayClass newGroup = _groups.Find(T => T.GroupName.Contains(targetName));
                newGroup.AddRecord(sourceRecord);

                PlotGroups();
            }
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void SliderVerticalShift1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Camera1.Target = new Vector3(0, (float) SliderVerticalShift1.Value * -10, 0);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void SliderVerticalShift2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Camera2.Target = new Vector3(0, (float) SliderVerticalShift2.Value * -10, 0);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void CheckBoxShowLegend_Click(object sender, RoutedEventArgs e)
        {
                MachineState.ChartLegend.ShowLegend = CheckBoxShowLegend.IsChecked == true;
        }

        //--------------------------------------------------------------------------------
        //
        //
        private void CheckBoxHighlightPlot_Click(object sender, RoutedEventArgs e)
        {
            Chart1.EnableHighlight = CheckBoxHighlightPlot.IsChecked == true;
        }
    }

    //*********************************************************************
    //
    //
    public class DataClass
    {
        public List<RecordClass> AllRecords;
        public List<RecordClass> LabquestRecords;
        public List<RecordClass> PLCRecords;

        private List<string> _prefixWords;
        private List<List<string>> _fileDataLines;

        private List<string> _allFileNames;
        private List<string> _plcFileNames;
        private List<string> _labquestFileNames;
        private int _timeIndex;
        private int _dataIndex;
        private bool _zeroTime;
        private bool _removeCommonPrefix;
        private double _labquestHoursOffset;
        public TimeSpan TotalTimeSpan;
        public DateTime StartTime;
        public DateTime StopTime;

        public List<UserOutDataClass> UserOut;
        public bool Working;
        public Thread ThreadReadFiles;

        public event EventHandler<object> ReadComplete;

        //---------------------------------------------------------------
        //
        //
        public DataClass()
        {
            UserOut = new List<UserOutDataClass>();
            AllRecords = new List<RecordClass>();
            LabquestRecords = new List<RecordClass>();
            PLCRecords = new List<RecordClass>();
        }

        //---------------------------------------------------------------
        //
        //
        private void ReadFiles()
        {
            UserOut.Clear();
            AllRecords.Clear();
            LabquestRecords.Clear();
            PLCRecords.Clear();

            _fileDataLines = new List<List<string>>();

            //
            foreach (var plcFile in _plcFileNames)
                GetPLCRecords(plcFile);

            //
            // The files may be out of order in time. Sort them so that they
            //   are in the correct time order. 
            //
            if (FixRecordsOrder())
                ProcessPLCRecords();

            //
            // Process Labquest file if there.
            //
            foreach (var labquestFileName in _labquestFileNames)
                ReadLabquestFile(labquestFileName, _labquestHoursOffset);

            foreach (var plcRecord in PLCRecords)
                AllRecords.Add(plcRecord);

            foreach (var labquestRecord in LabquestRecords)
                AllRecords.Add(labquestRecord);


            //
            // Sort the data so that all names with numbers have 
            //    at lest 3 digits.
            //
            foreach (var rec in AllRecords)
            {
                Regex regexObj = new Regex(@"([\d]+)|([a-zA-Z_]+)");
                Match matchResults = regexObj.Match(rec.Name);
                string newName = "";
                while (matchResults.Success)
                {
                    if (Regex.IsMatch(matchResults.Value, @"\d"))
                    {
                        int.TryParse(matchResults.Value, out int val);
                        newName = newName + val.ToString("000");
                    }
                    else
                        newName = newName + matchResults.Value;

                    matchResults = matchResults.NextMatch();
                }

                rec.Name = newName;
            }

            //
            // Sort them
            //
            AllRecords = AllRecords.OrderBy(T => (T.Name)).ToList();

            //
            // Fix the numbers back
            //
            foreach (var rec in AllRecords)
            {
                Regex regexObj = new Regex(@"([\d]+)|([a-zA-Z_]+)");
                Match matchResults = regexObj.Match(rec.Name);
                string newName = "";
                while (matchResults.Success)
                {
                    if (Regex.IsMatch(matchResults.Value, @"\d"))
                    {
                        int.TryParse(matchResults.Value, out int val);
                        newName = newName + val.ToString();
                    }
                    else
                        newName = newName + matchResults.Value;

                    matchResults = matchResults.NextMatch();
                }

                rec.Name = newName;
            }

            //
            // Were done!
            //
            Working = false;
            if (ThreadReadFiles != null)
                ReadComplete?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        public bool ReadFilesBackground(List<string> fileNames, List<string> prefixes, int timeIndex, int dataIndex,
            bool zeroTime,
            bool removeCommonPrefix,
            double labquestHoursOffset)
        {
            _labquestHoursOffset = labquestHoursOffset;
            if (Working)
                return (false);

            _allFileNames = fileNames;
            if (_allFileNames == null)
                return false;

            _prefixWords = prefixes;
            _timeIndex = timeIndex;
            _dataIndex = dataIndex;
            _zeroTime = zeroTime;
            _removeCommonPrefix = removeCommonPrefix;

            //
            // Separate out the PLC files from the labquest files. You will know its a labquest file
            //   because the first line will have the word "Run 1:" in it. If found, save 
            //   and process after all the others so that the time lines up.
            //
            _plcFileNames = new List<string>();
            _labquestFileNames = new List<string>();
            foreach (var fileName in _allFileNames)
            {
                try
                {
                    System.IO.StreamReader file = new System.IO.StreamReader(fileName);
                    string line = file.ReadLine();
                    file.Close();
                    if (line != null)
                    {

                        if (line.Contains("Run 1:"))
                        {
                            _labquestFileNames.Add(fileName);
                        }
                        else
                        {
                            _plcFileNames.Add(fileName);
                        }
                    }
                }
                catch
                {
                    AddString("*** Error   File \"" + fileName + "\" is being used by another process", Colors.Red, 0,
                        true);
                    AddString("*** Abort Process", Colors.Red, 0, true);
                    Working = false;
                    return (false);
                }
            }

            ThreadReadFiles = new Thread(ReadFiles);
            ThreadReadFiles.Start();
            Working = true;

            return (true);
        }

        //---------------------------------------------------------------
        //
        //
        public bool ReadLabquestFile(string fileName, double hoursOffset)
        {
            if (fileName.Length == 0)
                return false;

            if (File.Exists(fileName) == false)
                return false;

            AddString("Reading Labquest File \"" + fileName + "\" ", Colors.Black, 0, true);


            //
            //  TimeShift=-1.2
            //  Run 1: Time (h),Run 1: Oxygen Gas (%),Run 1: Oxygen Gas (%),Run 1: Relative Humidity (%)
            //
            //  Note - the above can repeat and each column represents a new time that follows
            //  the last as shown below:
            //
            //   Run 1: Time (h),Run 1: Oxygen Gas (%),Run 1: Oxygen Gas (%),Run 1: Relative Humidity (%),Run 2: Time (h),Run 2: Oxygen Gas (%),Run 2: Oxygen Gas (%),Run 2: Relative Humidity (%),Run 3: Time (h),Run 3: Oxygen Gas (%),Run 3: Oxygen Gas (%),Run 3: Relative Humidity (%),Run 4: Time (h),Run 4: Oxygen Gas (%),Run 4: Oxygen Gas (%),Run 4: Relative Humidity (%),Run 5: Time (h),Run 5: Oxygen Gas (%),Run 5: Oxygen Gas (%),Run 5: Relative Humidity (%)
            //       0,20.54,20.35,19.83,0,0.26,0.19,9.05,0,0.26,0.19,9.5,0,0.28,0.31,9.24,0,0.31,0.41,9.28
            //       0.02,20.56,20.38,19.53,,,,,,,,,0.02,0.31,0.38,9.46,0.02,0.33,0.48,9.58
            //       0.03,20.59,20.39,10.49,,,,,,,,,,,,,0.03,0.33,0.53,9.01
            //      
            //
            List<string> lines = File.ReadAllLines(fileName).ToList();
            var header = lines[0].Split(',');
            int indexOffset = 1;

            int groupSize = header.Length;
            for (int i = 1; i < header.Length; i++)
            {
                if (header[i].Contains("2"))
                {
                    groupSize = i;
                    break;
                }
            }

            int runs = header.Length / groupSize;


            AddString("There were " + groupSize + " unique records", Colors.Blue, 3);
            AddString("There were " + header.Length + " data entries ", Colors.Blue, 3);
            if (runs == 1)
                AddString("There was 1 run", Colors.Blue, 3);
            else
                AddString("There were " + runs + " runs", Colors.Blue, 3);

            for (int i = 1; i < groupSize; i++)
            {
                int index = header[i].IndexOf(":", StringComparison.Ordinal) + 1;
                string uh = header[i].Substring(index);
                AddString("Header: \"" + uh + "\" found", Colors.Blue, 6);
            }

            //
            // Get the starting time if its there in the records.
            //
            TimeSpan startTime = new TimeSpan(0);
            if (LabquestRecords.Count != 0)
                startTime = LabquestRecords[0].DataSeries.XValues[0];

            List<double>[] dataList = new List<double>[header.Length];
            for (int i = 0; i < header.Length; i++)
            {
                dataList[i] = new List<double>();
            }

            List<RecordClass> newRecords = new List<RecordClass>();
            for (int i = 0; i < header.Length; i++)
            {
                if (i % groupSize == 0)
                    continue;
                string name = Path.GetFileNameWithoutExtension(fileName);
                RecordClass rec = new RecordClass("LQ_" + name + "_" + header[i]);
                newRecords.Add(rec);
            }

            for (int i = indexOffset; i < lines.Count; i++)
            {
                if (lines[i].Length < 10)
                    continue;

                var words = lines[i].Split(',');
                DateTime time = new DateTime(0);
                int recIndex = 0;
                for (int j = 0; j < header.Length; j++)
                {

                    if (words[j].Length > 0)
                    {
                        //
                        // The first record of the group is the time, use
                        //    that to get the time.
                        //
                        if (j % groupSize == 0)
                        {
                            //
                            // Get the time and add the offset. If it's negative, just ignore
                            //   the data and continue.
                            //
                            double.TryParse(words[j], out double hours);
                            time = (startTime + TimeSpan.FromHours(hours)).ToDateTime();
                        }
                        else
                        {
                            double.TryParse(words[j], out double value);
                            newRecords[recIndex].AddEvent(time, value);
                        }
                    }

                    if (j % groupSize != 0)
                        recIndex++;
                }
            }

            foreach (var rec in newRecords)
            {
                rec.BuildData(_zeroTime, hoursOffset);
                LabquestRecords.Add(rec);
            }

            AddString("Processed " + lines.Count + " lines", Colors.Blue, 3);
            AddString("Complete", Colors.Blue, 3);

            return (true);
        }

        //---------------------------------------------------------------
        //
        //
        public bool WriteAll(string fileName, bool reverse)
        {
            return (WriteListOfRecords(fileName, AllRecords, true));
        }

        //---------------------------------------------------------------
        //
        //
        public bool WriteListOfRecords(string fileName, List<RecordClass> records, bool reverse)
        {
            if (records.Count == 0)
                return false;

            try
            {
                using (StreamWriter outputFile = new StreamWriter(fileName))
                {
                    //
                    //  Use Names on top and dates on left.
                    //
                    if (reverse)
                    {
                        //
                        // First Column is dead.
                        //
                        outputFile.Write(",");
                        foreach (var record in records)
                            outputFile.Write(record.Name + ",");

                        outputFile.Write("\n");
                        RecordClass firstRecord = records[0];

                        for (int i = 0; i < firstRecord.Measures.Count; i++)
                        {
                            outputFile.Write(firstRecord.Measures[i].Date + ",");

                            int index = 0;
                            foreach (var t in records)
                            {
                                if (index >= t.Measures.Count)
                                    break;

                                if (i >= t.Measures.Count)
                                    break;

                                outputFile.Write(t.Measures[i].Value.ToString("0000.0") + ",");
                            }

                            outputFile.Write("\n");
                        }
                    }

                    //
                    // Use Dates on top, names on left
                    //
                    else
                    {
                        //
                        // First Column
                        //
                        outputFile.Write(",");
                        RecordClass firstRecord = records[0];

                        foreach (var date in firstRecord.Measures)
                        {
                            outputFile.Write(date.Date + ",");
                        }

                        outputFile.Write("\n");
                        foreach (var record in records)
                        {
                            outputFile.Write(record.Name + ",");

                            foreach (var measure in record.Measures)
                            {
                                outputFile.Write(measure.Value.ToString("0000.0") + ",");
                            }

                            outputFile.Write("\n");
                        }
                    }
                }

                return (true);
            }
            catch
            {
                return (false);
            }
        }

        //---------------------------------------------------------------
        //
        //
        public void AddString(string line, Color lineColor, int indent = 0, bool bold = false)
        {
            UserOutDataClass uo = new UserOutDataClass(line, lineColor, indent, bold);
            UserOut.Add(uo);
        }

        //---------------------------------------------------------------
        //
        //
        private bool GetPLCRecords(string fileName)
        {
            AddString(" ", Colors.Black);
            AddString("Reading file \"" + fileName + "\"", Colors.Black, 0, true);

            //
            // Check to see if it's there.
            //
            if (fileName == null || (File.Exists(fileName) == false))
            {
                AddString("*** File \"" + fileName + "\" not found", Colors.Red);
                return false;
            }

            //
            // Read the data, clean it
            //
            List<string> originalLines = File.ReadAllLines(fileName).ToList();
            if (originalLines.Count == 0)
            {
                AddString("*** No data in file \"" + fileName + "\" - ABORT", Colors.Red);
                return false;
            }

            List<string> dataLines = CleanInputData(originalLines);
            if (dataLines.Count == 0)
            {
                AddString("*** Cleaning input killed all data   - ABORT", Colors.Red);
                return false;
            }

            //
            // remove common prefix
            //
            if (_removeCommonPrefix)
            {
                dataLines = RemoveCommonWordsInName(dataLines).ToList();
                if (dataLines.Count == 0)
                {
                    AddString("*** Method RemoveCommonWordsInName killed all data   - ABORT", Colors.Red);
                    return false;
                }
            }

            //
            // Save this for later.
            //
            _fileDataLines.Add(dataLines);

            //
            // Find all the unique names and that will be the records.
            //
            int newRecordsAdded = 0;
            foreach (string line in dataLines)
            {
                var words = line.Split(',');

                string name = words[0];

                var record = PLCRecords.Find(T => T.Name.Equals(name));
                if (record == null)
                {
                    RecordClass t = new RecordClass(name);
                    PLCRecords.Add(t);
                    newRecordsAdded++;
                }
            }

            if (newRecordsAdded == 0)
                AddString("No new PLCRecords were added", Colors.Blue, 3);
            else
                AddString("There were  " + newRecordsAdded + " unique PLCRecords added", Colors.Blue, 3);

            AddString(
                "There were " + originalLines.Count.ToString("N0", CultureInfo.InvariantCulture) +
                " lines added from this file", Colors.Blue, 3);


            return true;
        }

        //--------------------------------------------------------------------------------
        //
        //
        private List<string> CleanInputData(List<string> fileLines)
        {
            //
            // Remove any garbage in the lines. Garbage is anything that does not
            //   start with HIM
            //
            List<string> cleanedLines = new List<string>();
            int index = 0;
            int errorCount = 0;
            foreach (var line in fileLines)
            {
                string fixedLine = line.Replace("\"", "");
                var words = line.Replace("\"", "").Split(',');

                bool found = false;
                foreach (var prefix in _prefixWords)
                {
                    if (words[0].Contains(prefix))
                    {
                        cleanedLines.Add(fixedLine);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    AddString("Removed \"" + fixedLine + "\"  from file on line " + index, Colors.Red, 3);
                    errorCount++;

                    if (errorCount > 20)
                    {
                        AddString(errorCount + " errors found! - processing file halted", Colors.Red, 3, true);
                        break;
                    }
                }

                index++;
            }

            return (cleanedLines);
        }

        //--------------------------------------------------------------------------------
        //
        //
        private List<string> RemoveCommonWordsInName(List<string> fileLines)
        {
            string commonPrefix = fileLines.Aggregate(GetCommonPrefixFromString);

            if (commonPrefix.Length > 0)
            {
                AddString("The common prefix \"" + commonPrefix + "\" was removed from each line ",
                    Colors.Blue, 3);
            }

            //
            // Remove it from the lines.
            //
            List<string> result =
                (fileLines.Select(s => s.Substring(commonPrefix.Length, s.Length - commonPrefix.Length)))
                .ToList();
            return result;
        }

        //--------------------------------------------------------------------------------
        //
        //
        private string GetCommonPrefixFromString(string s1, string s2)
        {
            int prefixLength = 0;

            for (int i = 0; i < Math.Min(s1.Length, s2.Length); i++)
            {
                if (s1[i] != s2[i])
                    break;

                prefixLength++;
            }

            string result = s1.Substring(0, prefixLength);
            return result;
        }

        //---------------------------------------------------------------
        //
        //
        private bool FixRecordsOrder()
        {
            //
            // Now we have unique records - so read all the records and put the data
            //   in the right position.
            //
            bool ok = false;
            try
            {
                while (!ok)
                {
                    ok = true;
                    for (int i = 0; i < _fileDataLines.Count - 1; i++)
                    {
                        var words1 = _fileDataLines[i][0].Split(',');
                        var words2 = _fileDataLines[i + 1][0].Split(',');

                        DateTime d1 = DateTime.Parse(words1[_timeIndex]);
                        DateTime d2 = DateTime.Parse(words2[_timeIndex]);

                        if (d2 < d1)
                        {
                            List<string> s1 = _fileDataLines[i];
                            _fileDataLines[i] = _fileDataLines[i + 1];
                            _fileDataLines[i + 1] = s1;
                            ok = false;
                            break;
                        }
                    }
                }

                return (true);
            }
            catch
            {
                AddString("*** Time Column failed, abort", Colors.Red);
                return (false);
            }
        }

        //---------------------------------------------------------------
        //
        //
        private bool ProcessPLCRecords()
        {
            if (_fileDataLines.Count == 0)
                return false;

            //
            // Now we have unique records - so read all the records and put the data
            //   in the right position.
            //
            List<string> allDataLines = new List<string>();
            foreach (var listOfLines in _fileDataLines)
            {
                allDataLines.AddRange(listOfLines);
            }


            int totalLineCount = 0;
            foreach (string line in allDataLines)
            {
                var words = line.Split(',');
                if (words.Length != 5)
                    continue;

                //
                // Get the corresponding record.
                //
                RecordClass dr = PLCRecords.Find(T => T.Name.Equals(words[0]));
                if (dr != null)
                {
                    if (dr.AddEvent(words[_timeIndex], words[_dataIndex]) != true)
                    {
                        AddString("Error reading data on line " + totalLineCount, Colors.Red, 3);
                    }
                }
                else
                {
                    AddString("Error on line " + totalLineCount + "  -\"" + words[0] + "\" is not valid", Colors.Red, 3,
                        true);
                }

                totalLineCount++;
            }

            if (PLCRecords[0].Measures.Count == 0)
            {
                AddString("There were no valid records to process", Colors.Red);
                return false;
            }

            int uniqueRecords = PLCRecords[0].Measures.Count;
            AddString("There were " + uniqueRecords + " unique data entries for each Record", Colors.Blue, 3);

            //
            // Sort all the data.
            //
            PLCRecords = PLCRecords.OrderBy(T => T.Name).ToList();

            //
            // Build all the data.
            //
            AddString(
                "\nBuilding PLCRecords  - [" + totalLineCount.ToString("N0", CultureInfo.InvariantCulture) +
                " lines to process]", Colors.Blue, 0, true);


            int validCount = 0;
            int allCount = 0;
            foreach (var record in PLCRecords)
            {
                record.Used = false;

                if (record.BuildData(_zeroTime, 0) == false)
                {
                    AddString(record.ErrorString, Colors.Red, 3);
                }
                else
                {
                    validCount++;
                }

                allCount++;

                //if (allCount % 10 == 0)

                AddString(
                    "Processed Record [" + allCount + "] \"" + record.Name + "\" with " + record.Measures.Count +
                    " points.", Colors.Blue, 3);

            }

            TotalTimeSpan = new TimeSpan(0);
            StartTime = PLCRecords[0].Measures[0].Date;
            StopTime = PLCRecords[0].Measures[0].Date;
            foreach (var record in PLCRecords)
            {
                if (TotalTimeSpan < record.TotalTime)
                    TotalTimeSpan = record.TotalTime;

                if (StartTime > record.StartTime)
                    StartTime = record.StartTime;

                if (record.StopTime > StopTime)
                    StopTime = record.StopTime;
            }

            AddString("There were " + (validCount + 1) + " records built", Colors.Blue, 3);
            AddString("Starting time :  " + StartTime.ToLongTimeString() + " " + StartTime.ToLongDateString(),
                Colors.Blue, 3);
            AddString("Stopping time :  " + StopTime.ToLongTimeString() + " " + StopTime.ToLongDateString(),
                Colors.Blue, 3);
            AddString("TimeSpan      :  " + TotalTimeSpan.TotalHours.ToString("0.00") + " hours", Colors.Blue, 3);
            AddString("Processing Complete", Colors.Blue, 0, true);

            return (true);
        }
    }


    //*********************************************************************
    //
    //
    public class UserOutDataClass
    {
        public Color LineColor;
        public string Line;
        public bool Bold;
        public int Indent;

        //---------------------------------------------------------------
        //
        //
        public UserOutDataClass(string line, Color lineColor, int indent = 0, bool bold = false)
        {
            Line = line;
            LineColor = lineColor;
            Indent = indent;
            Bold = bold;
        }
    }

    //*********************************************************************
    //
    //
    public class RecordClass
    {
        public string Name;
        public List<MeasureClass> Measures;
        public PlotInfoClass PlotInfo;
        public XyDataSeries<TimeSpan, double> DataSeries;
        public TimeSpan TotalTime;
        public DateTime StartTime;
        public DateTime StopTime;
        public bool Error;
        public string ErrorString;

        public bool Used { get; set; }

        public Label DisplayLabel
        {
            get
            {
                if (_displayLabel == null)
                {
                    _displayLabel = new Label()
                    {
                        Margin = new Thickness(0),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        FontSize = 12,
                        Height = 16,
                        //Background = Brushes.Gainsboro,
                        Foreground = Brushes.Black,
                        FontWeight = FontWeights.Bold,
                        Content = "_" + Name, // this is done becuase the first _ is not shown.
                        Padding = new Thickness(0, 0, 0, 0),
                        Tag = this,
                    };
                }

                return (_displayLabel);
            }
        }

        private Label _displayLabel;

        //---------------------------------------------------------------
        //
        //
        public RecordClass(string name)
        {
            Name = name;
            Measures = new List<MeasureClass>();

            DataSeries = new XyDataSeries<TimeSpan, double>
            {
                SeriesName = name,
            };

            PlotInfo = new PlotInfoClass()
            {
                AxisName = "",
                SeriesColor = Colors.Red,
                YAxisID = "",
                DataSeries = DataSeries,
                MaxScale = 0,
                MinScale = 0,
                Alignment = AxisAlignment.Left,
                IsVisible = true,
            };

            _displayLabel = null;
        }

        //---------------------------------------------------------------
        //
        //
        public bool AddEvent(string date, string value)
        {
            MeasureClass e = new MeasureClass(date, value);
            if (e.Error == false)
            {
                Measures.Add(e);
                return true;
            }

            //
            // Error!
            //
            return false;
        }


        //---------------------------------------------------------------
        //
        //
        public bool AddEvent(DateTime date, double value)
        {
            MeasureClass e = new MeasureClass(date, value);
            if (e.Error == false)
            {
                Measures.Add(e);
                return true;
            }

            //
            // Error!
            //
            return false;
        }

        //---------------------------------------------------------------
        //
        //
        public bool BuildData(bool zeroTime, double offsetHours)
        {
            int index = 0;

            try
            {
                TimeSpan lastTime = new TimeSpan(0);
                DateTime lastDate = DateTime.Now;
                TimeSpan startingDateTime = zeroTime ? Measures[0].Date.ToTimeSpan() : new TimeSpan(0);
                foreach (var t1 in Measures)
                {
                    //
                    // Create the right time.
                    //
                    TimeSpan t = t1.Date.ToTimeSpan() - startingDateTime + TimeSpan.FromHours(offsetHours);

                    //
                    // Check the time and make sure it's increasing. If not, stop and inform
                    //    error
                    if (t < lastTime)
                    {
                        if (Error == false)
                        {
                            ErrorString = "Record \"" + Name + "\" - date out of order. LastDate: " +
                                          lastDate.ToLongDateString() + "  NewDate: " +
                                          t1.Date.ToLongDateString() + " on Entry: " + index;
                            Error = true;
                        }
                    }
                    else
                    {
                        DataSeries.Append(t, t1.Value);
                        if (PlotInfo.MinDataValue > t1.Value)
                            PlotInfo.MinDataValue = t1.Value;

                        if (PlotInfo.MaxDataValue < t1.Value)
                            PlotInfo.MaxDataValue = t1.Value;

                        lastTime = t;
                        lastDate = t1.Date;
                    }

                    index++;
                }

                DataSeries.InvalidateParentSurface(RangeMode.None);
                StartTime = Measures[0].Date;
                StopTime = Measures[Measures.Count - 1].Date;
                TotalTime = (StopTime - StartTime).ToTimeSpan();

                return (!Error);
            }
            catch
            {
                ErrorString = "Record \"" + Name + "\" did not build right. Failed on entry " + index;
                Error = true;
                return (!Error);
            }
        }

        //---------------------------------------------------------------
        //
        //
        public RecordClass Clone()
        {
            RecordClass clone = new RecordClass(Name);

            return (clone);
        }
    }

    //*********************************************************************
    //
    //
    public class MeasureClass
    {
        public DateTime Date;
        public double Value;
        public bool Error;

        //---------------------------------------------------------------
        //
        //
        public MeasureClass(string date, string value)
        {
            try
            {
                Date = DateTime.Parse(date);
                Value = double.Parse(value);
            }
            catch
            {
                Error = true;
            }
        }

        //---------------------------------------------------------------
        //
        //
        public MeasureClass(DateTime date, double value)
        {
            Date = date;
            Value = value;
        }
    }
}