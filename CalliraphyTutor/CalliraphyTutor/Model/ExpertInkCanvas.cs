using CalligraphyTutor.StylusPlugins;
using CalligraphyTutor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CalligraphyTutor.Model
{
    [Guid("12345678-9012-3456-7890-123456789013")]
    class ExpertInkCanvas: InkCanvas
    {
        #region Vars & Properties
        /// <summary>
        /// Timer for animation
        /// </summary>
        private DispatcherTimer _dispatchTimer = new DispatcherTimer();
        private ExpertDynamicRenderer expertDynamicRenderer;
        /// <summary>
        /// boolean that needs to be set for each object instance on wether the animation should run or not
        /// </summary>
        private bool displayAnimation = true;
        public bool DisplayAnimation
        {
            get { return displayAnimation; }
            set
            {
                displayAnimation = value;
            }
        }

        private bool _isStylusDown = false;
        public bool IsStylusDown
        {
            get { return _isStylusDown; }
            set
            {
                _isStylusDown = value;

            }
        }
        //public static DependencyProperty DisplayAnimationProperty = DependencyProperty.RegisterAttached(
        //    "DisplayAnimation", typeof(bool), typeof(ExpertInkCanvas), new PropertyMetadata());

        Guid expertTimestamp = new Guid("12345678-9012-3456-7890-123456789013");
        List<DateTime> StrokeTime = new List<DateTime>();

        LogStylusDataPlugin logStylusData = new LogStylusDataPlugin();

        #endregion
        #region Events definition
        /// <summary>
        /// Event raised when the pen is lifed or put down. declared it as static as the listening class would only listen to the object.
        /// thid event is subscribed by the expert inkcanvas and stops the animation
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
        }
        #endregion

        public ExpertInkCanvas()
        {
            expertDynamicRenderer = new ExpertDynamicRenderer();
            this.DynamicRenderer = expertDynamicRenderer;
            _dispatchTimer.Interval = new TimeSpan(10000);
            _dispatchTimer.Tick += _dispatchTimer_Tick;
            this.DefaultDrawingAttributes.IsHighlighter = true;
            this.DefaultDrawingAttributes.Color = Colors.Gray;
            this.DefaultDrawingAttributes.FitToCurve = false;
            this.DefaultDrawingAttributes.StylusTip = StylusTip.Ellipse;
            StudentInkCanvas.PenDownUpEvent += StudentInkCanvas_PenDownUpEvent;
            this.StylusPlugIns.Add(logStylusData);

        }

        private void StudentInkCanvas_PenDownUpEvent(object sender, StudentInkCanvas.PenDownUpEventEventArgs e)
        {
            IsStylusDown = e.state;
        }


        #region EventHandlers

        private void _dispatchTimer_Tick(object sender, EventArgs e)
        {
            if(DisplayAnimation == false)
            {
                return;
            }

            //if strokes is not empty and the user is not currently writing
            if (this.Strokes.Count > 0 && IsStylusDown == false)
            {
                StrokeCollection s = Strokes;
                //play the animation
                Application.Current.Dispatcher.InvokeAsync(new Action(
                    () =>
                    {
                        DisplayStrokePattern(s);
                    }));
            }
        }

        protected override void OnStrokesReplaced(InkCanvasStrokesReplacedEventArgs e)
        {
            if (DisplayAnimation)
            {
                _dispatchTimer.Start();
            }
            
            //if the stroke doesnt have stylus points
            if (e.NewStrokes.Count == 0)
            {
                return;
            }
            //if stroke is loaded from the file change its color to gray
            StrokeCollection sc = new StrokeCollection();
            foreach (Stroke s in e.NewStrokes)
            {
                s.DrawingAttributes.Color = Colors.Gray;
                sc.Add(s);
            }
            ExpertStrokeLoadedEventEventArgs args = new ExpertStrokeLoadedEventEventArgs();
            args.strokes = sc;
            OnExpertStrokeLoaded(args);
            
            // start the animation
            base.OnStrokesReplaced(e);
        }//OnStrokesReplaced

        protected override void OnStylusMove(StylusEventArgs e)
        {
            //add styluspoint from the event after checking to ensure that the collection doesnt already posses them
            StrokeTime.Add(DateTime.Now);
            base.OnStylusMove(e);
        }

        protected override void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            if (DisplayAnimation)
            {
                _dispatchTimer.Start();
            }
            this.Strokes.Remove(e.Stroke);
            if (e.Stroke.StylusPoints.Count > 2)
            {
                //Debug.WriteLine("after filtering " + e.Stroke.StylusPoints.Count);
                //ExpertStroke customStroke = FilterExpertData( new ExpertStroke(e.Stroke.StylusPoints));
                ExpertStroke customStroke = new ExpertStroke(e.Stroke.StylusPoints);
                //Debug.WriteLine("after filtering " + customStroke.StylusPoints.Count);
                customStroke.AddPropertyData(expertTimestamp, StrokeTime.ToArray());
                this.InkPresenter.Strokes.Add(customStroke);
            }
            StrokeTime = new List<DateTime>();
        }

        #endregion

        #region Native Methods

        private int animationCurrentFrame = 0;
        private int globalInitialChildCount = -1; // set -1 to run it once
        private List<Ellipse> ChildEllipseHolder = new List<Ellipse>();
        private List<Point> ChildEllipsePointHolder = new List<Point>();
        //is the number of dots to produce
        private int maxEllipses = 20;
        /// <summary>
        /// Indicates the way in which the pattern was drawn by means of tiny animation
        /// </summary>
        /// <param name="inkCanvas"></param>
        private void DisplayStrokePattern(StrokeCollection strokeCollection)
        {

            //if its the first run remember the default number of  children
            if (globalInitialChildCount < 0)
            {
                globalInitialChildCount = this.Children.Count;
                return;
            }

            //create a single large stroke collecting all the styluspoint collection
            StylusPointCollection spc = new StylusPointCollection();
            for (int i = 0; i < strokeCollection.Count; i++)
            {
                spc.Add(strokeCollection[i].StylusPoints);
            }
            Stroke s = new Stroke(spc);

            if (animationCurrentFrame < s.StylusPoints.Count + maxEllipses)
            {
                if (animationCurrentFrame < s.StylusPoints.Count - 1)
                {
                    Point p = s.StylusPoints[animationCurrentFrame].ToPoint();
                    ChildEllipsePointHolder.Add(p);
                    Ellipse ellipse = new Ellipse
                    {
                        Width =  10,
                        Height =  10
                    };
                    double left = p.X - (ellipse.Width / 2);
                    double top = p.Y - (ellipse.Height / 2);
                    Color EllipseColor = Color.FromArgb(180, 128, 0, 128);
                    ellipse.Fill = new SolidColorBrush(EllipseColor);
                    ellipse.Margin = new Thickness(left, top, 0, 0);
                    this.Children.Add(ellipse);
                    ChildEllipseHolder.Add(ellipse);

                }

                if (animationCurrentFrame >= maxEllipses)
                {
                    try
                    { 
                        //for making the following ellipse smaller.
                        for(int i = ChildEllipseHolder.Count-1; i > 0; i--)
                        {
                            ChildEllipseHolder[i].Width -= 0.25;
                            ChildEllipseHolder[i].Height -= 0.25;
                            ChildEllipseHolder[i].Margin = new Thickness(
                                ChildEllipsePointHolder[i].X- ChildEllipseHolder[i].Width/2,
                                ChildEllipsePointHolder[i].Y - ChildEllipseHolder[i].Height / 2,0,0);
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
                animationCurrentFrame = 0;
            }
            
        }

        Point prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
        /// <summary>
        /// Method for removing excess styluspoints from a expert stroke. has to be performed befored saving the guid not after loading, in which case guid is lost
        /// cannot write a method currently that automatically stores all guid
        /// </summary>
        /// <param name="stroke"></param>
        //private ExpertStroke FilterExpertData(Stroke stroke)
        //{
        //    //create a copy of stroke for iterating
        //    ExpertStroke tempStroke = new ExpertStroke(stroke.StylusPoints);
        //    for (int i = 0; i < tempStroke.StylusPoints.Count; i++)
        //    {
        //        Point pt = tempStroke.StylusPoints[i].ToPoint();
        //        Vector v = Point.Subtract(prevPoint, pt);

        //        if(v.Length < 2)
        //        {
        //            tempStroke.StylusPoints.RemoveAt(i);
        //            continue;
        //        }
        //        else
        //        {
        //            prevPoint = pt;
        //        }

        //    }
        //    return tempStroke;
        //}


        #endregion
    }
}
