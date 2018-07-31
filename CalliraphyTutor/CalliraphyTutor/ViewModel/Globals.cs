﻿using CalligraphyTutor.Model;
using System;
using System.Collections.Generic;
using System.Speech.Synthesis;

namespace CalligraphyTutor.ViewModel
{
    public sealed class Globals
    {
        private static readonly Lazy<Globals> lazy = new Lazy<Globals>(() => new Globals());
        public static Globals Instance { get { return lazy.Value; } }

        private bool _isStylusDown = false;
        public bool IsStylusDown
        {
            get { return _isStylusDown; }
            set
            {
                _isStylusDown = value;
                
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



        //class for opening and saving to file
        private FileManager _fileManager = new FileManager();
        public FileManager GlobalFileManager
        {
            get { return _fileManager; }
        }

        private Globals()
        {
            _speech.Rate = 2;
        }

    }
}
