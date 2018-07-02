using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace CalligraphyTutor.Model
{
    class StudentInkCanvas: InkCanvas
    {
        #region vars
        //reference StylusPointCollection used for adding new stroke on hit test.
        private StylusPointCollection _tempSPCollection;
        //to ensure that the stroke is saved on the consequtive change rather than the immediately
        private bool _FirstChange = true;
        //holds the color as the dynamic renderer color can be changed before the stroke is actually created
        private Color _StrokeColor = Colors.Black;

        CalligraphyDynamicRenderer customRenderer;
        ExpertInkCanvas _expertCanvas;
        

        private Color _c = Colors.Black;
        public Color DefaultColor
        {
            get { return _c; }
            set
            {
                _c = value;
                customRenderer.DefaultColor = _c;
            }
        }

        #endregion

        public StudentInkCanvas()
        {
            _expertCanvas = new ExpertInkCanvas();
            _tempSPCollection = new StylusPointCollection();
            Application.Current.Dispatcher.InvokeAsync(new Action(
                    () =>
                    {
                        customRenderer = new CalligraphyDynamicRenderer();
                        this.DynamicRenderer = customRenderer;
                        customRenderer.DynamicRendererBrushChanged += CustomRenderer_DynamicRendererBrushChanged;
                    }));
        }

        #region events
        private void CustomRenderer_DynamicRendererBrushChanged(object sender, EventArgs e)
        {
            //no need to change the  color if its the first time 
            if (_FirstChange == true)
            {
                _StrokeColor = DefaultColor;
                _FirstChange = false;
            }
            else
            {
                if (_tempSPCollection.Count >= 2)
                {
                    AddStroke();
                    _StrokeColor = DefaultColor;
                }
            }
        }
        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            _expertCanvas.ToggleDispatchTimer();
            base.OnStylusDown(e);
        }
        protected override void OnStylusUp(StylusEventArgs e)
        {
            _expertCanvas.ToggleDispatchTimer();
            base.OnStylusUp(e);
        }
        //this event is not raised when stroke object is added programatically but rather when the pen is lifted up
        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            //remove the stroke and instead create new stroke from _tempSPCollection at the end even though the color may not have changed
            this.Strokes.Remove(e.Stroke);
            if(_tempSPCollection.Count > 2)
            {
                //AddStroke();
                this.Strokes.Remove(e.Stroke);
                //using custom renderer color is too late as the Color has already changed due to this method triggered on color change event
                DrawingStroke customStroke = new DrawingStroke(_tempSPCollection, _StrokeColor);
                this.Strokes.Add(customStroke);
                //InkCanvasStrokeCollectedEventArgs args = new InkCanvasStrokeCollectedEventArgs(customStroke);
                _tempSPCollection = new StylusPointCollection();
                //base.OnStrokeCollected(args);
            }

        }
        protected override void OnStylusMove(StylusEventArgs e)
        {
            _tempSPCollection.Add(e.GetStylusPoints(this).Reformat(_tempSPCollection.Description));
            base.OnStylusMove(e);
        }

        #endregion

        /// <summary>
        /// method that uses the _tempStyluspointCollection to create and save stroke
        /// </summary>
        private void AddStroke()
        {

            DrawingStroke customStroke = new DrawingStroke(_tempSPCollection, _StrokeColor);
            this.Strokes.Add(customStroke);
            //empty the stylusPointcollection
            _tempSPCollection = new StylusPointCollection();

            Debug.WriteLine("Stroke added: Total stroke count = " + this.Strokes.Count);
        }
    }
}
