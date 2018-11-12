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
    /// <summary>
    /// Student stroke class which accepts the color as a attribute
    /// 
    /// </summary>
    class StudentStroke: Stroke
    {
        #region vars

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

        Guid timestamp = new Guid("12345678-9012-3456-7890-123456789012");
        #endregion

        public StudentStroke(StylusPointCollection stylusPoints) : base(stylusPoints)
        {
            globals = Globals.Instance;

        }

        public StudentStroke(StylusPointCollection stylusPoints, Color c) : base(stylusPoints)
        {
            StrokeColor = c;
            globals = Globals.Instance;

        }

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
