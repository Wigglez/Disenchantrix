using System.ComponentModel;
using System.IO;
using Styx.Common;
using Styx.Helpers;
using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Disenchantrix {
    public class ItemSettings : Settings {

        // ===========================================================
        // Constants
        // ===========================================================

        // ===========================================================
        // Fields
        // ===========================================================

        private static ItemSettings _instance;

        // ===========================================================
        // Constructors
        // ===========================================================
        public ItemSettings() : base(Path.Combine(Path.Combine(Utilities.AssemblyDirectory, "Settings"), string.Format(@"Settings\{0}\{1}.xml", "Disenchantrix", "ItemSettings"))) {
            
        }
        
        // ===========================================================
        // Getter & Setter
        // ===========================================================

        public static ItemSettings Instance { get { return _instance ?? (_instance = new ItemSettings()); } }

        [Setting]
        [DefaultValue(false)]
        [Category("Item Type")]
        [DisplayName("Disenchant Soulbound")]
        [Description("Toggles if Disenchantrix should disenchant soulbound items.")]
        public bool DisenchantSoulbound { get; set; }

        [Setting]
        [DefaultValue(true)]
        [Category("Item Quality")]
        [DisplayName("Disenchant Greens")]
        [Description("Toggles if Disenchantrix should disenchant green quality items.")]
        public bool DisenchantGreen { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Item Quality")]
        [DisplayName("Disenchant Blues")]
        [Description("Toggles if Disenchantrix should disenchant blue quality items.")]
        public bool DisenchantBlue { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Item Quality")]
        [DisplayName("Disenchant Purples")]
        [Description("Toggles if Disenchantrix should disenchant purple quality items.")]
        public bool DisenchantPurple { get; set; }

        // ===========================================================
        // Methods for/from SuperClass/Interfaces
        // ===========================================================

        // ===========================================================
        // Methods
        // ===========================================================

        // ===========================================================
        // Inner and Anonymous Classes
        // ===========================================================

    }
}
