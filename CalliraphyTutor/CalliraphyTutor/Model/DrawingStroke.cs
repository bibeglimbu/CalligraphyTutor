using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace CalligraphyTutor.Model
{
    class DrawingStroke : Stroke
    {
        #region vars
        [ThreadStatic]
        static private Brush brush = null;

        [ThreadStatic]
        static private Pen pen = null;

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
        #endregion

        public DrawingStroke(StylusPointCollection stylusPoints) : base(stylusPoints)
        {
            StrokeColor = _color;

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

        public DrawingStroke(StylusPointCollection stylusPoints, Color c) : base(stylusPoints)
        {
            StrokeColor = c;
            MaxPressure = this.StylusPoints[0].GetPropertyValue(StylusPointProperties.NormalPressure);
            MinPressure = MaxPressure;

            foreach (StylusPoint sp in this.StylusPoints)
            {
                int temp = sp.GetPropertyValue(StylusPointProperties.NormalPressure);
                _maxPressure = Math.Max(temp, _maxPressure);
                _minPressure = Math.Min(temp, _minPressure);
            }

        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            drawingAttributes.Color = StrokeColor;
            base.DrawCore(drawingContext, drawingAttributes);
        }
    }
}
