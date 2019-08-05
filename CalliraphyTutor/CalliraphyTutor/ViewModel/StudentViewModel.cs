using CalligraphyTutor.CustomInkCanvas;
using CalligraphyTutor.Managers;
using CalligraphyTutor.Model;
using CalligraphyTutor.StylusPlugins;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CalligraphyTutor.ViewModel
{
    public class StudentViewModel: ViewModelBase
    {
        #region Vars & properties

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
        /// Color of the button for chaning the PreviousColor based on the state
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

        private bool speedIsChecked = true;
        public bool SpeedIsChecked
        {
            get { return speedIsChecked; }
            set
            {
                speedIsChecked = value;
                RaisePropertyChanged("SpeedIsChecked");
            }
        }
        private bool pressureIsChecked = true;
        public bool PressureIsChecked
        {
            get { return pressureIsChecked; }
            set
            {
                pressureIsChecked = value;
                RaisePropertyChanged("PressureIsChecked");
            }
        }
        private bool strokeIsChecked = true;
        public bool StrokeIsChecked
        {
            get { return strokeIsChecked; }
            set
            {
                strokeIsChecked = value;
                RaisePropertyChanged("StrokeIsChecked");
            }
        }

        private bool _isStylusDown = false;
        public bool IsStylusDown
        {
            get { return _isStylusDown; }
            set
            {
                _isStylusDown = value;
                RaisePropertyChanged("IsStylusDown");
            }
        }
        #endregion

        #region Instantiations
        /// <summary>
        /// Indicates if the data is being sent to the Learning hub.
        /// </summary>
        private bool StudentIsRecording = false;
        private bool ExpertStrokeLoaded = false;
        /// <summary>
        /// Audio based feedback
        /// </summary>
        SpeechManager mySpeechManager;
        /// <summary>
        /// Instantiation of the learning hub
        /// </summary>
        ConnectorHub.ConnectorHub myConnectorHub;
        // Declare a System.Threading.CancellationTokenSource.
        CancellationTokenSource cts;

        //expert data value holders
        private float PenPressure_Expert = 0f;
        //private float Tilt_X_Expert = 0f;
        //private float Tilt_Y_Expert = 0f;
        private double StrokeVelocity_Expert = 0d;
        #endregion

        #region RelayCommand
        public RelayCommand RecordButtonCommand { get; set; }
        public RelayCommand ClearButtonCommand { get; set; }
        public RelayCommand LoadButtonCommand { get; set; }
        public RelayCommand<StylusEventArgs> StylusDownEventCommand { get; set; }
        public RelayCommand<StylusEventArgs> StylusUpEventCommand { get; set; }
        public RelayCommand<StylusEventArgs> StylusMoveEventCommand { get; set; }
        #endregion

        #region send data
        //LogStylusDataPlugin logData;
        private float PenPressure_Student = 0f;
        private float Tilt_X_Student = 0f;
        private float Tilt_Y_Student = 0f;
        private double StrokeVelocity_Student = 0d;
        private double StrokeDeviation_Student = 0d;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentViewModel()
        {
            RecordButtonCommand = new RelayCommand(StartRecordingData);
            ClearButtonCommand = new RelayCommand(ClearStrokes);
            LoadButtonCommand = new RelayCommand(LoadStrokes);
            StylusDownEventCommand = new RelayCommand<StylusEventArgs>(OnStylusDown);
            StylusUpEventCommand = new RelayCommand<StylusEventArgs>(OnStylusUp);
            StylusMoveEventCommand = new RelayCommand<StylusEventArgs>(OnStylusMoved);
            mySpeechManager = SpeechManager.Instance;

            //LogStylusDataPlugin.StylusMoveProcessEnded += LogData_StylusMoveProcessEnded;
            HitStrokeTesterPlugin.StylusMoveProcessEnded += HitStrokeTesterPlugin_StylusMoveProcessEnded;
            ExpertInkCanvas.ExpertStrokeLoadedEvent += ExpertInkCanvas_ExpertStrokeLoadedEvent;
        }

        #region EventDefinition
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
        #endregion

        #region EventHandlers
        private void ExpertInkCanvas_ExpertStrokeLoadedEvent(object sender, ExpertInkCanvas.ExpertStrokeLoadedEventEventArgs e)
        {
            ExpertStrokeLoaded = e.state;
        }
       //private void LogData_StylusMoveProcessEnded(object sender, StylusMoveProcessEndedEventArgs e)
       // {
       //     //assign the values
       //     PenPressure_Student = e.Pressure;
       //     Tilt_X_Student = e.XTilt;
       //     Tilt_Y_Student = e.YTilt;
       //     StrokeVelocity_Student = e.StrokeVelocity;
       //     //send data to learning hub if the student is recording
       //     if (this.StudentIsRecording == true)
       //     {
       //         try
       //         {
       //             SendDataAsync();
       //         }
       //         catch (Exception ex)
       //         {
       //             Debug.WriteLine("failed sending data: " + ex.StackTrace);
       //         }
       //     }
        //}
        private void HitStrokeTesterPlugin_StylusMoveProcessEnded(object sender, HitStrokeTesterPlugin.StylusMoveProcessEndedEventArgs e)
        {
            //assign the values
            PenPressure_Expert = e.Pressure * 4;
            StrokeDeviation_Student = e.Deviation;
            //Tilt_X_Expert = e.XTilt;
            //Tilt_Y_Expert = e.YTilt;
            //send data to learning hub if the student is recording
            if (this.StudentIsRecording == true)
            {
                try
                {
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
            mySpeechManager.Speech.SpeakAsync("stop recording");
            StartRecordingData();
        }
        private void MyConnectorHub_startRecordingEvent(object sender)
        {
            mySpeechManager.Speech.SpeakAsync("start recording");
            StartRecordingData();
        }
        #endregion

        #region Icommand Overrides
        public void OnStylusDown(StylusEventArgs e)
        {
            cts = new CancellationTokenSource();
            Debug.WriteLine("PenDown");
            //StylusEventArgs args = (StylusEventArgs)param; 
        }
        private void OnStylusUp(StylusEventArgs e)
        {
            //cancel async task
            if (cts != null)
            {
                cts.Cancel();
            }
            //Debug.WriteLine("PenUp");
            //reset all value when the pen is lifted
            PenPressure_Student = 0f;
            Tilt_X_Student = 0f;
            Tilt_Y_Student = 0f;
            StrokeVelocity_Student = 0d;
            StrokeDeviation_Student = 0d;

        }
        /// <summary>
        /// value that holds the last iteration point to calculate the distance. Instantiated 
        /// </summary>
        private Point prevPoint = new Point(double.NegativeInfinity, double.NegativeInfinity);
        private void OnStylusMoved(Object param)
        {
            //cast the parameter as stylyus event args
            StylusEventArgs args = (StylusEventArgs)param;
            if (((StudentInkCanvas)(args.Source)).SpeedChecked == true && ExpertStrokes.Count !=0)
            {
                //add a value to the expert average velocity to provide a area of error
                Stroke ExpertStroke = SelectNearestExpertStroke(new Stroke(args.GetStylusPoints(((StudentInkCanvas)(args.Source)))),ExpertStrokes);
                //StrokeVelocity_Expert = CalculateAverageExpertStrokeVelocity(ExpertStroke);
                double ExpertVelocity = StrokeVelocity_Expert + 5;
                if (SpeedIsChecked == true && ExpertStrokeLoaded == true)
                {
                    if (StrokeVelocity_Student > ExpertVelocity + 5)
                    {
                        playSound(cts.Token);
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
            names.Add("StrokeVelocity_Student");
            names.Add("PenPressure_Student");
            names.Add("Tilt_X_Student");
            names.Add("Tilt_Y_Student");
            names.Add("StrokeDeviation_Student");
            names.Add("PenPressure_Expert");
            //names.Add("StrokeVelocity_Expert");
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
            values.Add(StrokeVelocity_Student.ToString());
            values.Add(PenPressure_Student.ToString());
            values.Add(Tilt_X_Student.ToString());
            values.Add(Tilt_Y_Student.ToString());
            values.Add(StrokeDeviation_Student.ToString());
            values.Add(PenPressure_Expert.ToString());
            //values.Add(StrokeVelocity_Expert.ToString());
            myConnectorHub.StoreFrame(values);
            //mySpeechManager.Speech.SpeakAsync("Student Data sent");
        }
        #endregion

        #region Methods called by the buttons
        public void ClearStrokes()
        {
            StudentStrokes.Clear();
        }
        private void LoadStrokes()
        {
            //when the stroke is loaded initiate the learning hub. Having it in constructors will not work
            initLearningHub();
            StrokeCollection tempStrokeCollection = new StrokeCollection();
            tempStrokeCollection = FileManager.Instance.LoadStroke();
            if(tempStrokeCollection==null || tempStrokeCollection.Count == 0)
            {
                Debug.WriteLine("No Strokes Found");
                return;
            }
            ExpertStrokes = tempStrokeCollection;
            //Debug.WriteLine("guids " + ExpertStrokes[ExpertStrokes.Count - 1].GetPropertyDataIds().Length);

            //start the timer
            //_studentTimer.Start();
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
                                RecordButtonColor = new SolidColorBrush(Colors.LightGreen);
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
            //mySpeechManager.Speech.SpeakAsync("Student is recording "+ this.StudentIsRecording);
        }

        #endregion


        /// <summary>
        /// calculates the velocity of the stroke in seconds for expert stroke since the expert stroke could not be used in collection
        /// </summary>
        public double GetExpertStrokeVelocity(Stroke s)
        {
            GuidAttribute IMyInterfaceAttribute = (GuidAttribute)Attribute.GetCustomAttribute(typeof(ExpertInkCanvas), typeof(GuidAttribute));
            //Debug.WriteLine("IMyInterface Attribute: " + IMyInterfaceAttribute.Value);
            Guid ExpertVelocity = new Guid(IMyInterfaceAttribute.Value);
            double strokeVelocity = 0d;

            if (s.ContainsPropertyData(ExpertVelocity))
            {
                object velocity = s.GetPropertyData(ExpertVelocity);

                if (velocity is double)
                {
                    strokeVelocity = (double)velocity;
                }

            }
            else
            {
                SendDebugMessage("The StrokeCollection does not have Velocity.");
            }

            return strokeVelocity;

        }

        /// <summary>
        /// Call this method if you want this debug message to be displayed in the UI
        /// </summary>
        /// <param name="debugmessage"></param>
        public void SendDebugMessage(String debugmessage)
        {
            MessengerInstance.Send(debugmessage, "DebugMessage");
        }

        /// <summary>
        /// returns the nearest expert stroke
        /// </summary>
        /// <param name="argsStroke"></param>
        /// <param name="expertSC"></param>
        /// <returns></returns>
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


        #region  Play Sound
        /// <summary>
        /// returns the bin folder in the directory
        /// </summary>
        //string directory = Environment.CurrentDirectory;
        //string directory = AppDomain.CurrentDomain.BaseDirectory;
        string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
            if ((DateTime.Now - PlayDateTime).TotalSeconds > 1.5)
            {
                PlayDateTime = DateTime.Now;
                await Task.Run(() => player.Play());
                Debug.WriteLine(PlayDateTime);

            }

        }

        #endregion
    }
}
