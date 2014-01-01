using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using Styx.Common;

namespace Disenchantrix {
    public class BlacklistEntry {
        [XmlAttribute]
        public uint Entry { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
    }

    public class BlacklistDatabase {
        // ===========================================================
        // Constants
        // ===========================================================

        // ===========================================================
        // Fields
        // ===========================================================

        private BindingList<BlacklistEntry> _blacklistedItems = new BindingList<BlacklistEntry>();
        public static BlacklistDatabase Instance = new BlacklistDatabase();

        // ===========================================================
        // Constructors
        // ===========================================================

        static BlacklistDatabase() {
            var folderPath = Path.GetDirectoryName(SettingsFilePath);

            if(folderPath != null && !Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
            }

            Load();
        }

        // ===========================================================
        // Getter & Setter
        // ===========================================================

        public static string SettingsFilePath {
            get { return Path.Combine(Utilities.AssemblyDirectory, string.Format(@"Settings\{0}\{1}.xml", "Disenchantrix", "BlacklistDatabase")); }
        }


        public BindingList<BlacklistEntry> BlacklistedItems { get { return _blacklistedItems; } set { _blacklistedItems = value; } }

        // ===========================================================
        // Methods for/from SuperClass/Interfaces
        // ===========================================================

        // ===========================================================
        // Methods
        // ===========================================================
        public static void Load() {
            try {
                Instance = ObjectXMLSerializer<BlacklistDatabase>.Load(SettingsFilePath);
            } catch(Exception) {
                Instance = new BlacklistDatabase();
            }
        }

        public static void Save() {
            ObjectXMLSerializer<BlacklistDatabase>.Save(Instance, SettingsFilePath);
        }

        // ===========================================================
        // Inner and Anonymous Classes
        // ===========================================================
    }
}
