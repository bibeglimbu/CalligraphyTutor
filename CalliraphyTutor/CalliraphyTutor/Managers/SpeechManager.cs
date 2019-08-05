
using System;
using System.Speech.Synthesis;

namespace CalligraphyTutor.Managers
{
    //lazy implementation of the Speech Manager to ensure that only one instance of the audio is played at a time
    public sealed class SpeechManager
    {
        private static readonly Lazy<SpeechManager> lazy = new Lazy<SpeechManager>(() => new SpeechManager());
        public static SpeechManager Instance { get { return lazy.Value; } }

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

        private SpeechManager()
        {
            _speech.Rate = 2;
        }

    }
}
