using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace CalligraphyTutor.ViewModel
{
    public sealed class LearningHubManager
    {
        private static readonly Lazy<LearningHubManager> lazy = new Lazy<LearningHubManager>(() => new LearningHubManager());
        public static LearningHubManager Instance { get { return lazy.Value; } }

        ConnectorHub.ConnectorHub myConnector;
        ConnectorHub.FeedbackHub myFeedback;

        Globals globals;

        public event EventHandler<DebugEventArgs> DebugReceived;
        private void OnDebugReceived(DebugEventArgs e)
        {
            EventHandler<DebugEventArgs> handler = DebugReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public class DebugEventArgs : EventArgs
        {
            public string message { get; set; }
        }

        private bool _expertIsRecording = false;
        public bool ExpertIsRecording
        {
            get { return _expertIsRecording; }
            set
            {
                _expertIsRecording = value;
            }
        }

        private LearningHubManager()
        {
            globals = Globals.Instance;
            Initialize();
        }

        public void Initialize ()
        {
            myConnector = new ConnectorHub.ConnectorHub();
            //myFeedback = new ConnectorHub.FeedbackHub();
            myConnector.init();
            //myFeedback.init();
            
        }

        public void SendReady()
        {
            StartListeningToHub();
            myConnector.sendReady();
        }

        #region events
        public event EventHandler<EventArgs> StartRecordingEvent;
        private void OnStartRecordingReceived(EventArgs e)
        {
            EventHandler<EventArgs> handler = StartRecordingEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<EventArgs> StopRecordingEvent;
        private void OnStopRecordingReceived(EventArgs e)
        {
            EventHandler<EventArgs> handler = StopRecordingEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        #endregion

        #region EventHandlers
        private void MyConnector_stopRecordingEvent(object sender)
        {
            SendDebug("start recording received");
            OnStopRecordingReceived(EventArgs.Empty);
        }

        private void MyConnector_startRecordingEvent(object sender)
        {
            SendDebug("start recording received");
            OnStartRecordingReceived(EventArgs.Empty);
        }
        #endregion

        public void StopListeningToHub()
        {
            Debug.WriteLine("ExpertModel: stopped listening");
            myConnector.startRecordingEvent -= MyConnector_startRecordingEvent;
            myConnector.stopRecordingEvent -= MyConnector_stopRecordingEvent;
        }

        public void StartListeningToHub()
        {
            SendDebug("ExpertModel: started listening");
            myConnector.startRecordingEvent += MyConnector_startRecordingEvent;
            myConnector.stopRecordingEvent += MyConnector_stopRecordingEvent;
        }

        public void SetValueNames(List<string> names)
        {
            try
            {
                myConnector.setValuesName(names);
                if (globals.Speech.State != SynthesizerState.Speaking)
                {
                    globals.Speech.SpeakAsync("Expert Values set ");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }

        public void StoreFrame(List<String> values)
        {
            try
            {
                myConnector.storeFrame(values);
                if (globals.Speech.State != SynthesizerState.Speaking)
                {
                    globals.Speech.SpeakAsync("Expert data sent");
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void SendDebug(string s)
        {
            DebugEventArgs args = new DebugEventArgs();
            args.message = s;
            OnDebugReceived(args);
        }
    }
}
