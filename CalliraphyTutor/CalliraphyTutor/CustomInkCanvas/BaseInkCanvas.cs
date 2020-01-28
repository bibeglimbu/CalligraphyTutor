using CalligraphyTutor.StylusPlugins;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CalligraphyTutor.CustomInkCanvas
{
    /// <summary>
    /// Globally accessible attached property that is used for updating the debug message in the Mainwindowviewmodel binded to this UIelement.
    /// </summary>
    public class DebugMessageHandler : DependencyObject
    {
        #region Attached Property
        public static readonly DependencyProperty DebugMessageProperty =
            DependencyProperty.RegisterAttached("DebugMessage", typeof(string), typeof(DebugMessageHandler), new PropertyMetadata(default(string)));
        public static void SetDebugMessage(UIElement element, string value)
        {
            element.SetValue(DebugMessageProperty, value);
        }
        public static string GetDebugMessage(UIElement element)
        {
            return (string)element.GetValue(DebugMessageProperty);
        }
        #endregion
    }

    class BaseInkCanvas : InkCanvas
    {
        #region Dependency Property
        public static readonly DependencyProperty IsStylusInRangeProperty = DependencyProperty.Register(
            "IsStylusInRange", typeof(bool), typeof(BaseInkCanvas),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsStylusInRangeChanged))
            );
        public bool IsStylusInRange
        {
            get { return (bool)GetValue(IsStylusInRangeProperty); }
            set { SetValue(IsStylusInRangeProperty, value); }
        }

        private static void OnIsStylusInRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Debug.WriteLine("StylusInRange");
        }

        #endregion

        #region variables
        //ensures that the expert and the student inkcanvas provides the same writing feeling
        DrawingAttributes myInkDrawingAttributes = new DrawingAttributes();
        //ensures that the data collection for both vies follow the same logic
        //LogStylusDataPlugin logStylusData = new LogStylusDataPlugin();
        #endregion

        public BaseInkCanvas()
        {
            //this.StylusPlugIns.Add(logStylusData);
            SetmyInkDrawingAttributes(Colors.DarkBlue, new Size(2, 2));
            this.DefaultDrawingAttributes = myInkDrawingAttributes;
        }

        #region overrides
        protected override void OnStylusInRange(StylusEventArgs e)
        {
            IsStylusInRange = true;
            base.OnStylusInRange(e);
        }
        protected override void OnStylusOutOfRange(StylusEventArgs e)
        {
            IsStylusInRange = false;
            base.OnStylusOutOfRange(e);
        }
        #endregion

        #region Native Methods
        /// <summary>
        /// Set the Drawing Attributes
        /// </summary>
        /// <param name="color"></param>
        /// <param name="size"></param>
        private void SetmyInkDrawingAttributes(Color color, Size size)
        {
            myInkDrawingAttributes.Color = color;
            myInkDrawingAttributes.Width = size.Width;
            myInkDrawingAttributes.Height = size.Height;
        }

        /// <summary>
        /// returns the nearest expert strokes as colletion when Pen in range based on hit test with stroke
        /// </summary>
        /// <param name="argsStroke"></param>
        /// <param name="expertSC"></param>
        /// <returns></returns>
        public StrokeCollection ReturnNearestExpertStrokes(Point StylusPoint) 
        {
            StrokeCollection sc = new StrokeCollection();
            //iterate through each stroke to find the nearest stroke
            if (Strokes.Count != 0)
            {
                //check which strokes the current stylus point is hitting with a extended triangle to animate those strokes
                foreach (Stroke es in Strokes)
                {
                    if (es.HitTest(StylusPoint, 100d))
                    {
                        sc.Add(es);

                    }
                }

            }
            return sc;
        }
        #endregion
    }
}
