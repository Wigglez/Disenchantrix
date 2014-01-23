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
        public ItemSettings() : base(Path.Combine(Path.Combine(Utilities.AssemblyDirectory, "Settings"), string.Format(@"{0}\{1}.xml", "Disenchantrix", "ItemSettings"))) {
            
        }
        
        // ===========================================================
        // Getter & Setter
        // ===========================================================

        public static ItemSettings Instance { get { return _instance ?? (_instance = new ItemSettings()); } }

        [Setting]
        [DefaultValue(4)]
        [Category("Tunables")]
        [DisplayName("Disenchant Delay")]
        [Description("Default: 4. The amount of time (in seconds) Disenchantrix waits to disenchant the next item.")]
        public int DisenchantDelay { get; set; }

        // Type
        [Setting]
        [DefaultValue(true)]
        [Category("Item Type")]
        [DisplayName("Disenchant Weapons")]
        [Description("Default: True. Toggles if Disenchantrix should disenchant weapons.")]
        public bool DisenchantWeapon { get; set; }

        // Quality
        [Setting]
        [DefaultValue(true)]
        [Category("Item Quality")]
        [DisplayName("Disenchant Greens")]
        [Description("Default: True. Toggles if Disenchantrix should disenchant green quality items.")]
        public bool DisenchantGreen { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Item Quality")]
        [DisplayName("Disenchant Blues")]
        [Description("Default: False. Toggles if Disenchantrix should disenchant blue quality items.")]
        public bool DisenchantBlue { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Item Quality")]
        [DisplayName("Disenchant Purples")]
        [Description("Default: False. Toggles if Disenchantrix should disenchant purple quality items.")]
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
