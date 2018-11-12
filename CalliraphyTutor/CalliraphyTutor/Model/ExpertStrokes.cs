using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using CalligraphyTutor.ViewModel;

namespace CalligraphyTutor.Model
{
    /// <summary>
    /// Defines the behaviour of the expert stroke which are loaded in student view model
    /// </summary>
    class ExpertStrokes: Stroke
    {
        Brush brush;
        Pen pen;

        Guid timestamp = new Guid("12345678-9012-3456-7890-123456789012");

        private System.Windows.Media.Color _strokeColor = Colors.Green;
        public Color StrokeColor
        {
            get { return _strokeColor; }
            set
            {
                _strokeColor = value;
            }
        }

        public ExpertStrokes(StylusPointCollection stylusPoints)
            : base(stylusPoints)
        {
            StrokeColor = Color.FromArgb(Convert.ToByte(50 * this.StylusPoints[this.StylusPoints.Count / 2].PressureFactor), StrokeColor.R, StrokeColor.G, StrokeColor.B);
            brush = new SolidColorBrush(StrokeColor);
            pen = new Pen(brush, Globals.Instance.StrokeWidth);
        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            drawingAttributes.Color = StrokeColor;
            drawingAttributes.Width = Globals.Instance.StrokeWidth;
            drawingAttributes.Height = Globals.Instance.StrokeHeight;
            base.DrawCore(drawingContext, DrawingAttributes);
        }
    }
}
