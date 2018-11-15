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
                myConnectorHub.init();
                myConnectorHub.sendReady();
                myConnectorHub.startRecordingEvent += MyConnectorHub_startRecordingEvent;
                myConnectorHub.stopRecordingEvent += MyConnectorHub_stopRecordingEvent;
                SetValueNames();
            });
        }

        /// <summary>
        /// sets the value names that needs to be recorded
        /// </summary>
        private void SetValueNames()
        {
            List<string> names = new List<string>();
            names.Add("PenPressure");
            names.Add("Tilt_X");
            names.Add("Tilt_Y");
            names.Add("StrokeVelocity");
            myConnectorHub.setValuesName(names);

        }
        
        /// <summary>
        /// For calling the <see cref="SendData(StylusEventArgs, StylusPoint)"/> async
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        public async void SendDataAsync(StylusEventArgs args)
        {
            await Task.Run(() => { SendData(args); });
        }
        /// <summary>
        /// Method for sending data
        /// </summary>
        /// <param name="args"></param>
        /// <param name="expertPoint"></param>
        private void SendData(StylusEventArgs args)
        {
            List<string> values = new List<string>();
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                String PenPressure = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.NormalPressure).ToString();
                values.Add(PenPressure);
                String Tilt_X = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.XTiltOrientation).ToString();
                values.Add(Tilt_X);
                String Tilt_Y = args.GetStylusPoints((InkCanvas)args.Source).Last().GetPropertyValue(StylusPointProperties.YTiltOrientation).ToString();
                values.Add(Tilt_Y);
                double StrokeVelocity = CalculateStrokeVelocity(args);
                if(Double.IsNaN(StrokeVelocity) || Double.IsInfinity(StrokeVelocity))
                {
                    StrokeVelocity = 0;
                }
                values.Add(StrokeVelocity.ToString());
            }));
            myConnectorHub.storeFrame(values);
            //globals.Speech.SpeakAsync("Student Data sent");
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

        private void OnStylusMoved(object param)
        {

            //send data to learning hub if recording button is clicked
            if (ExpertIsRecording == true)
            {
                try
                {
                    StylusEventArgs args = (StylusEventArgs)param;
                    SendDataAsync(args);

                }
                catch (Exception ex)
                {
                      Debug.WriteLine(ex.StackTrace);
                }
            }
        }

        private void OnStylusInRange(object Param)
        {
            if (myConnectorHub == null)
            {
                initLearningHub();
            }

        }

        /// <summary>
        /// Method to calculate distance 
        /// </summary>
        /// <param name="startingPoint"></param>
        /// <param name="finalPoint"></param>
        /// <returns></returns>
        public double CalcualteDistance(Point startingPoint, Point finalPoint)
        {
            double distance = Math.Sqrt(Math.Pow(Math.Abs(startingPoint.X - finalPoint.X), 2)
                    + Math.Pow(Math.Abs(startingPoint.Y - finalPoint.Y), 2));

            //divide the distance with PPI for the surface laptop to convert to inches and then multiply to change into mm
            double distancePPI = (distance / 200) * 25.4;
            return distancePPI;
        }

        private Point initVelocityPoint;
        private DateTime initVelocityTime;
        private Point finalVelocityPoint;
        /// <summary>
        /// calculates the velocity of the stroke in seconds
        /// </summary>
        public double CalculateStrokeVelocity(StylusEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                //if it is the first time running assign the inital point and retrun
                if (initVelocityPoint == null || initVelocityTime == null)
                {
                    initVelocityPoint = e.StylusDevice.GetStylusPoints((ExpertInkCanvas)e.Source).Last().ToPoint();
                    initVelocityTime = DateTime.Now;
                    return;
                }
                //else assign the last point and 
                finalVelocityPoint = e.StylusDevice.GetStylusPoints((ExpertInkCanvas)e.Source).Last().ToPoint();
            }));

            double velocity = CalcualteDistance(initVelocityPoint, finalVelocityPoint) / (DateTime.Now - initVelocityTime).Milliseconds;
            initVelocityPoint = finalVelocityPoint;
            initVelocityTime = DateTime.Now;
            return velocity;

        }
        #endregion
    }
}
