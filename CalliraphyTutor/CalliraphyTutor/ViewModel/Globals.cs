using CalligraphyTutor.Model;
using System;

namespace CalligraphyTutor.ViewModel
{
    public static class Globals
    {
        private static bool _isRecording = false;
        public static bool IsStylusDown=false;

        public static bool IsRecording
        {
            get { return _isRecording; }
            set
            {
                _isRecording = value;
            }
        }

        private static DateTime _lastExecution = DateTime.Now;
        public static DateTime LastExecution
        {
            get { return _lastExecution; }
            set
            {
                _lastExecution = value;
            }
        }

        //class for opening and saving to file
        private static FileManager _fileManager = new FileManager();
        public static FileManager GlobalFileManager
        {
            get { return _fileManager; }
        }
    }
}
