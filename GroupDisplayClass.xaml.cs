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
using SciChart.Charting.Visuals.Axes;

namespace CVSDataViewer_V2
{
    //*********************************************************************
    //
    //
    public partial class GroupDisplayClass : UserControl
    {
        public string GroupName
        {
            get => TextBoxGroupName.Text;
            set => TextBoxGroupName.Text = value;
        }

        public string Results
        {
            set => LabelResults.Content = value;
        }

        public string AxisName
        {
            get => TextBoxAxisTitle.Text;
            set => TextBoxAxisTitle.Text = value;
        }

        public AxisAlignment AxisPosition
        {
            get => (ComboBoxAxisPosition.SelectedIndex == 0)
                ? AxisAlignment.Left
                : AxisAlignment.Right;

            set => ComboBoxAxisPosition.SelectedIndex = value == AxisAlignment.Left ? 0 : 1;
        }

        public double MaxScale
        {
            get
            {
                double.TryParse(TextBoxMaxScale.Text, out double maxScale);
                return (maxScale);
            }
            set => TextBoxMaxScale.Text = value.ToString("0");
        }

        public double MinScale
        {
            get
            {
                double.TryParse(TextBoxMinScale.Text, out double minScale);
                return (minScale);
            }
            set => TextBoxMinScale.Text = value.ToString("0");
        }

        public Color PlotColor
        {
            get => ((Color) ColorPicker1.SelectedColor);
            set => ColorPicker1.SelectedColor = value;
        }

        public event EventHandler<object> EventDelete;
        public event EventHandler<object> EventAddRecords;
        public event EventHandler<object> EventShow;
        public event EventHandler<object> EventHide;
        public event EventHandler<object> EventMouseEnter;
        public event EventHandler<object> EventMouseExit;
        public event EventHandler<object> EventSomethingChanged;

        public List<RecordClass> Records;
        public int Count => Records.Count;

        //---------------------------------------------------------------
        //
        //
        public GroupDisplayClass()
        {
            InitializeComponent();
            {
                Records = new List<RecordClass>();
            }
        }

        //---------------------------------------------------------------
        //
        //
        public void Clear()
        {
            Records.Clear();
        }

        //---------------------------------------------------------------
        //
        //
        public void AddRecord(RecordClass r)
        {
            Records.Add(r);
            Results = "AllRecords: " + Records.Count;
        }

        //---------------------------------------------------------------
        //
        //
        public void RemoveRecord(RecordClass r)
        {
            if (r == null)
                return;

            Records.Remove(r);
            Results = "AllRecords: " + Records.Count;
        }

        //---------------------------------------------------------------
        //
        //
        private void ColorPicker1_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            EventSomethingChanged?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        private void TextBoxAxisTitle_KeyUp(object sender, KeyEventArgs e)
        {
            EventSomethingChanged?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        private void TextBoxMaxScale_KeyUp(object sender, KeyEventArgs e)
        {
            EventSomethingChanged?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        private void TextBoxMinScale_KeyUp(object sender, KeyEventArgs e)
        {
            EventSomethingChanged?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        private void ComboBoxAxisPosition_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EventSomethingChanged?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        private void TextBoxGroupName_KeyUp(object sender, KeyEventArgs e)
        {

            EventSomethingChanged?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            EventMouseEnter?.Invoke(this, null);

        }

        //---------------------------------------------------------------
        //
        //
        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            EventMouseExit?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        private void ButtonShow_Click(object sender, RoutedEventArgs e)
        {
            if (ButtonShow.Content.Equals("Show"))
            {
                ButtonShow.Content = "Hide";
                EventShow?.Invoke(this, null);
            }
            else
            {
                ButtonShow.Content = "Show";
                EventHide?.Invoke(this, null);
            }
        }

        //---------------------------------------------------------------
        //
        //
        private void CheckboxClicked(object sender, RoutedEventArgs e)
        {
            EventSomethingChanged?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            EventAddRecords?.Invoke(this, null);
        }

        //---------------------------------------------------------------
        //
        //
        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            EventDelete?.Invoke(this, null);
        }

    }
}
