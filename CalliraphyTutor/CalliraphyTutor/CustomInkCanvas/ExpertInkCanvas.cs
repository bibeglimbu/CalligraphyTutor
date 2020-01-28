using CalligraphyTutor.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CalligraphyTutor.CustomInkCanvas
{
    class ExpertInkCanvas : BaseInkCanvas
    {
        #region CustomDependencyProperty

        /// <summary>
        /// Dependency property for binding the Number of student strokes from view model
        /// </summary>
        public static DependencyProperty StudentStrokeCountProperty = DependencyProperty.Register("StudentStrokeCount", typeof(int), typeof(ExpertInkCanvas),
            new FrameworkPropertyMetadata(0, new PropertyChangedCallback(OnStudentStrokeCountChanged)));
        private static void OnStudentStrokeCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("ExpertIC/StudentStrokeCount : " + ((int)e.NewValue).ToString()) ;
            ((ExpertInkCanvas)d).StudentStrokeCount = (int)e.NewValue;
        }
        /// <summary>
        /// Number of Strokes in the student
        /// </summary>
        public int StudentStrokeCount
        {
            get { return (int)GetValue(StudentStrokeCountProperty); }
            set
            {
                SetValue(StudentStrokeCountProperty, value);
            }
        }

        #endregion Custom Dependency Property

        #region Variables
        /// <summary>
        /// Timer for animation
        /// </summary>
        private DispatcherTimer _dispatchTimer = new DispatcherTimer();

        Guid ExpertVelocity_Guid = new Guid("12345678-9012-3456-7890-123456789E12");
        
        /// <summary>
        /// true when the animation is still playing
        /// </summary>
        public bool AnimationPlaying = false;

        /// <summary>
        /// Manage
        /// </summary>
        StrokeAttributesManager myStrokeAttManager;
        #endregion

        #region Properties
        private bool _expertStrokesLoaded = false;
        /// <summary>
        /// True if the <see cref="ExpertStrokes"/> has been loaded.
        /// </summary>
        public bool ExpertStrokeLoaded
        {
            get { return _expertStrokesLoaded; }
            set
            {
                _expertStrokesLoaded = value;
            }
        }

        private bool _displayAnimation = true;
        /// <summary>
        /// True when animation is to be displayed
        /// </summary>
        public bool DisplayAnimation
        {
            get { return _displayAnimation; }
            set
            {
                _displayAnimation = value;
            }
        }

        #endregion

        #region Events definition
        /// <summary>
        /// Event raised when the expert stroke is loaded. Is subscribed by student ink canvas.
        /// </summary>
        public static event EventHandler<ExpertStrokeLoadedEventEventArgs> ExpertStrokeLoadedEvent;
        protected virtual void OnExpertStrokeLoaded(ExpertStrokeLoadedEventEventArgs e)
        {
            EventHandler<ExpertStrokeLoadedEventEventArgs> handler = ExpertStrokeLoadedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class ExpertStrokeLoadedEventEventArgs : EventArgs
        {
            public StrokeCollection strokes { get; set; }
            public bool state { get; set; }
        }
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public ExpertInkCanvas()
        {
            _dispatchTimer.Interval = new TimeSpan(10000);
            _dispatchTimer.Tick += _dispatchTimer_Tick;
            _dispatchTimer.Start();
            StudentInkCanvas.PenOutOfRangeEvent += StudentInkCanvas_PenOutOfRangeEvent;
            StudentInkCanvas.PenInRangeEvent += StudentInkCanvas_PenInRangeEvent;
            myStrokeAttManager = new StrokeAttributesManager();
        }

        #region EventHandlers
        private void StudentInkCanvas_PenInRangeEvent(object sender, StudentInkCanvas.PenInRangeEventEventArgs e)
        {
           //base.IsStylusInRange = true;
            //reset the animation variables
            ResetAnimation();
            _dispatchTimer.Stop();
            //set the animation playing to false as the thread can be terminsated in the middle and it can still be true even when no animation is running
            AnimationPlaying = false;

        }

        private void StudentInkCanvas_PenOutOfRangeEvent(object sender, StudentInkCanvas.PenOutOfRangeEventArgs e)
        {
            //base.IsStylusInRange = false;
            //start the animation when this event is detected
            if (AnimationPlaying == false)
            {
                //set the IsStylusInrange to true
                _dispatchTimer.Start();
            }
        }

        private void _dispatchTimer_Tick(object sender, EventArgs e)
        {
            //if the user has not requested any animation, is writing or there no expert strokes loaded
            if(DisplayAnimation == false || base.IsStylusInRange == true || this.Strokes.Count ==0)
            {
                return;
            }

            //play the animation
            //GetTimestamp(stroke.Last());
            Application.Current.Dispatcher.InvokeAsync(new Action( () =>
            {
                //if (StudentStrokes<0||StudentStrokes>this.Strokes.Count)
                //{
                //    StudentStrokes = 0;
                //}
                //Debug.WriteLine("StudentStrokeCount"+ StudentStrokeCount);
                Stroke sc = this.Strokes[StudentStrokeCount];
                DisplayStrokesPatterns(sc);
            }));
        }
        #endregion

        #region OverRiders

        protected override void OnStrokesReplaced(InkCanvasStrokesReplacedEventArgs e)
        {            
            //if the stroke doesnt have stylus points
            if (e.NewStrokes.Count == 0)
            {
                return;
            }
            //if stroke is loaded from the file change its PreviousColor to gray
            StrokeCollection sc = new StrokeCollection();
            foreach (Stroke s in e.NewStrokes)
            {
                s.DrawingAttributes.Color = Colors.Gray;
                sc.Add(s);
            }
            ExpertStrokeLoaded = true;
            ExpertStrokeLoadedEventEventArgs args = new ExpertStrokeLoadedEventEventArgs();
            args.strokes = sc;
            args.state = ExpertStrokeLoaded;
            OnExpertStrokeLoaded(args);

            // start the animation
            base.OnStrokesReplaced(e);
        }//OnStrokesReplaced

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            //add styluspoint from the event after checking to ensure that the collection doesnt already posses them
            myStrokeAttManager.StrokeTime.Add(e.Timestamp);
            Debug.WriteLine("TimeTaken_PenDown:" + e.Timestamp);
            base.OnStylusDown(e);
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            //add styluspoint from the event after checking to ensure that the collection doesnt already posses them
            myStrokeAttManager.StrokeTime.Add(e.Timestamp);
            Debug.WriteLine("TimeTaken_StylusUp:" + e.Timestamp);
            base.OnStylusMove(e);
        }

        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            //add styluspoint from the event after checking to ensure that the collection doesnt already posses them
            this.Strokes.Remove(e.Stroke);
            if (e.Stroke.StylusPoints.Count > 2)
            {
                //create a custom Stroke
                Stroke customStroke = new Stroke(e.Stroke.StylusPoints);
                double expertVelocity = myStrokeAttManager.CalculateVelocity(customStroke);
                Debug.WriteLine("Expert Velocity: " + expertVelocity);
                //attach customStroke
                customStroke.AddPropertyData(ExpertVelocity_Guid, expertVelocity);
                this.Strokes.Add(customStroke);
            }
            myStrokeAttManager.StrokeTime = new List<int>();
        }

        #endregion

        #region Native Methods
        /// <summary>
        /// Holds the animation frame
        /// </summary>
        private int animationCurrentFrame = 0;
        /// <summary>
        /// Holds the number of children in the UIelement
        /// </summary>
        private int globalInitialChildCount = -1; // set -1 to run it once
        private List<Ellipse> ChildEllipseHolder = new List<Ellipse>();
        private List<Point> ChildEllipsePointHolder = new List<Point>();
        //is the number of dots to produce
        private int maxEllipses = 20;
        /// <summary>
        /// Indicates the way in which the pattern was drawn by means of animation
        /// </summary>
        /// <param name="stroke">The Collection of Expert Strokes</param>
        private void DisplayStrokesPatterns(Stroke stroke)
        {
            //if its the first run remember the default number of  children and set AnimationPlaying to true;
            if (globalInitialChildCount < 0)
            {
                AnimationPlaying = true;
                globalInitialChildCount = this.Children.Count;
                DebugMessageHandler.SetDebugMessage(this,"Global Child count: " + globalInitialChildCount.ToString());
                return;
            }

            //create a single large stroke collecting all the styluspoint collection
            //StylusPointCollection spc = new StylusPointCollection();
            //for (int i = 0; i < stroke.Count; i++)
            //{
            //    spc.Add(stroke[i].StylusPoints);
            //}
            //Stroke s = new Stroke(spc);
            //if the animation frame of animation is less than the count of stylus points and the number of ellipses
            if (animationCurrentFrame < stroke.StylusPoints.Count + maxEllipses)
            {
                //if the animation frame is less than the number of children
                if (animationCurrentFrame < stroke.StylusPoints.Count - 1)
                {
                    //get the styluspoint location according to the frame
                    Point p = stroke.StylusPoints[animationCurrentFrame].ToPoint();
                    //add the point to the EllipseChildHolder
                    ChildEllipsePointHolder.Add(p);
                    //draw an ellipse in that point
                    Ellipse ellipse = new Ellipse
                    {
                        Width = 10,
                        Height = 10
                    };
                    double left = p.X - (ellipse.Width / 2);
                    double top = p.Y - (ellipse.Height / 2);
                    //change the color according to the sequence
                    Color EllipseColor = Color.FromArgb(255, 128, 0, 128);
                    ellipse.Fill = new SolidColorBrush(EllipseColor);
                    ellipse.Margin = new Thickness(left, top, 0, 0);
                    //this.Children.Insert(0,ellipse);
                    ellipse.Tag = "animation";
                    this.Children.Add(ellipse);
                    ChildEllipseHolder.Add(ellipse);

                }

                if (animationCurrentFrame >= maxEllipses)
                {
                    try
                    {
                        //for making the following ellipse smaller.
                        for (int i = ChildEllipseHolder.Count - 1; i > 0; i--)
                        {
                            ChildEllipseHolder[i].Width -= 0.25;
                            ChildEllipseHolder[i].Height -= 0.25;
                            ChildEllipseHolder[i].Margin = new Thickness(
                                ChildEllipsePointHolder[i].X - ChildEllipseHolder[i].Width / 2,
                                ChildEllipsePointHolder[i].Y - ChildEllipseHolder[i].Height / 2, 0, 0);
                        }
                        this.Children.RemoveAt(globalInitialChildCount);
                        ChildEllipseHolder.RemoveAt(0);
                        ChildEllipsePointHolder.RemoveAt(0);
                    }
                    catch
                    {
                        DebugMessageHandler.SetDebugMessage(this, "ExpertInkCanvas/ChildrenCount= " + this.Children.Count);
                    }
                }
                animationCurrentFrame += 1;
            }
            else
            {
                AnimationPlaying = false;
                ResetAnimation();
            }

        }

        /// <summary>
        /// resets the variables needed to restart the animation
        /// </summary>
        private void ResetAnimation()
        {
            try
            {
                    Children.RemoveRange(globalInitialChildCount, Children.Count); 
            }
            catch (Exception e)
            {
                DebugMessageHandler.SetDebugMessage(this, e.Message);
            }

            globalInitialChildCount = -1;
            animationCurrentFrame = 0;
            ChildEllipseHolder.Clear();
            ChildEllipsePointHolder.Clear();
            //_dispatchTimer.Stop();
        }


    }
    #endregion
}
