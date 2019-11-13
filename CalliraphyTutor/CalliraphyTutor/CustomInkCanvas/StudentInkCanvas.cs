using CalligraphyTutor.Managers;
using CalligraphyTutor.StylusPlugins;
using CalligraphyTutor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

        /// <summary>
        /// Dependency property for binding Student velocity to the view model
        /// </summary>
        public static DependencyProperty StudentVelocityProperty = DependencyProperty.Register("StudentVelocity", typeof(double), typeof(StudentInkCanvas),
            new PropertyMetadata(0d, new PropertyChangedCallback(OnStudentVelocityChanged)));
        private static void OnStudentVelocityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Debug.WriteLine(e.NewValue);
        }
        /// <summary>
        /// holds the veolocity at which the stroke is being drawn
        /// </summary>
        public double StudentVelocity
        {
            get { return (double)GetValue(StudentVelocityProperty); }
            set
            {
                SetValue(StudentVelocityProperty, value);
            }
        }

        #endregion

        #region Vars

        /// <summary>
        /// velocity at which the expert draws the stroke
        /// </summary>
        public double ExpertVelocity = -0.01d;

        //LogStylusDataPlugin logStylusDataPlugin = new LogStylusDataPlugin();

        /// <summary>
        /// Custom Renderer for chaning the behaviour of the ink as it is being drawn
        /// </summary>
        private StudentDynamicRenderer studentCustomRenderer = new StudentDynamicRenderer();

        /// <summary>
        /// Passes on the points to the StrokeClass which changes how the look of the final stroke.
        /// </summary>
        private List<Point> hitChangedPoints = new List<Point>();

        /// <summary>
        /// Stores time for calculating the time reqired to create throw the pen moved event
        /// </summary>
        //List<int> tempStrokeTime = new List<int>();

        Guid ExpertVelocity_Guid = new Guid("12345678-9012-3456-7890-123456789E12");

        /// <summary>
        /// Declare a System.Threading.CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts;
        /// <summary>
        /// threshold for the speed
        /// </summary>
        const double SpeedThreshold = 0.25d;

        /// <summary>
        /// Manages the time and attributes of the stroke
        /// </summary>
        StrokeAttributesManager myStrokeAttManager;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentInkCanvas()
        {
            myStrokeAttManager = new StrokeAttributesManager();
            //instantiate the customDynamicRenderer
            studentCustomRenderer = new StudentDynamicRenderer();
            this.DynamicRenderer = studentCustomRenderer;
            this.EditingMode = InkCanvasEditingMode.Ink;
            StudentDynamicRenderer.HitChangePointsEvent += StudentDynamicRenderer_HitChangePointsEvent;
            StudentDynamicRenderer.NearestStylusPointCalculatedEvent += StudentDynamicRenderer_NearestStylusPointCalculatedEvent;
            this.DefaultDrawingAttributes.FitToCurve = false;
            this.DefaultDrawingAttributes.StylusTip = StylusTip.Ellipse;
        }

        #region events definition

        /// <summary>
        /// Event raised when the pen is lifed or put down. declared it as static as the listening class would only listen to the object.
        /// thid event is subscribed by the expert inkcanvas and stops the animation
        /// </summary>
        public static event EventHandler<PenOutOfRangeEventArgs> PenOutOfRangeEvent;
        protected virtual void OnPenOutOfRangeEvent(PenOutOfRangeEventArgs e)
        {
            EventHandler<PenOutOfRangeEventArgs> handler = PenOutOfRangeEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class PenOutOfRangeEventArgs : EventArgs
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
        private void StudentDynamicRenderer_HitChangePointsEvent(object sender, StudentDynamicRenderer.HitChangePointsEventArgs e)
        {
            hitChangedPoints = e.hitChangedPoints;
        }
        private void StudentDynamicRenderer_NearestStylusPointCalculatedEvent(object sender, StudentDynamicRenderer.NearestExpertStylusPointCalculatedEventArgs e)
        {
            ExpertVelocity = GetGuidValue(e.stroke, ExpertVelocity_Guid);
            Debug.WriteLine("Expert Velocity: " + ExpertVelocity);
        }
        #endregion

        #region OverRides

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            cts = new CancellationTokenSource();
            //Add the initial time for the event
            myStrokeAttManager.StrokeTime.Add(e.Timestamp);
            //raise an event to let expert inkcanvas know that the pen is down
            //PenDownUpEventEventArgs args = new PenDownUpEventEventArgs();
            //args.IsPenDown = true;
            //OnPenDownUpEvent(args);
            base.OnStylusDown(e);
        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            //cancel async task
            if (cts != null)
            {
                cts.Cancel();
            }
            //reset the prevSPPoint
            //prevSPPoint = new StylusPoint(double.NegativeInfinity, double.NegativeInfinity);
            base.OnStylusUp(e);
        }

        protected override void OnStylusInRange(StylusEventArgs e)
        {
            PenInRangeEventEventArgs args = new PenInRangeEventEventArgs();
            args.Stylus_Point = e.GetPosition((InkCanvas)e.Source);
            OnPenInRangeEvent(args);
            base.OnStylusInRange(e);
        }

        protected override void OnStylusOutOfRange(StylusEventArgs e)
        {
            //raise an event to let expert inkcanvas know that the pen is up
            PenOutOfRangeEventArgs args = new PenOutOfRangeEventArgs();
            //args.IsPenDown = false;
            OnPenOutOfRangeEvent(args);
            base.OnStylusOutOfRange(e);
        }

        /// <summary>
        /// value that holds the last iteration point to calculate the distance.
        /// </summary>
        protected override void OnStylusMove(StylusEventArgs e)
        {
            Stroke tempStroke = new Stroke(e.GetStylusPoints((InkCanvas)e.Source));
            if(tempStroke.StylusPoints.Count <= 0)
            {
                return;
            }
            Debug.WriteLine("Students POint: " + e.GetStylusPoints((InkCanvas)e.Source).Count);
            //add the time at which the event is thrown
            myStrokeAttManager.StrokeTime.Add(e.Timestamp);
            //provide the feedback on the speed. 
            GiveSpeedFeedbackAsync(tempStroke);
            base.OnStylusMove(e);
        }

        //this event is not raised when stroke object is added programatically but rather when the pen is lifted up
        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            void addStroke(StylusPointCollection c, bool h, out StylusPointCollection tspc)
            {
                DrawingAttributes d = new DrawingAttributes();
                //change color acc to earlier hitstate
                if (h == true)
                {
                    d.Color = Colors.LightGreen;
                }
                else if (h == false)
                {
                    d.Color = Colors.Red;
                }
                Stroke stroke = new Stroke(c, d);
                this.Strokes.Add(stroke);
                tspc = new StylusPointCollection();
            }
            //if stroke is checked and the hit points are not null
            if (hitChangedPoints.Count !=0)
            {
                //remove the stroke and instead create new stroke from _tempSPCollection at the end even though the PreviousColor may not have changed
                this.Strokes.Remove(e.Stroke);
                //stylus point collection that holds all the points in the arguements
                StylusPointCollection spc = new StylusPointCollection(e.Stroke.StylusPoints);
                StylusPointCollection tempSPC = new StylusPointCollection();
                bool HitStateChanged = false;
                for (int i = 0; i < spc.Count; i++)
                {
                    tempSPC.Add(spc[i]);
                    //if its the first iteration
                    if (i == 0)
                    {
                        HitStateChanged = hitChangedPoints.Contains(spc[i].ToPoint());
                        continue;
                    }
                    //if its the last point
                    if(i == spc.Count - 1)
                    {
                        addStroke(tempSPC, HitStateChanged, out tempSPC);
                        return;
                    }
                    //if the stroke hit state has changed
                    bool tempHitCheck = hitChangedPoints.Contains(spc[i].ToPoint());
                    if (tempHitCheck != HitStateChanged)
                    {
                        //pass it to the new collection for continuation
                        StylusPoint tempSPholder = tempSPC.Last();
                        addStroke(tempSPC, HitStateChanged, out tempSPC);
                        tempSPC.Add(tempSPholder);
                        HitStateChanged = tempHitCheck;
                    }
                }
            }
            hitChangedPoints = new List<Point>();
        }

        #endregion

        #region Native Methods
        private async void GiveSpeedFeedbackAsync(Stroke s)
        {
            await Task.Run(() => {
                //calculate the student velocity which notifies the viewmodel by updating the dependency property
                double  tempVelocity = myStrokeAttManager.CalculateVelocity(s);
                if(tempVelocity <=0 || Double.IsInfinity(tempVelocity) || Double.IsNaN(tempVelocity))
                {
                    Debug.WriteLine("StudentStrokeCount Invalid Value: " + tempVelocity + System.Environment.NewLine);
                    return;
                }

                //if the students velocity is higher than the experts velocity + threshold
                if (tempVelocity > ExpertVelocity + SpeedThreshold)
                {
                    playSound(cts.Token);
                    Debug.WriteLine("Sound gong played");
                }
                if (myStrokeAttManager.StrokeTime.Count > 2)
                {
                    myStrokeAttManager.StrokeTime.RemoveRange(0, myStrokeAttManager.StrokeTime.Count - 1);
                }
                this.Dispatcher.Invoke(() =>
                {
                    StudentVelocity = tempVelocity;
                    
                });
            });

        }

        /// <summary>
        /// Get Custom GUID properties from stroke
        /// </summary>
        /// <param name="s"></param>
        private double GetGuidValue(Stroke s, Guid guid)
        {
            double tempVelocity = 0d;
            if (s.ContainsPropertyData(guid))
            {
                object data = s.GetPropertyData(guid);
                if (data is double)
                {
                    tempVelocity = (double)data;
                }

            }
            return tempVelocity;
        }

        #endregion

        #region  Play Sound
        /// <summary>
        /// returns the bin folder in the directory
        /// </summary>
        //string directory = Environment.CurrentDirectory;
        //string directory = AppDomain.CurrentDomain.BaseDirectory;
        string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
                //Debug.WriteLine(PlayDateTime);

            }

        }

        #endregion
    }
}
