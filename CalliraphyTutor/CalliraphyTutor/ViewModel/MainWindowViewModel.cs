using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CalligraphyTutor.Model;

namespace CalligraphyTutor.ViewModel
{
    public class MainWindowViewModel: BindableBase 
    {
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

        private string _debugText = "DebugText ";
        public string DebugText
        {
            get { return _debugText; }
            set
            {
               _debugText += "\r\n" + value;
                RaisePropertyChanged("DebugText");
            }
        }

        public class DebugEventArgs : EventArgs
        {
            public string message { get; set; }
        }

        private BindableBase _CurrentViewModel;
        public BindableBase CurrentViewModel
        {
            get { return _CurrentViewModel; }
            set { SetProperty(ref _CurrentViewModel, value); }
        }

        //private UserControlViewModels UCVW;
        private ExpertViewModel expertViewModel;
        private StudentViewModel studentViewModel;
        private MainViewModel mainViewModel;
        private Globals globals;


        public MainWindowViewModel()
        {
            globals = Globals.Instance;
            mainViewModel = new MainViewModel();
            CurrentViewModel = mainViewModel;
        }




        //assign all the variables here for recording


        private ICommand _buttonClicked;
        public ICommand ExpertButton_clicked
        {
            get
            {
                _buttonClicked = new RelayCommand(
                    param => this.OnNav("Expert"),
                    null
                    );

                return _buttonClicked;
            }
        }
        public ICommand StudentButton_clicked
        {
            get
            {
                _buttonClicked = new RelayCommand(
                    param => this.OnNav("Student"),
                    null
                    );

                return _buttonClicked;
            }
        }
        public ICommand CloseButton_clicked
        {
            get
            {
                _buttonClicked = new RelayCommand(
                    param => this.CloseApplication(param),
                    null
                    );

                return _buttonClicked;
            }
        }

        private void CloseApplication(Object window)
        {
            ((Window)window).Close();
        }

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
                    break;
                case "Student":
                    studentViewModel = new StudentViewModel();
                    studentViewModel.DebugReceived += StudentViewModel_DebugReceived;
                    CurrentViewModel = studentViewModel;
                    break;
                case "MainWindow":
                default:
                    CurrentViewModel = this;
                    break;
            }
        }

        private void StudentViewModel_DebugReceived(object sender, StudentViewModel.DebugEventArgs e)
        {
            DebugText = e.message;
        }
    }
}
