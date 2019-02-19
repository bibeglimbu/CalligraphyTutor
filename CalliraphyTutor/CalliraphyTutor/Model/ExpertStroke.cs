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
using CalligraphyTutor.ViewModel;

namespace CalligraphyTutor.Model
{
    /// <summary>
    /// Defines the behaviour of the expert stroke which are loaded in student view model
    /// </summary>
    class ExpertStroke: Stroke
    {
        Brush brush;
        Pen pen;

        Guid expertTimestamp = new Guid("12345678-9012-3456-7890-123456789013");

        //private double averageVelocity = 0;
        //public double AverageExpertVelocity
        //{
        //    get
        //    {
        //        return averageVelocity;
        //    }
        //}

        private System.Windows.Media.Color _strokeColor = Colors.Gray;
        public Color StrokeColor
        {
            get { return _strokeColor; }
            set
            {
                _strokeColor = value;
            }
        }

        public ExpertStroke(StylusPointCollection stylusPoints)
            : base(stylusPoints)
        {
            StrokeColor = Color.FromArgb(Convert.ToByte(255 * this.StylusPoints[this.StylusPoints.Count / 2].PressureFactor), StrokeColor.R, StrokeColor.G, StrokeColor.B);
            brush = new SolidColorBrush(StrokeColor);
            pen = new Pen(brush, Globals.Instance.StrokeWidth);
        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            //averageVelocity = CalculateStrokeVelocity(this);
            //Debug.WriteLine("average velocity of the stroke: " + (float)averageVelocity);
            drawingAttributes.Color = StrokeColor;
            drawingAttributes.Width = Globals.Instance.StrokeWidth;
            drawingAttributes.Height = Globals.Instance.StrokeHeight;
            base.DrawCore(drawingContext, DrawingAttributes);
        }
    }
}
