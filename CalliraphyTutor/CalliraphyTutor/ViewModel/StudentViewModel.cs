using CalligraphyTutor.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
                    //return the stylus point to be used as ref. Hit test is also performed with in this method.
                    StylusPoint expertSP = SelectExpertPoint(args, ExpertStrokes);

                    //calculate velocity
                    if(CalculateStudentStrokeVelocity(args)> CalculateAverageExpertStrokeVelocity(currentStroke) && (DateTime.Now - PlayDateTime).Seconds > 1.5)
                    {
                        playSound();
                    }

                        //check if the student is between the experts range, current issue with the pressure at 4000 level while drawn but changes to 1000 when stroke is created
                        if (studentSP.GetPropertyValue(StylusPointProperties.NormalPressure) > expertSP.GetPropertyValue(StylusPointProperties.NormalPressure) + 400)
                        {
                            //new Task(() => globals.Speech.SpeakAsync("Pressure too high")).Start();
                           //Debug.WriteLine("Pressure High" + studentSP.GetPropertyValue(StylusPointProperties.NormalPressure));
                        //if value is higher set the Expert pressure factor to -1 which produces darker color
                        ((StudentInkCanvas)args.Source).ExpertPressureFactor = -1;
                        }
                        else if (studentSP.GetPropertyValue(StylusPointProperties.NormalPressure) < expertSP.GetPropertyValue(StylusPointProperties.NormalPressure) - 400)
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

                    //send data to learning hub
                    if (this.StudentIsRecording == true)
                    {
                        try
                        {
                            SendDataAsync(args, expertSP);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("failed sending data: " + ex.StackTrace);
                        }
                    }
                    else
                    {
                        //if (globals.Speech.State != SynthesizerState.Speaking)
                        //{
                        //    globals.Speech.SpeakAsync("student is Recording" + this.StudentIsRecording);
                        //}

                    }
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
                myConnectorHub.init();
                myConnectorHub.sendReady();
                myConnectorHub.startRecordingEvent += MyConnectorHub_startRecordingEvent;
                myConnectorHub.stopRecordingEvent += MyConnectorHub_stopRecordingEvent;
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
        /// Calls the <see cref="LearningHubManager"/>'s SetValueNames method which assigns the variables names for storing
        /// </summary>
        private void SetValueNames()
        {
            List<string> names = new List<string>();
            names.Add("StrokeVelocity");
            names.Add("PenPressure");
            names.Add("Tilt_X");
            names.Add("Tilt_Y");
            names.Add("StrokeDeviation");
            myConnectorHub.setValuesName(names);
        }

        /// <summary>
        /// For calling the <see cref="SendData(StylusEventArgs, StylusPoint)"/> async
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        public async void SendDataAsync(StylusEventArgs args, StylusPoint expertPoint)
        {
            await Task.Run(() => { SendData(args, expertPoint); });
        }
        /// <summary>
        /// Method for sending data
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        private void SendData(StylusEventArgs args, StylusPoint expertPoint)
        {
            List<string> values = new List<string>();
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                
                double StrokeVelocity = CalculateStudentStrokeVelocity(args);
                if(Double.IsNaN(StrokeVelocity) || Double.IsInfinity(StrokeVelocity))
                {
                    StrokeVelocity = 0;
                    Globals.Instance.Speech.SpeakAsync("double is not a number");
                }
                values.Add(StrokeVelocity.ToString());
                String PenPressure = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.NormalPressure).ToString();
                values.Add(PenPressure);
                String Tilt_X = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.XTiltOrientation).ToString();
                values.Add(Tilt_X);
                String Tilt_Y = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.YTiltOrientation).ToString();
                values.Add(Tilt_Y);
                decimal StrokeDeviation = (decimal) CalcualteDistance(args.GetStylusPoints((InkCanvas)args.Source).Last().ToPoint(), expertPoint.ToPoint());
                values.Add(StrokeDeviation.ToString());

            }));
            myConnectorHub.storeFrame(values);
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

        /// <summary>
        /// Method to calculate distance 
        /// </summary>
        /// <param name="startingPoint"></param>
        /// <param name="finalPoint"></param>
        /// <returns></returns>
        public double CalcualteDistance(Point startingPoint, Point finalPoint )
        {
            double distance = Math.Sqrt(Math.Pow(Math.Abs(startingPoint.X - finalPoint.X), 2)
                    + Math.Pow(Math.Abs(startingPoint.Y - finalPoint.Y), 2));

            //divide the distance with PPI for the surface laptop to convert to inches and then multiply to change into mm
            double distancePPI = (distance / 200)* 25.4;
            return distancePPI;
        }

        /// <summary>
        /// Stroke that holds the stroke to which the current styluspoint belongs to
        /// </summary>
        Stroke currentStroke;
        /// <summary>
        /// Method for returning the nearest styluspoint in the expert stroke from the point at which the pen is standing.
        /// </summary>
        /// <param name="e">stylus args must be sent to cehange the dynamic rederer color</param>
        /// <param name="SC"></param>
        /// <returns></returns>
        private StylusPoint SelectExpertPoint(StylusEventArgs e, StrokeCollection SC)
        {
            // Styluspoint that is closest to the current position of the pen
            StylusPoint refStylusPoint = new StylusPoint();
            Point point = new Point(double.NegativeInfinity, double.NegativeInfinity);
            Application.Current.Dispatcher.Invoke(new Action(() => 
            {
                point = e.GetStylusPoints((InkCanvas)e.Source).Last().ToPoint();
            }));
            
            // Value for storing the distance from the pen to the closest expert stylus point
            double _strokeDeviation = -1.0d;

            foreach (Stroke s in SC)
            {
                //iterate through all the stylus point
                foreach (StylusPoint sp in s.StylusPoints)
                {
                    //if the point lies exactly on top and hit test returns true, exit the whole loop and return the point.
                    if (new Rect(sp.X, sp.Y, 10, 10).IntersectsWith(new Rect(point.X, point.Y, 10, 10)))
                    {
                        refStylusPoint = sp;
                        currentStroke = s;
                        //change the color of the dynamic renderer to black
                        ChangeStrokeColor(e, Colors.Green);
                        //return to exit from the whole method
                        return refStylusPoint;
                    }
                    //calculate the distance from the point to the pen
                    double tempDisplacement = CalcualteDistance(point, sp.ToPoint());
                    //if it is the first time the assign value and return
                    if (_strokeDeviation < 0)
                    {
                        _strokeDeviation = tempDisplacement;
                        refStylusPoint = sp;
                        currentStroke = s;
                        continue;
                    }
                    //if the new distance is smaller than the previous distance, store the value
                    if (tempDisplacement < _strokeDeviation)
                    {
                        _strokeDeviation = tempDisplacement;
                        refStylusPoint = sp;
                        currentStroke = s;
                    }
                }
                
            }
            //if none of the points return a hit, change color and return the closest sp
            ChangeStrokeColor(e, Colors.Red);
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
            ((StudentInkCanvas)e.Source).StrokeColor = color;
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
        /// play the audio asynchronously
        /// </summary>
        public async void playSound()
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(directory + @"\Sounds\Error.wav");
            PlayDateTime = DateTime.Now;
            await Task.Run(()=>player.PlaySync());
        }

        private Point initVelocityPoint;
        private DateTime initVelocityTime;
        private Point finalVelocityPoint;
        /// <summary>
        /// calculates the velocity of the stroke in seconds
        /// </summary>
        public double CalculateStudentStrokeVelocity(StylusEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                //if it is the first time running assign the inital point and retrun
                if (initVelocityPoint == null || initVelocityTime == null)
                {
                    initVelocityPoint = e.StylusDevice.GetStylusPoints((StudentInkCanvas)e.Source).Last().ToPoint();
                    initVelocityTime = DateTime.Now;
                    return;
                }
                //else assign the last point and 
                finalVelocityPoint = e.StylusDevice.GetStylusPoints((StudentInkCanvas)e.Source).Last().ToPoint();
            }));

            double velocity = CalcualteDistance(initVelocityPoint, finalVelocityPoint)/ (DateTime.Now - initVelocityTime).TotalSeconds;
            initVelocityPoint = finalVelocityPoint;
            initVelocityTime = DateTime.Now;
            return velocity;
            
        }

        //Guid expertTimestamp = new Guid("12345678-9012-3456-7890-123456789013");
        /// <summary>
        /// calculates the velocity of the stroke in seconds for expert stroke since the expert stroke could not be used in collection
        /// </summary>
        public double CalculateAverageExpertStrokeVelocity(Stroke s)
        {
            GuidAttribute IMyInterfaceAttribute = (GuidAttribute)Attribute.GetCustomAttribute(typeof(ExpertInkCanvas), typeof(GuidAttribute));
            Debug.WriteLine("IMyInterface Attribute: " + IMyInterfaceAttribute.Value);
            Guid expertTimestamp = new Guid(IMyInterfaceAttribute.Value);

            Debug.WriteLine("guids " + s.GetPropertyDataIds().Length);
            double totalStrokeLenght = 0;
            double velocity = 0;
            for (int i = 0; i < s.StylusPoints.Count - 1; i++)
            {
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
