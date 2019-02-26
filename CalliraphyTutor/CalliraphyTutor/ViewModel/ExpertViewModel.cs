using CalligraphyTutor.Model;
using CalligraphyTutor.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using System.Runtime.InteropServices;
using CalligraphyTutor.StylusPlugins;

namespace CalligraphyTutor.ViewModel
{
    public class ExpertViewModel: BindableBase
    {
        #region VARS & Property

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
        //name of the button
        private string _recordButtonName = "Start Recording";
        public String RecordButtonName
        {
            get { return _recordButtonName; }
            set
            {
                    _recordButtonName = value;
                    RaisePropertyChanged();
            }
        }
        //PreviousColor of the button
        private Brush brush = new SolidColorBrush(Colors.White);
        public Brush RecordButtonColor
        {
            get { return brush; }
            set
            {
                    brush = value;
                    RaisePropertyChanged();
            }
        }

        private bool isChecked = true;
        /// <summary>
        /// enables evaluation mode where no feedback is provided at all.
        /// </summary>
        public bool IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                Debug.WriteLine("IsChecked = " + IsChecked);
                RaisePropertyChanged();
            }
        }

        private float PenPressure = 0f;
        private float Tilt_X = 0f;
        private float Tilt_Y = 0f;
        private double StrokeVelocity = 0d;

        private StrokeCollection _expertStrokes = new StrokeCollection();
        public StrokeCollection ExpertStrokes
        {
            get { return _expertStrokes; }
            set
            {
                Debug.WriteLine("ExpertStroke added");
                _expertStrokes = value;
                RaisePropertyChanged();
            }
        }

        ConnectorHub.ConnectorHub myConnectorHub;
        Globals globals;

        /// <summary>
        /// holds state if the recroding button is clicked or not
        /// </summary>
        private bool ExpertIsRecording = false;

        #endregion

        public ExpertViewModel()
        {
            globals = Globals.Instance;
            LogStylusDataPlugin.StylusMoveProcessEnded += LogStylusDataPlugin_StylusMoveProcessEnded;   
        }

        private void LogStylusDataPlugin_StylusMoveProcessEnded(object sender, StylusMoveProcessEndedEventArgs e)
        {
            //send data to learning hub if the student is recording
            if (this.ExpertIsRecording == true)
            {
                    //assign the values
                    PenPressure = e.Pressure;
                    Tilt_X = e.XTilt;
                    Tilt_Y = e.YTilt;
                    StrokeVelocity = e.StrokeVelocity;
                    SendDataAsync();

            }
        }

        private void MyConnectorHub_stopRecordingEvent(object sender)
        {
            //SendDebug("stop");
            StartRecordingData();
        }

        private void MyConnectorHub_startRecordingEvent(object sender)
        {
            //SendDebug("start");
            StartRecordingData();
        }

        #region Send data
        /// <summary>
        /// initializes the learning hub
        /// </summary>
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

        /// <summary>
        /// sets the value names that needs to be recorded
        /// </summary>
        private void SetValueNames()
        {
            List<string> names = new List<string>();
            names.Add("StrokeVelocity_Student");
            names.Add("PenPressure_Student");
            names.Add("Tilt_X_Student");
            names.Add("Tilt_Y_Student");

            myConnectorHub.SetValuesName(names);

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
                values.Add(StrokeVelocity.ToString());
                values.Add(PenPressure.ToString());
                values.Add(Tilt_X.ToString());
                values.Add(Tilt_Y.ToString());
                myConnectorHub.StoreFrame(values);
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.StackTrace);
            }

        }

        #endregion

        #region Methods called by the buttons
        private ICommand _buttonClicked;
        public void ClearStrokes()
        {
            ExpertStrokes.Clear();

        }
        public ICommand ClearButton_clicked
        {
            get
            {
                _buttonClicked = new RelayCommand(
                    param => ClearStrokes(),
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
                    param => StartRecordingData(),
                    null
                    );

                return _buttonClicked;
            }
        }

        private void StartRecordingData()
        {
            //SendDebug(RecordButtonName.ToString());
            if (ExpertIsRecording.Equals(false))
            {
                
                Application.Current.Dispatcher.InvokeAsync(new Action(
                    () =>
                    {
                        ExpertIsRecording = true;
                        RecordButtonName = "Stop Recording";
                        RecordButtonColor = new SolidColorBrush(Colors.Green);
                        ExpertStrokes.Clear();
                    }));
                if (globals.Speech.State != SynthesizerState.Speaking)
                {
                    globals.Speech.SpeakAsync("Expert Is recording" + ExpertIsRecording.ToString());
                }
               
            }
            else
            {
                Application.Current.Dispatcher.InvokeAsync(new Action(
                    () =>
                    {
                        SaveStrokes();
                        ExpertIsRecording = false;
                        RecordButtonName = "Start Recording";
                        RecordButtonColor = new SolidColorBrush(Colors.White);
                        ExpertStrokes.Clear();
                    }));
                if (globals.Speech.State != SynthesizerState.Speaking )
                {
                    globals.Speech.SpeakAsync("Expert Is recording" + ExpertIsRecording.ToString());
                }

            }
        }

        public void SaveStrokes()
        {
            if (ExpertStrokes.Count != 0)
            {
                Debug.WriteLine("guids " + ExpertStrokes[ExpertStrokes.Count-1].GetPropertyDataIds().Length);
                FileManager.Instance.SaveStroke(ExpertStrokes);
            }
            else
            {
                Debug.WriteLine("Expert Canvas Strokes is: " + ExpertStrokes.Count);
            }

        }
        #endregion

        #region Events
        private ICommand _stylusDown;
        public ICommand ExpertCanvas_OnStylusDown
        {
            get
            {
                _stylusDown = new RelayCommand(
                    param => OnStylusDown(param),
                    null
                    );

                return _stylusDown;
            }
        }
        private ICommand _stylusInRange;
        public ICommand ExpertCanvas_StylusInRange
        {
            get
            {
                _stylusInRange = new RelayCommand(
                    param => OnStylusInRange(param),
                    null
                    );

                return _stylusInRange;
            }
        }

        private void OnStylusInRange(object Param)
        {
            if (myConnectorHub == null)
            {
                initLearningHub();
            }

        }

        private void OnStylusDown(object Param)
        {
            StylusEventArgs args = (StylusEventArgs)Param;
            if (IsChecked == true)
            {
                ((ExpertInkCanvas)args.Source).DisplayAnimation = false;
            }
            else
            {
                ((ExpertInkCanvas)args.Source).DisplayAnimation = true;
            }

        }
        #endregion
    }
}
