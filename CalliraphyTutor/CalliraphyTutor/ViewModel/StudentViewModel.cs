using CalligraphyTutor.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Speech.Synthesis;

namespace CalligraphyTutor.ViewModel
{
    class StudentViewModel: BindableBase
    {
        #region Vars & properties

        private int _animationTimer = 0;
        public int UpdateAnimation
        {
            get { return _animationTimer; }
            set
            {
                _animationTimer = value;
                OnPropertyChanged("UpdateAnimation");
            }
        }

        //central time for controlling all animtion in student mode
        private DispatcherTimer _studentTimer = new DispatcherTimer();
        public DispatcherTimer StudentTimer
        {
            get { return _studentTimer; }
            set
            {
                _studentTimer = value;
                OnPropertyChanged("StudentTimer");
            }
        }

        //screen size of the application
        private int _screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        public int ScreenWidth
        {
            get { return _screenWidth; }
            set
            {
                _screenWidth = value;
                //NotifyOfPropertyChange(() => ScreenWidth);
                OnPropertyChanged("ScreenWidth");
            }
        }
        private int _screenHeight = (int)SystemParameters.PrimaryScreenHeight;
        public int ScreenHeight
        {
            get { return _screenHeight; }
            set
            {
                _screenHeight = value;
                //NotifyOfPropertyChange(() => ScreenHeight);
                OnPropertyChanged("ScreenHeight");
            }
        }
        //name of the button
        private string _recordButtonName = "Start Recording";
        public String RecordButtonName
        {
            get { return _recordButtonName; }
            set
            {
                _recordButtonName = value;
                OnPropertyChanged("RecordButtonName");
            }
        }
        //color of the button
        private Brush _brushColor = new SolidColorBrush(Colors.White);
        public Brush RecordButtonColor
        {
            get { return _brushColor; }
            set
            {
                _brushColor = value;
                OnPropertyChanged("RecordButtonColor");
            }
        }

        private StrokeCollection _studentStrokes = new StrokeCollection();
        private StrokeCollection _expertStrokes = new StrokeCollection();
        public StrokeCollection StudentStrokes
        {
            get { return _studentStrokes; }
            set
            {
                _studentStrokes = value;
                OnPropertyChanged("StudentStrokes");
            }
        }
        public StrokeCollection ExpertStrokes
        {
            get { return _expertStrokes; }
            set
            {
                _expertStrokes = value;
                OnPropertyChanged("ExpertStrokes");
            }
        }

        private static bool _expertStrokesLoaded = false;
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
        public bool StayOpen
        {
            get { return _stayOpen; }
            set
            {
                _stayOpen = value;
                OnPropertyChanged("StayOpen");
            }
        }

        private SpeechSynthesizer synthesizer;
        #endregion

        public StudentViewModel()
        {
            HubConnector.myConnector.startRecordingEvent += MyConnector_startRecordingEvent;
            HubConnector.myConnector.stopRecordingEvent += MyConnector_stopRecordingEvent;

            StudentTimer.Interval = new TimeSpan(10000);
            StudentTimer.Tick += AnimationTimer_Tick;

            synthesizer = new SpeechSynthesizer();
            synthesizer.Volume = 100;  // 0...100
            synthesizer.Rate = 1;     // -10...10
        }

        #region Events Definition
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

        #region Events
        //Method called everytime the timer is updated
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            UpdateAnimation += 1;
        }

        private void MyConnector_startRecordingEvent(Object sender)
        {
            Debug.WriteLine("start");
            setValueNames();
            StartRecordingData();
        }

        private void MyConnector_stopRecordingEvent(Object sender)
        {
            if (ExpertStrokeLoaded == false)
            {
                LoadStrokes();
            }
            Debug.WriteLine("stop");
            StartRecordingData();

        }

        //reference StylusPointCollection used for checking Current partition on hit test.
        private StylusPointCollection _tempSPCollectionCurrentPartition = new StylusPointCollection();
        private ICommand _StylusMoved;
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

        public void StudentView_OnStylusMoved(Object param)
        {
            StylusEventArgs args = (StylusEventArgs)param;
            StylusPointCollection spc= args.GetStylusPoints((InkCanvas)args.Source);
            StylusPoint sp= args.GetStylusPoints((InkCanvas)args.Source).Last();
            //once the expert spc is loaded check the hittest and perform the action.
           
            if (ExpertStrokeLoaded == true)
            {
                //send message to hololens
                if (sp.GetPropertyValue(StylusPointProperties.X) >= 9000 || sp.GetPropertyValue(StylusPointProperties.X) >= 11000)
                {
                    Debug.WriteLine("Ensure the angle of the pen is roughly 45");
                    //HubConnector.myConnector.sendFeedback("Ensure the angle of the pen is roughly 45 ");
                    synthesizer.Speak("Ensure the angle of the pen is roughly 45 ");
                }

                //start saving the sp to the holder
                _tempSPCollectionCurrentPartition.Add(spc.Reformat(_tempSPCollectionCurrentPartition.Description));
                Color c = Colors.Black;
                //start checking for hittest to change color
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(
                    () =>
                    {
                        foreach (Stroke s in _expertStrokes)
                        {
                            if (HitTestwithExpertStroke(s,args))
                            {
                                c = Colors.Black;
                                //if the color changes then we donot need to run through all the strokes in the expertstroke
                                //break in order to prevent the color turning blue again
                                break;
                            }
                            else
                            {
                                c = Colors.Red;
                            }
                        }
                        ChangeDynamicRendererColor((StudentInkCanvas)args.Source, c);
                    }));

                //check the partition
                if (_tempSPCollectionCurrentPartition.Count >= 2)
                {
                    //int temp = partitionCount;
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(
                        () =>
                        {
                            CheckPartitionDirection(args);
                        }));
                }  
            }
        }
        #endregion

        #region Send data
        public void setValueNames()
        {
            List<string> names = new List<string>();
            names.Add("PenPressure");
            HubConnector.SetValuesName(names);

        }

        public void SendData(StylusEventArgs args)
        {
            List<string> values = new List<string>();
            String v = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.NormalPressure).ToString();
            values.Add(v);
            //Debug.WriteLine(v);
            HubConnector.SendData(values);
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
            ExpertStrokes = new StrokeCollection(Globals.GlobalFileManager.LoadStroke());
            ExpertStrokeLoaded = true;
            //start the timer
            //StudentTimer.Start();
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
        public ICommand RecordButton_clicked
        {
            get
            {
                _buttonClicked = new RelayCommand(
                    param => this.StartRecordingData(),
                    null
                    );

                return _buttonClicked;
            }
        }

        private void StartRecordingData()
        {

            if (Globals.IsRecording == false)
            {
                StudentStrokes.Clear();
                StayOpen = false;
                Globals.IsRecording = true;
                RecordButtonName = "Stop Recording";
                RecordButtonColor = new SolidColorBrush(Colors.Green);
            }
            else if (Globals.IsRecording == true)
            {
                Globals.IsRecording = false;
                StayOpen = true;
                RecordButtonName = "Start Recording";
                StudentStrokes.Clear();
                RecordButtonColor = new SolidColorBrush(Colors.White);
            }
        }
        #endregion

        #region Native methods
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
                    if (Globals.IsRecording == true)
                    {
                        try
                        {
                            HubConnector.myConnector.sendFeedback("Myo ping");
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

                if ((DateTime.Now - Globals.LastExecution).TotalSeconds > 1)
                {
                    //check if the student is between the experts range, current issue with the pressure at 4000 level while drawn but changes to 1000 when stroke is created
                    if (e.GetStylusPoints(((StudentInkCanvas)e.Source)).Last().GetPropertyValue(StylusPointProperties.NormalPressure)/4 > (((LoadingStroke)_expertStrokes[partitionCount]).MaxPressure + 50))
                    {
                        new Task(() => synthesizer.Speak("Pressure too high")).Start();
                        //Debug.WriteLine("Pressure "+ e.GetStylusPoints(((StudentInkCanvas)e.Source)).Last().GetPropertyValue(StylusPointProperties.NormalPressure));
                    }
                    if (e.GetStylusPoints(((StudentInkCanvas)e.Source)).Last().GetPropertyValue(StylusPointProperties.NormalPressure)/4 < (((LoadingStroke)_expertStrokes[partitionCount]).MinPressure - 50))
                    {
                        new Task(() => synthesizer.Speak("Pressure too low")).Start();
                    }
                    Globals.LastExecution = DateTime.Now;
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
            double threshold = 25d;
            //Point p = e.GetPosition(this); get the position of the stylus
            return expertStroke.HitTest(e.GetPosition((InkCanvas)e.Source), threshold);
            //return expertStroke.HitTest(e.GetStylusPoints((StudentInkCanvas)e.Source).First().ToPoint(), threshold);
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
            LoadingStroke ds;
            if (ExpertStrokes[partitionCount - 1] is LoadingStroke)
            {
                ds = (LoadingStroke)ExpertStrokes[partitionCount - 1];
                //ExpertMaxPressure.Add(ds.MaxPressure);
                //Debug.WriteLine("Student View model expert max: " + ds.MaxPressure);
                //ExpertMinPressure.Add(ds.MinPressure);
                //Debug.WriteLine("Student View model expert min: " + ds.MinPressure);
                args.ExpertMaxPressure = ds.MaxPressure;
                args.ExpertMinPressure = ds.MinPressure;
            }
            else
            {
                Debug.WriteLine("ExpertStrokes: "+ (ExpertStrokes[partitionCount - 1] is LoadingStroke));
            }
            OnMaxMinchanged(args);

        }
        #endregion
    }
}
