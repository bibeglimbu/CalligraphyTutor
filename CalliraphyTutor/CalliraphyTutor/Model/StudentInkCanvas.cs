using CalligraphyTutor.ViewModel;
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
        /// Custom Renderer for chaning the behaviour of the ink as it is being drawn
        /// </summary>
        private StudentDynamicRenderer studentCustomRenderer;

        private Color _c = Colors.Green;
        /// <summary>
        /// Property which determines the default color of the stroke
        /// </summary>
        public Color StrokeColor
        {
            get { return _c; }
            set
            {
                _c = value;
                //change the dynamic rendere color
                ColorChangedEventArgs args = new ColorChangedEventArgs();
                args.color = _c;
                OnBrushColorChanged(args);
                //Add stroke when color changes
                AddStroke(_tempSPCollection, prevColor);
                prevColor = _c;
            }
        }

        /// <summary>
        /// Value that holds if the pressure applied is higher or lower that the experts. Must return either -1,0 or 1
        /// </summary>
        private int expertPressureFactor = 0;
        public int ExpertPressureFactor
        {
            get { return expertPressureFactor; }
            set
            {
                if(value == 0 || value == -1 || value == 1)
                expertPressureFactor = value;
                PressureChangedEventArgs args = new PressureChangedEventArgs();
                args.pressurefactor = expertPressureFactor;
                OnExpertPressureChanged(args);
            }
        }


        Guid timestamp = new Guid("12345678-9012-3456-7890-123456789012");
        List<DateTime> StrokeTime = new List<DateTime>();
        #endregion

        //declare the class singleton as we only need one instance in a window
        //private static readonly Lazy<StudentInkCanvas> lazy = new Lazy<StudentInkCanvas>(() => new StudentInkCanvas());
        //public static StudentInkCanvas Instance { get { return lazy.Value; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentInkCanvas()
        {
            _tempSPCollection = new StylusPointCollection();
            //instantiate the customDynamicRenderer
            studentCustomRenderer = new StudentDynamicRenderer();
            this.DynamicRenderer = studentCustomRenderer;
            Debug.WriteLine("InkPresenter status: " + this.InkPresenter.IsEnabled);
            prevColor = StrokeColor;
        }

        #region events definition

        /// <summary>
        /// Event raised when the pen is lifed or put down. declared it as static as the listening class would only listen to the object.
        /// </summary>
        public static event EventHandler PenDownUpEvent;
        protected virtual void OnPenDownUpEvent(EventArgs  e)
        {
            EventHandler handler = PenDownUpEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// event that notifies that the color has changed
        /// </summary>
        public static event EventHandler<ColorChangedEventArgs> BrushColorChangedEvent;
        protected virtual void OnBrushColorChanged(ColorChangedEventArgs c)
        {
            EventHandler<ColorChangedEventArgs> handler = BrushColorChangedEvent;
            if (handler != null)
            {
                handler(this, c);
            }
        }
        public class ColorChangedEventArgs : EventArgs
        {
            public Color color { get; set; }
        }

        /// <summary>
        /// event that notifies that the pressure applied is higher or lower than the experts
        /// </summary>
        public static event EventHandler<PressureChangedEventArgs> PressureChangedEvent;
        protected virtual void OnExpertPressureChanged(PressureChangedEventArgs c)
        {
            EventHandler<PressureChangedEventArgs> handler = PressureChangedEvent;
            if (handler != null)
            {
                handler(this, c);
            }
        }
        public class PressureChangedEventArgs : EventArgs
        {
            public int pressurefactor { get; set; }
        }
        #endregion

        #region eventHandlers
        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            OnPenDownUpEvent(EventArgs.Empty);
        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            OnPenDownUpEvent(EventArgs.Empty);
        }

        //this event is not raised when stroke object is added programatically but rather when the pen is lifted up
        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            //remove the stroke and instead create new stroke from _tempSPCollection at the end even though the color may not have changed
            this.Strokes.Remove(e.Stroke);
            if (_tempSPCollection.Count > 2)
            {
                AddStroke(_tempSPCollection, StrokeColor);
            }
            Debug.WriteLine("Strokes Count: " +  this.Strokes.Count);

        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            //add styluspoint from the event after checking to ensure that the collection doesnt already posses them
            foreach(StylusPoint sp in e.GetStylusPoints(this))
            {
                if (_tempSPCollection.Contains(sp) == false)
                {
                    _tempSPCollection.Add(e.GetStylusPoints(this).Reformat(_tempSPCollection.Description));
                }
               
            }
            
            StrokeTime.Add(DateTime.Now);
            base.OnStylusMove(e);
        }

        #endregion
        //Color that holds the color frm earlier stroke
        private Color prevColor;
        /// <summary>
        /// method that uses the _tempStyluspointCollection to create studentStroke and save stroke in this canvas StrokeCollection
        /// </summary>
        private void AddStroke(StylusPointCollection spc, Color c)
        {
            if(spc.Count < 2)
            {
                return;
            }
            Debug.WriteLine("StudentInkcanvas before: "+spc.Count);
            //save the stroke temporarily for filtering the data
            Stroke tempStroke = FilterStrokeData(new Stroke(spc));
            Debug.WriteLine("StudentInkcanvas after: " + tempStroke.StylusPoints.Count);
            //convert the stroke into studentcanvasStroke
            StudentStroke customStroke = new StudentStroke(tempStroke.StylusPoints, c);
            customStroke.AddPropertyData(timestamp,StrokeTime.ToArray());
            this.InkPresenter.Strokes.Add(customStroke);
           
            //store the last point temporarily to
            //StylusPoint prevfirstStylusPoint = tempStroke.StylusPoints.First();
            // create a new stylusPointCollection
            _tempSPCollection = new StylusPointCollection();
            //add the last point to the new collection to avoid breaking off
            //_tempSPCollection.Add(prevfirstStylusPoint);
            StrokeTime = new List<DateTime>();

            Debug.WriteLine("Stroke added: Total stroke count = " + this.Strokes.Count);
        }

        Point prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
        /// <summary>
        /// Method for removing excess styluspoints from a expert stroke
        /// </summary>
        /// <param name="stroke"></param>
        private Stroke FilterStrokeData(Stroke stroke)
        {
            //create a copy of stroke for iterating
            ExpertStrokes tempStroke = new ExpertStrokes(stroke.StylusPoints);
            for (int i = 0; i < tempStroke.StylusPoints.Count; i++)
            {
                Point pt = tempStroke.StylusPoints[i].ToPoint();
                Vector v = Point.Subtract(prevPoint, pt);

                if (v.Length < 2)
                {
                    //remove excess points between the 2 points 
                    tempStroke.StylusPoints.RemoveAt(i);
                    continue;
                }
                else
                {
                    prevPoint = pt;
                }

            }
            return tempStroke;
        }

       
    }
}
