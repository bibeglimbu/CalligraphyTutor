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
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CalligraphyTutor.ViewModel
{
    public class StudentViewModel: BindableBase
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

        //for turing the popon on and off
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

        private bool _hitState = false;
        /// <summary>
        /// true if the current point is hitting the expert point
        /// </summary>
        public bool StudentHitState
        {
            get { return _hitState; }
            set
            {
                _hitState = value;
                RaisePropertyChanged("StudentHitState");
            }
        }

        bool _pressureChecked = true;
        /// <summary>
        /// Check if you want feedback on pressure
        /// </summary>
        public bool PressureIsChecked
        {
            get { return _pressureChecked; }
            set
            {
                _pressureChecked = value;
                RaisePropertyChanged("PressureIsChecked");
            }
        }

        bool _speedChecked = true;
        /// <summary>
        /// Check if you want feedback on speed
        /// </summary>
        public bool SpeedIsChecked
        {
            get { return _speedChecked; }
            set
            {
                _speedChecked = value;
                RaisePropertyChanged("SpeedIsChecked");
            }
        }

        bool _strokeChecked = true;
        /// <summary>
        /// Check if you want feedback on Stroke
        /// </summary>
        public bool StrokeIsChecked
        {
            get { return _strokeChecked; }
            set
            {
                _strokeChecked = value;
                RaisePropertyChanged("StrokeIsChecked");
            }
        }

        //LogStylusDataPlugin logData;
        private float PenPressure = 0f;
        private float Tilt_X = 0f;
        private float Tilt_Y = 0f;
        private double StrokeVelocity = 0d;
        private double StrokeDeviation = 0d;

        Globals globals;
        ConnectorHub.ConnectorHub myConnectorHub;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public StudentViewModel()
        {
            globals = Globals.Instance;
            LogStylusDataPlugin.StylusMoveProcessEnded += LogData_StylusMoveProcessEnded;
            StudentDynamicRenderer.StudentDeviationCalculatedEvent += StudentDynamicRenderer_StudentDeviationCalculatedEvent;
            ResultsViewModel.ButtonClicked += ResultsViewModel_ButtonClicked;
            //assign the variables to be rorded in the learning hub
        }

        private void StudentDynamicRenderer_StudentDeviationCalculatedEvent(object sender, StudentDynamicRenderer.StudentDeviationCalculatedEventArgs e)
        {
            StrokeDeviation = e.deviation;
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

        /// <summary>
        /// Event handlerfor handling onstylusmoveprocessedended events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogData_StylusMoveProcessEnded(object sender, StylusMoveProcessEndedEventArgs e)
        {
            //assign the values
            PenPressure = e.Pressure;
            Tilt_X = e.XTilt;
            Tilt_Y = e.YTilt;
            StrokeVelocity = e.StrokeVelocity;
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
            //StylusEventArgs args = (StylusEventArgs)param; 
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
            StrokeVelocity = 0d;
            StrokeDeviation = 0d;

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
            StrokeCollection tempStrokeCollection = new StrokeCollection();
            tempStrokeCollection = FileManager.Instance.LoadStroke();
            if(tempStrokeCollection==null || tempStrokeCollection.Count == 0)
            {
                Debug.WriteLine("No Strokes Found");
                return;
            }
            ExpertStrokes = tempStrokeCollection;
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
    }
}
