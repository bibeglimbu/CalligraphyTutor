using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalligraphyTutor.Model
{
    static class HubConnector
    {
        private static ConnectorHub.ConnectorHub _myConnector;
        private static ConnectorHub.FeedbackHub _myFeedback;

        public static ConnectorHub.ConnectorHub myConnector {
            get {
                if (_myConnector == null)
                {
                    StartConnection();
                }
                return _myConnector;
            } }

        public static ConnectorHub.FeedbackHub myFeedback
        {
            get
            {
                if (_myFeedback == null)
                {
                    StartConnection();
                }
                return _myFeedback;
            }
        }


        public static void StartConnection()
        {
            _myConnector = new ConnectorHub.ConnectorHub();
            _myFeedback = new ConnectorHub.FeedbackHub();

            myConnector.init();
            myConnector.sendReady();
        }

        public static void SendData(List<string> values)
        {
            myConnector.storeFrame(values);
        }

        public static void SetValuesName(List<string> names)
        {
            myConnector.setValuesName(names);
        }
    }
}
