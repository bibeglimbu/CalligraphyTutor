
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;

namespace CalligraphyTutor.Model
{
    class CalligraphyDynamicRenderer: DynamicRenderer
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
            fillBrush = new SolidColorBrush(DefaultColor);
            base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);
        }
        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            if(RendererSC.Count >= 1)
            {
                HitTest(RendererSC, rawStylusInput);
            }
            
            base.OnStylusMove(rawStylusInput);
        }

        protected override void OnStylusMoveProcessed(object callbackData, bool targetVerified)
        {
            base.OnStylusMoveProcessed(callbackData, targetVerified);
        }

        private void HitTest(StrokeCollection sc, RawStylusInput raw)
        {
            foreach(Stroke s in sc)
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
