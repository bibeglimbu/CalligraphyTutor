using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;

namespace CalligraphyTutor.StylusPlugins
{
    class StylusPointsDependencyProperty : StylusPlugIn
    {
        public ObservableCollection<StylusPoint> SPCollection = new ObservableCollection<StylusPoint>();
        protected override void OnStylusMove(RawStylusInput rawStylusInput)
        {
            // Call the base class before modifying the data.
            base.OnStylusMove(rawStylusInput);

            // Restrict the stylus input.
            StoreStylusInput(rawStylusInput);
        }

        protected override void OnStylusUp(RawStylusInput rawStylusInput)
        {
            // Call the base class before modifying the data.
            base.OnStylusUp(rawStylusInput);

            // Restrict the stylus input
            SPCollection.Clear();
        }

        private void StoreStylusInput(RawStylusInput rawStylusInput)
        {
            foreach (StylusPoint sp in rawStylusInput.GetStylusPoints())
            {
                SPCollection.Add(sp);
            }
        }
    }
}
