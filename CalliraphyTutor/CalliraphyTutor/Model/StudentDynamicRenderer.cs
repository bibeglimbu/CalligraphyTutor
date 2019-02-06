using CalligraphyTutor.ViewModel;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;

namespace CalligraphyTutor.Model
{
    class StudentDynamicRenderer: DynamicRenderer
    {
        #region Vars & properties
        //nullable bool true if the students stroke is hitting the expert Stroke
        private bool IsColliding=false;

        /// <summary>
        /// value that holds if the pressure applied is higher or lower than the experts value
        /// </summary>
        // private int pressureState = 0;
        #endregion

        private bool IsPressureChecked = false;

        public StudentDynamicRenderer()
        {
            StudentInkCanvas.HitTestWithExpertEvent += StudentInkCanvas_HitTestWithExpertEvent;
            //StudentInkCanvas.PressureChangedEvent += StudentInkCanvas_PressureChangedEvent;
            StudentInkCanvas.PressureCheckedEvent += StudentInkCanvas_PressureCheckedEvent;
        }

        private void StudentInkCanvas_PressureCheckedEvent(object sender, StudentInkCanvas.PressureCheckedEventArgs e)
        {
            IsPressureChecked = e.state;
        }

        private void StudentInkCanvas_HitTestWithExpertEvent(object sender, StudentInkCanvas.HitTestWithExpertEventArgs e)
        {
            //if the args state is the same as the old state
            if (e.state == IsColliding)
            {
                return;
            } else
            {
                //change the state and throw and event
                IsColliding = e.state;
            }
        }

        //private void StudentInkCanvas_PressureChangedEvent(object sender, StudentInkCanvas.PressureChangedEventArgs e)
        //{
        //    pressureState = e.pressurefactor;
        //}

        protected override void OnDraw(DrawingContext drawingContext, StylusPointCollection stylusPoints,
                                       Geometry geometry, Brush fillBrush)
        {
            Color StrokeColor = Colors.Red;
            if (IsColliding == true)
            {
                StrokeColor = Colors.Green;
            }

            if (IsColliding == false)
            {
                StrokeColor = Colors.Red;
            }
            //if pressure feedback is requested
            if (IsPressureChecked)
            {
                StrokeColor = Color.FromArgb(Convert.ToByte(255 * stylusPoints[stylusPoints.Count / 2].PressureFactor), StrokeColor.R, StrokeColor.G, StrokeColor.B);
            }
            fillBrush = new SolidColorBrush(StrokeColor);
                base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);

            
        }

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            this.DrawingAttributes.Width = Globals.Instance.StrokeWidth;
            this.DrawingAttributes.Height = Globals.Instance.StrokeHeight;
            base.OnStylusDown(rawStylusInput);
        }

        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
        }
    }
}
