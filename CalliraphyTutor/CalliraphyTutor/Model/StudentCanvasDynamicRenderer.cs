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
    class StudentCanvasDynamicRenderer: DynamicRenderer
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

        public StudentCanvasDynamicRenderer()
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
            for (int i = 0; i < stylusPoints.Count; i++)
            {
                    fillBrush = new SolidColorBrush(DefaultColor);
                    //fillBrush.Opacity *= stylusPoints[i].PressureFactor;
                    base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);
            }

        }

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            base.OnStylusMove(rawStylusInput);
        }

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            this.DrawingAttributes.Width = 5d;
            this.DrawingAttributes.Height = 5d;
            base.OnStylusDown(rawStylusInput);
        }
    }
}
