using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CalligraphyTutor.CustomInkCanvas;
using CalligraphyTutor.Managers;
using CalligraphyTutor.ViewModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace CalligraphyTutor
{

    public class MainWindowViewModel : ViewModelBase
    {
        #region Property

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

        private string _debugText = "DebugMessages will be displayed here ";
        public string DebugText
        {
            get { return _debugText; }
            set
            {
                _debugText += "\r\n" + value;
                RaisePropertyChanged("DebugText");
            }
        }

        private string _imageAddress = AppDomain.CurrentDomain.BaseDirectory + "\\Resources\\OU_Logo_White.png";
        public string ImageAddress
        {
            get { return _imageAddress; }
        }

        private bool expertbuttonenabled = true;
        public bool ExpertButtonIsEnabled
        {
            get { return expertbuttonenabled; }
            set
            {
                expertbuttonenabled = value;
                RaisePropertyChanged("ExpertButtonIsEnabled");
            }
        }

        private bool studentbuttonenabled = true;
        public bool StudentButtonIsEnabled
        {
            get { return studentbuttonenabled; }
            set
            {
                studentbuttonenabled = value;
                RaisePropertyChanged("StudentButtonIsEnabled");
            }
        }

        private ViewModelBase _currentViewModel;
        public ViewModelBase CurrentViewModel
        {
            get { return _currentViewModel; }
            set {
                _currentViewModel = value;
                RaisePropertyChanged("CurrentViewModel");
            }
        }

        #endregion

        #region Instantiations
        //private UserControlViewModels UCVW;
        private ExpertViewModel expertViewModel;
        private StudentViewModel studentViewModel;
        public IEnumerable<ViewModelBase> VMCollection;
        private SpeechManager mySpeechManager;
        /// <summary>
        /// Instantiation of the learning hub
        /// </summary>
        public static ConnectorHub.ConnectorHub myConnectorHub;
        #endregion

        #region RelayCommand
        public RelayCommand ExpertButtonCommand { get; set; }
        public RelayCommand StudentButtonCommand { get; set; }
        public RelayCommand CloseButtonCommand { get; set; }
        #endregion

        public MainWindowViewModel()
        {
            mySpeechManager = SpeechManager.Instance;
            myConnectorHub = new ConnectorHub.ConnectorHub();
            myConnectorHub.Init();

            VMCollection = new ViewModelBase[]
            {
                expertViewModel, studentViewModel
            };
            CurrentViewModel = this;

            ExpertButtonCommand = new RelayCommand(()=>OnNav("Expert"), false);
            StudentButtonCommand = new RelayCommand(() => OnNav("Student"), false);
            CloseButtonCommand = new RelayCommand(() => CloseApplication(), false);
            MessengerInstance.Register<string>(this,"DebugMessage",(debug => SetDebugText(debug)));

        }
 
        #region Native Methods

        public void OnNav(string destination)
        {
            DebugText = "Current ViewModel changed ";
            expertViewModel = null;
            studentViewModel = null;

            switch (destination)
            {
                case "Expert":
                    expertViewModel = new ExpertViewModel();
                    CurrentViewModel = expertViewModel;
                    ExpertButtonIsEnabled = false;
                    StudentButtonIsEnabled = true;
                    break;
                case "Student":
                    studentViewModel = new StudentViewModel();
                    CurrentViewModel = studentViewModel;
                    ExpertButtonIsEnabled = true;
                    StudentButtonIsEnabled = false;
                    break;
                case "MainWindow":
                default:
                    CurrentViewModel = this;
                    break;
            }
        }

        #endregion

        #region EventHandler
        /// <summary>
        /// Method updates the <see cref=" DebugText"/>
        /// </summary>
        /// <param name="s"></param>
        private void SetDebugText(String s)
        {
            DebugText = s;
        }

        private void CloseApplication()
        {
            System.Windows.Application.Current.Shutdown();
        }

        #endregion
    }
}
