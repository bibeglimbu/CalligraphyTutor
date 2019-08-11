using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;

namespace CalligraphyTutor.Managers
{
    /// <summary>
    /// Class holds the time taken to draw the strokes and also provides methods for calculating attributes like the stroke length and velocity
    /// </summary>
    class StrokeAttributesManager
    {
        #region Properties
        public List<int> StrokeTime = new List<int>();
        #endregion

        public StrokeAttributesManager()
        {

        }

        #region Native Methods

        /// <summary>
        /// Returns expert velocity in seconds
        /// </summary>
        /// <returns></returns>
        public double CalculateVelocity(Stroke s)
        {
            int timeTaken = (StrokeTime.Last() - StrokeTime.First());
            double velocity = CalculateStrokeLength(s) / Convert.ToDouble(timeTaken);
            return velocity;
        }

        /// <summary>
        /// Adds the total lenght of the stroke
        /// </summary>
        /// <param name="s"></param>
        public double CalculateStrokeLength(Stroke s)
        {
            double tempStrokeLenght = 0.0d;
            for (int i = 0; i < s.StylusPoints.Count - 1; i++)
                {
                tempStrokeLenght += CalcualteDistance(s.StylusPoints[i].ToPoint(),
                        s.StylusPoints[i + 1].ToPoint());
                }
            return tempStrokeLenght;
        }

        /// <summary>
        /// Returns the distance between two points in double
        /// </summary>
        /// <param name="startingPoint"></param>
        /// <param name="finalPoint"></param>
        /// <returns></returns>
        public double CalcualteDistance(Point startingPoint, Point finalPoint)
        {
            double distance = Point.Subtract(startingPoint, finalPoint).Length;
            return distance;
        }

        #endregion
    }
}
