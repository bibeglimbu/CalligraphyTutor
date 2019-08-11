﻿using CalligraphyTutor.CustomInkCanvas;
using CalligraphyTutor.StylusPlugins;
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

namespace CalligraphyTutor.StylusPlugins
{
    /// <summary>
    /// Custom Dynamic renderer for rendering ink while it is being drawn.
    /// </summary>
    class StudentDynamicRenderer : DynamicRenderer
    {
        #region Variables
        /// <summary>
        /// Holds the expert Strokes. Recommended to iterate via strokes first and then the styluspoint to save iterative cycles
        /// </summary>
        StrokeCollection ExpertStrokeCollection = new StrokeCollection();
        /// <summary>
        /// holds the nearest styluspoint in the <see cref=" ExpertStroke"/>
        /// </summary>
        StylusPoint ExpertStylusPoint = new StylusPoint();

        //Variables holding the types of feedback
        private bool IsPressureChecked = false;
        private bool IsStrokeChecked = false;
        private bool IsSpeedChecked = false;
        private bool IsExpertStrokeLoaded = false;

        /// <summary>
        /// Defines the current Color of the stroke
        /// </summary>
        private Color StrokeColor = Colors.Red;
        /// <summary>
        /// Holds the allowed threshold for the change in color
        /// </summary>
        private const double HitThreshold = 5d;

        //list that holds the collection of points where the stroke hit test occured
        private List<Point> hitChangedPoints = new List<Point>();
        #endregion

        #region eventsDefintion
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
            public Stroke stroke { get; set; }
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
            public List<Point> hitChangedPoints { get; set; }

        }

        #endregion

        public StudentDynamicRenderer()
        {
            ExpertInkCanvas.ExpertStrokeLoadedEvent += ExpertInkCanvas_ExpertStrokeLoadedEvent;
            StudentInkCanvas.PressureCheckedEvent += StudentInkCanvas_PressureCheckedEvent;
            StudentInkCanvas.StrokeCheckedEvent += StudentInkCanvas_StrokeCheckedEvent;
            StudentInkCanvas.SpeedCheckedEvent += StudentInkCanvas_SpeedCheckedEvent;

        }

        #region eventhandlers
        private void ExpertInkCanvas_ExpertStrokeLoadedEvent(object sender, ExpertInkCanvas.ExpertStrokeLoadedEventEventArgs e)
        {
            IsExpertStrokeLoaded = e.state;
            ExpertStrokeCollection = e.strokes;
        }

        private void StudentInkCanvas_StrokeCheckedEvent(object sender, StudentInkCanvas.StrokeCheckedEventArgs e)
        {
            IsStrokeChecked = e.state;
            StrokeColor = Colors.Green;
        }
        private void StudentInkCanvas_PressureCheckedEvent(object sender, StudentInkCanvas.PressureCheckedEventArgs e)
        {
            IsPressureChecked = e.state;
            //StrokeColor = Colors.Green;
        }
        private void StudentInkCanvas_SpeedCheckedEvent(object sender, StudentInkCanvas.SpeedCheckedEventArgs e)
        {
            IsSpeedChecked = e.state;
        }

        #endregion

        #region overrides

        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            base.OnStylusMove(rawStylusInput);
            //collect the stylus point
            StylusPointCollection stylusPoints = rawStylusInput.GetStylusPoints();
            //if there are no points in the collection
            if (stylusPoints.Count <=0 )
            {
                return;
            }
            rawStylusInput.NotifyWhenProcessed(stylusPoints);
            
        }

        protected override void OnStylusMoveProcessed(object callbackData, bool targetVerified)
        {
            // Check that the element actually receive the OnStylusUp input.
            if (targetVerified)
            {
                StylusPointCollection spc = callbackData as StylusPointCollection;

                //pass the points into a stroke to find the neartest stroke
                Stroke tempStroke = new Stroke(spc);

                //if the expert stroke is loaded
                if (IsExpertStrokeLoaded == true)
                {
                    //get the expert strokes collection that are overlapping with the current ink stroke
                    StrokeCollection tempStrokeCollection = SelectBoundingStrokeCollection(tempStroke, ExpertStrokeCollection);
                    //if the tempstrokecollection returns empty exit the method.
                    if (tempStrokeCollection.Count > 0)
                    {
                        //get the stylusPoint and the stroke over lapping the pen point from the tempStrokeCollection
                        Stroke ExpertStroke;
                        ExpertStylusPoint = SelectNearestExpertPoint(tempStroke, tempStrokeCollection, out ExpertStroke);

                        //raise the event that the nearest expert point is selected
                        NearestExpertStylusPointCalculatedEventArgs args = new NearestExpertStylusPointCalculatedEventArgs();
                        args.styluspoint = ExpertStylusPoint;
                        args.stroke = ExpertStroke;
                        OnNearestExpertStylusPointCalculated(args);

                        //if StrokeFeedback is requested
                        if (IsStrokeChecked == true)
                        {
                            if (tempStroke.HitTest(ExpertStylusPoint.ToPoint(), HitThreshold))
                            {
                                foreach (StylusPoint sp in tempStroke.StylusPoints)
                                {
                                    hitChangedPoints.Add(sp.ToPoint());
                                }
                                StrokeColor = Color.FromArgb(255, 0, 255, 0);
                            }
                            else
                            {
                                StrokeColor = Color.FromArgb(255, 255, 0, 0);
                            }
                        }
                        //If Pressure is checked change the color of the Stroke
                        if (IsPressureChecked == true)
                        {
                            List<float> pressureFactorList = new List<float>();
                            foreach (StylusPoint s in tempStroke.StylusPoints)
                            {
                                pressureFactorList.Add(s.PressureFactor);
                            }
                            float StudentPressureFactor = pressureFactorList.Average();
                            //multiplier to darken or lighen the PreviousColor
                            float ExpertPressureFactor = ExpertStylusPoint.PressureFactor;
                            //if pressure is higher produce darker color by passing negetive number
                            float ColorWeight = ExpertPressureFactor - StudentPressureFactor;
                            StrokeColor = ChangeColorBrightness(StrokeColor, ColorWeight);
                        }

                    }
                   
                }
            }
        }

        protected override void OnDraw(DrawingContext drawingContext, StylusPointCollection stylusPoints,
                                       Geometry geometry, Brush fillBrush)
        {
            fillBrush = new SolidColorBrush(StrokeColor);
            base.OnDraw(drawingContext, stylusPoints, geometry, fillBrush);
        }

        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            base.OnStylusDown(rawStylusInput);
            Stroke tempStroke = new Stroke(rawStylusInput.GetStylusPoints());
            if (tempStroke.HitTest(ExpertStylusPoint.ToPoint(), HitThreshold))
            {
                foreach (StylusPoint sp in tempStroke.StylusPoints)
                {
                    hitChangedPoints.Add(sp.ToPoint());
                }
                StrokeColor = Color.FromArgb(255, 0, 255, 0);
            }
            else
            {
                StrokeColor = Color.FromArgb(255, 255, 0, 0);
            }
            
        }

        protected override void OnStylusUp(RawStylusInput rawStylusInput)
        {
            base.OnStylusUp(rawStylusInput);
            //raise the event and pass the collection
            HitChangePointsEventArgs args = new HitChangePointsEventArgs();
            args.hitChangedPoints = hitChangedPoints;
            OnHitChangePoints(args);
            //reset the collection
            hitChangedPoints = new List<Point>();
            
        }

        #endregion

        #region Native Methods
        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
        }

        /// <summary>
        /// Method to calculate tempDistance 
        /// </summary>
        /// <param name="startingPoint"></param>
        /// <param name="finalPoint"></param>
        /// <returns></returns>
        public double CalcualteDistance(Point startingPoint, Point finalPoint)
        {
            double distance = Point.Subtract(startingPoint, finalPoint).Length;
            return distance;
        }

        /// <summary>
        /// Method for returning the collection of styluspoints in the expert stroke from the point at which the pen is standing. Check that none of the Parameters are empty
        /// </summary>
        /// <param name="s">Stroke formed from the event args styluspoint collection</param>
        /// <param name="SC"></param>
        /// <returns></returns>
        private StylusPoint SelectNearestExpertPoint(Stroke RenderedStroke, StrokeCollection ExpertStrokeCollection, out Stroke ExpertStroke)
        {
            //delcare value that holds the  distance between the pen and the expert stroke.
            double tempStrokeDeviation = -0.01d;
            //the styluspoint to be returned to the expert, set the first point as default
            StylusPoint tempStylusPoint = ExpertStrokeCollection.First().StylusPoints.First();
            //assign the first stroke from expertstrokecollection
            Stroke tempStroke = ExpertStrokeCollection.First();
            //get the bouding rect of the args stroke
            Rect rect = RenderedStroke.GetBounds();
            //get the center point of the arg stroke for calculating the tempDistance
            Point centerPoint = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);

            // local function iterates through each stroke and their stylusPoint
            void loop()
            {
                foreach (Stroke s in ExpertStrokeCollection)
                {
                    //iterate through each styluspoint in a stroke
                    foreach (StylusPoint sp in s.StylusPoints)
                    {
                        double tempDistance = CalcualteDistance(centerPoint, sp.ToPoint());
                        //if it is the first time
                        if (tempStrokeDeviation < 0)
                        {
                            tempStrokeDeviation = tempDistance;
                            continue;
                        }
                        //if the distance is less than 0.5, return the point and exit the loop
                        if (tempDistance <= 0.5d)
                        {
                            //assign the values
                            tempStroke = s;
                            tempStylusPoint = sp;
                            return;

                        }
                        else if (tempDistance < tempStrokeDeviation)
                        //if the point does not intersect the expert stroke, check if the new tempDistance is smaller than the previous tempDistance
                        {
                            //assign the new shorter tempDistance, and assign the new point as ref point
                            tempStrokeDeviation = tempDistance;
                            tempStylusPoint = sp;
                            tempStroke = s;
                        }
                    }
                }
            }
            //call the local function
            loop();
            ExpertStroke = tempStroke;
            return tempStylusPoint;

        }

        /// <summary>
        /// Method that returns collection of Expertstrokes that are colliding with the current stroke. 
        /// This is similar to <see cref="BaseInkCanvas"/>'s ReturnExpertStroke collection but instead of a
        /// taking a single input as a point which is updated on physical points hover, which cannot be
        /// accessed in this thread, it takes the whole stroke from the event.
        /// </summary>
        /// <param name="RenderedStroke"></param>
        /// <param name="ExpertStrokeCollection"></param>
        /// <returns></returns>
        private StrokeCollection SelectBoundingStrokeCollection(Stroke RenderedStroke, StrokeCollection ExpertStrokeCollection)
        {
            //holds all the expert strokes that intersects with the current stroke being drawn
            StrokeCollection expertStrokeCollection = new StrokeCollection();

            foreach (Stroke es in ExpertStrokeCollection)
            {
                //if the strokes intersect add it to the collection
                if (RenderedStroke.GetBounds().IntersectsWith(es.GetBounds()))
                {
                    expertStrokeCollection.Add(es);
                }
            }

            return expertStrokeCollection;
        }
        #endregion

    }
}
