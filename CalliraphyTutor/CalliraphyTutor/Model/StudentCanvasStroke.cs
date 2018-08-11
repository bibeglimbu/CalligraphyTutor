using CalligraphyTutor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace CalligraphyTutor.Model
{
    class StudentCanvasStroke: Stroke
    {
        #region vars
        Brush brush;
        Pen pen ;

        Globals globals;
        private Color _color = Colors.Black;
        public Color StrokeColor
        {
            get { return _color; }
            set
            {
                _color = value;
            }
        }
        // init opp to run it through the loop
        private int _maxPressure;
        public int MaxPressure
        {
            get { return _maxPressure; }
            set
            {
                _maxPressure = value;
            }
        }
        private int _minPressure;
        public int MinPressure
        {
            get { return _minPressure; }
            set
            {
                _minPressure = value;
            }
        }

        Guid timestamp = new Guid("12345678-9012-3456-7890-123456789012");
        #endregion

        public StudentCanvasStroke(StylusPointCollection stylusPoints) : base(stylusPoints)
        {
            globals = Globals.Instance;
            brush = new SolidColorBrush(StrokeColor);
            pen = new Pen(brush, globals.StrokeWidth);

            MaxPressure = this.StylusPoints[0].GetPropertyValue(StylusPointProperties.NormalPressure);
            MinPressure = MaxPressure;

            foreach (StylusPoint sp in this.StylusPoints)
            {
                int temp = sp.GetPropertyValue(StylusPointProperties.NormalPressure);
                _maxPressure = Math.Max(temp, _maxPressure);
                _minPressure = Math.Min(temp, _minPressure);
            }
            //Debug.WriteLine("MaxPressure: " + MaxPressure + " MinPressure: " + MinPressure);
            //Debug.WriteLine("MaxAngle: " + MaxAngle + " MinAngle: " + MinAngle);

        }

        public StudentCanvasStroke(StylusPointCollection stylusPoints, Color c) : base(stylusPoints)
        {
            StrokeColor = c;
            globals = Globals.Instance;
            brush = new SolidColorBrush(StrokeColor);
            pen = new Pen(brush, globals.StrokeWidth);

            MaxPressure = this.StylusPoints[0].GetPropertyValue(StylusPointProperties.NormalPressure);
            MinPressure = MaxPressure;

            foreach (StylusPoint sp in this.StylusPoints)
            {
                int temp = sp.GetPropertyValue(StylusPointProperties.NormalPressure);
                _maxPressure = Math.Max(temp, _maxPressure);
                _minPressure = Math.Min(temp, _minPressure);
            }

        }

        //protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        //{

        //    drawingAttributes.Width = globals.StrokeWidth;
        //    drawingAttributes.Height = globals.StrokeHeight;

        //    //Allocate memory to store the previous point to draw from.
        //    Point prevPoint = new Point(double.NegativeInfinity,
        //                            double.NegativeInfinity);

        //    //Draw linear line between  all the StylusPoints in the Stroke.
        //    for (int i = 0; i < this.StylusPoints.Count; i++)
        //    {
        //        Point pt = this.StylusPoints[i].ToPoint();
        //        Vector v = Point.Subtract(prevPoint, pt);

        //        brush.Opacity = this.StylusPoints[i].PressureFactor;
        //        pen = new Pen(brush, globals.StrokeWidth);
        //        //drawingAttributes.Color = Color.FromArgb(Convert.ToByte(this.StylusPoints[i].PressureFactor), StrokeColor.R, StrokeColor.G, StrokeColor.B);
        //        if (v.Length > 2)
        //        {
        //            drawingContext.DrawRoundedRectangle(brush, pen, new Rect(prevPoint, pt), 0.5d, 0.5d);
        //            prevPoint = pt;
        //        }

        //    }


        //}

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            if (this.ContainsPropertyData(timestamp))
            {
                object data = this.GetPropertyData(timestamp);
                List<DateTime> timeStamps = new List<DateTime>();
                foreach(DateTime dt in (Array)data)
                {
                    timeStamps.Add(dt);
                }
                Debug.WriteLine(timeStamps.Count);
                Debug.WriteLine("Total time taken to draw the stroke "+ (timeStamps.Last() - timeStamps.First()).TotalSeconds);
            }
            drawingAttributes.Color = StrokeColor;
            drawingAttributes.Width = globals.StrokeWidth;
            drawingAttributes.Height = globals.StrokeHeight;
            base.DrawCore(drawingContext, DrawingAttributes);
        }
    }
}
