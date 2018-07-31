using CalligraphyTutor.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CalligraphyTutor.ViewModel
{
    public class ResultsViewModel: BindableBase
    {
        #region property
        public ObservableCollection<Point> LineGraphsPoint = new ObservableCollection<Point>();

        private List<int> ExpertMaxPressure = new List<int>();
        private List<int> ExpertMinPressure = new List<int>();
        private List<int> StudentMaxPressure = new List<int>();
        private List<int> StudentMinPressure = new List<int>();
        private string expertMax = ".";
        public string ExpertMaxText
        {
            get { return expertMax; }
            set
            {
                expertMax = value;
                RaisePropertyChanged("ExpertMaxText");
            }
        }
        private string expertMin = ".";
        public string ExpertMinText
        {
            get { return expertMin; }
            set
            {
                expertMin = value;
                RaisePropertyChanged("ExpertMinText");
            }
        }
        private string studentMax = ".";
        public string StudentMaxText
        {
            get { return studentMax; }
            set
            {
                studentMax = value;
                RaisePropertyChanged("StudentMaxText");
            }
        }
        private string studentMin = ".";
        public string StudentMinText
        {
            get { return studentMin; }
            set
            {
                studentMin = value;
                RaisePropertyChanged("StudentMinText");
            }
        }
        StudentViewModel svm;
        #endregion

        public ResultsViewModel()
        {
            svm = new StudentViewModel();
            StudentViewModel.MaxMinChanged += StudentViewModel_MaxMinChanged;
        }

        #region EventsDefiniton
        public static event EventHandler<EventArgs> ButtonClicked;
        protected virtual void OnButtonClicked(EventArgs e)
        {
            EventHandler<EventArgs> handler =ButtonClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        #endregion

        #region Button Events
        private ICommand _buttonClicked;
        public ICommand CloseButton_clicked
        {
            get
            {
                _buttonClicked = new RelayCommand(
                    param => this.HideWindow(),
                    null
                    );

                return _buttonClicked;
            }
        }
        private void HideWindow()
        {
            Debug.WriteLine("Close pop up");
            OnButtonClicked(EventArgs.Empty);
        }

        #endregion

        #region native methods

        private void StudentViewModel_MaxMinChanged(object sender, StudentViewModel.MaxMinChangedEventArgs e)
        {
            ExpertMaxPressure.Add(e.ExpertMaxPressure);
            ExpertMinPressure.Add(e.ExpertMinPressure);
            StudentMaxPressure.Add(e.StudentMaxPressure);
            StudentMinPressure.Add(e.StudentMinPressure);
            DrawGraph();
        }

        public void DrawGraph()
        {
            //int x, y;
            //for (int i = 0; i < ExpertMaxPressure.Count; i++)
            //{
            //    x = (ExpertMaxPressure[i] / 4096) * 100;
            //    y = x + 10;
            //    LineGraphsPoint.Add(new Point(x, y));
            //}


            //LineGraph lg = new LineGraph();
            //lines.Children.Add(lg);
            //lg.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
            //lg.Description = String.Format("Data series {0}", i + 1);
            //lg.StrokeThickness = 2;
            //lg.Plot(x,y);

            //LineGraphs.Add(lg);

            ExpertMaxText = FeedText(ExpertMaxPressure);
            ExpertMinText = FeedText(ExpertMinPressure);
            StudentMaxText = FeedText(StudentMaxPressure);
            StudentMinText = FeedText(StudentMinPressure);

        }

        public string FeedText(List<int> oc)
        {
            Debug.WriteLine(oc.Count);
            string s = "";
            foreach (int i in oc)
            {
                s += i.ToString() + System.Environment.NewLine;
            }
            return s;
        }

        #endregion
    }
}
