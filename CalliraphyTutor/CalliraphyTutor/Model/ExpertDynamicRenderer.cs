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
    class ExpertDynamicRenderer: DynamicRenderer
    {
        #region Vars & properties
        private Color _c = Colors.Gray;
        public Color StrokeColor
        {
            get { return _c; }
            set
            {
                _c = value;
            }
        }

        #endregion

        public ExpertDynamicRenderer()
        {

        }

        protected override void OnDraw(DrawingContext drawingContext, StylusPointCollection stylusPoints,
                                       Geometry geometry, Brush fillBrush)
        {
            StrokeColor = Color.FromArgb(Convert.ToByte(255 * stylusPoints[stylusPoints.Count / 2].PressureFactor), StrokeColor.R, StrokeColor.G, StrokeColor.B);
            fillBrush = new SolidColorBrush(StrokeColor);
            base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);
        }

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            base.OnStylusMove(rawStylusInput);
        }

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            Application.Current.Dispatcher.Invoke(new Action(
                () =>
                {
                    this.DrawingAttributes.Width = Globals.Instance.StrokeWidth;
                    this.DrawingAttributes.Height = Globals.Instance.StrokeHeight;
                }));
            base.OnStylusDown(rawStylusInput);
        }
    }
}
