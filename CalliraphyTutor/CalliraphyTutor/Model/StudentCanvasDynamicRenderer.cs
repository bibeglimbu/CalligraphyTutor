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
                OnDynamicRendererBrushchanged(EventArgs.Empty);
            }
        }

        StrokeCollection _sc = new StrokeCollection();
        public StrokeCollection RendererSC
        {
            get
            {
                return _sc;
            }
            set
            {
                _sc = value;
            }
        }

        private Point prevPoint;

        #endregion

        #region events
        public event EventHandler<EventArgs> DynamicRendererBrushChanged;
        protected virtual void OnDynamicRendererBrushchanged(EventArgs e)
        {
            EventHandler<EventArgs> handler = DynamicRendererBrushChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        #endregion

        protected override void OnDraw(DrawingContext drawingContext, StylusPointCollection stylusPoints,
                                       Geometry geometry, Brush fillBrush)
        {
            for (int i = 0; i < stylusPoints.Count; i++)
            {
                Point pt = (Point)stylusPoints[i];
                Vector v = Point.Subtract(prevPoint, pt);

                // Only draw if we are at least 4 units away 
                // from the end of the last ellipse. Otherwise, 
                // we're just redrawing and wasting cycles.
                if (v.Length > 2)
                {
                    fillBrush = new SolidColorBrush(DefaultColor);
                    fillBrush.Opacity *= stylusPoints[i].PressureFactor;
                    
                    prevPoint = pt;
                    base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);

                }
                
            }


        }

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            if (RendererSC.Count >= 1)
            {
                HitTest(RendererSC, rawStylusInput);
            }

            base.OnStylusMove(rawStylusInput);
        }

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            this.DrawingAttributes.Width = 5d;
            this.DrawingAttributes.Height = 5d;
            // Allocate memory to store the previous point to draw from.
            prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
            base.OnStylusDown(rawStylusInput);
        }

        protected override void OnStylusMoveProcessed(object callbackData, bool targetVerified)
        {
            base.OnStylusMoveProcessed(callbackData, targetVerified);
        }

        private void HitTest(StrokeCollection sc, RawStylusInput raw)
        {
            foreach (Stroke s in sc)
            {
                if (s.HitTest(raw.GetStylusPoints()[0].ToPoint()))
                {

                    DefaultColor = Colors.Black;
                }
                else
                {
                    DefaultColor = Colors.Red;
                }
            }
        }
    }
}
