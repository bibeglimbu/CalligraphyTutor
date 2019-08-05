using CalligraphyTutor.Managers;
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

namespace CalligraphyTutor.CustomStroke
{ 
    /// <summary>
    /// Student stroke class which accepts the PreviousColor as a attribute
    /// 
    /// </summary>
    class StudentStroke: Stroke
    {
        #region vars

        SpeechManager mySpeechManager;
        private Color _color = Colors.Green;
        public Color StrokeColor
        {
            get { return _color; }
            set
            {
                _color = value;
            }
        }

        private bool PressureChecked = false;
        Guid studentTimestamp = new Guid("12345678-9012-3456-7890-123456789012");
        #endregion

        public StudentStroke(StylusPointCollection stylusPoints, bool PressureChecked) : base(stylusPoints)
        {
            mySpeechManager = SpeechManager.Instance;
            this.PressureChecked = PressureChecked;
        }

        public StudentStroke(StylusPointCollection stylusPoints, Color c,bool PressureChecked) : base(stylusPoints)
        {
            StrokeColor = c;
            this.PressureChecked = PressureChecked;
            mySpeechManager = SpeechManager.Instance;

        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            if (this.ContainsPropertyData(studentTimestamp))
            {
                object data = this.GetPropertyData(studentTimestamp);
                List<DateTime> timeStamps = new List<DateTime>();
                foreach(DateTime dt in (Array)data)
                {
                    timeStamps.Add(dt);
                }
                Debug.WriteLine(timeStamps.Count);
                //Debug.WriteLine("Total time taken to draw the stroke "+ (timeStamps.Last() - timeStamps.First()).TotalSeconds);
            }
            if (PressureChecked==true)
            {
                //StrokeColor = Color.FromArgb(Convert.ToByte(255 * this.StylusPoints[this.StylusPoints.Count / 2].PressureFactor), StrokeColor.R, StrokeColor.G, StrokeColor.B);
                StrokeColor = Color.FromArgb(Convert.ToByte(255), StrokeColor.R, StrokeColor.G, StrokeColor.B);
            }
            
            drawingAttributes.Color = StrokeColor;
            base.DrawCore(drawingContext, DrawingAttributes);

        }
    }
}
