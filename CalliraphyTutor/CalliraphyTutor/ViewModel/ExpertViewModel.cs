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

namespace CalligraphyTutor.ViewModel
{
    public class ExpertViewModel: BindableBase
    {
        #region VARS & Property
        private Button button = new Button();
        //screen size of the application
        private int _screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        public int ScreenWidth
        {
            get { return _screenWidth; }
            set
            {
                _screenWidth = value;
                RaisePropertyChanged();
            }
        }
        private int _screenHeight = (int)SystemParameters.PrimaryScreenHeight;
        public int ScreenHeight
        {
            get { return _screenHeight; }
            set
            {
                _screenHeight = value;
                RaisePropertyChanged();
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
        //color of the button
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

        //        public static readonly DependencyProperty ExpertStrokesProperty = DependencyProperty.RegisterAttached(
        //"ExpertStrokes", typeof(StrokeCollection), typeof(ExpertViewModel), new PropertyMetadata());

        //        public StrokeCollection ExpertStrokes
        //        {
        //            get { return (StrokeCollection)GetValue(ExpertStrokesProperty); }
        //            set { SetValue(ExpertStrokesProperty, value); }
        //        }

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

        private DispatcherTimer _timer = new DispatcherTimer();
        public DispatcherTimer UpdateTimer
        {
            get { return _timer; }
            set
            {
                _timer = value;
                RaisePropertyChanged("UpdateTimer");
            }
        }
        private int _text = 0;
        public int TestNumber
        {
            get { return _text; }
            set
            {
                _text = value;
                RaisePropertyChanged();
            }
        }

        private DrawingAttributes _expertAttributes = new DrawingAttributes();
        public DrawingAttributes ExpertAttributes
        {
            get { return _expertAttributes; }

            set
            {
                _expertAttributes = value;
                RaisePropertyChanged();
            }
        }

        LearningHubManager LHManager;
        Globals globals;

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

        public ExpertViewModel()
        {
            globals = Globals.Instance;
            LHManager = LearningHubManager.Instance;
            SetValueNames();
            LHManager.SendReady();
            LHManager.StartRecordingEvent += LHManager_StartRecordingEvent;
            LHManager.StopRecordingEvent += LHManager_StopRecordingEvent;

            ExpertAttributes.Width = 5d;
            ExpertAttributes.Height = 5d;

            UpdateTimer.Interval = new TimeSpan(1000);
            UpdateTimer.Tick += UpdateTimer_Tick;

        }

        private void LHManager_StopRecordingEvent(object sender, EventArgs e)
        {
            SendDebug( "stop");
            UpdateTimer.Stop();
            StartRecordingData();
        }

        private void LHManager_StartRecordingEvent(object sender, EventArgs e)
        {
            SendDebug( "start");
            UpdateTimer.Start();
            StartRecordingData();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
            TestNumber += 1;
            Debug.WriteLine(TestNumber);
        }

        #region Send data


        private void SetValueNames()
        {
                List<string> names = new List<string>();
                names.Add("PenPressure");
                names.Add("Tilt_X");
                names.Add("Tilt_Y");
                LHManager.SetValueNames(names);

        }
        private void SendData(StylusEventArgs args)
        {
                List<string> values = new List<string>();
                String pressure = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.NormalPressure).ToString();
                values.Add(pressure);
                String Xangle = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.XTiltOrientation).ToString();
                values.Add(Xangle);
                String Yangle = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.YTiltOrientation).ToString();
                values.Add(Yangle);
                //Debug.WriteLine(v);
                LHManager.StoreFrame(values);
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
            SendDebug(RecordButtonName.ToString());
            if (LHManager.ExpertIsRecording.Equals(false))
            {
                LHManager.ExpertIsRecording = true;
                Application.Current.Dispatcher.InvokeAsync(new Action(
                    () =>
                    {
                        RecordButtonName = "Stop Recording";
                        RecordButtonColor = new SolidColorBrush(Colors.Green);
                    }));
                if (globals.Speech.State != SynthesizerState.Speaking)
                {
                    globals.Speech.SpeakAsync("Expert Is recording" + LHManager.ExpertIsRecording.ToString());
                }
                
                ExpertStrokes.Clear();
                return;

            }
            else
            {
                LHManager.ExpertIsRecording = false;
                Application.Current.Dispatcher.InvokeAsync(new Action(
                    () =>
                    {
                        RecordButtonName = "Start Recording";
                        RecordButtonColor = new SolidColorBrush(Colors.White);
                    }));
                if (globals.Speech.State != SynthesizerState.Speaking)
                {
                    globals.Speech.SpeakAsync("Expert Is recording" + LHManager.ExpertIsRecording.ToString());
                }
                
                SaveStrokes();
                ExpertStrokes.Clear();
                return;
            }
        }

        public void SaveStrokes()
        {
            if (ExpertStrokes.Count != 0)
            {
                globals.GlobalFileManager.SaveStroke(ExpertStrokes);
            }
            else
            {
                Debug.WriteLine("Expert Canvas Strokes is: " + ExpertStrokes.Count);
            }

        }
        #endregion

        #region Events

        private ICommand _stylusMoved;
        public ICommand ExpertCanvas_OnStylusMoved
        {
            get
            {
                _stylusMoved = new RelayCommand(
                    param => OnStylusMoved(param),
                    null
                    );

                return _stylusMoved;
            }
        }

        private void OnStylusMoved(object param)
        {
            //run only of 1/4th of a sec has elapsed
            //if ((DateTime.Now - globals.LastExecution).TotalSeconds >= 0.1)
            //{
            
            //send data to learning hub

            if (LHManager.ExpertIsRecording == true)
            {
                try
                {
                    StylusEventArgs args = (StylusEventArgs)param;
                    SendData(args);
                    //if(globals.Speech.State != SynthesizerState.Speaking)
                    //{
                    //    globals.Speech.SpeakAsync("Expert data sent ");
                    //}
                }
                catch (Exception ex)
                {
                      Debug.WriteLine(ex.StackTrace);
                }
            }
            else
            {
                if (globals.Speech.State != SynthesizerState.Speaking)
                {
                    globals.Speech.SpeakAsync("Expert is recording " + LHManager.ExpertIsRecording.ToString());
                }
                
            }
            //globals.LastExecution = DateTime.Now;
            //}

            
        }

        public void SendDebug(string s)
        {
            DebugEventArgs args = new DebugEventArgs();
            args.message = s;
            OnDebugReceived(args);
        }
        #endregion
    }
}
