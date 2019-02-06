using CalligraphyTutor.StylusPlugins;
using CalligraphyTutor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace CalligraphyTutor.Model
{
    class StudentInkCanvas : InkCanvas
    {
        #region vars

        /// <summary>
        /// Dependency property for binding the hit state from view model
        /// </summary>
        public static DependencyProperty StrokeHitStateProperty = DependencyProperty.Register("StrokeHitState", typeof(bool), typeof(StudentInkCanvas),
            new FrameworkPropertyMetadata(default(bool), new PropertyChangedCallback(OnStrokeHitStateChanged)));
        /// <summary>
        /// event thrown by the dependecy property used for updating the property
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnStrokeHitStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Debug.WriteLine(e.NewValue);
            ((StudentInkCanvas)d).StrokeHitState = (bool)e.NewValue;
        }

        private bool PreStrokeHitState = false;
        /// <summary>
        /// Property which determines the default color based on hittest state with expert stroke
        /// </summary>
        public bool StrokeHitState
        {
            get { return (bool)GetValue(StrokeHitStateProperty); }
            set
            {
                HitTestWithExpertEventArgs args = new HitTestWithExpertEventArgs();
                args.state = value;
                //raise hit test value changed event
                OnHitTestWithExpert(args);
                SetValue(StrokeHitStateProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for binding the Stroke checked state from view model
        /// </summary>
        public static DependencyProperty StrokeCheckedProperty = DependencyProperty.Register("StrokeChecked", typeof(bool), typeof(StudentInkCanvas),
            new FrameworkPropertyMetadata(default(bool), new PropertyChangedCallback(OnStrokeCheckedChanged)));
        private static void OnStrokeCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Debug.WriteLine(e.NewValue);
            ((StudentInkCanvas)d).StrokeChecked = (bool)e.NewValue;
        }

        /// <summary>
        /// Property which determines the default color based on hittest state with expert stroke
        /// </summary>
        public bool StrokeChecked
        {
            get { return (bool)GetValue(StrokeCheckedProperty); }
            set
            {
                SetValue(StrokeCheckedProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for binding the pressure checked state from view model
        /// </summary>
        public static DependencyProperty PressureCheckedProperty = DependencyProperty.Register("PressureChecked", typeof(bool), typeof(StudentInkCanvas),
            new FrameworkPropertyMetadata(default(bool), new PropertyChangedCallback(OnPressureCheckedChanged)));
        private static void OnPressureCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Debug.WriteLine(e.NewValue);
            ((StudentInkCanvas)d).PressureChecked = (bool)e.NewValue;
        }

        /// <summary>
        /// true if the feedback is requested for pressure
        /// </summary>
        public bool PressureChecked
        {
            get { return (bool)GetValue(PressureCheckedProperty); }
            set
            {
                PressureCheckedEventArgs args = new PressureCheckedEventArgs();
                args.state = value;
                OnPressureChecked(args);
                SetValue(PressureCheckedProperty, value);
            }
        }

        /// <summary>
        /// Custom Renderer for chaning the behaviour of the ink as it is being drawn
        /// </summary>
        private StudentDynamicRenderer studentCustomRenderer = new StudentDynamicRenderer();


        /// <summary>
        /// Value that holds if the pressure applied is higher or lower that the experts. Must return either -1,0 or 1
        /// </summary>
        //private int expertPressureFactor = 0;
        //public int ExpertPressureFactor
        //{

        //    get { return expertPressureFactor; }
        //    set
        //    {
        //        if (value == 0 || value == -1 || value == 1)
        //            expertPressureFactor = value;
        //        //if (PressureChecked)
        //        //{
        //        //    PressureChangedEventArgs args = new PressureChangedEventArgs();
        //        //    args.pressurefactor = expertPressureFactor;
        //        //    OnExpertPressureChanged(args);
        //        //}

        //    }
        //}

        Guid studentTimestamp = new Guid("12345678-9012-3456-7890-123456789012");
        List<DateTime> StrokeTime = new List<DateTime>();

        LogStylusDataPlugin logStylusDataPlugin = new LogStylusDataPlugin();
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentInkCanvas() : base()
        {
            //instantiate the customDynamicRenderer
            studentCustomRenderer = new StudentDynamicRenderer();
            this.DynamicRenderer = studentCustomRenderer;
            //Debug.WriteLine("InkPresenter status: " + this.InkPresenter.IsEnabled);
            this.StylusPlugIns.Add(logStylusDataPlugin);
            LogStylusDataPlugin.StylusMoveProcessEnded += LogStylusDataPlugin_StylusMoveProcessEnded;

        }

        private void LogStylusDataPlugin_StylusMoveProcessEnded(object sender, StylusMoveProcessEndedEventArgs e)
        {
            //if the StrokeHitState is the same as the previous state do nothing or else
            if (StrokeHitState != PreStrokeHitState)
            {
                if (hitChangedPoints.Keys.Contains<Stroke>(e.StrokeRef) == false)
                {
                    if (PreStrokeHitState == false)
                    {
                        hitChangedPoints.Add(e.StrokeRef, Colors.Red);
                    }
                    if (PreStrokeHitState == true)
                    {
                        hitChangedPoints.Add(e.StrokeRef, Colors.Green);
                    }
                }
                PreStrokeHitState = StrokeHitState;
            }
        }

        #region events definition

        /// <summary>
        /// Event raised when the pen is lifed or put down. declared it as static as the listening class would only listen to the object.
        /// thid event is subscribed by the expert inkcanvas and stops the animation
        /// </summary>
        public static event EventHandler PenDownUpEvent;
        protected virtual void OnPenDownUpEvent(EventArgs e)
        {
            EventHandler handler = PenDownUpEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// event that notifies that the hittest was detected
        /// </summary>
        public static event EventHandler<HitTestWithExpertEventArgs> HitTestWithExpertEvent;
        protected virtual void OnHitTestWithExpert(HitTestWithExpertEventArgs e)
        {
            EventHandler<HitTestWithExpertEventArgs> handler = HitTestWithExpertEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class HitTestWithExpertEventArgs : EventArgs
        {
            public bool state { get; set; }
        }

        /// <summary>
        /// event that updates if the feedback on pressure should be given
        /// </summary>
        public static event EventHandler<PressureCheckedEventArgs> PressureCheckedEvent;
        protected virtual void OnPressureChecked(PressureCheckedEventArgs e)
        {
            EventHandler<PressureCheckedEventArgs> handler = PressureCheckedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class PressureCheckedEventArgs : EventArgs
        {
            public bool state { get; set; }
        }

        /// <summary>
        /// event that notifies that the pressure applied is higher or lower than the experts
        /// </summary>
        //public static event EventHandler<PressureChangedEventArgs> PressureChangedEvent;
        //protected virtual void OnExpertPressureChanged(PressureChangedEventArgs c)
        //{
        //    EventHandler<PressureChangedEventArgs> handler = PressureChangedEvent;
        //    if (handler != null)
        //    {
        //        handler(this, c);
        //    }
        //}
        //public class PressureChangedEventArgs : EventArgs
        //{
        //    public int pressurefactor { get; set; }
        //}
        #endregion

        #region eventHandlers
        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            //raise the pendown event for the expertinkcanvas to stop the animation
            OnPenDownUpEvent(EventArgs.Empty);
            base.OnStylusDown(e);

        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            //StrokeTime = new List<DateTime>();
            OnPenDownUpEvent(EventArgs.Empty);
            if (hitChangedPoints.Keys.Contains<Stroke>(new Stroke(e.GetStylusPoints((InkCanvas)e.Source))) == false)
            {
                if (PreStrokeHitState == false)
                {
                    hitChangedPoints.Add(new Stroke(e.GetStylusPoints((InkCanvas)e.Source)), Colors.Red);
                }
                if (PreStrokeHitState == true)
                {
                    hitChangedPoints.Add(new Stroke(e.GetStylusPoints((InkCanvas)e.Source)), Colors.Green);
                }
            }
            base.OnStylusUp(e);
        }

        Dictionary<Stroke, Color> hitChangedPoints = new Dictionary<Stroke, Color>();
        protected override void OnStylusMove(StylusEventArgs e)
        {
            StrokeTime.Add(DateTime.Now);
            base.OnStylusMove(e);
        }

        //this event is not raised when stroke object is added programatically but rather when the pen is lifted up
        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            Debug.WriteLine(hitChangedPoints.Count);
            if (e.Stroke.StylusPoints.Count < 1)
            {
                return;
            }

            if (StrokeChecked)
            {
                //remove the stroke and instead create new stroke from _tempSPCollection at the end even though the color may not have changed
                this.Strokes.Remove(e.Stroke);
                //stylus point collection that holds all the points in the arguements
                StylusPointCollection spc = new StylusPointCollection(e.Stroke.StylusPoints);
                //styluspoint collection where the points are stored until they are passed as stroke
                StylusPointCollection tempStrokeSPC = new StylusPointCollection();
                //iterate through each points until it hits the hitChangedPoints
                for (int i = 0; i < spc.Count; i++)
                {
                    //if the tempStrokeSPC is empty
                    if (tempStrokeSPC.Count == 0)
                    {
                        //add the first point to the collection and move on to next iteration
                        tempStrokeSPC.Add(spc[i]);
                        continue;
                    }
                    else
                    {
                        //add the current point to tempStrokeSPC
                        tempStrokeSPC.Add(spc[i]);
                    }
                    if (hitChangedPoints.Keys.Count > 1)
                    {
                        Stroke foundStroke = null;
                        //iterate through each points in hitchangedpoints to check if the points intersect each other
                        foreach (Stroke p in hitChangedPoints.Keys.ToList())
                        {
                            
                            if (p.HitTest(spc[i].ToPoint(), 5))
                            {
                                //if the points intersect, create a stroke with the points in tempStrokeSPC
                                StudentStroke customStroke = new StudentStroke(tempStrokeSPC, hitChangedPoints[p], PressureChecked);
                                //customStroke.AddPropertyData(studentTimestamp, StrokeTime.ToArray());
                                //add the strokes in INKcanvas
                                this.Strokes.Add(customStroke);
                                //pass the customstroke to base class
                                //InkCanvasStrokeCollectedEventArgs args = new InkCanvasStrokeCollectedEventArgs(customStroke);
                                //base.OnStrokeCollected(args);

                                //Remove the hit point from the dictionary
                                //hitChangedPoints.Remove(p);
                                foundStroke = p;
                                //empty the tempStrokeSPC
                                tempStrokeSPC = new StylusPointCollection();
                                break;
                            }
                        }
                        if (foundStroke != null)
                        {
                            hitChangedPoints.Remove(foundStroke);
                        }
                    }
                }
                //if there are no hits at all or only one value stored in the hitchangedpoints
                if (tempStrokeSPC.Count > 0)
                {
                    StudentStroke customStroke = new StudentStroke(tempStrokeSPC, hitChangedPoints.Values.Last(), PressureChecked);
                    //customStroke.AddPropertyData(studentTimestamp, StrokeTime.ToArray());
                    this.Strokes.Add(customStroke);
                    //InkCanvasStrokeCollectedEventArgs args = new InkCanvasStrokeCollectedEventArgs(customStroke);
                    //base.OnStrokeCollected(args);
                }
            }
            else
            {
                this.Strokes.Remove(e.Stroke);
                StudentStroke customStroke = new StudentStroke(e.Stroke.StylusPoints, PressureChecked);
                this.Strokes.Add(customStroke);
            }

            Debug.WriteLine("Strokes Count: " + this.Strokes.Count);
            //StrokeTime = new List<DateTime>();
            hitChangedPoints = new Dictionary<Stroke, Color>();

        }

        #endregion
        //Color that holds the color frm earlier stroke

        /// <summary>
        /// method that uses the _tempStyluspointCollection to create studentStroke and save stroke in this canvas StrokeCollection
        /// </summary>
        //private void AddStroke(StylusPointCollection spc, Color c)
        //{
        //    if(spc.Count < 2)
        //    {
        //        return;
        //    }
            
        //    //save the stroke temporarily for filtering the data
        //    StudentStroke tempStroke = FilterStrokeData(new Stroke(spc));
        //    //convert the stroke into studentcanvasStroke
        //    StudentStroke customStroke = new StudentStroke(tempStroke.StylusPoints, c);
        //    customStroke.AddPropertyData(studentTimestamp,StrokeTime.ToArray());
        //    this.InkPresenter.Strokes.Add(customStroke);
        //    Debug.WriteLine("stylus point: " + customStroke.StylusPoints.Count + " timestamps collected" + StrokeTime.Count);
        //    //store the last point temporarily to
        //    //StylusPoint prevfirstStylusPoint = tempStroke.StylusPoints.First();
        //    // create a new stylusPointCollection
        //    _tempSPCollection = new StylusPointCollection();
        //    //add the last point to the new collection to avoid breaking off
        //    //_tempSPCollection.Add(prevfirstStylusPoint);
        //    StrokeTime = new List<DateTime>();
            
        //    Debug.WriteLine("Stroke added: Total stroke count = " + this.Strokes.Count);
        //}

        Point prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
        /// <summary>
        /// Method for removing excess styluspoints from a expert stroke
        /// </summary>
        /// <param name="stroke"></param>
        private StudentStroke FilterStrokeData(Stroke stroke)
        {
            //create a copy of stroke for iterating
            StudentStroke tempStroke = new StudentStroke(stroke.StylusPoints, PressureChecked);
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
