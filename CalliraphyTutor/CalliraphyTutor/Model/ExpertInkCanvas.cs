﻿using CalligraphyTutor.ViewModel;
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

        private DispatcherTimer _dispatchTimer = new DispatcherTimer();
        Globals globals;

        //ExpertCanvasDynamicRenderer expertCustomRenderer;
        #endregion

        public ExpertInkCanvas(): base()
        {
            //expertCustomRenderer = new ExpertCanvasDynamicRenderer();
            //this.DynamicRenderer = expertCustomRenderer;

            globals = Globals.Instance;
            _dispatchTimer.Interval = new TimeSpan(10000);
            _dispatchTimer.Tick += _dispatchTimer_Tick;
            this.DefaultDrawingAttributes.Width = 5d;
            this.DefaultDrawingAttributes.Height = 5d;
            
        }

        #region Events
        
        private void _dispatchTimer_Tick(object sender, EventArgs e)
        {
            if (this.Strokes.Count > 0 && globals.IsStylusDown == false)
            {
                StrokeCollection s = Strokes;
                Application.Current.Dispatcher.InvokeAsync(new Action(
                    () =>
                    {
                        DisplayStrokePattern(s);
                    }));
            }
        }

        //reference StylusPointCollection used for adding new stroke on hit test.
        private StylusPointCollection _tempSPCollection = new StylusPointCollection();

        protected override void OnStrokesReplaced(InkCanvasStrokesReplacedEventArgs e)
        {
            _dispatchTimer.Start();
            if (e.NewStrokes.Count == 0)
            {
                return;
            }
            //imp not to use e.newStrokes as iterator as this is a editable collection and can change.
            StrokeCollection sc = e.NewStrokes;
            //temp holder for new stroke collection
            StrokeCollection scHolder = new StrokeCollection();
            foreach (Stroke s in sc)
            {
                scHolder.Add(PartitionStrokeDirection(s));
            }
            e.NewStrokes.Replace(e.NewStrokes, scHolder);
            // start the animation
            

            base.OnStrokesReplaced(e);
        }//OnStrokesReplaced

        public void ToggleDispatchTimer()
        {
            globals.IsStylusDown = !globals.IsStylusDown;
        }
        #endregion

        #region Native Methods
        /// <summary>
        /// Checks if the current stylus points collides with the expertStroke
        /// </summary>
        /// <param name="expertStroke"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool HitTestwithExpertStroke(Stroke expertStroke, StylusPoint sp)
        {
            double threshold = 25d;
            //Point p = e.GetPosition(this); get the position of the stylus
            return expertStroke.HitTest(sp.ToPoint(), threshold);
        }

        private Point prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
        
        /// <summary>
        /// partitions the stroke into smallers strokes to store them based on the first non zero vector. 
        /// this vector is compared to another vector generated by the end points until it reaches more than 90
        /// </summary>
        /// <param name="stroke">The original expert stroke to be divided</param>
        private StrokeCollection PartitionStrokeVector(Stroke stroke)
        {
            int nextPoint = 0;
            double minLengthOfSegment = 20d;
            double tempSegmentLength = 0d;
            Vector initVector = new Vector(0,0);
            Vector secVector = new Vector(0, 0);
            StylusPointCollection tempStylusPointCollection = new StylusPointCollection();
            StrokeCollection tempStrokeCollection = new StrokeCollection();

            //start from the consequtive stylus point until all the points are finished
            for (int i = 1; i <= stroke.StylusPoints.Count - 1; i++)
            {
                tempStylusPointCollection.Add(stroke.StylusPoints[i]);
                //calculate the distance between the 2 points
                tempSegmentLength += Math.Sqrt((Math.Pow(stroke.StylusPoints[i - 1].X - stroke.StylusPoints[i].X, 2) +
                    Math.Pow(stroke.StylusPoints[i - 1].Y - stroke.StylusPoints[i].Y, 2)));
                if(tempSegmentLength >= minLengthOfSegment)
                {
                    secVector = Point.Subtract(stroke.StylusPoints[nextPoint].ToPoint(), stroke.StylusPoints[i].ToPoint());
                    tempSegmentLength = 0;
                    nextPoint = i;
                    if (initVector.Equals(new Vector(0, 0)))
                    { 
                        initVector = secVector;
                        nextPoint = i;
                        continue;
                    }

                    //derive the angle in degrees
                    double angleBetweenVectors = Vector.AngleBetween(secVector, initVector);
                    if (Math.Abs(angleBetweenVectors) > 45)
                    {
                        Debug.WriteLine("Angle Between the Vectors: " + angleBetweenVectors);
                        //initVector = secVector;
                        if (i <= stroke.StylusPoints.Count - 1)
                        {
                            tempStylusPointCollection.Add(stroke.StylusPoints[i + 1]);
                        }

                        if (tempStylusPointCollection.Count != 0)
                        {
                            Color c;
                            if (tempStrokeCollection.Count % 2 > 0)
                            {
                                 c = Colors.Blue;
                            }
                            else
                            {
                                c = Colors.Red;
                            }
                            ExpertCanvasStroke s = new ExpertCanvasStroke(tempStylusPointCollection, c);
                            tempStrokeCollection.Add(s);
                            tempStylusPointCollection = new StylusPointCollection();
                            initVector = secVector;
                        }

                    }
                    
                }
                if (i >= stroke.StylusPoints.Count - 1 && tempStylusPointCollection.Count != 0)
                {
                    if(tempStylusPointCollection.Count != 0)
                    {
                        ExpertCanvasStroke s = new ExpertCanvasStroke(tempStylusPointCollection, Colors.Red);
                        tempStrokeCollection.Add(s);
                        tempStylusPointCollection = new StylusPointCollection();

                    }

                }
            }
            Debug.WriteLine("TempStrokeCollection Count: "+ tempStrokeCollection.Count);
            return tempStrokeCollection;
        }

        enum Direction { right, left, up, down, intialDirection};
        int strokeLoadCount = 0;
        /// <summary>
        /// partitions the stroke into smallers strokes to store them based on the direction.
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        private StrokeCollection PartitionStrokeDirection(Stroke stroke)
        {
            Debug.WriteLine("Stroke load count: " + strokeLoadCount);
            strokeLoadCount += 1;
            double minSegmentLength = 20d;
            //distance between each SP needs to be added for total linear distance
            double tempSegmentLength = 0;
            Direction directionOfSegment;
            Direction previousDirection = Direction.intialDirection;
            StrokeCollection tempStrokeCollection = new StrokeCollection();
            StylusPointCollection tempStylusPointCollection = new StylusPointCollection();
            //determine the first temp segment for calculating the distance
            for (int i = 1; i <= stroke.StylusPoints.Count-1; i++)
            {
                tempStylusPointCollection.Add(stroke.StylusPoints[i-1]);
                tempSegmentLength += Math.Sqrt((Math.Pow(stroke.StylusPoints[i-1].X - stroke.StylusPoints[i].X, 2) +
                    Math.Pow(stroke.StylusPoints[i-1].Y - stroke.StylusPoints[i].Y, 2)));
                if (tempSegmentLength >= minSegmentLength)
                {
                    tempSegmentLength = 0;
                    if (System.Math.Abs(stroke.StylusPoints[i-1].X - stroke.StylusPoints[i].X) > 
                        System.Math.Abs(stroke.StylusPoints[i-1].Y - stroke.StylusPoints[i].Y))
                    {
                        // change in x is greater, now find left or right
                        if ((stroke.StylusPoints[i-1].X - stroke.StylusPoints[i].X) < 0)
                        {
                            directionOfSegment = Direction.right;
                        }
                        else
                        {
                            directionOfSegment = Direction.left;
                        }
                    }
                    else
                    {
                        // change in y is greater, now find up or down
                        if ((stroke.StylusPoints[i-1].Y - stroke.StylusPoints[i].Y) < 0)
                        {
                            directionOfSegment = Direction.down;
                        }
                        else
                        {
                            directionOfSegment = Direction.up;
                        }
                    }
                    //if its the first time direction is assigned.
                    if (previousDirection.Equals(Direction.intialDirection))
                    {
                        previousDirection = directionOfSegment;
                        continue;
                    }

                    if(directionOfSegment != previousDirection)
                    {
                        Debug.WriteLine("Previous Direction: " + previousDirection + " , SP index: "+ i);
                        if (tempStylusPointCollection.Count > 1)
                        {
                            if (i < stroke.StylusPoints.Count - 1)
                            {
                                tempStylusPointCollection.Add(stroke.StylusPoints[i]);
                            }
                            if (tempStylusPointCollection.Count != 0)
                            {
                                Color c = AssignColor(previousDirection);
                                
                                ExpertCanvasStroke s = new ExpertCanvasStroke(tempStylusPointCollection, c);
                                tempStrokeCollection.Add(s);
                                tempStylusPointCollection = new StylusPointCollection();
                                previousDirection = directionOfSegment;
                            }
                        }

                    }

                }

                if(i >= stroke.StylusPoints.Count -1)
                {
                    if(tempStylusPointCollection.Count != 0)
                    {
                        Color c = AssignColor(previousDirection);
                        ExpertCanvasStroke s = new ExpertCanvasStroke(tempStylusPointCollection, c);
                        tempStrokeCollection.Add(s);
                        tempStylusPointCollection = new StylusPointCollection();

                    }

                }
                    
            }

            return tempStrokeCollection;

        }

        private Color AssignColor(Direction d)
        {
            //default is less than 90
            Color c = Colors.Black;
            if (d == Direction.down)
            {
                return c = Colors.Black;
            }
            if (d == Direction.left)
            {
                return c = Colors.Blue;
            }
            if (d  == Direction.up)
            {
                return c = Colors.Red;
            }
            if (d == Direction.right)
            {
                return c = Colors.Green;
            }

            return c;
        }

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

        #endregion
    }
}
