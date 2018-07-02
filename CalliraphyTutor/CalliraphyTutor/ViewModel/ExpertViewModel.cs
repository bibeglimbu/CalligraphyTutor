using CalligraphyTutor.Model;
using System;
using System.Collections.Generic;
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

namespace CalligraphyTutor.ViewModel
{
    class ExpertViewModel: BindableBase
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
        private Brush brush = new SolidColorBrush(Colors.White);
        public Brush RecordButtonColor
        {
            get { return brush; }
            set
            {
                brush = value;
                OnPropertyChanged("RecordButtonColor");
            }
        }

        private StrokeCollection _expertStrokes = new StrokeCollection();
        public StrokeCollection ExpertStrokes
        {
            get { return _expertStrokes; }
            set
            {
                _expertStrokes = value;
                OnPropertyChanged("RecordButtonColor");
            }
        }

        #endregion

        public ExpertViewModel()
        {
            HubConnector.myConnector.startRecordingEvent += MyConnector_startRecordingEvent;
            HubConnector.myConnector.stopRecordingEvent += MyConnector_stopRecordingEvent;
        }

        

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
            ExpertStrokes.Clear();

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

        public void StartRecordingData()
        {

            if (Globals.IsRecording == false)
            {
                ExpertStrokes.Clear();
                Globals.IsRecording = true;
                RecordButtonName = "Stop Recording";
                RecordButtonColor = new SolidColorBrush(Colors.Green);

            }
            else if (Globals.IsRecording == true)
            {
                Globals.IsRecording = false;
                RecordButtonName = "Start Recording";
                SaveStrokes();
                ExpertStrokes.Clear();
                RecordButtonColor = new SolidColorBrush(Colors.White);
            }
        }

        public void SaveStrokes()
        {
            if (ExpertStrokes.Count != 0)
            {
                Globals.GlobalFileManager.SaveStroke(ExpertStrokes);
            }
            else
            {
                Debug.WriteLine("Expert Canvas Strokes is: " + ExpertStrokes.Count);
            }

        }
        #endregion

        #region Events
        
        private void MyConnector_startRecordingEvent(Object sender)
        {
            Debug.WriteLine("start");
            setValueNames();
            StartRecordingData();
        }

        private void MyConnector_stopRecordingEvent(Object sender)
        {
            Debug.WriteLine("stop");
            StartRecordingData();

        }

        private ICommand _stylusMoved;
        public ICommand ExpertCanvas_OnStylusMoved
        {
            get
            {
                _stylusMoved = new RelayCommand(
                    param => this.OnStylusMoved(param),
                    null
                    );

                return _stylusMoved;
            }
        }

        private void OnStylusMoved(object param)
        {
            //run only of 1/4th of a sec has elapsed
            if ((DateTime.Now - Globals.LastExecution).TotalSeconds >= 0.02)
            {
                Debug.WriteLine("Globals.IsRecording"+ Globals.IsRecording);
                //send data to learning hub
                if (Globals.IsRecording == true)
                {
                    try
                    {
                            StylusEventArgs args = (StylusEventArgs)param;
                            SendData(args);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }

            Globals.LastExecution = DateTime.Now;
        }
        #endregion
    }
}
