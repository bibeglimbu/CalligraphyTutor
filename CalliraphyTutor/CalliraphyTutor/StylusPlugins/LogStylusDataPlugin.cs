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

namespace CalligraphyTutor.StylusPlugins
{
    // EventArgs for the StrokeRendered event.
    public class StylusMoveProcessEndedEventArgs : EventArgs
    {
        public Stroke StrokeRef { get; set; }
        public float Pressure { get; set; }
        public float XTilt { get; set; }
        public float YTilt { get; set; }
        public double StrokeVelocity { get; set; }
    }

    class LogStylusDataPlugin : StylusPlugIn
    {
        #region eventsDefintion
        public static event EventHandler<StylusMoveProcessEndedEventArgs> StylusMoveProcessEnded;
        protected virtual void OnStylusMoveProcessEnded(StylusMoveProcessEndedEventArgs e)
        {
            EventHandler<StylusMoveProcessEndedEventArgs> handler = StylusMoveProcessEnded;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        #endregion

        #region OverRides
        /// <summary>
        /// starting point of the whole stroke
        /// </summary>
        Point initStrokeStartPoint;
        /// <summary>
        /// Value used to stroke the initial time for calculating velocity
        /// </summary>
        private DateTime initStrokeStartTime;

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            base.OnStylusDown(rawStylusInput);
            //store the init point and time
            Stroke s = new Stroke(rawStylusInput.GetStylusPoints());
            Rect rect = s.GetBounds();
            initStrokeStartPoint = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            //store the initial time
            initStrokeStartTime = DateTime.Now;
            //Debug.WriteLine(rawStylusInput.StylusDeviceId);
        }

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            // Call the base class before modifying the data.
            base.OnStylusMove(rawStylusInput);
            StylusPointCollection spc = new StylusPointCollection(rawStylusInput.GetStylusPoints());
            //spCollection.Add(spc);
            rawStylusInput.NotifyWhenProcessed(spc);
            
        }
        // This method is called on the application thread.
        protected override void OnStylusMoveProcessed(object callbackData, bool targetVerified)
        {
            // Check that the element actually receive the OnStylusUp input.
            if (targetVerified)
            {
                StylusPointCollection strokePoints = callbackData as StylusPointCollection;

                //if there are no points in the collection
                if (strokePoints == null)
                {
                    return;
                }
                //declare an array and store all the pressure values in the array
                float[] pressure = new float[strokePoints.Count];
                for (int i = 0; i < strokePoints.Count; i++)
                {
                    pressure[i] = strokePoints[i].GetPropertyValue(StylusPointProperties.NormalPressure);
                }
                //assign a midrange pressure temporarily to pass as the event args
                float tempPressure = (pressure.Max() + pressure.Min()) / 2;

                //Xtilt
                float[] xtilt = new float[strokePoints.Count];
                for (int i = 0; i < strokePoints.Count; i++)
                {
                    try
                    {
                        xtilt[i] = strokePoints[i].GetPropertyValue(StylusPointProperties.XTiltOrientation);
                    }
                    catch (Exception e) { }
                    
                }
                float tempXtilt = (xtilt.Max() + xtilt.Min()) / 2;
                //Ytilt
                float[] ytilt = new float[strokePoints.Count];
                for (int i = 0; i < strokePoints.Count; i++)
                {
                    try
                    {
                        ytilt[i] = strokePoints[i].GetPropertyValue(StylusPointProperties.YTiltOrientation);
                    }
                    catch (Exception e) { }
                    
                }
                float tempYtilt = (ytilt.Max() + ytilt.Min()) / 2;

                //assign strokeVelocity
                StrokeVelocity = CalculateStudentStrokeVelocity(strokePoints);

                Stroke tempStroke = new Stroke(strokePoints);

                StylusMoveProcessEndedEventArgs args = new StylusMoveProcessEndedEventArgs();
                args.StrokeRef = tempStroke;
                args.Pressure = tempPressure;
                args.XTilt = tempXtilt;
                args.YTilt = tempYtilt;
                args.StrokeVelocity = StrokeVelocity;

                OnStylusMoveProcessEnded(args);

            }
        }

        /// <summary>
        /// holds the total distance from pen down to pen up;
        /// </summary>
        double totalDistance = 0.0d;
        /// <summary>
        /// velocity at which the stroke is being drawn
        /// </summary>
        double StrokeVelocity = 0.0d;
        protected override void OnStylusUp(RawStylusInput rawStylusInput)
        {
            base.OnStylusUp(rawStylusInput);
            totalDistance = 0;
            StrokeVelocity = 0;
        }

        #endregion

        #region Native Methods
        /// <summary>
        /// calculates the velocity of the stroke in Seconds. ensure that the distance between the 2 points is not 0 when calling this method.
        /// </summary>
        public double CalculateStudentStrokeVelocity(StylusPointCollection e)
        {
            //add the distance covered so far
            totalDistance += CalcualteTotalDistance(initStrokeStartPoint,e);
            //calcualte the time covered so far
            double timespan = (DateTime.Now - initStrokeStartTime).TotalSeconds;
            //Debug.Write("Distance; " + distance + " "+ "Timespan :" + timespan);
            double velocity = 0;
            //if distance or time is o return 0
            if (timespan > 0)
            {
                velocity = totalDistance / timespan;
                //if the velocity is NaN or O return 0 as default value
                if (Double.IsNaN(velocity) || Double.IsInfinity(velocity))
                {
                    velocity = 0;
                }
            }

            return velocity;
        }

        /// <summary>
        /// Method to calculate distance by getting the center of the stroke using bounds and using the first point of the stroke.
        /// </summary>
        /// <param name="startingPoint"></param>
        /// <param name="finalPoint"></param>
        /// <returns></returns>
        public double CalcualteTotalDistance(Point startPoint, StylusPointCollection e)
        {

            //for(int startPoint=0; startPoint < e.Count-1; startPoint++)
            //{
            //    localDistance += Math.Sqrt(Math.Pow(Math.Abs(e[startPoint].X - e[startPoint+1].X), 2) + Math.Pow(Math.Abs(e[startPoint].Y - e[startPoint+1].Y), 2));
            //}
            Stroke s = new Stroke(e);
            Rect rect = s.GetBounds();
            Point RectCenter = new Point(rect.Left + rect.Width/2, rect.Top + rect.Height/2);
            //double localDistance = Math.Sqrt(Math.Pow(Math.Abs(startPoint.X - rect.X), 2) + Math.Pow(Math.Abs(startPoint.Y - rect.Y), 2));
            double localDistance = Point.Subtract(startPoint, RectCenter).Length;
            //double distanceinmm = distance*(conversion factor from inch to mm)/parts per inch (which is the dot pitch)
            double distanceMM = (localDistance/ 267) * 25.4;
            //assign the current center point as the new init start point
            initStrokeStartPoint = RectCenter;
            return distanceMM;
        }
        #endregion

    }
}
