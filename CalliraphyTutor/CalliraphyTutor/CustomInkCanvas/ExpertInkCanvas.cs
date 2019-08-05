
using CalligraphyTutor.CustomStroke;
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
        #region Variables
        /// <summary>
        /// Timer for animation
        /// </summary>
        private DispatcherTimer _dispatchTimer = new DispatcherTimer();
        /// <summary>
        /// Stores the time taken to create the stroke
        /// </summary>
        Guid ExpertTimestamp = new Guid("12345678-9012-3456-7890-123456789E13");
        Guid ExpertVelocity = new Guid("12345678-9012-3456-7890-123456789E12");
        List<DateTime> StrokeTime = new List<DateTime>();
        /// <summary>
        /// true when the animation is still playing
        /// </summary>
        public bool AnimationPlaying = false;
        /// <summary>
        /// holds the total length of the stroke
        /// </summary>
        private double strokeLength = 0.0f;
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

            StudentInkCanvas.PenDownUpEvent += StudentInkCanvas_PenDownUpEvent;
            StudentInkCanvas.PenInRangeEvent += StudentInkCanvas_PenInRangeEvent;
        }



        #region EventHandlers
        private void StudentInkCanvas_PenInRangeEvent(object sender, StudentInkCanvas.PenInRangeEventEventArgs e)
        {
            //start the animation when this event is detected
            if (AnimationPlaying == false)
            {
                ref_Stylus_Point = e.Stylus_Point;
                //set the IsStylusInrange to true
                _dispatchTimer.Start();
            }
        }

        private void StudentInkCanvas_PenDownUpEvent(object sender, StudentInkCanvas.PenDownUpEventEventArgs e)
        {
            IsStylusDown = e.IsPenDown;
            //reset the animation variables
            ResetAnimation();
            //set the animation playing to false as the thread can be terminsated in the middle and it can still be true even when no animation is running
            AnimationPlaying = false;
        }

        /// <summary>
        /// Reference point to update the collection of animation for animation. passes the point of the top of the pen
        /// </summary>
        public Point ref_Stylus_Point;
        private void _dispatchTimer_Tick(object sender, EventArgs e)
        {
            //if the user has not requested any animation
            if(DisplayAnimation == false)
            {
                return;
            }
            //return if the IsStylus is down which stops the animation immediately as we donot want to distract the write
            if(IsStylusDown == true)
            {
                return;
            }
            //reutn if there is no refernce point
            if(ref_Stylus_Point==null)
            {
                return;
            }
            StrokeCollection sc = base.ReturnNearestExpertStrokes(ref_Stylus_Point);
            //if no points were returned
            if (sc.Count == 0)
                {
                    return;
                }
            //play the animation
            //GetTimestamp(sc.Last());
            Application.Current.Dispatcher.InvokeAsync(new Action( () =>
            {
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
            //reset the animation variables
            ResetAnimation();
            //set the animation playing to false as the thread can be terminsated in the middle and it can still be true even when no animation is running
            AnimationPlaying = false;
            //add styluspoint from the event after checking to ensure that the collection doesnt already posses them
            StrokeTime.Add(DateTime.Now);
            base.OnStylusDown(e);
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            //add to the strokelength holder to calculate the final velocity
            CalculateStrokeLength(new Stroke(e.GetStylusPoints((InkCanvas)e.Source)));
            base.OnStylusMove(e);
        }

        protected override void OnStylusInAirMove(StylusEventArgs e)
        {
            //update the ref_Stylus_Point if Animation has finished playing
            if (AnimationPlaying == false)
            {
                ref_Stylus_Point = e.GetPosition((InkCanvas)e.Source);
            }
            base.OnStylusInAirMove(e);
        }

        protected override void OnStylusInRange(StylusEventArgs e)
        {
            //If the stylus has left the digitizer and returned and no animation is being played
            if (AnimationPlaying == false)
            {
                ref_Stylus_Point = e.GetPosition((InkCanvas)e.Source);
                //set the IsStylusInrange to true
                _dispatchTimer.Start();
            }
            //Debug.WriteLine("InRange: displayAnimatin +" + DisplayAnimation.ToString());
            base.OnStylusInRange(e);
        }

        protected override void OnStylusOutOfRange(StylusEventArgs e)
        {
            
            //if the stylus is out of range and no animation is playing currently
            if (AnimationPlaying == false)
            {
                //set StylusInRange to false
                ResetAnimation();
            }

            //Debug.WriteLine("OutOfRange: displayAnimatin +" + DisplayAnimation.ToString());
            base.OnStylusLeave(e);
        }

        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            //add styluspoint from the event after checking to ensure that the collection doesnt already posses them
            StrokeTime.Add(DateTime.Now);
            this.Strokes.Remove(e.Stroke);
            if (e.Stroke.StylusPoints.Count > 2)
            {
                //create a custom Stroke
                Stroke customStroke = new Stroke(e.Stroke.StylusPoints);
                //attach the time data
                customStroke.AddPropertyData(ExpertTimestamp, StrokeTime.ToArray());
                double expertVelocity = CalculateExpertVelocity();
                customStroke.AddPropertyData(ExpertVelocity, expertVelocity);
                this.Strokes.Add(customStroke);
            }
            StrokeTime = new List<DateTime>();
            strokeLength = 0.0f;
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
        /// <param name="inkCanvas"></param>
        private void DisplayStrokesPatterns(StrokeCollection sc)
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
            StylusPointCollection spc = new StylusPointCollection();
            for (int i = 0; i < sc.Count; i++)
            {
                spc.Add(sc[i].StylusPoints);
            }
            Stroke s = new Stroke(spc);
            //if the animation frame of animation is less than the count of stylus points and the number of ellipses
            if (animationCurrentFrame < s.StylusPoints.Count + maxEllipses)
            {
                //if the animation frame is less than the number of children
                if (animationCurrentFrame < s.StylusPoints.Count - 1)
                {
                    //get the styluspoint location according to the frame
                    Point p = s.StylusPoints[animationCurrentFrame].ToPoint();
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
                        Debug.WriteLine("ExpertInkCanvas/ChildrenCount= " + this.Children.Count);
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
            _dispatchTimer.Stop();
        }

        /// <summary>
        /// Extracts the datatime GUID from the stroke. Currently bugs the GUI thread and should only be used when crucial
        /// </summary>
        /// <param name="s"></param>
        private void GetTimestamp(Stroke s)
        {
                if (s.ContainsPropertyData(ExpertTimestamp))
                {
                    object date = s.GetPropertyData(ExpertTimestamp);
                    object velocity = s.GetPropertyData(ExpertVelocity);

                    if (date is DateTime[])
                    {
                        DebugMessageHandler.SetDebugMessage(this, "StrokeTime: " + ((DateTime[])date)[1].ToString());
                    }

                    if (velocity is double)
                    {
                        DebugMessageHandler.SetDebugMessage(this, "StrokeVelocity: " + velocity.ToString());
                    }

                }
                else
                {
                    DebugMessageHandler.SetDebugMessage(this, "The StrokeCollection does not have a timestamp.");
                }  
        }

        /// <summary>
        /// Adds the total lenght of the stroke
        /// </summary>
        /// <param name="s"></param>
        private async void CalculateStrokeLength(Stroke s)
        {
            await Task.Run(() => {
                for(int i=0; i<s.StylusPoints.Count-1; i++)
                {
                    strokeLength += CalcualteDistance(s.StylusPoints[i].ToPoint(), 
                        s.StylusPoints[i+1].ToPoint());
                }
            });
        }

        /// <summary>
        /// Returns expert velocity in seconds
        /// </summary>
        /// <returns></returns>
        private double CalculateExpertVelocity()
        {
            double timeTaken = (StrokeTime.Last() - StrokeTime.First()).TotalSeconds;
            double velocity = strokeLength / timeTaken;
            return velocity;
        }
    }
    #endregion
}
