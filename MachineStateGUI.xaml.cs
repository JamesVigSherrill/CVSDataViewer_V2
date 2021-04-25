using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Visuals;
using SciChart.Charting.Visuals.Annotations;
using SciChart.Charting.Visuals.PaletteProviders;
using SciChart.Charting.Visuals.PointMarkers;
using SciChart.Charting.Visuals.RenderableSeries;
using SciChart.Core.Extensions;
using SciChart.Data.Model;
using Utilities;

namespace CVSDataViewer_V2
{
    //*********************************************************************
    //
    //
    public partial class MachineStateGUI : UserControl
    {
        public List<GasClass> Gases;
        public List<TemperatureClass> TopTemperatures;
        public List<TemperatureClass> BottomTemperatures;

        public int MaxPosition;
        public PlotInfoClass Plot;

        private  XyDataSeries<double, double> _dataSeriesAr;
        private  XyDataSeries<double, double> _dataSeriesArCH4;
        private  XyDataSeries<double, double> _dataSeriesArH2;
        private  XyDataSeries<double, double> _dataSeriesType2;

        private  XyDataSeries<double, double> _dataSeriesPressureUp;
        private  XyDataSeries<double, double> _dataSeriesPressureDown;
        private  XyDataSeries<double, double> _dataSeriesPressure;
        private  XyDataSeries<double, double> _dataSeriesFlow;
        private  XyDataSeries<double, double> _dataSeriesVelocity;

        private  XyDataSeries<double, double> _dataSeriesTopTemperature;
        private  XyDataSeries<double, double> _dataSeriesBottomTemperature;

        private SciChartColorPaletteProvider _paletteProvider;

        private  FastImpulseRenderableSeries _ar;
        private  FastImpulseRenderableSeries _arCH4;
        private  FastImpulseRenderableSeries _arH2;
        private  FastImpulseRenderableSeries _type2;

        private FastMountainRenderableSeries _pressureUp;
        private FastMountainRenderableSeries _pressureDown;
        private  FastMountainRenderableSeries _pressure;
        private  FastMountainRenderableSeries _flow;
        private  FastMountainRenderableSeries _velocity;

        private  FastLineRenderableSeries _topTemperature;
        private  FastLineRenderableSeries _bottomTemperature;
        private  TextAnnotation _textDisplay;

        private BoxAnnotation _annotationDeadZone;
        private BoxAnnotation _annotationDeadZoneNarrow;

        public bool Valid = false;

        //---------------------------------------------------------------
        //
        //
        public MachineStateGUI()
        {
            InitializeComponent();
        }

        //---------------------------------------------------------------
        //
        //
        public void Initialize(List<RecordClass> records, string flowKey1, string flowKey2, string TempKey1)
        {
            Gases = new List<GasClass>();
            MaxPosition = 0;
            foreach (var rec in records)
            {
                if (rec.PlotInfo.RenderSeries == null)
                    continue;

                GasClass gc = new GasClass(rec, flowKey1, flowKey2);
                if (gc.Valid)
                {
                    if (gc.Position > 0)
                    {
                        Gases.Add(gc);
                        if (gc.Position > MaxPosition)
                            MaxPosition = (int) gc.Position;
                    }
                }
            }

            TopTemperatures = new List<TemperatureClass>();
            foreach (var rec in records)
            {
                if (rec.PlotInfo.RenderSeries == null)
                    continue;

                TemperatureClass tc = new TemperatureClass(rec, "top", TempKey1, "HC");
                if (tc.Valid)
                    TopTemperatures.Add(tc);
            }


            BottomTemperatures = new List<TemperatureClass>();
            foreach (var rec in records)
            {
                if (rec.PlotInfo.RenderSeries == null)
                    continue;

                TemperatureClass tc = new TemperatureClass(rec, "bottom", TempKey1, "HC");
                if (tc.Valid)
                    BottomTemperatures.Add(tc);
            }

            _paletteProvider = new SciChartColorPaletteProvider();

            if (Gases.Count == 0)
            {
                Valid = false;
                return;
            }

            _dataSeriesAr = new XyDataSeries<double, double>()
            {
                SeriesName = "Ar",
            };
            _dataSeriesArH2 = new XyDataSeries<double, double>()
            {
                SeriesName = "ArH2",
            };
            _dataSeriesArCH4 = new XyDataSeries<double, double>()
            {
                SeriesName = "ArCH4",
            };
            _dataSeriesType2 = new XyDataSeries<double, double>()
            {
                SeriesName = "Type2",
            };

            _dataSeriesPressureUp = new XyDataSeries<double, double>()
            {
                SeriesName = "PressureUp",
            };
            _dataSeriesPressureDown = new XyDataSeries<double, double>()
            {
                SeriesName = "PressureDown",
            };
            _dataSeriesPressure = new XyDataSeries<double, double>()
            {
                SeriesName = "Pressure",
            };
            _dataSeriesFlow = new XyDataSeries<double, double>()
            {
                SeriesName = "Flow",
            };
            _dataSeriesVelocity = new XyDataSeries<double, double>()
            {
                SeriesName = "Velocity",
            };
            _dataSeriesTopTemperature = new XyDataSeries<double, double>()
            {
                SeriesName = "TopTemperature",
            };
            _dataSeriesBottomTemperature = new XyDataSeries<double, double>()
            {
                SeriesName = "BottomTemperature",
            };


            _ar = new FastImpulseRenderableSeries()
            {
                Name = "Ar",
                DataSeries = _dataSeriesAr,
                PointMarker = new EllipsePointMarker
                {
                    Width = 12,
                    Height = 12,
                    Fill = Colors.Cyan
                },
                Stroke = Colors.Cyan,
                StrokeThickness = 6,
                XAxisId = "XBase",
                YAxisId = "RightFlow",
                IsEnabled = true,
                Visibility = Visibility.Visible,
            };

            _arH2 = new FastImpulseRenderableSeries()
            {
                Name = "ArH2",
                DataSeries = _dataSeriesArH2,
                PointMarker = new EllipsePointMarker
                {
                    Width = 12,
                    Height = 12,
                    Fill = Colors.Red
                },
                Stroke = Colors.Red,
                StrokeThickness = 6,
                XAxisId = "XBase",
                YAxisId = "RightFlow",
                IsEnabled = true,
                Visibility = Visibility.Visible,
            };

            _arCH4 = new FastImpulseRenderableSeries()
            {
                Name = "ArCH4",
                DataSeries = _dataSeriesArCH4,
                PointMarker = new EllipsePointMarker
                {
                    Width = 12,
                    Height = 12,
                    Fill = Colors.Purple
                },
                Stroke = Colors.Purple,
                StrokeThickness = 6,
                XAxisId = "XBase",
                YAxisId = "RightFlow",
                IsEnabled = true,
                Visibility = Visibility.Visible,
            };

            _type2 = new FastImpulseRenderableSeries()
            {
                Name = "Type2",
                DataSeries = _dataSeriesType2,
                PointMarker = new EllipsePointMarker
                {
                    Width = 8,
                    Height = 8,
                    Fill = Colors.Gold
                },
                Stroke = Colors.Gold,
                StrokeThickness = 6,
                XAxisId = "XBase",
                YAxisId = "RightFlow",
                IsEnabled = true,
                Visibility = Visibility.Visible,
            };

            _pressureUp = new FastMountainRenderableSeries()
            {
                Name = "PressureUp",
                DataSeries = _dataSeriesPressureUp,
                Stroke = Colors.Cyan,
                Fill = Brushes.Bisque,
                Opacity = .1,
                StrokeThickness = 2,
                XAxisId = "XBase",
                YAxisId = "LeftPressureFlowVelocity",
                IsEnabled = true,
                Visibility = Visibility.Visible,
            };
            _pressureDown = new FastMountainRenderableSeries()
            {
                Name = "PressureUp",
                DataSeries = _dataSeriesPressureDown,
                Stroke = Colors.Cyan,
                Fill = Brushes.Bisque,
                Opacity = .1,
                StrokeThickness = 2,
                XAxisId = "XBase",
                YAxisId = "LeftPressureFlowVelocity",
                IsEnabled = true,
                Visibility = Visibility.Visible,
            };

            _pressure = new FastMountainRenderableSeries()
            {
                Name = "Pressure",
                DataSeries = _dataSeriesPressure,
                Stroke = Colors.Cyan,
                Fill = Brushes.Bisque,
                Opacity = .1,
                StrokeThickness = 2,
                XAxisId = "XBase",
                YAxisId = "LeftPressureFlowVelocity",
                IsEnabled = true,
                Visibility = Visibility.Visible,
            };

            _flow = new FastMountainRenderableSeries()
            {
                Name = "Flow",
                DataSeries = _dataSeriesFlow,
                Stroke = Colors.Cyan,
                Fill = Brushes.Bisque,
                Opacity = .1,
                StrokeThickness = 2,
                XAxisId = "XBase",
                YAxisId = "LeftPressureFlowVelocity",
                IsEnabled = true,
                Visibility = Visibility.Visible,
            };

            _velocity = new FastMountainRenderableSeries()
            {
                Name = "Velocity",
                DataSeries = _dataSeriesVelocity,
                Stroke = Colors.Cyan,
                Fill = Brushes.Bisque,
                Opacity = .1,
                StrokeThickness = 2,
                XAxisId = "XBase",
                YAxisId = "LeftPressureFlowVelocity",
                IsEnabled = true,
                Visibility = Visibility.Visible,
            };


            _topTemperature = new FastLineRenderableSeries()
            {
                Name = "TopTemperature",
                DataSeries = _dataSeriesTopTemperature,
                Stroke = Colors.Red,
                StrokeThickness = 2,
                XAxisId = "XBase",
                YAxisId = "YAxisLeftTemperature",
                IsEnabled = true,
                Visibility = Visibility.Visible,
                PaletteProvider = (IPaletteProvider) _paletteProvider,
            };

            _bottomTemperature = new FastLineRenderableSeries()
            {
                Name = "BottomTemperature",
                DataSeries = _dataSeriesBottomTemperature,
                Stroke = Colors.DarkOrange,
                StrokeThickness = 2,
                XAxisId = "XBase",
                YAxisId = "YAxisLeftTemperature",
                IsEnabled = true,
                Visibility = Visibility.Visible,
                PaletteProvider = (IPaletteProvider)_paletteProvider,
            };

            ChartMachineState.RenderableSeries.Clear();
            ChartMachineState.RenderableSeries.Add(_ar);
            ChartMachineState.RenderableSeries.Add(_arH2);
            ChartMachineState.RenderableSeries.Add(_arCH4);
            ChartMachineState.RenderableSeries.Add(_type2);
            ChartMachineState.RenderableSeries.Add(_pressure);
            ChartMachineState.RenderableSeries.Add(_flow);
            ChartMachineState.RenderableSeries.Add(_velocity);
            ChartMachineState.RenderableSeries.Add(_topTemperature);
            ChartMachineState.RenderableSeries.Add(_bottomTemperature);

            ChartMachineState.Annotations.Clear();
            int index = 0;
            double pos = 0;
            foreach (var gc in Gases)
            {
                if (Math.Abs(gc.Position - pos) > .01)
                {
                    /*
                    BoxAnnotation cb = new BoxAnnotation()
                    {
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.WhiteSmoke,
                        BorderThickness = new Thickness(1),
                        XAxisId = "XBase",
                        YAxisId = "RightFlow",
                        X1 = gc.Position - 65,
                        X2 = gc.Position + 65,
                        Y1 = -.05,
                        Y2 = -.25,
                        CornerRadius = new CornerRadius(2, 2, 2, 2),
                    };
                    ChartMachineState.Annotations.Add(cb);
                    */
                    TextAnnotation t = new TextAnnotation()
                    {
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(1),
                        XAxisId = "XBase",
                        YAxisId = "RightFlow",
                        FontSize = 10,
                        X1 = gc.Position - 65,
                        Y1 = -.02,
                        //Text = "U" + ((index) / 12 + 1).ToString("0") + "\n" + ((index % 12) + 1).ToString("00"),
                        Text = "U" + ((index) / 12 + 1).ToString("0") + ":" + ((index % 12) + 1).ToString("00"),
                    };
                    index++;
                    ChartMachineState.Annotations.Add(t);
                }

                pos = gc.Position;
            }

            _annotationDeadZone = new BoxAnnotation()
            {
                Background = Brushes.PaleVioletRed,
                CoordinateMode = AnnotationCoordinateMode.RelativeY,
                Opacity = .2,
                BorderBrush = Brushes.WhiteSmoke,
                BorderThickness = new Thickness(2),
                XAxisId = "XBase",
                YAxisId = "LeftPressureFlowVelocity",
                CornerRadius = new CornerRadius(10, 10, 10, 10),
            };
            ChartMachineState.Annotations.Add(_annotationDeadZone);

            _annotationDeadZoneNarrow = new BoxAnnotation()
            {
                Background = Brushes.Transparent,
                CoordinateMode = AnnotationCoordinateMode.RelativeY,
                Opacity = .6,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                XAxisId = "XBase",
                YAxisId = "LeftPressureFlowVelocity",
                CornerRadius = new CornerRadius(1, 1, 1, 1),
            };
            ChartMachineState.Annotations.Add(_annotationDeadZoneNarrow);

            _textDisplay = new TextAnnotation()
            {
                CoordinateMode = AnnotationCoordinateMode.Relative,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                XAxisId = "XBase",
                YAxisId = "RightFlow",
                X1 = .3,
                X2 = .7,
                Y1 = .01,
                Text = "Text",
            };
            ChartMachineState.Annotations.Add(_textDisplay);
            Valid = true;
        }

        //---------------------------------------------------------------
        //
        //
        public void PlotIt(Point mousePoint, double minVelocity, double chamberWidth, double chamberHeight,
            bool showFlow)
        {
            if (!Valid)
                return;

            try
            {
                //
                // The mouseX is in position on the surface of the plot. Convert it to Ticks
                //
                var xCalc = Gases[0].Record.PlotInfo.RenderSeries.XAxis.GetCurrentCoordinateCalculator();
                double xTicks = xCalc.GetDataValue(mousePoint.X);

                long startTick = (long) Gases[0].Record.DataSeries.XValues.First().Ticks;
                long endTick = (long) Gases[0].Record.DataSeries.XValues.Last().Ticks;

                long deltaTick = endTick - startTick;
                long xPos = (long) (xTicks - startTick);
                double percentRun = (xPos / (double) deltaTick);

                if (percentRun > 1)
                    percentRun = 1;
                if (percentRun < 0)
                    percentRun = 0;

                //
                // Find the index at teh right position.
                //
                int colIndex = (int) (percentRun * (Gases[0].Record.DataSeries.XValues.Count - 1));
                _textDisplay.Text = "TotalTime:  " +
                                   Gases[0].Record.DataSeries.XValues[colIndex].TotalHours.ToString("0.000") + "\n" +
                                   Gases[0].Record.DataSeries.XValues[colIndex].ToDateTime().ToLongDateString() + "  " +
                                   "Time: " + Gases[0].Record.DataSeries.XValues[colIndex].ToDateTime().ToLongTimeString();


                _dataSeriesAr.Clear();
                _dataSeriesArH2.Clear();
                _dataSeriesArCH4.Clear();
                _dataSeriesType2.Clear();
                _dataSeriesPressureUp.Clear();
                _dataSeriesPressureDown.Clear();
                _dataSeriesPressure.Clear();
                _dataSeriesFlow.Clear();
                _dataSeriesVelocity.Clear();
                _dataSeriesTopTemperature.Clear();
                _dataSeriesBottomTemperature.Clear();

                //
                // Create the impulses for all gases.
                //
                double maxVal = 0;
                foreach (var gc in Gases)
                {
                    double val = gc.Record.DataSeries.YValues[colIndex];
                    double pos = gc.Position;

                    if (gc.Record.Name.Contains("Type2"))
                        _dataSeriesType2.Append(pos - 20, val);
                    if (gc.Record.Name.Contains("CH4"))
                        _dataSeriesArCH4.Append(pos + 20, val);
                    else if (gc.Record.Name.Contains("H2"))
                        _dataSeriesArH2.Append(pos + 20, val);
                    else
                        _dataSeriesAr.Append(gc.Position, val);

                    //
                    // Find the maxValue of all the impulses. 
                    //
                    if (val > maxVal)
                        maxVal = val;
                }

                bool lastValZero = false;
                double lastVal = 0;
                foreach (var tc in TopTemperatures)
                {
                    double val = tc.Record.DataSeries.YValues[colIndex];
                    double pos = tc.Position;
                    if (val <= 35 || lastValZero)
                        _dataSeriesTopTemperature.Append(pos, lastVal, new PlotColorMetaData(Colors.White));
                    else
                        _dataSeriesTopTemperature.Append(pos, val, new PlotColorMetaData(Colors.Red));

                    if (val <= 35)
                        lastValZero = true;
                    else
                    {
                        lastValZero = false;
                        lastVal = val;
                    }
                }

                lastValZero = false;
                lastVal = 0;
                foreach (var tc in BottomTemperatures)
                {
                    double val = tc.Record.DataSeries.YValues[colIndex];
                    double pos = tc.Position;
                    if (val <= 35 || lastValZero)
                        _dataSeriesBottomTemperature.Append(pos, lastVal, new PlotColorMetaData(Colors.White));
                    else
                        _dataSeriesBottomTemperature.Append(pos, val, new PlotColorMetaData(Colors.DarkOrange));

                    if (val <= 35)
                        lastValZero = true;
                    else
                    {
                        lastValZero = false;
                        lastVal = val;
                    }
                }

                //
                // Create the flow up 
                //
                double runningSumUp = 0;
                List<Point> flowUp = new List<Point>();
                for (int i = 0; i < Gases.Count; i++)
                {
                    double val = Gases[i].Record.DataSeries.YValues[colIndex];
                    double pos = Gases[i].Position;

                    //
                    // Note - divide by 2 becuase the running sum is the TOTAL
                    //   flow - and only 1/2 goes through each end.
                    //
                    if (val > 0)
                    {
                        runningSumUp = runningSumUp + val;
                        _dataSeriesPressureUp.Append(pos, runningSumUp / 2);
                        flowUp.Add(new Point(pos, runningSumUp / 2));
                    }
                }

                // 
                // Create the flow Down
                // 
                double runningSumDown = 0;
                List<Point> flowDown = new List<Point>();
                for (int i = Gases.Count - 1; i >= 0; i--)
                {
                    double val = Gases[i].Record.DataSeries.YValues[colIndex];
                    double pos = Gases[i].Position;

                    if (val > 0)
                    {
                        runningSumDown = runningSumDown + val;
                        _dataSeriesPressureDown.Insert(0, pos, runningSumDown / 2);
                        flowDown.Insert(0, new Point(pos, runningSumDown / 2));
                    }
                }

                double midFlow = runningSumUp / 2.0;

                //
                // Make the Flow and Velocity plot
                //
                double maxFlow = 0;
                double maxVelocity = 0;
                Point[] velocity = new Point[flowUp.Count];

                for (int i = 0; i < flowUp.Count; i++)
                {
                    double pos = flowUp[i].X;
                    double flow = Math.Abs(flowUp[i].Y - flowDown[i].Y);
                    _dataSeriesFlow.Append(pos, flow);

                    if (flow > maxFlow)
                        maxFlow = flow;

                    //
                    // Velocity is the flow / (ChamberHeight * ChamberWidth)
                    //   flow is in liters/min so we need to convert to cubicMM / min
                    //  
                    //   1 liter == 1,000,000 cubic mm.
                    //
                    double vel = (flow * 1000000) / (chamberHeight * chamberWidth); // in mm/min
                    vel = vel / 1000; // now it is M/min
                    vel = vel / 60; // now it is M/sec
                    vel = vel * 100; // now it is cm/sec

                    _dataSeriesVelocity.Append(pos, vel);
                    velocity[i] = new Point(pos, vel);

                    if (vel > maxVelocity)
                        maxVelocity = vel;
                }

                //
                // Make the pressure plot. Becuase the pressure plot 
                //    uses the same scale as the Flow plot - it will not look 
                //    right if you look at velocity. If that is the case,
                //    simply scale it to the velocity value so it looks OK.
                //
                double scale = showFlow ? 1 : maxVelocity / maxFlow;
                for (int i = 0; i < flowUp.Count; i++)
                {
                    double pos = flowUp[i].X;
                    if (flowUp[i].Y < flowDown[i].Y)
                        _dataSeriesPressure.Append(pos, flowUp[i].Y * scale);
                    else
                        _dataSeriesPressure.Append(pos, flowDown[i].Y * scale);
                }

                //
                // Now go through the flows and find where they cross. At that point,
                //    use the two lines (up and down) to find the actual
                //    position of the cross. Use that point to mark the dead zone.
                //
                // The DeadZone chamberWidth will be anywhere near the crossing point where
                //    flow is less than minVelocity 
                //
                for (int i = 1; i < flowUp.Count; i++)
                {
                    if (flowUp[i].Y > flowDown[i].Y)
                    {
                        Point pU1 = flowUp[i - 1];
                        Point pU2 = flowUp[i];

                        Point pD1 = flowDown[i];
                        Point pD2 = flowDown[i - 1];

                        Point p = FindIntersection(pU1, pU2, pD1, pD2);
                        double xUp = flowUp.Last().X;
                        double xDown = flowUp.First().X;

                        //
                        // Find the flow going up that is X more than
                        //    the midFlow - that is the top of the DeadZone
                        //
                        for (int j = i; j < flowUp.Count; j++)
                        {
                            if (velocity[j].Y > minVelocity)
                            {
                                xUp = velocity[j].X + 20;
                                break;
                            }
                        }

                        //
                        // Find the flow going down that is X more than
                        //    the midFlow - that is the bottom of the DeadZone
                        //
                        for (int j = i; j >= 0; j--)
                        {
                            if (velocity[j].Y > minVelocity)
                            {
                                xDown = velocity[j].X - 20;
                                break;
                            }
                        }

                        _annotationDeadZone.X1 = xDown;
                        _annotationDeadZone.X2 = xUp;
                        _annotationDeadZone.Y1 = .90;
                        _annotationDeadZone.Y2 = .05;

                        _annotationDeadZoneNarrow.X1 = p.X-10;
                        _annotationDeadZoneNarrow.X2 = p.X + 10;
                        _annotationDeadZoneNarrow.Y1 = .90;
                        _annotationDeadZoneNarrow.Y2 = .05;

                        break;
                    }
                }

                double leftAxisRange;
                if (showFlow)
                {
                    ChartMachineState.YAxes[0].AxisTitle = "Individual Flow (Liters/Min)";
                    leftAxisRange = maxFlow;
                    _velocity.IsVisible = false;
                    _flow.IsVisible = true;
                }
                else
                {
                    ChartMachineState.YAxes[0].AxisTitle = "Velocity (cm/Sec)";
                    leftAxisRange = maxVelocity;
                    _velocity.IsVisible = true;
                    _flow.IsVisible = false;
                }

                //
                // Set the ranges on the axis. Make them related to each other
                //    such that the 0 on each is at the same point.
                // 
                if (leftAxisRange < 1)
                    leftAxisRange = 0;

                double bot1 = -.5;
                double top1 = 6;

                double top0 = leftAxisRange * 1.1;
                double bot0 = bot1 * (top0 / top1);

                ChartMachineState.YAxes[1].VisibleRangeLimit = new DoubleRange(bot1, top1);
                ChartMachineState.YAxes[1].VisibleRange = new DoubleRange(bot1, top1);
                
                ChartMachineState.YAxes[0].VisibleRangeLimit = new DoubleRange(bot0, top0);
                ChartMachineState.YAxes[0].VisibleRange = new DoubleRange(bot0, top0);

                ChartMachineState.YAxes[2].VisibleRangeLimit = new DoubleRange(0, 1200);
                ChartMachineState.YAxes[2].VisibleRange = new DoubleRange(0, 1200);
                
                ChartMachineState.XAxes[0].VisibleRangeLimit = new DoubleRange(-100, MaxPosition + 500);
                ChartMachineState.XAxes[0].VisibleRange = new DoubleRange(-100, MaxPosition + 500);

                ChartMachineState.UpdateLayout();
            }

            catch
            {
                
            }
        }

        //---------------------------------------------------------------
        //
        //
        public Point FindIntersection(Point s1, Point e1, Point s2, Point e2)
        {
            Double a1 = e1.Y - s1.Y;
            Double b1 = s1.X - e1.X;
            Double c1 = a1 * s1.X + b1 * s1.Y;

            Double a2 = e2.Y - s2.Y;
            Double b2 = s2.X - e2.X;
            Double c2 = a2 * s2.X + b2 * s2.Y;

            Double delta = a1 * b2 - a2 * b1;
            //If lines are parallel, the result will be (NaN, NaN).
            return delta == 0
                ? new Point(float.NaN, float.NaN)
                : new Point((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
        }

        //---------------------------------------------------------------
        //
        //
        private void ChartMachineState_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ChartMachineState.YAxes[1].VisibleRange = ChartMachineState.YAxes[1].VisibleRangeLimit;
            ChartMachineState.YAxes[0].VisibleRange = ChartMachineState.YAxes[0].VisibleRangeLimit;
            ChartMachineState.YAxes[2].VisibleRange = ChartMachineState.YAxes[2].VisibleRangeLimit;
            ChartMachineState.XAxes[0].VisibleRange = ChartMachineState.XAxes[0].VisibleRangeLimit;
        }
    }

    //*********************************************************************
    //
    //
    public class GasClass
    {
        public const int Distance = 200;
        public int Position;
        public int PositionCm;
        public RecordClass Record;
        public bool Valid;
        public GasClass(RecordClass rc, string key1, string key2)
        {
            Valid = false;
            Record = rc;
            string name = rc.Name;

            if (name.Contains(key1) == false || name.Contains(key2) == false)
                return;

            string uMatch = Regex.Match(name, "(_U[\\d]+)").Value;
            uMatch = Regex.Match(uMatch, "([\\d]+)").Value;

            string hit = "(" + key2 + "[\\d]+)";

            string mFCMatch = Regex.Match(name, hit).Value;
            mFCMatch = Regex.Match(mFCMatch, "([\\d]+)").Value;

            int.TryParse(uMatch, out int u);
            int.TryParse(mFCMatch, out int mfc);

            if (mfc > 0 && u > 0)
            {
                Position = (u - 1) * (Distance * 12) + mfc * Distance;
                PositionCm = Position / 10;
                Valid = true;
            }
        }
    }

    //*********************************************************************
    //
    //
    public class TemperatureClass
    {
        public const double Distance = 200 * (2.0 / 3.0);
        public int Position;
        public int PositionCm;
        public RecordClass Record;
        public bool Valid;

        public TemperatureClass(RecordClass rc, string key1, string key2, string key3)
        {
            Valid = false;
            Record = rc;
            string name = rc.Name;

            if (name.Contains(key1) == false || name.Contains(key2) == false || name.Contains(key3) == false)
                return;

            string uMatch = Regex.Match(name, "(_U[\\d]+)").Value;
            uMatch = Regex.Match(uMatch, "([\\d]+)").Value;

            string hit2 = "(" + key3 + "[\\d]+)";
            string hc = Regex.Match(name, hit2).Value;
            hc = Regex.Match(hc, "([\\d]+)").Value;

            string hit3 = "(TC" + "[\\d]+)";
            string tc = Regex.Match(name, hit3).Value;
            tc = Regex.Match(tc, "([\\d]+)").Value;

            int.TryParse(uMatch, out int unit);
            int.TryParse(hc, out int heatingCoil);
            int.TryParse(tc, out int thermocoupleNum);

            if (heatingCoil > 0 && unit > 0 && thermocoupleNum > 0)
            {
                Position = (int) (((unit - 1) * (Distance * 18)) +
                                  (heatingCoil * 2 * Distance) +
                                  ((thermocoupleNum - 1) * Distance));
                PositionCm = Position / 10;
                Valid = true;
            }
        }
    }

}
