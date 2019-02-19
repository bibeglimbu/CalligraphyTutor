using CalligraphyTutor.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;

namespace CalligraphyTutor.Model
{
    class StudentDynamicRenderer: DynamicRenderer
    {
        #region Vars & properties
        //nullable bool true if the students stroke is hitting the expert Stroke
        private bool IsColliding=false;

        public StrokeCollection ExpertStrokeCollection = new StrokeCollection();
        public StylusPointCollection ExpertStylusPointsCollection = new StylusPointCollection();
        StylusPoint ExpertStylusPoint = new StylusPoint();

        bool IsPressureChecked = false;
        bool IsStrokeChecked = false;
        bool IsSpeedChecked = false;

        /// <summary>
        /// Strove deviation calculated base don the distance from the expert point
        /// </summary>
        private double StrokeDeviation = -0.01d;
        private float expertPressure = 0;
        /// <summary>
        /// Value that holds if the pressure applied is higher or lower that the experts. Must return either between 0 & 1
        /// </summary>
        public float ExpertPressure
        {

            get { return expertPressure; }
            set
            {
                expertPressure = value;
                //if (PressureChecked)
                //{
                //    PressureChangedEventArgs args = new PressureChangedEventArgs();
                //    args.pressurefactor = expertPressure;
                //    OnExpertBasedPressureChanged(args);
                //}

            }
        }
        #endregion

        public StudentDynamicRenderer()
        {
            StudentInkCanvas.PressureCheckedEvent += StudentInkCanvas_PressureCheckedEvent;
            StudentInkCanvas.StrokeCheckedEvent += StudentInkCanvas_StrokeCheckedEvent;
            StudentInkCanvas.SpeedCheckedEvent += StudentInkCanvas_SpeedCheckedEvent;
            ExpertInkCanvas.ExpertStrokeLoadedEvent += ExpertInkCanvas_ExpertStrokeLoadedEvent;
        }

        #region eventsDefintion
        /// <summary>
        /// event that updates when the velocity is calculated
        /// </summary>
        public static event EventHandler<ExpertVelocityCalculatedEventArgs> ExpertVelocityCalculatedEvent;
        protected virtual void OnExpertVelocityCalculated(ExpertVelocityCalculatedEventArgs e)
        {
            EventHandler<ExpertVelocityCalculatedEventArgs> handler = ExpertVelocityCalculatedEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class ExpertVelocityCalculatedEventArgs : EventArgs
        {
            public double velocity { get; set; }
        }

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
        #endregion

        private void StudentInkCanvas_SpeedCheckedEvent(object sender, StudentInkCanvas.SpeedCheckedEventArgs e)
        {
            IsSpeedChecked = e.state;
        }

        private void StudentInkCanvas_StrokeCheckedEvent(object sender, StudentInkCanvas.StrokeCheckedEventArgs e)
        {
            IsStrokeChecked = e.state;
        }

        private void ExpertInkCanvas_ExpertStrokeLoadedEvent(object sender, ExpertInkCanvas.ExpertStrokeLoadedEventEventArgs e)
        {
            ExpertStrokeCollection = e.strokes;
            foreach(Stroke s in e.strokes)
            {
                ExpertStylusPointsCollection.Add(s.StylusPoints);
            }
        }

        private void StudentInkCanvas_PressureCheckedEvent(object sender, StudentInkCanvas.PressureCheckedEventArgs e)
        {
            IsPressureChecked = e.state;
        }

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            // Run the base class before modifying the data
            base.OnStylusMove(rawStylusInput);
        }

        private bool PreStrokeHitState = false;
        public Dictionary<Stroke, Color> hitChangedPoints = new Dictionary<Stroke, Color>();
        protected override void OnStylusUp(RawStylusInput rawStylusInput)
        {
            Debug.WriteLine("hitchangedpoint"+hitChangedPoints.Count);
            StrokeDeviation = -0.01d;
            base.OnStylusUp(rawStylusInput);
        }

        Color StrokeColor = Colors.Green;
        bool firstDraw = true;
        protected override void OnDraw(DrawingContext drawingContext, StylusPointCollection stylusPoints,
                                       Geometry geometry, Brush fillBrush)
        {
                    Stroke tempStroke = new Stroke(stylusPoints);
                    //if the expert stroke is loaded
                    if (ExpertStrokeCollection.Count != 0)
                    {
                        //get the nearest stroke
                        Stroke expertStroke = SelectNearestExpertStroke(tempStroke, ExpertStrokeCollection);
                        if (expertStroke == null)
                        {
                            Debug.WriteLine("expertstroke is null");
                            return;
                        }
                        //give feedback on speed
                        if (IsSpeedChecked == true && ExpertStylusPointsCollection != null)
                        {
                            //add a value to the expert average velocity to provide a area of error
                            double ExpertVelocity = CalculateAverageExpertStrokeVelocity(expertStroke) + 5;
                            ExpertVelocityCalculatedEventArgs args = new ExpertVelocityCalculatedEventArgs();
                            args.velocity = ExpertVelocity;
                            OnExpertVelocityCalculated(args);
                        }
                        //return the nearest expert stylus point to be used as ref. Hit test is also performed with in this method.
                        ExpertStylusPoint = SelectExpertPoint(tempStroke, expertStroke);
                        if (ExpertStylusPoint == null)
                        {
                            Debug.WriteLine("expertstylusPoint is null");
                            return;
                        }
                    }
                    //it is crucial to use the ExpertStylusPointsCollection collection or else the hit test are missed
                    foreach (StylusPoint sp in ExpertStylusPointsCollection)
                    {
                        //if the the stroke hits any of the expert stylus point
                        if (tempStroke.HitTest(sp.ToPoint(), 5))
                        {
                            IsColliding = true;
                            break;
                        }
                        //if it didnt hit any and it is the last point point assign 
                        if (sp == ExpertStylusPointsCollection.Last())
                        {
                            IsColliding = false;
                        }
                    }

                    //if it the first time the pen is dropped donot store the value regardless if the hit test changes or not
                    if (firstDraw == false)
                    {
                        //ensure that the last stroke is always stored and removed in the next iteration
                        if (hitChangedPoints.Count > 0)
                        {
                            hitChangedPoints.Remove(hitChangedPoints.Keys.Last());
                        }
                        //if the state has changed add the point
                        if (IsColliding != PreStrokeHitState)
                        {
                            if (!hitChangedPoints.Keys.Contains<Stroke>(new Stroke(stylusPoints)))
                            {
                                hitChangedPoints.Add(new Stroke(stylusPoints), StrokeColor);
                            }
                            PreStrokeHitState = IsColliding;
                        }
                        //add the recent stroke as point
                        hitChangedPoints.Add(new Stroke(stylusPoints), StrokeColor);
                    }
                    else
                    {
                        firstDraw = false;
                    }


                    if (IsStrokeChecked == true && ExpertStylusPointsCollection != null)
                    {
                        if (IsColliding == true)
                        {
                            StrokeColor = Colors.Green;
                        }

                        if (IsColliding == false)
                        {
                            StrokeColor = Colors.Red;
                        }
                    }
                    Color BrushStrokeColor = Colors.Green;
                    //if pressure feedback is requested
                    if (IsPressureChecked == true && ExpertStylusPointsCollection != null)
                    {
                        List<float> pressureFactorList = new List<float>();
                        foreach (StylusPoint s in stylusPoints)
                        {
                            pressureFactorList.Add(s.PressureFactor);
                        }
                        float StudentPressureFactor = pressureFactorList.Average();
                        //multiplier to darken or lighen the color
                        //StrokeColor = Color.FromArgb(Convert.ToByte(255 * stylusPoints[stylusPoints.Count / 2].PressureFactor), StrokeColor.R, StrokeColor.G, StrokeColor.B);
                        BrushStrokeColor = ChangeColorBrightness(StrokeColor, ExpertStylusPoint.PressureFactor, StudentPressureFactor);
                //send vibration information to myo
                    }
                    fillBrush = new SolidColorBrush(BrushStrokeColor);
                    base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);
               
        }

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            this.DrawingAttributes.Width = Globals.Instance.StrokeWidth;
            this.DrawingAttributes.Height = Globals.Instance.StrokeHeight;
            base.OnStylusDown(rawStylusInput);
        }

        private float ColorWeight = 0f;
        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        public Color ChangeColorBrightness(Color color, float ExpertPressureFactor, float StudentPressureFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;
            float pressure = StudentPressureFactor;

            //if pressure is higher than the expert + threshold, 
            if (ExpertPressureFactor + 0.1 < StudentPressureFactor)
            {
                ColorWeight = 1-(StudentPressureFactor - ExpertPressureFactor);
                red *= ColorWeight;
                green *= ColorWeight;
                blue *= ColorWeight;
                //Debug.WriteLine("Pressure high : "+ ColorWeight);
            }
            else 
            //if the pressure is lower than the expert + threshold, 
            if (ExpertPressureFactor - 0.1 > StudentPressureFactor)
            {
                ColorWeight = 1 + ( ExpertPressureFactor - StudentPressureFactor);
                red = (255 - red) * ColorWeight + red;
                green = (255 - green) * ColorWeight + green;
                blue = (255 - blue) * ColorWeight + blue;
                //Debug.WriteLine("Pressure low : " + ColorWeight);
            }
            //if the pressure is with in the range
            else
            {
                red = (float)color.R;
                green = (float)color.G;
                blue = (float)color.B;
                //Debug.WriteLine("Pressure in range");
            }

            return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
        }

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
        private StylusPoint SelectExpertPoint(Stroke argsStroke, Stroke expertStroke)
        {

            // assign the first stylus point in the expert stroke as the refstyluspoint
            StylusPoint refStylusPoint = expertStroke.StylusPoints[0];
            //get the bouding rect of the args stroke
            Rect rect = argsStroke.GetBounds();
            //get the center point of the arg stroke for calculating the distance
            Point centerPoint = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            //iterate through all the stylus point of expertstroke
            foreach (StylusPoint sp in expertStroke.StylusPoints)
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
                if (argsStroke.HitTest(sp.ToPoint(), 2.5))
                {
                    //return the stylus point
                    refStylusPoint = sp;
                    //assing StrokeDeviation as 0
                    StrokeDeviation = 0;
                    StudentDeviationCalculatedEventArgs args = new StudentDeviationCalculatedEventArgs();
                    args.deviation = StrokeDeviation;
                    OnStudentDeviationCalculated(args);
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
        private Stroke SelectNearestExpertStroke(Stroke argsStroke, StrokeCollection expertSC)
        {
            //assign the first stroke to the temp stroke holder
            Stroke stroke = expertSC[0];
            //get the bouding rect of the args stroke
            Rect rect = argsStroke.GetBounds();
            //get the center point of the arg stroke for calculating the distance
            Point centerPoint = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            //distance between the args stroke and the current expert stroke
            double tempStrokeDistance = -0.1d;
            //iterate through each stroke to find the nearest stroke
            foreach (Stroke es in expertSC)
            {
                //bound each expert stroke with a rectangle, get the center position and calculate the distance between the 2 points
                double distance = CalcualteDistance(centerPoint,
                    new Point(es.GetBounds().Left + es.GetBounds().Width / 2, es.GetBounds().Top + es.GetBounds().Height / 2));
                //if it is the first time running
                if (tempStrokeDistance < 0)
                {
                    //assign the values and continue
                    tempStrokeDistance = distance;
                    stroke = es;
                    continue;
                }
                //if it is not the first time running and the tempDistance is smaller than the tempStrokeDistance
                if (distance < tempStrokeDistance)
                {
                    //assign the smallest distance and the stroke that gave that valie
                    tempStrokeDistance = distance;
                    stroke = es;
                }
            }
            return stroke;
        }

        /// <summary>
        /// calculates the velocity of the stroke in seconds for expert stroke since the expert stroke could not be used in collection
        /// </summary>
        public double CalculateAverageExpertStrokeVelocity(Stroke s)
        {
            GuidAttribute IMyInterfaceAttribute = (GuidAttribute)Attribute.GetCustomAttribute(typeof(ExpertInkCanvas), typeof(GuidAttribute));
            //Debug.WriteLine("IMyInterface Attribute: " + IMyInterfaceAttribute.Value);
            Guid expertTimestamp = new Guid(IMyInterfaceAttribute.Value);

            //Debug.WriteLine("guids " + argsStroke.GetPropertyDataIds().Length);
            double totalStrokeLenght = 0;
            double velocity = 0;
            for (int i = 0; i < s.StylusPoints.Count - 1; i++)
            {
                //add all the distance between each stylus points in the stroke
                totalStrokeLenght += CalcualteDistance(s.StylusPoints[i].ToPoint(), s.StylusPoints[i + 1].ToPoint());
            }

            List<DateTime> timeStamps = new List<DateTime>();
            if (s.ContainsPropertyData(expertTimestamp))
            {
                object data = s.GetPropertyData(expertTimestamp);
                foreach (DateTime dt in (Array)data)
                {
                    timeStamps.Add(dt);
                }

                velocity = totalStrokeLenght / (timeStamps.Last() - timeStamps.First()).TotalSeconds;
            }
            return velocity;

        }
    }
}
