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
    class ResultsViewModel: BindableBase
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
                OnPropertyChanged("ExpertMaxText");
            }
        }
        private string expertMin = ".";
        public string ExpertMinText
        {
            get { return expertMin; }
            set
            {
                expertMin = value;
                OnPropertyChanged("ExpertMinText");
            }
        }
        private string studentMax = ".";
        public string StudentMaxText
        {
            get { return studentMax; }
            set
            {
                studentMax = value;
                OnPropertyChanged("StudentMaxText");
            }
        }
        private string studentMin = ".";
        public string StudentMinText
        {
            get { return studentMin; }
            set
            {
                studentMin = value;
                OnPropertyChanged("StudentMinText");
            }
        }

        #endregion

        public ResultsViewModel()
        {
            StudentViewModel.MaxMinChanged += StudentViewModel_MaxMinChanged;
        }

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
    }
}
