using CalligraphyTutor.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;

namespace CalligraphyTutor.StylusPlugins
{
    /// <summary>
    /// plugin that continiously checks for any hit of the pen with the expert stroke
    /// </summary>
    class HitStrokeTesterPlugin : StylusPlugIn
    {
        //public StrokeCollection ExpertStrokeCollection = new StrokeCollection();
        public StylusPointCollection ExpertStylusPointsCollection = new StylusPointCollection();
        StylusPoint ExpertStylusPoint = new StylusPoint();
        /// <summary>
        /// Strove deviation calculated base don the distance from the expert point
        /// </summary>
        private double StrokeDeviation = -0.01d;
        /// <summary>
        /// true if the pen hits the expert stroke
        /// </summary>
        private bool IsColliding = false;
        private bool PreviousIsColliding = false;
        private bool StrokeIsChecked = false;
        private bool ExpertStrokeLoaded = false;

        public HitStrokeTesterPlugin()
        {
            ExpertInkCanvas.ExpertStrokeLoadedEvent += ExpertInkCanvas_ExpertStrokeLoadedEvent;
            StudentInkCanvas.StrokeCheckedEvent += StudentInkCanvas_StrokeCheckedEvent;
        }


        #region eventsDefintion

        /// <summary>
        /// event that updates when the velocity is calculated
        /// </summary>
        public static event EventHandler<StudentDeviationCalculatedEventArgs> StudentDeviationCalculatedEvent;
        protected virtual void OnStudentDeviationCalculated(StudentDeviationCalculatedEventArgs e)
        {
            EventHandler<StudentDeviationCalculatedEventArgs> handler = StudentDeviationCalculatedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class StudentDeviationCalculatedEventArgs : EventArgs
        {
            public double deviation { get; set; }
        }
        /// <summary>
        /// event that updates when the velocity is calculated
        /// </summary>
        public static event EventHandler<NearestExpertStylusPointCalculatedEventArgs> NearestStylusPointCalculatedEvent;
        protected virtual void OnNearestExpertStylusPointCalculated(NearestExpertStylusPointCalculatedEventArgs e)
        {
            EventHandler<NearestExpertStylusPointCalculatedEventArgs> handler = NearestStylusPointCalculatedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class NearestExpertStylusPointCalculatedEventArgs : EventArgs
        {
            public StylusPoint styluspoint { get; set; }
        }
        /// <summary>
        /// event that updates when the velocity is calculated
        /// </summary>
        public static event EventHandler<HitStateChangedEventArgs> HitStateChangedEvent;
        protected virtual void OnHitStateChanged(HitStateChangedEventArgs e)
        {
            EventHandler<HitStateChangedEventArgs> handler = HitStateChangedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class HitStateChangedEventArgs : EventArgs
        {
            public bool state { get; set; }
            public Color color { get; set; }
        }
        /// <summary>
        /// event that updates when the <see cref="hitChangedPoints"/> need to be passed
        /// </summary>
        public static event EventHandler<HitChangePointsEventArgs> HitChangePointsEvent;
        protected virtual void OnHitChangePoints(HitChangePointsEventArgs e)
        {
            EventHandler<HitChangePointsEventArgs> handler = HitChangePointsEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class HitChangePointsEventArgs : EventArgs
        {
            public Dictionary<Stroke, Color> hitChangedPoints { get; set; }

        }
        #endregion

        #region eventHandlers

        private void StudentInkCanvas_StrokeCheckedEvent(object sender, StudentInkCanvas.StrokeCheckedEventArgs e)
        {
            StrokeIsChecked = e.state;
        }
        private void ExpertInkCanvas_ExpertStrokeLoadedEvent(object sender, ExpertInkCanvas.ExpertStrokeLoadedEventEventArgs e)
        {
            //ExpertStrokeCollection = e.strokes;
            foreach (Stroke s in e.strokes)
            {
                ExpertStylusPointsCollection.Add(s.StylusPoints);
            }
            ExpertStrokeLoaded = e.state;
        }
        #endregion

        #region overRides
        private Point prevPoint_hitPoints;
        private Point prevPoint_stylusPoints;
        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            prevPoint_hitPoints = new Point(double.NegativeInfinity, double.NegativeInfinity);
            prevPoint_stylusPoints = new Point(double.NegativeInfinity, double.NegativeInfinity);
            base.OnStylusDown(rawStylusInput);
        }
        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            //call the OnstylusMoveProcessed Method
            rawStylusInput.NotifyWhenProcessed(rawStylusInput.GetStylusPoints());
            base.OnStylusMove(rawStylusInput);
        }
        private Dictionary<Stroke, Color> hitChangedPoints = new Dictionary<Stroke, Color>();
        private bool firstDraw = true;
        private Color PreviousColor = Colors.Green;
        private Color CurrentColor = Colors.Green;
        protected override void OnStylusMoveProcessed(object callbackData, bool targetVerified)
        {
            // Check that the element actually receive the OnStylusUp input.
            if (targetVerified)
            {
                //collect the stylus point
                StylusPointCollection stylusPoints = callbackData as StylusPointCollection;
                //if there are no points in the collection
                if (stylusPoints == null)
                {
                    return;
                }
                //pass the points into a stroke
                Stroke tempStroke = new Stroke(stylusPoints);
                //return the nearest expert stylus point to be used as ref. Hit test is also performed with in this method 
                //IsColliding value is set with in the method and current color is also set
                if (ExpertStrokeLoaded)
                {
                    for (int i = 0; i < stylusPoints.Count; i++)
                    {
                        Point pt = (Point)stylusPoints[i];
                        Vector v = Point.Subtract(prevPoint_stylusPoints, pt);
                        if (v.Length > 4)
                        {

                            ExpertStylusPoint = SelectNearestExpertPoint(tempStroke, ExpertStylusPointsCollection);
                            //raise the event that the nearest expert point is selected
                            NearestExpertStylusPointCalculatedEventArgs args = new NearestExpertStylusPointCalculatedEventArgs();
                            args.styluspoint = ExpertStylusPoint;
                            OnNearestExpertStylusPointCalculated(args);
                            prevPoint_stylusPoints = pt;
                            break;
                        }
                    }
                }

                if(!StrokeIsChecked && ExpertStrokeLoaded)
                {
                    HitStateChangedEventArgs hitargs = new HitStateChangedEventArgs();
                    //if the stroke feedback is not requested return default values
                    hitargs.state = false;
                    hitargs.color = Colors.Green;
                    OnHitStateChanged(hitargs);
                    return;
                }
                if(firstDraw == true)
                {
                    firstDraw = false;
                    return;
                }
                //if it the first time the pen is dropped donot store the value regardless if the hit test changes or not
                if (firstDraw == false)
                {
                    //is colliding was set while selecting the nearest point
                    if (PreviousIsColliding != IsColliding)
                    {
                        //raise the hitstate changed event
                        HitStateChangedEventArgs hitargs = new HitStateChangedEventArgs();
                        hitargs.state = IsColliding;
                        hitargs.color = CurrentColor;
                        OnHitStateChanged(hitargs);
                        for (int i = 0; i < stylusPoints.Count; i++)
                        {
                            Point pt = (Point)stylusPoints[i];
                            Vector v = Point.Subtract(prevPoint_hitPoints, pt);
                            if (v.Length > 4)
                            {
                                if (!hitChangedPoints.Keys.Contains<Stroke>(tempStroke))
                                {
                                    hitChangedPoints.Add(tempStroke, PreviousColor);
                                }
                                prevPoint_hitPoints = pt;
                                break;
                            }
                        }

                        //assign the current values to the previous states
                        PreviousIsColliding = IsColliding;
                        PreviousColor = CurrentColor;
                    }

                }

            }
        }
        protected override void OnStylusUp(RawStylusInput rawStylusInput)
        {
            //add the last point of the stroke when the pen is lifted
            Stroke tempStroke = new Stroke(rawStylusInput.GetStylusPoints());
            //if the particular stroke is not already inserted into the dictionary, add them
            if (!hitChangedPoints.Keys.Contains<Stroke>(tempStroke))
            {
                hitChangedPoints.Add(tempStroke, PreviousColor);
            }
            //raise the event and pass the collection
            HitChangePointsEventArgs args = new HitChangePointsEventArgs();
            args.hitChangedPoints = hitChangedPoints;
            OnHitChangePoints(args);
            rawStylusInput.NotifyWhenProcessed(tempStroke.StylusPoints);
            base.OnStylusUp(rawStylusInput);
        }
        protected override void OnStylusUpProcessed(object callbackData, bool targetVerified)
        {
            //reset the values
            StrokeDeviation = -0.01d;
            hitChangedPoints = new Dictionary<Stroke, Color>();
            base.OnStylusUpProcessed(callbackData, targetVerified);
            
        }
        #endregion

        #region NativeMethods

        /// <summary>
        /// Method to calculate distance 
        /// </summary>
        /// <param name="startingPoint"></param>
        /// <param name="finalPoint"></param>
        /// <returns></returns>
        public double CalcualteDistance(Point startingPoint, Point finalPoint)
        {
            //double distance = Math.Sqrt(Math.Pow(Math.Abs(startingPoint.X - finalPoint.X), 2)
            //        + Math.Pow(Math.Abs(startingPoint.Y - finalPoint.Y), 2));
            double distance = Point.Subtract(startingPoint, finalPoint).Length;
            //distanceinmm = distance*(conversion factor from inch to mm)/parts per inch (which is the dot pitch)
            double distanceInMm = (distance / 267) * 25.4;
            return distanceInMm;
        }

        /// <summary>
        /// Method for returning the nearest styluspoint in the expert stroke from the point at which the pen is standing.
        /// </summary>
        /// <param name="s">Stroke formed from the event args styluspoint collection</param>
        /// <param name="SC"></param>
        /// <returns></returns>
        private StylusPoint SelectNearestExpertPoint(Stroke RenderedStroke, StylusPointCollection expertStylusPointCollection)
        {
            //set IsCOlliding to false, such that, if none of the points are intersected set IsColliding to false
            IsColliding = false;
            CurrentColor = Colors.Red;
            // assign the first stylus point in the expert stroke as the refstyluspoint
            StylusPoint refStylusPoint = expertStylusPointCollection.First();
            //get the bouding rect of the args stroke
            Rect rect = RenderedStroke.GetBounds();
            //get the center point of the arg stroke for calculating the distance
            Point centerPoint = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            //iterate through all the stylus point of expertstroke
            foreach (StylusPoint sp in expertStylusPointCollection)
            {
                //calculate the distance from the point to the pen
                double distance = CalcualteDistance(centerPoint, sp.ToPoint());
                //if it is the first time, assign value and continue
                if (StrokeDeviation < 0)
                {
                    StrokeDeviation = distance;
                    continue;
                }
                //check if the current point in the expertstroke hits one of the argsStroke
                if (RenderedStroke.HitTest(sp.ToPoint(), 2.5))
                {
                    //return the stylus point
                    refStylusPoint = sp;
                    //assing StrokeDeviation as 0
                    StrokeDeviation = 0;
                    //assign the color
                    CurrentColor = Colors.Green;
                    StudentDeviationCalculatedEventArgs args = new StudentDeviationCalculatedEventArgs();
                    args.deviation = StrokeDeviation;
                    OnStudentDeviationCalculated(args);
                    IsColliding = true;
                    //exit the wole loop
                    return refStylusPoint;
                }
                //if the point does not intersect the expert stroke, check if the new distance is smaller than the previous distance
                if (distance < StrokeDeviation)
                {
                    //assign the new shorter distance, and assign the new point as ref point
                    StrokeDeviation = distance;
                    StudentDeviationCalculatedEventArgs args = new StudentDeviationCalculatedEventArgs();
                    args.deviation = StrokeDeviation;
                    OnStudentDeviationCalculated(args);
                    refStylusPoint = sp;
                }
            }
            return refStylusPoint;
        }
        #endregion
    }
}
