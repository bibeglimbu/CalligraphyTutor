using CalligraphyTutor.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using CalligraphyTutor.StylusPlugins;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using CalligraphyTutor.CustomInkCanvas;
using CalligraphyTutor.Managers;
using System.Windows.Threading;

namespace CalligraphyTutor.ViewModel
{
    public class ExpertViewModel: ViewModelBase
    {
        #region Property
        private int _screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        /// <summary>
        /// Used for stretching the canvas horizontally
        /// </summary>
        public int ScreenWidth
        {
            get { return _screenWidth; }
            set
            {
                _screenWidth = value;
                RaisePropertyChanged("ScreenWidth");
            }
        }

        private bool isChecked = true;
        /// <summary>
        /// Animation is disabled when checked
        /// </summary>
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                //Debug.WriteLine("IsChecked = " + IsChecked);
                RaisePropertyChanged();
            }
        }

        private StrokeCollection _expertStrokes = new StrokeCollection();
        /// <summary>
        /// Used for holding a reference to the strokes property of the inkcanvas
        /// </summary>
        public StrokeCollection ExpertStrokes
        {
            get { return _expertStrokes; }
            set
            {
                SendDebugMessage("ExpertStroke added");
                _expertStrokes = value;
                RaisePropertyChanged("ExpertStrokes");
            }
        }

        private string _debugMessage = "";
        /// <summary>
        /// Property that Updates the debug UI in the main window
        /// </summary>
        public string DebugMessage
        {
            get { return _debugMessage; }
            set {
                _debugMessage = value;
                SendDebugMessage(_debugMessage);
            }

        }

        private SpeechManager mySpeechManager = SpeechManager.Instance;

        private bool _loadButtonIsEnabled = false;
        public bool LoadButtonIsEnabled
        {
            get { return _loadButtonIsEnabled; }
            set
            {
                _loadButtonIsEnabled = value;
                RaisePropertyChanged("LoadButtonIsEnabled");
            }
        }

        private string _recordButtonName = "Start Recording";
        /// <summary>
        /// Used to switch the name of the button when pressed
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

        private Brush brush = new SolidColorBrush(Colors.White);
        /// <summary>
        /// Used to switch the color of the button when pressed
        /// </summary>
        public Brush RecordButtonColor
        {
            get { return brush; }
            set
            {
                brush = value;
                RaisePropertyChanged("RecordButtonColor");
            }
        }

        #endregion

        #region Vars
        /// <summary>
        /// holds state if the recroding button is clicked or not
        /// </summary>
        public bool ExpertIsRecording = false;

        #endregion

        #region ReplayCommand
        public RelayCommand<StylusEventArgs> StylusMoveEventCommand { get; set; }
        public RelayCommand<StylusEventArgs> StylusUpEventCommand { get; set; }
        public RelayCommand RecordButtonCommand { get; set; }
        public RelayCommand ClearButtonCommand { get; set; }
        public RelayCommand LoadButtonCommand { get; set; }
        #endregion

        #region StylusData
        private float PenPressure = 0f;
        private float Tilt_X = 0f;
        private float Tilt_Y = 0f;
        private double Pos_X = 0d;
        private double Pos_Y = 0d;
        //private double StrokeVelocity = 0d;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public ExpertViewModel()
        {
            StylusMoveEventCommand = new RelayCommand<StylusEventArgs>(OnStylusMoved);
            StylusUpEventCommand = new RelayCommand<StylusEventArgs>(OnStylusUp);
            RecordButtonCommand = new RelayCommand(StartRecordingData);
            ClearButtonCommand = new RelayCommand(ClearStrokes);
            try
            {
                InitLearningHub();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            
        }

        #region Methods called by the buttons
        public void ClearStrokes()
        {
            ExpertStrokes.Clear();
            SendDebugMessage("Canvas cleared");

        }

        public void SaveStrokes()
        {
            if (ExpertStrokes.Count != 0)
            {
                SendDebugMessage("guids " + ExpertStrokes[ExpertStrokes.Count-1].GetPropertyDataIds().Length);
                FileManager.Instance.SaveStroke(ExpertStrokes);
            }
            else
            {
                SendDebugMessage("Expert Canvas Strokes is: " + ExpertStrokes.Count);
            }

        }
        #endregion

        #region EventHandlers
        public void OnStylusUp(StylusEventArgs e)
        {
            //rest the values when the pen is put up.
            PenPressure = 0f;
            Tilt_X = 0f;
            Tilt_Y = 0f;
            //StrokeVelocity = 0d;
    }
        public void OnStylusMoved(StylusEventArgs e)
        {
            StylusPointCollection strokePoints = e.GetStylusPoints((UIElement)e.OriginalSource);
            if (strokePoints.Count == 0)
            {
                SendDebugMessage("No StylusPoints");
                return;
            }
            if (ExpertIsRecording == true)
            {
                Task.Run(() => {
                    foreach (StylusPoint sp in strokePoints)
                    {
                        PenPressure = sp.GetPropertyValue(StylusPointProperties.NormalPressure);
                        Tilt_X = sp.GetPropertyValue(StylusPointProperties.XTiltOrientation);
                        Tilt_Y = sp.GetPropertyValue(StylusPointProperties.YTiltOrientation);
                        Pos_X = sp.GetPropertyValue(StylusPointProperties.X);
                        Pos_Y = sp.GetPropertyValue(StylusPointProperties.Y);
                        //StrokeVelocity = 0d;
                        SendDataAsync();
                    }
                });

            }

        }

        private void MyConnectorHub_stopRecordingEvent(object sender)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
                        () =>
                        {
                            StopRecordingData();
                        }));
        }

        private void MyConnectorHub_startRecordingEvent(object sender)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(
                        () =>
                        {
                            StartRecordingData();
                        }));
        }
        #endregion

        #region Native Methods
        /// <summary>
        /// Call this method if you want this debug message to be displayed in the UI
        /// </summary>
        /// <param name="debugmessage"></param>
        public void SendDebugMessage(String debugmessage)
        {
            MessengerInstance.Send(debugmessage, "DebugMessage");
        }

        /// <summary>
        /// Called when recording is started
        /// </summary>
        private void StartRecordingData()
        {
            //SendDebug(RecordButtonName.ToString());
            if (ExpertIsRecording == false)
            {
                ExpertIsRecording = true;
                RecordButtonName = "Stop Recording";
                RecordButtonColor = new SolidColorBrush(Colors.LightGreen);
                ClearStrokes();   
            }
            if (mySpeechManager.Speech.State != SynthesizerState.Speaking)
            {
                mySpeechManager.Speech.SpeakAsync("Expert Is recording" + this.ExpertIsRecording.ToString());
            }

        }

        private void StopRecordingData()
        {
            if (ExpertIsRecording == true)
            {
                ExpertIsRecording = false;
                SaveStrokes();
                RecordButtonName = "Start Recording";
                RecordButtonColor = new SolidColorBrush(Colors.White);
                ClearStrokes();
            }
            if (mySpeechManager.Speech.State != SynthesizerState.Speaking)
            {
                mySpeechManager.Speech.SpeakAsync("Expert Is recording" + this.ExpertIsRecording.ToString());
            }
        }

        #endregion

        #region Send data
        /// <summary>
        /// initializes the learning hub
        /// </summary>
        private  void InitLearningHub()
        {
                SetValueNames();
                MainWindowViewModel.myConnectorHub.StartRecordingEvent += MyConnectorHub_startRecordingEvent;
                MainWindowViewModel.myConnectorHub.StopRecordingEvent += MyConnectorHub_stopRecordingEvent;
        }

        /// <summary>
        /// sets the value names that needs to be recorded
        /// </summary>
        private void SetValueNames()
        {
            List<string> names = new List<string>();
            //names.Add("StrokeVelocity");
            names.Add("PenPressure");
            names.Add("Tilt_X");
            names.Add("Tilt_Y");
            names.Add("Pos_X");
            names.Add("Pos_Y");
            MainWindowViewModel.myConnectorHub.SetValuesName(names);

        }

        /// <summary>
        /// For calling the <see cref="SendData(StylusEventArgs, StylusPoint)"/> async
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        public async void SendDataAsync()
        {
            await Task.Run(() => { SendData(); });
        }
        /// <summary>
        /// Method for sending data
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        private void SendData()
        {
            try
            {
                List<string> values = new List<string>();
                //values.Add(StrokeVelocity.ToString());
                values.Add(PenPressure.ToString());
                values.Add(Tilt_X.ToString());
                values.Add(Tilt_Y.ToString());
                values.Add(Pos_X.ToString());
                values.Add(Pos_Y.ToString());
                MainWindowViewModel.myConnectorHub.StoreFrame(values);
                
            }
            catch (Exception e)
            {
                SendDebugMessage("Sending Message Failed: "+e.Message);
            }

        }

        #endregion
    }
}
