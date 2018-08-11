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
        /// <summary>
        /// Reference StylusPointCollection used for adding new stroke on hit test.
        /// </summary>
        private StylusPointCollection _tempSPCollection;
        /// <summary>
        /// Ensures that the stroke is saved on the consequtive change rather than the immediately
        /// </summary>
        private bool _FirstChange = true;
        /// <summary>
        /// Holds the color as the dynamic renderer color can be changed before the stroke is actually created
        /// </summary>
        private Color _StrokeColor = Colors.Black;
        /// <summary>
        /// Custom Renderer for chaning the behaviour of the ink as it is being drawn
        /// </summary>
        StudentCanvasDynamicRenderer studentCustomRenderer;

        ExpertInkCanvas _expertCanvas;

        private Color _c = Colors.Black;
        /// <summary>
        /// Property which determines the default color of the stroke
        /// </summary>
        public Color DefaultColor
        {
            get { return _c; }
            set
            {
                _c = value;
                studentCustomRenderer.DefaultColor = _c;
            }
        }

        Guid timestamp = new Guid("12345678-9012-3456-7890-123456789012");
        List<DateTime> StrokeTime = new List<DateTime>();
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentInkCanvas()
        {
            _expertCanvas = new ExpertInkCanvas();
            _tempSPCollection = new StylusPointCollection();
            studentCustomRenderer = new StudentCanvasDynamicRenderer();
            this.DynamicRenderer = studentCustomRenderer;
            studentCustomRenderer.DynamicRendererBrushChanged += CustomRenderer_DynamicRendererBrushChanged;
        }

        #region events
        /// <summary>
        /// Event handler that handles that <see cref="StudentCanvasDynamicRenderer"/> DynamicRendererBrushChanged Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CustomRenderer_DynamicRendererBrushChanged(object sender, EventArgs e)
        {
           
            if (_FirstChange == true)
            {
                _StrokeColor = DefaultColor;
                //no need to change the  color if its the first time 
                _FirstChange = false;
            }
            else
            {
                if (_tempSPCollection.Count >= 2)
                {
                    //if the color changes add the stroke to Strokes
                    AddStroke();
                    //change the color back to default
                    _StrokeColor = DefaultColor;
                }
            }
        }

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            //Start/Stop the dispatch timer which stops the animation
            _expertCanvas.ToggleDispatchTimer();
            base.OnStylusDown(e);
        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            //Start/Stop the dispatch timer which stops the animation
            _expertCanvas.ToggleDispatchTimer();
            //if(_tempSPCollection.Count > 2)
            //{
            //    AddStroke();
            //}
           
            base.OnStylusUp(e);
        }

        //this event is not raised when stroke object is added programatically but rather when the pen is lifted up
        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            //remove the stroke and instead create new stroke from _tempSPCollection at the end even though the color may not have changed
            this.Strokes.Remove(e.Stroke);
            if (_tempSPCollection.Count > 2)
            {

                //this.Strokes.Remove(e.Stroke);
                //using custom renderer color is too late as the Color has already changed due to this method triggered on color change event
                // StudentCanvasStroke customStroke = new StudentCanvasStroke(_tempSPCollection, _StrokeColor);
                //this.Strokes.Add(customStroke);
                //InkCanvasStrokeCollectedEventArgs args = new InkCanvasStrokeCollectedEventArgs(customStroke);
                //InkCanvasStrokeCollectedEventArgs args = new InkCanvasStrokeCollectedEventArgs(customStroke);

                //add the stroke when the pen is lifted. there is no need to call the base

                AddStroke();
                //base.OnStrokeCollected(args);
            }

        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            _tempSPCollection.Add(e.GetStylusPoints(this).Reformat(_tempSPCollection.Description));
            StrokeTime.Add(DateTime.Now);
            base.OnStylusMove(e);
        }

        #endregion

        /// <summary>
        /// method that uses the _tempStyluspointCollection to create and save stroke
        /// </summary>
        private void AddStroke()
        {

            StudentCanvasStroke customStroke = new StudentCanvasStroke(_tempSPCollection, _StrokeColor);
            customStroke.AddPropertyData(timestamp,StrokeTime.ToArray());
            this.Strokes.Add(customStroke);
            
            //empty the stylusPointcollection
            _tempSPCollection = new StylusPointCollection();
            StrokeTime = new List<DateTime>();

            Debug.WriteLine("Stroke added: Total stroke count = " + this.Strokes.Count);
        }
    }
}
