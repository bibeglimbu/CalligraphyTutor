using CalligraphyTutor.Model;
using System;
using System.Collections.Generic;
using System.Speech.Synthesis;
using System.Windows.Ink;

namespace CalligraphyTutor.ViewModel
{
    public sealed class Globals
    {
        private static readonly Lazy<Globals> lazy = new Lazy<Globals>(() => new Globals());
        public static Globals Instance { get { return lazy.Value; } }



        private double _strokeWidth = 5d;
        public double StrokeWidth
        {
            get { return _strokeWidth; }
            set
            {
                _strokeWidth = value;

            }
        }

        private double _strokeHeight = 2.5d;
        public double StrokeHeight
        {
            get { return _strokeHeight; }
            set
            {
                _strokeHeight = value;

            }
        }

        private SpeechSynthesizer _speech = new SpeechSynthesizer();
        public SpeechSynthesizer Speech
        {
            get { return _speech; }
        }


        private DateTime _lastExecution = DateTime.Now;
        public DateTime LastExecution
        {
            get { return _lastExecution; }
            set
            {
                _lastExecution = value;
            }
        }

        private Globals()
        {
            _speech.Rate = 2;
        }

    }
}
