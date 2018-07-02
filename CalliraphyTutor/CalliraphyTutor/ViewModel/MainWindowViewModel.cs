using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

using CalligraphyTutor.View;
using CalligraphyTutor.Model;
using CalligraphyTutor.ViewModel;
using System.Diagnostics;

namespace CalligraphyTutor.ViewModel
{
    class MainWindowViewModel: BindableBase 
    {
        private int _screenWidth = (int)SystemParameters.PrimaryScreenWidth;
        public int ScreenWidth
        {
            get { return _screenWidth; }
            set
            {
                _screenWidth = value;
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
                OnPropertyChanged("ScreenHeight");
            }
        }

        private ExpertViewModel expertViewModel = new ExpertViewModel();
        private StudentViewModel studentViewModel = new StudentViewModel();

        private BindableBase _CurrentViewModel;
        public BindableBase CurrentViewModel
        {
            get { return _CurrentViewModel; }
            set { SetProperty(ref _CurrentViewModel, value); }
        }

        public MainWindowViewModel()
        {
            HubConnector.StartConnection();
            CurrentViewModel = this;
        }

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

        public void OnNav(string destination)
        {

            switch (destination)
            {
                case "Expert":
                    CurrentViewModel = expertViewModel;
                    break;
                case "Student":
                    CurrentViewModel = studentViewModel;
                    break;
                case "MainWindow":
                default:
                    CurrentViewModel = this;
                    break;
            }
        }
    }
}
