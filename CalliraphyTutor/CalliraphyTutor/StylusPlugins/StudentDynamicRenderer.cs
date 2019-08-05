using CalligraphyTutor.CustomInkCanvas;
using CalligraphyTutor.StylusPlugins;
using CalligraphyTutor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;

namespace CalligraphyTutor.StylusPlugins
{
    /// <summary>
    /// Custom Dynamic renderer for rendering ink while it is being drawn.
    /// </summary>
    class StudentDynamicRenderer : DynamicRenderer
    {
        #region Vars & properties
        private bool IsPressureChecked = false;
        private bool IsStrokeChecked = false;
        private bool IsSpeedChecked = false;
        private bool IsExpertStrokeLoaded = false;
        //private float StudentPressureFactor = 0;
        private StylusPoint ExpertStylusPoint = new StylusPoint();
        private Color StrokeColor = Colors.Green;
        #endregion

        public StudentDynamicRenderer()
        {
            ExpertInkCanvas.ExpertStrokeLoadedEvent += ExpertInkCanvas_ExpertStrokeLoadedEvent;
            StudentInkCanvas.PressureCheckedEvent += StudentInkCanvas_PressureCheckedEvent;
            StudentInkCanvas.StrokeCheckedEvent += StudentInkCanvas_StrokeCheckedEvent;
            StudentInkCanvas.SpeedCheckedEvent += StudentInkCanvas_SpeedCheckedEvent;
            HitStrokeTesterPlugin.HitStateChangedEvent += HitStrokeTesterPlugin_HitStateChangedEvent;
            HitStrokeTesterPlugin.NearestStylusPointCalculatedEvent += HitStrokeTesterPlugin_NearestStylusPointCalculatedEvent;
        }

        #region eventhandlers
        private void ExpertInkCanvas_ExpertStrokeLoadedEvent(object sender, ExpertInkCanvas.ExpertStrokeLoadedEventEventArgs e)
        {
            IsExpertStrokeLoaded = e.state;
        }
        private void HitStrokeTesterPlugin_HitStateChangedEvent(object sender, HitStrokeTesterPlugin.HitStateChangedEventArgs e)
        {
            //only change color if the Stroke feedback is requested
            if (IsStrokeChecked)
            {
                StrokeColor = e.color;
            }
        }
        private void HitStrokeTesterPlugin_NearestStylusPointCalculatedEvent(object sender, HitStrokeTesterPlugin.NearestExpertStylusPointCalculatedEventArgs e)
        {
            if (acceptNewPoint)
            {
                ExpertStylusPoint = e.styluspoint;
                acceptNewPoint = false;
            }
            
        }
        private void StudentInkCanvas_SpeedCheckedEvent(object sender, StudentInkCanvas.SpeedCheckedEventArgs e)
        {
            IsSpeedChecked = e.state;
        }
        private void StudentInkCanvas_StrokeCheckedEvent(object sender, StudentInkCanvas.StrokeCheckedEventArgs e)
        {
            IsStrokeChecked = e.state;
            StrokeColor = Colors.Green;
        }
        private void StudentInkCanvas_PressureCheckedEvent(object sender, StudentInkCanvas.PressureCheckedEventArgs e)
        {
            IsPressureChecked = e.state;
            //StrokeColor = Colors.Green;
        }
        #endregion

        #region overrides

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
            base.OnStylusDown(rawStylusInput);
        }
        private Point prevPoint;
        private bool acceptNewPoint = true;
        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            StylusPointCollection stylusPoints = rawStylusInput.GetStylusPoints();
            for (int i = 0; i < stylusPoints.Count; i++)
            {
                Point pt = (Point)stylusPoints[i];
                Vector v = Point.Subtract(prevPoint, pt);
                if (v.Length > 4)
                {
                    acceptNewPoint = true;
                    prevPoint = pt;
                    break;
                }
            }
            base.OnStylusMove(rawStylusInput);
        }
        protected override void OnDraw(DrawingContext drawingContext, StylusPointCollection stylusPoints,
                                       Geometry geometry, Brush fillBrush)
        {
            //never a=a*3 with color
            Color TempStrokeColor = StrokeColor;
            if (IsPressureChecked && IsExpertStrokeLoaded)
            {
                List<float> pressureFactorList = new List<float>();
                foreach (StylusPoint s in stylusPoints)
                {
                    pressureFactorList.Add(s.PressureFactor);
                }
                float StudentPressureFactor = pressureFactorList.Average();
                //multiplier to darken or lighen the PreviousColor
                //StrokeColor = Color.FromArgb(Convert.ToByte(255 * stylusPoints[stylusPoints.Count / 2].PressureFactor), StrokeColor.R, StrokeColor.G, StrokeColor.B);
                float ExpertPressureFactor = ExpertStylusPoint.PressureFactor;
                //if pressure is higher produce darker color my passing negetive number
                float ColorWeight = ExpertPressureFactor - StudentPressureFactor;
                Debug.WriteLine(ColorWeight);
                TempStrokeColor = ChangeColorBrightness(StrokeColor, ColorWeight);

            }
            fillBrush = new SolidColorBrush(TempStrokeColor);
            base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);
        }

        protected override void OnStylusDownProcessed(object callbackData, bool targetVerified)
        {
            StrokeColor = Colors.Green;
        }
        #endregion

        #region Native Methods
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
        #endregion

    }
}
