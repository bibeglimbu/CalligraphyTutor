using CalligraphyTutor.CustomStroke;
using CalligraphyTutor.StylusPlugins;
using CalligraphyTutor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace CalligraphyTutor.CustomInkCanvas
{
    class StudentInkCanvas : BaseInkCanvas
    {
        #region Dependency Property

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
        /// Property which determines the default PreviousColor based on hittest state with expert stroke
        /// </summary>
        public bool StrokeChecked
        {
            get { return (bool)GetValue(StrokeCheckedProperty); }
            set
            {
                StrokeCheckedEventArgs args = new StrokeCheckedEventArgs();
                args.state = value;
                OnStrokeChecked(args);
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
        /// Dependency property for binding the pressure checked state from view model
        /// </summary>
        public static DependencyProperty SpeedCheckedProperty = DependencyProperty.Register("SpeedChecked", typeof(bool), typeof(StudentInkCanvas),
            new FrameworkPropertyMetadata(default(bool), new PropertyChangedCallback(OnSpeedCheckedChanged)));
        private static void OnSpeedCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Debug.WriteLine(e.NewValue);
            ((StudentInkCanvas)d).SpeedChecked = (bool)e.NewValue;
        }
        /// <summary>
        /// true if the feedback is requested for speed
        /// </summary>
        public bool SpeedChecked
        {
            get { return (bool)GetValue(SpeedCheckedProperty); }
            set
            {
                SpeedCheckedEventArgs args = new SpeedCheckedEventArgs();
                args.state = value;
                OnSpeedChecked(args);
                SetValue(SpeedCheckedProperty, value);
            }
        }
        #endregion

        #region Property

        #endregion

        #region Vars
        /// <summary>
        /// Velocity at which the student draws the stroke
        /// </summary>
        private double studentVelocity = -0.01d;
        /// <summary>
        /// velocity at which the expert draws the stroke
        /// </summary>
        private double expertVelocity = -0.01d;

        //LogStylusDataPlugin logStylusDataPlugin = new LogStylusDataPlugin();

        /// <summary>
        /// Plugin for checking the hitStroke with the expert stroke
        /// </summary>
        HitStrokeTesterPlugin hitStrokeTesterPlugin = new HitStrokeTesterPlugin();
        /// <summary>
        /// Custom Renderer for chaning the behaviour of the ink as it is being drawn
        /// </summary>
        private StudentDynamicRenderer studentCustomRenderer = new StudentDynamicRenderer();

        /// <summary>
        /// Passes on the points to the StrokeClass which changes how the look of the final stroke.
        /// </summary>
        private Dictionary<Stroke, Color> hitChangedPoints = new Dictionary<Stroke, Color>();

        
        Guid studentTimestamp = new Guid("12345678-9012-3456-7890-123456789012");
        /// <summary>
        /// Stores time required to create the stroke
        /// </summary>
        List<DateTime> StrokeTime = new List<DateTime>();
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentInkCanvas()
        {
            //instantiate the customDynamicRenderer
            studentCustomRenderer = new StudentDynamicRenderer();
            this.DynamicRenderer = studentCustomRenderer;
            StudentViewModel.ExpertVelocityCalculatedEvent += StudentViewModel_ExpertVelocityCalculatedEvent;
            this.EditingMode = InkCanvasEditingMode.Ink;
            this.StylusPlugIns.Add(hitStrokeTesterPlugin);
            //this.StylusPlugIns.Add(logStylusDataPlugin);
            LogStylusDataPlugin.StylusMoveProcessEnded += LogStylusDataPlugin_StylusMoveProcessEnded;
            HitStrokeTesterPlugin.HitChangePointsEvent += HitStrokeTesterPlugin_HitChangePointsEvent;
            this.DefaultDrawingAttributes.FitToCurve = false;
            this.DefaultDrawingAttributes.StylusTip = StylusTip.Ellipse;
        }

        #region events definition

        /// <summary>
        /// Event raised when the pen is lifed or put down. declared it as static as the listening class would only listen to the object.
        /// thid event is subscribed by the expert inkcanvas and stops the animation
        /// </summary>
        public static event EventHandler<PenDownUpEventEventArgs> PenDownUpEvent;
        protected virtual void OnPenDownUpEvent(PenDownUpEventEventArgs e)
        {
            EventHandler<PenDownUpEventEventArgs> handler = PenDownUpEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class PenDownUpEventEventArgs : EventArgs
        {
            public bool IsPenDown { get; set; }
        }

        /// <summary>
        /// Event raised when the pen is in Range. declared it as static as the listening class would only listen to the object.
        /// thid event is subscribed by the expert inkcanvas and stops the animation
        /// </summary>
        public static event EventHandler<PenInRangeEventEventArgs> PenInRangeEvent;
        protected virtual void OnPenInRangeEvent(PenInRangeEventEventArgs e)
        {
            EventHandler<PenInRangeEventEventArgs> handler = PenInRangeEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class PenInRangeEventEventArgs : EventArgs
        {
            public Point Stylus_Point { get; set; }
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
        /// event that updates if the feedback on Stroke should be given
        /// </summary>
        public static event EventHandler<StrokeCheckedEventArgs> StrokeCheckedEvent;
        protected virtual void OnStrokeChecked(StrokeCheckedEventArgs e)
        {
            EventHandler<StrokeCheckedEventArgs> handler = StrokeCheckedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class StrokeCheckedEventArgs : EventArgs
        {
            public bool state { get; set; }
        }

        /// <summary>
        /// event that updates if the feedback on Speed should be given
        /// </summary>
        public static event EventHandler<SpeedCheckedEventArgs> SpeedCheckedEvent;
        protected virtual void OnSpeedChecked(SpeedCheckedEventArgs e)
        {
            EventHandler<SpeedCheckedEventArgs> handler = SpeedCheckedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class SpeedCheckedEventArgs : EventArgs
        {
            public bool state { get; set; }
        }

        #endregion

        #region eventHandlers
        private void StudentViewModel_ExpertVelocityCalculatedEvent(object sender, StudentViewModel.ExpertVelocityCalculatedEventArgs e)
        {
            expertVelocity = e.velocity;
        }
        private void HitStrokeTesterPlugin_HitChangePointsEvent(object sender, HitStrokeTesterPlugin.HitChangePointsEventArgs e)
        {
            hitChangedPoints = e.hitChangedPoints;
        }
        private void LogStylusDataPlugin_StylusMoveProcessEnded(object sender, StylusMoveProcessEndedEventArgs e)
        {
            studentVelocity = e.StrokeVelocity;
        }

        #endregion

        #region OverRides
        protected override void OnStylusButtonDown(StylusButtonEventArgs e)
        {
            //raise an event to let expert inkcanvas know that the pen is down
            PenDownUpEventEventArgs args = new PenDownUpEventEventArgs();
            args.IsPenDown = true;
            OnPenDownUpEvent(args);
            base.OnStylusButtonDown(e);
        }

        protected override void OnStylusButtonUp(StylusButtonEventArgs e)
        {
            //raise an event to let expert inkcanvas know that the pen is up
            PenDownUpEventEventArgs args = new PenDownUpEventEventArgs();
            args.IsPenDown = false;
            OnPenDownUpEvent(args);
            base.OnStylusButtonUp(e);
        }

        protected override void OnStylusInRange(StylusEventArgs e)
        {
            PenInRangeEventEventArgs args = new PenInRangeEventEventArgs();
            args.Stylus_Point = e.GetPosition((InkCanvas)e.Source);
            OnPenInRangeEvent(args);
            base.OnStylusInRange(e);
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            StrokeTime.Add(DateTime.Now);
            base.OnStylusMove(e);
        }

        //this event is not raised when stroke object is added programatically but rather when the pen is lifted up
        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            //Debug.WriteLine(hitChangedPoints.Count);
            if (e.Stroke.StylusPoints.Count < 1)
            {
                return;
            }

            //if stroke is checked and the hit points are not null
            if (StrokeChecked == true && hitChangedPoints.Keys.Count !=0)
            {
                //remove the stroke and instead create new stroke from _tempSPCollection at the end even though the PreviousColor may not have changed
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
                    //if there is only one value in hitchangedpoints the it didnt hit any states
                    if (hitChangedPoints.Keys.Count > 1)
                    {
                        //iterate through each points in hitchangedpoints to check if the points intersect each other
                        foreach (Stroke p in hitChangedPoints.Keys.ToList())
                        {
                            if (p == hitChangedPoints.Keys.Last())
                            {
                                break;
                            }
                            if (p.HitTest(spc[i].ToPoint(), 2.5))
                            {
                                //if the points intersect, create a stroke with the points in tempStrokeSPC
                                StudentStroke customStroke = new StudentStroke(tempStrokeSPC, hitChangedPoints[p], PressureChecked);
                                //customStroke.AddPropertyData(studentTimestamp, StrokeTime.ToArray());
                                //add the strokes in INKcanvas
                                this.Strokes.Add(customStroke);
                                //Remove the hit point from the dictionary
                                hitChangedPoints.Remove(p);
                                //empty the tempStrokeSPC
                                tempStrokeSPC = new StylusPointCollection();
                                break;
                            }
                        }
                    }
                    if (i == spc.Count - 1 && tempStrokeSPC.Count!=0)
                    {
                        //if the points intersect, create a stroke with the points in tempStrokeSPC
                        StudentStroke customStroke = new StudentStroke(tempStrokeSPC, hitChangedPoints.Values.Last(), PressureChecked);
                        //customStroke.AddPropertyData(studentTimestamp, StrokeTime.ToArray());
                        //add the strokes in INKcanvas
                        this.Strokes.Add(customStroke);
                        //empty the tempStrokeSPC
                        tempStrokeSPC = new StylusPointCollection();
                    }
                }
                //if there are no hits at all or only one value stored in the hitchangedpoints
                //if (tempStrokeSPC.Count > 0)
                //{
                //    //StudentStroke customStroke = new StudentStroke(tempStrokeSPC, studentCustomRenderer.hitChangedPoints.Values.Last(), PressureChecked);
                //    StudentStroke customStroke = new StudentStroke(tempStrokeSPC, PressureChecked);
                //    //customStroke.AddPropertyData(studentTimestamp, StrokeTime.ToArray());
                //    this.Strokes.Add(customStroke);
                //    //InkCanvasStrokeCollectedEventArgs args = new InkCanvasStrokeCollectedEventArgs(customStroke);
                //    //base.OnStrokeCollected(args);
                //}
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
    }
}
