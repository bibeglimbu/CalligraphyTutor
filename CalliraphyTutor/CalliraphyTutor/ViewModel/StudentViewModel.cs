using CalligraphyTutor.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CalligraphyTutor.ViewModel
{
    public class StudentViewModel: BindableBase
    {
        #region Vars & properties

        /// <summary>
        /// Frames of the stroke animation
        /// </summary>
        private int _updateAnimation = 0;

        /// <summary>
        /// Timer for controlling all animtion in student mode
        /// </summary>
        private DispatcherTimer _studentTimer = new DispatcherTimer();

        //Helps set the screen size of the application in the view
        private int _screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        public int ScreenWidth
        {
            get { return _screenWidth; }
            set
            {
                _screenWidth = value;
                RaisePropertyChanged("ScreenWidth");
            }
        }
        private int _screenHeight = (int)SystemParameters.PrimaryScreenHeight;
        public int ScreenHeight
        {
            get { return _screenHeight; }
            set
            {
                _screenHeight = value;

                RaisePropertyChanged("ScreenHeight");
            }
        }
        
        private string _recordButtonName = "Start Recording";
        /// <summary>
        /// Text to be displayed on the dynamic button
        /// </summary>
        public String RecordButtonName
        {
            get { return _recordButtonName; }
            set
            {
                _recordButtonName = value;
                RaisePropertyChanged("RecordButtonName");
            }
        }
        
        private Brush _brushColor = new SolidColorBrush(Colors.White);
        /// <summary>
        /// Color of the button for chaning the color based on the state
        /// </summary>
        public Brush RecordButtonColor
        {
            get { return _brushColor; }
            set
            {
                _brushColor = value;
                RaisePropertyChanged("RecordButtonColor");
            }
        }

        private StrokeCollection _studentStrokes = new StrokeCollection();
        private StrokeCollection _expertStrokes = new StrokeCollection();
        /// <summary>
        /// StrokeCollection that binds to the Strokes of the StudentInkCanvas Strokes property.
        /// </summary>
        public StrokeCollection StudentStrokes
        {
            get { return _studentStrokes; }
            set
            {
                _studentStrokes = value;
                RaisePropertyChanged("StudentStrokes");
            }
        }
        /// <summary>
        /// StrokeCollection that binds to the ExpertStrokes of the ExpertInkCanvas Strokes property.
        /// </summary>
        public StrokeCollection ExpertStrokes
        {
            get { return _expertStrokes; }
            set
            {
                _expertStrokes = value;
                RaisePropertyChanged("ExpertStrokes");
            }
        }

        private static bool _expertStrokesLoaded = false;
        /// <summary>
        /// Indicates if the <see cref="ExpertStrokes"/> has been loaded or not.
        /// </summary>
        public static bool ExpertStrokeLoaded
        {
            get { return _expertStrokesLoaded; }
            set
            {
                _expertStrokesLoaded = value;
            }
        }
        //var for turning the pop up on and off
        private bool _stayOpen = false;
        /// <summary>
        /// Indicates if the <see cref="ResultsViewModel"/> is open or not.
        /// </summary>
        public bool StayOpen
        {
            get { return _stayOpen; }
            set
            {
                _stayOpen = value;
                RaisePropertyChanged("StayOpen");
            }
        }

        private bool _isRecording = false;
        /// <summary>
        /// Indicates if the data is being sent to the Learning hub.
        /// </summary>
        public bool StudentIsRecording
        {
            get { return _isRecording; }
            set
            {
                _isRecording = value;
            }
        }

        Globals globals;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentViewModel()
        {

            globals = Globals.Instance;
            _studentTimer.Interval = new TimeSpan(10000);
            _studentTimer.Tick += AnimationTimer_Tick;

            ResultsViewModel.ButtonClicked += ResultsViewModel_ButtonClicked;
            //assign the variables to be rorded in the learning hub
            setValueNames();
        }

        #region Events Definition

        /// <summary>
        /// Event for publishing the debug message to be displayed in the Main window debug box.
        /// </summary>
        public event EventHandler<DebugEventArgs> DebugReceived;
        protected virtual void OnDebugReceived(DebugEventArgs e)
        {
            EventHandler<DebugEventArgs> handler = DebugReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public class DebugEventArgs : EventArgs
        {
            public string message { get; set; }
        }

        /// <summary>
        /// Event raised when the Max Min value of the current stroke changes
        /// </summary>
        public static event EventHandler<MaxMinChangedEventArgs> MaxMinChanged;
        protected virtual void OnMaxMinchanged(MaxMinChangedEventArgs e)
        {
            EventHandler<MaxMinChangedEventArgs> handler = MaxMinChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public class MaxMinChangedEventArgs : EventArgs
        {
            public int ExpertMaxPressure { get; set; }
            public int ExpertMinPressure { get; set; }
            public int StudentMaxPressure { get; set; }
            public int StudentMinPressure { get; set; }
        }
        #endregion

        #region EventHandlers
        /// <summary>
        /// Event handler which toggles the <see cref="StayOpen"/> on and off.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResultsViewModel_ButtonClicked(object sender, EventArgs e)
        {
            StayOpen = false;
        }
        /// <summary>
        /// Event handler for receiving feedback from the <see cref="LearningHubManager"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="feedback"></param>
        private void MyFeedback_feedbackReceivedEvent(object sender, string feedback)
        {
            if (globals.Speech.State != SynthesizerState.Speaking)
            {
                ReadStream(feedback);
            }
        }

        /// <summary>
        /// Event handler for everytime the timer is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            _updateAnimation += 1;
        }

        /// <summary>
        /// Event handler when StartRecordingData message is received from <see cref="LearningHubManager"/>
        /// </summary>
        /// <param name="sender"></param>
        private void MyConnector_startRecordingEvent(Object sender)
        {
             StartRecordingData();
        }

        /// <summary>
        /// Event handler when StopRecordingData message is received from <see cref="LearningHubManager"/>
        /// </summary>
        /// <param name="sender"></param>
        private void MyConnector_stopRecordingEvent(Object sender)
        {
                    if (ExpertStrokeLoaded == false)
                    {
                        LoadStrokes();
                    }
                    StartRecordingData();
        }


        /// <summary>
        /// Reference StylusPointCollection used for checking Current partition on hit test.
        /// </summary>
        private StylusPointCollection _tempSPCollectionCurrentPartition = new StylusPointCollection();
        private ICommand _StylusMoved;

        /// <summary>
        /// Icommand method for binding to the button.
        /// </summary>
        public ICommand StudentInkCanvas_StylusMoved
        {
            get
            {
                _StylusMoved = new RelayCommand(
                    param => this.StudentView_OnStylusMoved(param),
                    null
                    );

                return _StylusMoved;
            }
        }

        Point prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
        public void StudentView_OnStylusMoved(Object param)
        {

            //cast the parameter as stylyus event args
            StylusEventArgs args = (StylusEventArgs)param;
            StylusPointCollection spc= args.GetStylusPoints((InkCanvas)args.Source);
            //if there are not styluspoints exit
            if (spc.Count == 0)
                return;

            //get the last point of the collection
            StylusPoint sp= args.GetStylusPoints((InkCanvas)args.Source).Last();

            //point for calculating hittest only at specific intervals
            Point pt = spc[0].ToPoint();
            Vector v = Point.Subtract(prevPoint, pt);
            //once the expert styluspoints are loaded check the hittest and perform the action.
            if (ExpertStrokeLoaded == true)
            {
                //check X angle of the pen
                if (sp.GetPropertyValue(StylusPointProperties.X) >= 9000 || sp.GetPropertyValue(StylusPointProperties.X) >= 11000)
                {
                    Debug.WriteLine("Ensure the angle of the pen is roughly 45");
                    //send feedback to the learning hub if the pens angle is outside the threshold
                    //myConnector.sendFeedback("Ensure the angle of the pen is roughly 45 ");

                    //read out a message
                    globals.Speech.Speak("Ensure the angle of the pen is roughly 45 ");
                }

                //start saving the stylusPoints to the holder
                _tempSPCollectionCurrentPartition.Add(spc.Reformat(_tempSPCollectionCurrentPartition.Description));
                
                //if the pen has moved a certain distance
                if(v.Length > 2)
                {
                    //start checking for hittest to change color
                    foreach (Stroke s in ExpertStrokes)
                    {
                        //if the stylus pen hits any of the currrent stroke in the list
                        if (HitTestwithExpertStroke(s, args))
                        {
                            //change the color of the dynamic renderer
                            ChangeDynamicRendererColor((StudentInkCanvas)args.Source, Colors.Red);
                            //if the color changes then we donot need to run through all the strokes in the expertstroke
                            //break in order to prevent the color turning blue again
                            //SendDebug("Dynamic renderer color changed to red");
                            break;
                        }

                        //if none of the strokes return a hit change the color back to black
                        if (s.Equals(ExpertStrokes.Last()))
                        {
                            ChangeDynamicRendererColor((StudentInkCanvas)args.Source, Colors.Black);
                            SendDebug("Dynamic renderer color changed to black");
                            break;
                        }
                    }

                    //check the partition direction
                    if (_tempSPCollectionCurrentPartition.Count >= 2)
                    {
                        //int temp = partitionCount;
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(
                            () =>
                            {
                                CheckPartitionDirection(args);
                            }));
                    }

                    //reset the point
                    prevPoint = pt;
                }
                
 
            }
        }
        #endregion

        #region Send data
        /// <summary>
        /// Calls the <see cref="LearningHubManager"/>'s SetValueNames method which assigns the variables names for storing
        /// </summary>
        private void setValueNames()
        {
            List<string> names = new List<string>();
            names.Add("PenPressure");
            names.Add("Tilt_X");
            names.Add("Tilt_Y");
            //myConnector.setValuesName(names);
        }

        private void SendData(StylusEventArgs args)
        {
            List<string> values = new List<string>();
            String v = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.NormalPressure).ToString();
            values.Add(v);
            //Debug.WriteLine(v);
            //myConnector.storeFrame(values);
        }
        #endregion

        #region Methods called by the buttons
        private ICommand _buttonClicked;
        public void ClearStrokes()
        {
            StudentStrokes.Clear();
        }
        public ICommand ClearButton_clicked
        {
            get
            {
                _buttonClicked = new RelayCommand(
                    param => this.ClearStrokes(),
                    null
                    );

                return _buttonClicked;
            }
        }
        private void LoadStrokes()
        {
            ExpertStrokes = new StrokeCollection(globals.GlobalFileManager.LoadStroke());
            ExpertStrokeLoaded = true;
            //start the timer
            //_studentTimer.Start();
        }
        public ICommand LoadButton_clicked
        {
            get
            {
                _buttonClicked = new RelayCommand(
                    param => this.LoadStrokes(),
                    null
                    );

                return _buttonClicked;
            }
        }
        public ICommand _recordButtonClicked;
        public ICommand RecordButton_clicked
        {
            get
            {
                if (_recordButtonClicked == null)
                {
                        _recordButtonClicked = new RelayCommand(
                            param => this.StartRecordingData(),
                            null
                            );
                }
                return _recordButtonClicked;
            }

        }

        private void StartRecordingData()
        {
            if (StudentIsRecording == false)
            {
                StudentStrokes.Clear();
                StudentIsRecording = true;
                RecordButtonName = "Stop Recording";
                RecordButtonColor = new SolidColorBrush(Colors.Green);
                StayOpen = false;
            }
            else if (StudentIsRecording == true)
            {
                StudentIsRecording = false;
                RecordButtonName = "Start Recording";
                StudentStrokes.Clear();
                RecordButtonColor = new SolidColorBrush(Colors.White);
                StayOpen = true;
            }
        }
        #endregion

        #region Native methods

        /// <summary>
        /// Method for reading out a string
        /// </summary>
        /// <param name="s"></param>
        private void ReadStream(String s)
        {
            if (s.Contains("Read"))
            {
                s.Remove(0, 4);
                globals.Speech.SpeakAsync("Grip the pen gently");

            }

        }

        //initialize with a third color so the first loop is executed to changed the color
        private Color _tempColor = Colors.Black;
        //states wether the color of the spc has changed in this iteration
        private bool _strokeColorChanged = false;
        /// <summary>
        /// change color of the dynamic renderer based on hittest and message myo
        /// </summary>
        /// <param name="e"></param>
        /// <param name="canvas"></param>
        private void ChangeDynamicRendererColor(StudentInkCanvas canvas, Color c)
        {
            //if the color has changed over the iteration
            if (_tempColor != c)
            {
                _strokeColorChanged = true;
                _tempColor = c;
            }

            // send error message to myo
            if (_strokeColorChanged == true)
            {
                //Debug.WriteLine("Color Changed");
                if (c == Colors.Red)
                {
                    if (StudentIsRecording == true)
                    {
                        try
                        {
                            //myConnector.sendFeedback("Myo");
                            Debug.WriteLine("StudentViewModel: Myo feedback Sent");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }


                }

                canvas.DefaultColor = c;
                _strokeColorChanged = false;
            }

        }

        private int partitionCount = 0;
        double tempSegmentLength = 0;
        double minSegmentLength = 20d;
        //holder for checking the tempsegment length
        StylusPointCollection spc = new StylusPointCollection();
        enum Direction { right, left, up, down, intialDirection };
        Direction previousDirection = Direction.intialDirection;
        /// <summary>
        /// partitions the spc into smallers strokes to store them based on the direction.
        /// </summary>
        /// <param name="stroke"></param>
        /// <returns></returns>
        private void CheckPartitionDirection(StylusEventArgs e)
        {
            //if e has no stylus points, return
            if (e.GetStylusPoints((StudentInkCanvas)e.Source).Count == 0)
            {
                return;
            }
            //if e has points add them to spc for holding
            spc.Add(e.GetStylusPoints((StudentInkCanvas)e.Source).Reformat(spc.Description));

            //declare distance holder between each first SP in spc
            Direction directionOfSegment;

            //calculate the distance of the spc
            tempSegmentLength = Math.Sqrt((Math.Pow(spc[spc.Count-1].X - spc[0].X, 2) +
                    Math.Pow(spc[spc.Count - 1].Y - spc[0].Y, 2)));
            //if spc.distance is larger than the minsegmentlenght
            if (tempSegmentLength >= minSegmentLength)
            {
                //reset the distance measring variables
                tempSegmentLength = 0;
                spc = new StylusPointCollection();
                //if(e has less than 2 points return)
                if (e.GetStylusPoints((StudentInkCanvas)e.Source).Count < 2)
                {
                    return;
                }

                //calculate the direction from the last 2 points
                if (System.Math.Abs(e.GetStylusPoints((StudentInkCanvas)e.Source)[e.GetStylusPoints((StudentInkCanvas)e.Source).Count - 1].X - e.GetStylusPoints((StudentInkCanvas)e.Source)[e.GetStylusPoints((StudentInkCanvas)e.Source).Count-2].X) >
                        System.Math.Abs(e.GetStylusPoints((StudentInkCanvas)e.Source)[e.GetStylusPoints((StudentInkCanvas)e.Source).Count - 1].Y - e.GetStylusPoints((StudentInkCanvas)e.Source)[e.GetStylusPoints((StudentInkCanvas)e.Source).Count - 2].Y))
                {
                    // change in x is greater, now find left or right
                    if ((e.GetStylusPoints((StudentInkCanvas)e.Source)[e.GetStylusPoints((StudentInkCanvas)e.Source).Count - 2].X - e.GetStylusPoints((StudentInkCanvas)e.Source)[e.GetStylusPoints((StudentInkCanvas)e.Source).Count - 1].X) < 0)
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
                    if ((e.GetStylusPoints((StudentInkCanvas)e.Source)[e.GetStylusPoints((StudentInkCanvas)e.Source).Count - 2].Y - e.GetStylusPoints((StudentInkCanvas)e.Source)[e.GetStylusPoints((StudentInkCanvas)e.Source).Count - 1].Y) < 0)
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
                    return;
                }
                //if its not the firs time compare to see if the direction has changed
                if (directionOfSegment != previousDirection)
                {
                    Debug.WriteLine("Student : Previous Direction: " + previousDirection);
                    //if _tempSPCollection is not empty
                    if (_tempSPCollectionCurrentPartition.Count > 1)
                    {
                        //start new partition
                        partitionCount += 1;
                        //synthesizer.Speak("Partition changed");
                        Debug.WriteLine(partitionCount);
                        //save the max min pressure only if the expert data is already loaded.
                        SaveMaxMin(_tempSPCollectionCurrentPartition, partitionCount);
                        //empty the _tempSPCollection
                        _tempSPCollectionCurrentPartition = new StylusPointCollection();
                        //assign the new Direction as the previous direction
                        previousDirection = directionOfSegment;
                    }
                }

                //if the partition count has exceded the no of expert partitions reset it.
                if (partitionCount >= ExpertStrokes.Count)
                {
                    partitionCount = 0;
                }

                if ((DateTime.Now - globals.LastExecution).TotalSeconds > 0.5)
                {
                    if (globals.Speech.State != SynthesizerState.Speaking)
                    {
                        //check if the student is between the experts range, current issue with the pressure at 4000 level while drawn but changes to 1000 when stroke is created
                        if (e.GetStylusPoints(((StudentInkCanvas)e.Source)).Last().GetPropertyValue(StylusPointProperties.NormalPressure) / 4 > (((ExpertCanvasStroke)_expertStrokes[partitionCount]).MaxPressure + 50))
                        {
                            new Task(() => globals.Speech.SpeakAsync("Pressure too high")).Start();
                            //Debug.WriteLine("Pressure "+ e.GetStylusPoints(((StudentInkCanvas)e.Source)).Last().GetPropertyValue(StylusPointProperties.NormalPressure));
                        }
                        if (e.GetStylusPoints(((StudentInkCanvas)e.Source)).Last().GetPropertyValue(StylusPointProperties.NormalPressure) / 4 < (((ExpertCanvasStroke)_expertStrokes[partitionCount]).MinPressure - 50))
                        {
                            new Task(() => globals.Speech.SpeakAsync("Pressure too low")).Start();
                        }

                    }

                    globals.LastExecution = DateTime.Now;
                }
            }

        }

        /// <summary>
        /// Checks if the current stylus points collides with the expertStroke
        /// </summary>
        /// <param name="expertStroke"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool HitTestwithExpertStroke(Stroke expertStroke, StylusEventArgs e)
        {
            double threshold = 5d;

            //use the first stylus point
            Point p = e.GetStylusPoints((InkCanvas)e.Source)[0].ToPoint();
            return expertStroke.HitTest(p, threshold);

            ////use the current position of the stylus pen
            //Point p = e.GetPosition(this); get the position of the stylus
            //return expertStroke.HitTest(e.GetPosition((InkCanvas)e.Source), threshold);
        }

        /// <summary>
        /// saves the max and min values of the student spc in results view model
        /// </summary>
        /// <param name="s"></param>
        private void SaveMaxMin(StylusPointCollection s, int partitionCount)
        {

            int maxPressure;
            int minPressure;
            MaxMinChangedEventArgs args = new MaxMinChangedEventArgs();
            maxPressure = s[0].GetPropertyValue(StylusPointProperties.NormalPressure);
            minPressure = maxPressure;
            //assign student MaxMin
            foreach (StylusPoint sp in s)
            {
                int temp = sp.GetPropertyValue(StylusPointProperties.NormalPressure);
                maxPressure = Math.Max(temp, maxPressure);
                minPressure = Math.Min(temp, minPressure);
            }
            args.StudentMaxPressure = maxPressure;
            args.StudentMinPressure = minPressure;
            //save average rather than max min in the student spc
            //StudentMaxPressure.Add(maxPressure);
            //Debug.WriteLine("Student View model student max: " + maxPressure);
            //StudentMinPressure.Add(minPressure);
            //Debug.WriteLine("Student View model student min: " + minPressure);

            //Assign Expert Pressure
            ExpertCanvasStroke ds;
            if (ExpertStrokes[partitionCount - 1] is ExpertCanvasStroke)
            {
                ds = (ExpertCanvasStroke)ExpertStrokes[partitionCount - 1];
                //ExpertMaxPressure.Add(ds.MaxPressure);
                //Debug.WriteLine("Student View model expert max: " + ds.MaxPressure);
                //ExpertMinPressure.Add(ds.MinPressure);
                //Debug.WriteLine("Student View model expert min: " + ds.MinPressure);
                args.ExpertMaxPressure = ds.MaxPressure;
                args.ExpertMinPressure = ds.MinPressure;
            }
            else
            {
                Debug.WriteLine("ExpertStrokes: "+ (ExpertStrokes[partitionCount - 1] is ExpertCanvasStroke));
            }
            OnMaxMinchanged(args);

        }
        #endregion

        public void SendDebug(string s)
        {
            DebugEventArgs args = new DebugEventArgs();
            args.message = s;
            OnDebugReceived(args);
        }
    }
}
