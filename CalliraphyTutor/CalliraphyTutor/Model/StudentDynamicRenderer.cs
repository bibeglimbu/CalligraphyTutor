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
        private Color _c = Colors.Black;
        public Color DefaultColor
        {
            get { return _c; }
            set
            {
                _c = value;
            }
        }

        #endregion

        public StudentDynamicRenderer()
        {
            StudentInkCanvas.BrushColorChangedEvent += Canvas_BrushColorChangedEvent;
        }

        private void Canvas_BrushColorChangedEvent(object sender, StudentInkCanvas.ColorChangedEventArgs e)
        {
            DefaultColor = e.color;
        }

        protected override void OnDraw(DrawingContext drawingContext, StylusPointCollection stylusPoints,
                                       Geometry geometry, Brush fillBrush)
        {
            fillBrush = new SolidColorBrush(DefaultColor);
            base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);
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
    }
}
