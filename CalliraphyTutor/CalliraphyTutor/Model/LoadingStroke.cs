using CalligraphyTutor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace CalligraphyTutor.Model
{
    /// <summary>
    /// Class to render the strokes after the pen is lifted
    /// </summary>
    class LoadingStroke: Stroke
    {
        [ThreadStatic]
        private Brush _strokeBrush = new SolidColorBrush();
        [ThreadStatic]
        private Pen _strokePen = new Pen();

        private Color _strokeColor = Colors.Black;
        public Color StrokeColor
        {
            get { return _strokeColor; }
            set
            {
                _strokeColor = value;
            }
        }

        // init opp to run it through the loop
        private int _maxPressure = 0;
        private int _minPressure = 4096;

        #region properties
        public int MaxPressure
        {
            get { return _maxPressure; }
            set
            {
                _maxPressure = value;
            }
        }
        public int MinPressure
        {
            get { return _minPressure; }
            set
            {
                _minPressure = value;
            }
        }

        Globals globals;
        #endregion

        public LoadingStroke(StylusPointCollection stylusPoints, Color c)
            : base(stylusPoints)
        {
            // Create the Brush afor drawing.
            StrokeColor = c;
            globals = Globals.Instance;
            AssignMaxMin(stylusPoints);
            //Debug.WriteLine("LoadingStrokes/ MaxPressure: " + MaxPressure + " MinPressure: " + MinPressure);
        }

        private void AssignMaxMin(StylusPointCollection stylusPoints)
        {
            MaxPressure = this.StylusPoints[0].GetPropertyValue(StylusPointProperties.NormalPressure);
            MinPressure = MaxPressure;
            foreach (StylusPoint sp in this.StylusPoints)
            {
                //float angle = sp.GetPropertyValue(StylusPointProperties.AltitudeOrientation);
                int temp = sp.GetPropertyValue(StylusPointProperties.NormalPressure);
                MaxPressure = Math.Max(temp, MaxPressure);
                MinPressure = Math.Min(temp, MinPressure);
            }
        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            drawingAttributes.Color = StrokeColor;
            drawingAttributes.IsHighlighter = true;
            drawingAttributes.Width = globals.StrokeWidth;
            drawingAttributes.Height = globals.StrokeHeight;
            base.DrawCore(drawingContext, DrawingAttributes);
        }
    }
}
