using CalligraphyTutor.StylusPlugins;
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

namespace CalligraphyTutor.Model
{
    class StudentInkCanvas : InkCanvas
    {
        #region vars

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

        /// <summary>
        /// Custom Renderer for chaning the behaviour of the ink as it is being drawn
        /// </summary>
        private StudentDynamicRenderer studentCustomRenderer = new StudentDynamicRenderer();

        private bool _isStylusDown = false;
        /// <summary>
        /// True if the pen is touching the digitizer
        /// </summary>
        public bool IsStylusDown
        {
            get { return _isStylusDown; }
            set
            {
                _isStylusDown = value;

            }
        }

        Guid studentTimestamp = new Guid("12345678-9012-3456-7890-123456789012");
        List<DateTime> StrokeTime = new List<DateTime>();

        LogStylusDataPlugin logStylusDataPlugin = new LogStylusDataPlugin();

        private double studentVelocity = 0d;
        private double expertVelocity = -0.01d;
        // Declare a System.Threading.CancellationTokenSource.
        CancellationTokenSource cts;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentInkCanvas() : base()
        {
            //instantiate the customDynamicRenderer
            studentCustomRenderer = new StudentDynamicRenderer();
            this.DynamicRenderer = studentCustomRenderer;
            StudentDynamicRenderer.ExpertVelocityCalculatedEvent += StudentCustomRenderer_ExpertVelocityCalculatedEvent;
            this.StylusPlugIns.Add(logStylusDataPlugin);
            LogStylusDataPlugin.StylusMoveProcessEnded += LogStylusDataPlugin_StylusMoveProcessEnded;
            this.DefaultDrawingAttributes.FitToCurve = false;
            this.DefaultDrawingAttributes.StylusTip = StylusTip.Ellipse;
        }

        private void LogStylusDataPlugin_StylusMoveProcessEnded(object sender, StylusMoveProcessEndedEventArgs e)
        {
            studentVelocity = e.StrokeVelocity;
        }

        private void StudentCustomRenderer_ExpertVelocityCalculatedEvent(object sender, StudentDynamicRenderer.ExpertVelocityCalculatedEventArgs e)
        {
            expertVelocity = e.velocity;
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
        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            //raise the pendown event for the expertinkcanvas to stop the animation
            IsStylusDown = true;
            PenDownUpEventEventArgs args = new PenDownUpEventEventArgs();
            args.state = IsStylusDown;
            OnPenDownUpEvent(args);
            cts = new CancellationTokenSource();
            base.OnStylusDown(e);

        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            //StrokeTime = new List<DateTime>();
            IsStylusDown = false;
            PenDownUpEventEventArgs args = new PenDownUpEventEventArgs();
            args.state = IsStylusDown;
            OnPenDownUpEvent(args);
            //cancel async task
            if (cts != null)
            {
                cts.Cancel();
            }
            base.OnStylusUp(e);
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            StrokeTime.Add(DateTime.Now);
            if (SpeedChecked == true && expertVelocity < 0)
            {
                if (studentVelocity > expertVelocity + 5)
                {
                    playSound(cts.Token);
                }

            }

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
                    //if there is only one value in hitchangedpoints the it didnt hit any states
                    if (studentCustomRenderer.hitChangedPoints.Keys.Count > 1)
                    {
                        //iterate through each points in hitchangedpoints to check if the points intersect each other
                        foreach (Stroke p in studentCustomRenderer.hitChangedPoints.Keys.ToList())
                        {
                            if (p == studentCustomRenderer.hitChangedPoints.Keys.Last())
                            {
                                break;
                            }
                            if (p.HitTest(spc[i].ToPoint(), 2.5))
                            {
                                //if the points intersect, create a stroke with the points in tempStrokeSPC
                                StudentStroke customStroke = new StudentStroke(tempStrokeSPC, studentCustomRenderer.hitChangedPoints[p], PressureChecked);
                                //customStroke.AddPropertyData(studentTimestamp, StrokeTime.ToArray());
                                //add the strokes in INKcanvas
                                this.Strokes.Add(customStroke);
                                //Remove the hit point from the dictionary
                                studentCustomRenderer.hitChangedPoints.Remove(p);
                                //empty the tempStrokeSPC
                                tempStrokeSPC = new StylusPointCollection();
                                break;
                            }
                        }
                    }
                    if (i == spc.Count - 1 && tempStrokeSPC.Count!=0)
                    {
                        //if the points intersect, create a stroke with the points in tempStrokeSPC
                        StudentStroke customStroke = new StudentStroke(tempStrokeSPC, studentCustomRenderer.hitChangedPoints.Values.Last(), PressureChecked);
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
            studentCustomRenderer.hitChangedPoints = new Dictionary<Stroke, Color>();

        }

        #endregion

        #region  Play Sound
        /// <summary>
        /// returns the bin folder in the directory
        /// </summary>
        string directory = Environment.CurrentDirectory;

        /// <summary>
        /// variable used to calculate if the sound should be played.
        /// </summary>
        DateTime PlayDateTime = DateTime.Now;

        /// <summary>
        /// play the audio asynchronously. the cancellation token cancells the lined up async task for this method
        /// </summary>
        public async void playSound(CancellationToken ct)
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(directory + @"\sounds\Error.wav");
            if ((DateTime.Now - PlayDateTime).TotalSeconds > 1.5)
            {
                PlayDateTime = DateTime.Now;
                await Task.Run(() => player.Play());
                Debug.WriteLine(PlayDateTime);

            }

        }

        #endregion
    }
}
