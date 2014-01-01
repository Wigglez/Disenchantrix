using System;
using System.Windows.Forms;

namespace Disenchantrix {
    public partial class DisenchantrixGUI : Form {
        public DisenchantrixGUI() { InitializeComponent(); }

        private void DisenchantrixGUI_Load(object sender, EventArgs e) {
            disenchantrixPropertyGrid.SelectedObject = ItemSettings.Instance;
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
            var o = disenchantrixPropertyGrid.SelectedObject as ItemSettings;

            if(o != null) {
                o.Save();
            }
        }
    }
}
