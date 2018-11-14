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
        private Color _c = Colors.Green;
        public Color DefaultColor
        {
            get { return _c; }
            set
            {
                _c = value;
            }
        }
        /// <summary>
        /// value that holds if the pressure applied is higher or lower than the experts value
        /// </summary>
        private int pressureState = 0;
        #endregion

        public StudentDynamicRenderer()
        {
            StudentInkCanvas.BrushColorChangedEvent += Canvas_BrushColorChangedEvent;
            StudentInkCanvas.PressureChangedEvent += StudentInkCanvas_PressureChangedEvent;
        }

        private void StudentInkCanvas_PressureChangedEvent(object sender, StudentInkCanvas.PressureChangedEventArgs e)
        {
            pressureState = e.pressurefactor;
        }

        private void Canvas_BrushColorChangedEvent(object sender, StudentInkCanvas.ColorChangedEventArgs e)
        {
            DefaultColor = e.color;
        }

        protected override void OnDraw(DrawingContext drawingContext, StylusPointCollection stylusPoints,
                                       Geometry geometry, Brush fillBrush)
        {
            //fillBrush = new SolidColorBrush(ChangeColorBrightness(DefaultColor, 0.5f));
            foreach(StylusPoint s in stylusPoints)
            {
                fillBrush = new SolidColorBrush(ChangeColorBrightness(DefaultColor, (s.PressureFactor) * pressureState));
                base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);
            }
            
        }

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            base.OnStylusMove(rawStylusInput);
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
