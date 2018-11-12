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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CalligraphyTutor.Model
{
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

        public static DependencyProperty DisplayAnimationProperty = DependencyProperty.RegisterAttached(
            "DisplayAnimation", typeof(bool), typeof(ExpertInkCanvas), new PropertyMetadata());

        #endregion

        public ExpertInkCanvas()
        {
            expertDynamicRenderer = new ExpertDynamicRenderer();
            this.DynamicRenderer = expertDynamicRenderer;
            _dispatchTimer.Interval = new TimeSpan(10000);
            _dispatchTimer.Tick += _dispatchTimer_Tick;
            StudentInkCanvas.PenDownUpEvent += StudentInkCanvas_PenDownUpEvent;
        }

        #region EventHandlers
        private void StudentInkCanvas_PenDownUpEvent(object sender, EventArgs e)
        {
            ToggleDispatchTimer();
        }

        private void _dispatchTimer_Tick(object sender, EventArgs e)
        {
            if(DisplayAnimation == false)
            {
                return;
            }

            //if strokes is not empty and the user is not currently writing
            if (this.Strokes.Count > 0 && Globals.Instance.IsStylusDown == false)
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
            _dispatchTimer.Start();
            //if the stroke doesnt have stylus points
            if (e.NewStrokes.Count == 0)
            {
                return;
            }
            //imp not to use e.newStrokes as iterator as this is a editable collection and can change.
            StrokeCollection sc = e.NewStrokes;
            //temp holder for new modified stroke collection which is to be passed as the new set of strokes
            StrokeCollection scHolder = new StrokeCollection();
            foreach (Stroke s in sc)
            {
                Debug.WriteLine("before filtering "+s.StylusPoints.Count);
                scHolder.Add(FilterExpertData(s));
                Debug.WriteLine("after filtering " + scHolder[scHolder.Count-1].StylusPoints.Count);
            }
            e.NewStrokes.Clear();
            e.NewStrokes.Add(scHolder);
            // start the animation
            base.OnStrokesReplaced(e);
        }//OnStrokesReplaced

        private void ToggleDispatchTimer()
        {
            Globals.Instance.IsStylusDown = !Globals.Instance.IsStylusDown;
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
                    ellipse.Fill = new SolidColorBrush(Colors.Purple);
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
        /// Method for removing excess styluspoints from a expert stroke
        /// </summary>
        /// <param name="stroke"></param>
        private Stroke FilterExpertData(Stroke stroke)
        {
            //create a copy of stroke for iterating
            ExpertStrokes tempStroke = new ExpertStrokes(stroke.StylusPoints);
            for (int i = 0; i < tempStroke.StylusPoints.Count; i++)
            {
                Point pt = tempStroke.StylusPoints[i].ToPoint();
                Vector v = Point.Subtract(prevPoint, pt);

                if(v.Length < 2)
                {
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

        #endregion
    }
}
