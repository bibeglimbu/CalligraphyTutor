using CalligraphyTutor.Model;
using CalligraphyTutor.StylusPlugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
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

        private bool _expertStrokesLoaded = false;
        /// <summary>
        /// Indicates if the <see cref="ExpertStrokes"/> has been loaded or not.
        /// </summary>
        public bool ExpertStrokeLoaded
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

        /// <summary>
        /// Indicates if the data is being sent to the Learning hub.
        /// </summary>
        private bool StudentIsRecording = false;


        Globals globals;
        ConnectorHub.ConnectorHub myConnectorHub;

        private bool _hitState = false;
        public bool StudentHitState
        {
            get { return _hitState; }
            set
            {
                _hitState = value;
                RaisePropertyChanged("StudentHitState");
            }
        }

        #endregion

        // Declare a System.Threading.CancellationTokenSource.
        CancellationTokenSource cts;

        //LogStylusDataPlugin logData;
        private float PenPressure = 0f;
        private float Tilt_X = 0f;
        private float Tilt_Y = 0f;
        private double StrokeVelocity = 0d;
        private double StrokeDeviation = -0.01d;

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentViewModel()
        {

            globals = Globals.Instance;
            _studentTimer.Interval = new TimeSpan(10000);
            _studentTimer.Tick += AnimationTimer_Tick;
            //logData = new LogStylusDataPlugin();

            LogStylusDataPlugin.StylusMoveProcessEnded += LogData_StylusMoveProcessEnded;
            ResultsViewModel.ButtonClicked += ResultsViewModel_ButtonClicked;
            //assign the variables to be rorded in the learning hub
            
        }

        /// <summary>
        /// Reference Expert Stroke
        /// </summary>
        Stroke expertStroke;
        /// <summary>
        /// Reference Expert StylusPoint
        /// </summary>
        StylusPoint expertStylusPoint;
        /// <summary>
        /// values for holding average expert velocity in each cycle
        /// </summary>
        private double ExpertVelocity = 0.0d;
        /// <summary>
        /// Event handlerfor handling onstylusmoveprocessedended events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogData_StylusMoveProcessEnded(object sender, StylusMoveProcessEndedEventArgs e)
        {
            //if the expert stroke is loaded
            if (ExpertStrokeLoaded == true)
            {
                //get the nearest stroke
                expertStroke = SelectNearestExpertStroke(e.StrokeRef, ExpertStrokes);
                if (expertStroke == null)
                {
                    Debug.WriteLine("expertstroke is null");
                    return;
                }
                //return the nearest expert stylus point to be used as ref. Hit test is also performed with in this method.
                expertStylusPoint = SelectExpertPoint(e.StrokeRef, expertStroke);
                if(expertStylusPoint == null)
                {
                    Debug.WriteLine("expertstylusPoint is null");
                    return;
                }
                //add a value to the expert average velocity to provide a area of error
                ExpertVelocity = CalculateAverageExpertStrokeVelocity(expertStroke) + 5;
                //this value has to be calculated for each cycle and stored. If calculated in repeated calls with in the same cycle it seems to return a different value
                if (e.StrokeVelocity > ExpertVelocity)
                    {
                        //Debug.WriteLine("Expert'argsStroke average Velocity; "+ ExpertVelocity);
                        //Debug.WriteLine("Student'argsStroke average Velocity; " + StudentSpeed);
                        playSound(cts.Token);
                    }
                }
            //send data to learning hub if the student is recording
            if (this.StudentIsRecording == true)
            {
                try
                {
                    //assign the values
                    PenPressure = e.Pressure;
                    Tilt_X = e.XTilt;
                    Tilt_Y = e.YTilt;
                    StrokeVelocity = e.StrokeVelocity;
                    SendDataAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("failed sending data: " + ex.StackTrace);
                }


            }
        }

        private void MyConnectorHub_stopRecordingEvent(object sender)
        {
            globals.Speech.SpeakAsync("stop recording");
            StartRecordingData();
        }

        private void MyConnectorHub_startRecordingEvent(object sender)
        {
            globals.Speech.SpeakAsync("start recording");
            StartRecordingData();
        }

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
            Debug.WriteLine("Feedback Received");
            //if (globals.Speech.State != SynthesizerState.Speaking)
            //{
            //    //ReadStream(feedback);
            //}
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

        private ICommand _StylusDown;
        /// <summary>
        /// Icommand method for binding to the event.
        /// </summary>
        public ICommand StudentInkCanvas_StylusDown
        {
            get
            {
                _StylusDown = new RelayCommand(
                    param => this.StudentView_OnStylusDown(param),
                    null
                    );

                return _StylusDown;
            }
        }
        private void StudentView_OnStylusDown(Object param)
        {
            Debug.WriteLine("PenDown");
            //start async task
            cts = new CancellationTokenSource();
            
        }

        private ICommand _StylusUp;
        /// <summary>
        /// Icommand method for binding to the event.
        /// </summary>
        public ICommand StudentInkCanvas_StylusUp
        {
            get
            {
                _StylusUp = new RelayCommand(
                    param => this.StudentView_OnStylusUp(param),
                    null
                    );

                return _StylusUp;
            }
        }
        private void StudentView_OnStylusUp(Object param)
        {
            Debug.WriteLine("PenUp");
            //reset all value when the pen is lifted
            PenPressure = 0f;
            Tilt_X = 0f;
            Tilt_Y = 0f;
            StrokeVelocity = -0.01d;
            StrokeDeviation = -0.01d;
            //cancel async task
            if (cts != null)
            {
                cts.Cancel();
            }
        }

        private ICommand _StylusMoved;
        /// <summary>
        /// Icommand method for binding to the event.
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
        /// <summary>
        /// value that holds the last iteration point to calculate the distance. Instantiated 
        /// </summary>
        private Point prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
        private void StudentView_OnStylusMoved(Object param)
        {

            //cast the parameter as stylyus event args
            StylusEventArgs args = (StylusEventArgs)param;
            //if there are no styluspoints, exit
            if (args.GetStylusPoints((InkCanvas)args.Source).Count == 0)
            {
                return;
            }
                
            //once the expert styluspoints are loaded
            if (ExpertStrokeLoaded == true)
            {
                //get the last point of the collection and the refernce expert point for it
                StylusPoint studentSP = args.GetStylusPoints((InkCanvas)args.Source).Last();

                //point for looping only at specific intervals
                //store the last point of the last interval
                Point pt = studentSP.ToPoint();
                Vector v = Point.Subtract(prevPoint, pt);

                //if the pen has moved a certain distance
                if (v.Length > 2)
                {


                        //check if the student is between the experts range, current issue with the pressure at 4000 level while drawn but changes to 1000 when stroke is created
                        if (studentSP.GetPropertyValue(StylusPointProperties.NormalPressure) > expertStylusPoint.GetPropertyValue(StylusPointProperties.NormalPressure) + 400)
                        {
                            //new Task(() => globals.Speech.SpeakAsync("Pressure too high")).Start();
                           //Debug.WriteLine("Pressure High" + studentSP.GetPropertyValue(StylusPointProperties.NormalPressure));
                        //if value is higher set the Expert pressure factor to -1 which produces darker color
                        ((StudentInkCanvas)args.Source).ExpertPressureFactor = -1;
                        }
                        else if (studentSP.GetPropertyValue(StylusPointProperties.NormalPressure) < expertStylusPoint.GetPropertyValue(StylusPointProperties.NormalPressure) - 400)
                        {
                            //new Task(() => globals.Speech.SpeakAsync("Pressure too low")).Start();
                            //Debug.WriteLine("Pressure Low" + studentSP.GetPropertyValue(StylusPointProperties.NormalPressure));
                        //if value is higher set the Expert pressure factor to +1 which produces lighter color
                        ((StudentInkCanvas)args.Source).ExpertPressureFactor = 1;
                    }
                    else
                    {
                        //if the value is with in the range set it to 0 which produces no change
                        ((StudentInkCanvas)args.Source).ExpertPressureFactor = 0;
                    }

             //reset the point
                    prevPoint = pt;
                }
            }
        }
        #endregion

        #region Send data

        private async void initLearningHub()
        {
            await Task.Run(() =>
            {
                myConnectorHub = new ConnectorHub.ConnectorHub();
                myConnectorHub.Init();
                myConnectorHub.SendReady();
                myConnectorHub.StartRecordingEvent += MyConnectorHub_startRecordingEvent;
                myConnectorHub.StopRecordingEvent += MyConnectorHub_stopRecordingEvent;
                SetValueNames();
            });
        }

        public void SaveStrokes()
        {
            if (StudentStrokes.Count != 0)
            {
                FileManager.Instance.SaveStroke(StudentStrokes);
            }
            else
            {
                Debug.WriteLine("Number of Student Strokes is: " + StudentStrokes.Count);
            }

        }

        /// <summary>
        /// Calls the <see cref="LearningHubManager"/>'argsStroke SetValueNames method which assigns the variables names for storing
        /// </summary>
        private void SetValueNames()
        {
            List<string> names = new List<string>();
            names.Add("StrokeSpeed");
            names.Add("PenPressure");
            names.Add("Tilt_X");
            names.Add("Tilt_Y");
            names.Add("StrokeDeviation");
            myConnectorHub.SetValuesName(names);
        }

        /// <summary>
        /// For calling the <see cref="SendData(StylusEventArgs, StylusPoint)"/> async
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        public async void SendDataAsync()
        {
            await Task.Run(() => SendData());
        }
        /// <summary>
        /// Method for sending data
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        private void SendData()
        {
            List<string> values = new List<string>();
            values.Add(StrokeVelocity.ToString());
            values.Add(PenPressure.ToString());
            values.Add(Tilt_X.ToString());
            values.Add(Tilt_Y.ToString());
            values.Add(StrokeDeviation.ToString());
            myConnectorHub.StoreFrame(values);
            //globals.Speech.SpeakAsync("Student Data sent");
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
            //when the stroke is loaded initiate the learning hub. Having it in constructors will not work
            initLearningHub();
            ExpertStrokes = new StrokeCollection(FileManager.Instance.LoadStroke());
            Debug.WriteLine("guids " + ExpertStrokes[ExpertStrokes.Count - 1].GetPropertyDataIds().Length);
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
            if (this.StudentIsRecording==false)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new System.Action(
                            () =>
                            {
                                if (ExpertStrokeLoaded == false)
                                {
                                    LoadStrokes();
                                }
                                StudentStrokes.Clear();
                                this.StudentIsRecording = true;
                                RecordButtonName = "Stop Recording";
                                RecordButtonColor = new SolidColorBrush(Colors.Green);
                                StayOpen = false;
                            }));

            }
            else 
            {
                Application.Current.Dispatcher.InvokeAsync(new Action(
                () =>
                {
                    this.StudentIsRecording = false;
                    RecordButtonName = "Start Recording";
                    SaveStrokes();
                    StudentStrokes.Clear();
                    RecordButtonColor = new SolidColorBrush(Colors.White);
                    //StayOpen = true;
                }));
            }
            //globals.Speech.SpeakAsync("Student is recording "+ this.StudentIsRecording);
        }

        #endregion

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
                if (argsStroke.HitTest(sp.ToPoint(), 5))
                    {
                    //return the stylus point
                        refStylusPoint = sp;
                    //assing StrokeDeviation as 0
                        StrokeDeviation = 0;
                    //change the student hit state to true
                        StudentHitState = true;
                    //exit the wole loop
                        return refStylusPoint;
                    }
                //if the point does not intersect the expert stroke, check if the new distance is smaller than the previous distance
                if (distance < StrokeDeviation)
                    {
                    //assign the new shorter distance, and assign the new point as ref point
                        StrokeDeviation = distance;
                        refStylusPoint = sp;
                    }
                }
            //if none of the points return a hit, change color and return the closest sp
            //ChangeStrokeColor(e, Colors.Red);
            StudentHitState = false;
            return refStylusPoint;
        }

        /// <summary>
        /// Variable that prevents assignment of colors when there is no change of stroke hit state
        /// </summary>
        private Color _previousColor = Colors.Green;
        /// <summary>
        /// Method for changing color
        /// </summary>
        /// <param name="e"></param>
        /// <param name="color"></param>
        public void ChangeStrokeColor(StylusEventArgs e, Color color)
        {
            string directory = Environment.CurrentDirectory;
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(directory + @"\Sounds\Error.wav");
            if (color == _previousColor)
            {
                return;
            }
            //((StudentInkCanvas)e.Source).StrokeColor = color;
            _previousColor = color;
        }

        /// <summary>
        /// returns the bin folder in the directory
        /// </summary>
        string directory = Environment.CurrentDirectory;

        /// <summary>
        /// variable used to calculate if the sound should be played.
        /// </summary>
        DateTime PlayDateTime = DateTime.Now;

        /// <summary>
        /// play the audio asynchronously. the cancellation token cancells the lined up async task for this method
        /// </summary>
        public async void playSound(CancellationToken ct)
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(directory + @"\sounds\Error.wav");
            if((DateTime.Now-PlayDateTime).TotalSeconds > 1.5)
            {
                PlayDateTime = DateTime.Now;
                await Task.Run(() => player.PlaySync());
                Debug.WriteLine(PlayDateTime);
                
            }
            
        }

        //Guid expertTimestamp = new Guid("12345678-9012-3456-7890-123456789013");
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
    }
}
