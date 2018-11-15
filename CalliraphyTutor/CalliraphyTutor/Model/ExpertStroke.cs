using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using CalligraphyTutor.ViewModel;

namespace CalligraphyTutor.Model
{
    /// <summary>
    /// Defines the behaviour of the expert stroke which are loaded in student view model
    /// </summary>
    class ExpertStroke: Stroke
    {
        Brush brush;
        Pen pen;

        Guid expertTimestamp = new Guid("12345678-9012-3456-7890-123456789013");

        private double averageVelocity = 0;
        public double AverageExpertVelocity
        {
            get
            {
                return averageVelocity;
            }
        }

        private System.Windows.Media.Color _strokeColor = Colors.Blue;
        public Color StrokeColor
        {
            get { return _strokeColor; }
            set
            {
                _strokeColor = value;
            }
        }

        public ExpertStroke(StylusPointCollection stylusPoints)
            : base(stylusPoints)
        {
            StrokeColor = Color.FromArgb(Convert.ToByte(50 * this.StylusPoints[this.StylusPoints.Count / 2].PressureFactor), StrokeColor.R, StrokeColor.G, StrokeColor.B);
            brush = new SolidColorBrush(StrokeColor);
            pen = new Pen(brush, Globals.Instance.StrokeWidth);
        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            averageVelocity = CalculateStrokeVelocity(this);
            Debug.WriteLine("average velocity of the stroke: " + (float)averageVelocity);
            drawingAttributes.Color = StrokeColor;
            drawingAttributes.Width = Globals.Instance.StrokeWidth;
            drawingAttributes.Height = Globals.Instance.StrokeHeight;
            base.DrawCore(drawingContext, DrawingAttributes);
        }

        /// <summary>
        /// calculates the velocity of the stroke in seconds
        /// </summary>
        public double CalculateStrokeVelocity(Stroke s)
        {
            double totalStrokeLenght = 0;
            for(int i = 0; i < s.StylusPoints.Count-1; i++)
            {
                totalStrokeLenght += CalcualteDistance(this.StylusPoints[i].ToPoint(), this.StylusPoints[i+1].ToPoint());
            }

            List<DateTime> timeStamps = new List<DateTime>();
            if (s.ContainsPropertyData(expertTimestamp))
            {
                object data = s.GetPropertyData(expertTimestamp);

                foreach (DateTime dt in (Array)data)
                {
                    timeStamps.Add(dt);
                }
            }

            double velocity = totalStrokeLenght / (timeStamps.Last() - timeStamps.First()).TotalSeconds;
            return velocity;

        }

        /// <summary>
        /// Method to calculate distance 
        /// </summary>
        /// <param name="startingPoint"></param>
        /// <param name="finalPoint"></param>
        /// <returns></returns>
        public double CalcualteDistance(Point startingPoint, Point finalPoint)
        {
            double distance = Math.Sqrt(Math.Pow(Math.Abs(startingPoint.X - finalPoint.X), 2)
                    + Math.Pow(Math.Abs(startingPoint.Y - finalPoint.Y), 2));

            //divide the distance with PPI for the surface laptop to convert to inches and then multiply to change into mm
            double distancePPI = (distance / 200) * 25.4;
            return distancePPI;
        }
    }
}
