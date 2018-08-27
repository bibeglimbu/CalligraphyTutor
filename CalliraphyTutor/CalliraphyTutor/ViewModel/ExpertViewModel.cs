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

        ConnectorHub.ConnectorHub myConnectorHub;
        Globals globals;
        private bool ExpertIsRecording = false;

        Guid timestamp = new Guid("12345678-9012-3456-7890-123456789013");
        List<DateTime> StrokeTime = new List<DateTime>();
        #endregion

        #region events
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

            ExpertAttributes.Width = 5d;
            ExpertAttributes.Height = 5d;
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
        private void initLearningHub()
        {
            myConnectorHub = new ConnectorHub.ConnectorHub();
            myConnectorHub.init();
            myConnectorHub.sendReady();
            myConnectorHub.startRecordingEvent += MyConnectorHub_startRecordingEvent;
            myConnectorHub.stopRecordingEvent += MyConnectorHub_stopRecordingEvent;
            SetValueNames();
        }

        private void SetValueNames()
        {
                List<string> names = new List<string>();
                names.Add("PenPressure");
                names.Add("Tilt_X");
                names.Add("Tilt_Y");
            myConnectorHub.setValuesName(names);

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
            myConnectorHub.storeFrame(values);
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
                if (globals.Speech.State != SynthesizerState.Speaking)
                {
                    globals.Speech.SpeakAsync("Expert Is recording" + ExpertIsRecording.ToString());
                }

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

        private ICommand _strokeCollected;
        public ICommand ExpertCanvas_OnStrokeCollected
        {
            get
            {
                _strokeCollected = new RelayCommand(
                    param => OnStrokeCollected(param),
                    null
                    );

                return _stylusMoved;
            }
        }

        private void OnStrokeCollected(Object param)
        {
            ((InkCanvasStrokeCollectedEventArgs)param).Stroke.AddPropertyData(timestamp, StrokeTime.ToArray());
        }

        private void OnStylusMoved(object param)
        {
            //run only of 1/4th of a sec has elapsed
            //if ((DateTime.Now - globals.LastExecution).TotalSeconds >= 0.1)
            //{
            StrokeTime.Add(DateTime.Now);

            //send data to learning hub
            if (ExpertIsRecording == true)
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
                    globals.Speech.SpeakAsync("Expert is recording " + ExpertIsRecording.ToString());
                }
                
            }
            //globals.LastExecution = DateTime.Now;
            //}

            
        }

        private void OnStylusInRange(object Param)
        {
            if (myConnectorHub == null)
            {
                initLearningHub();
            }

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
