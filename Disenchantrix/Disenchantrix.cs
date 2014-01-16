using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.Plugins;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Disenchantrix {
    public class Disenchantrix : HBPlugin {
        // ===========================================================
        // Constants
        // ===========================================================

        public static int DisenchantId = 13262;

        private static readonly TimeSpan MaxDelayForCastingComplete = TimeSpan.FromSeconds(4);

        // ===========================================================
        // Fields
        // ===========================================================

        private static Composite _root;

        private static readonly Stopwatch PulseTimer = new Stopwatch();

        public static WoWItem StoredItem;

        // ===========================================================
        // Constructors
        // ===========================================================

        // ===========================================================
        // Getter & Setter
        // ===========================================================

        public static List<WoWItem> DisenchantGreenList { get; set; }
        public static List<WoWItem> DisenchantBlueList { get; set; }
        public static List<WoWItem> DisenchantPurpleList { get; set; }

        // ===========================================================
        // Methods for/from SuperClass/Interfaces
        // ===========================================================

        public override string Name {
            get { return "Disenchantrix"; }
        }

        public override string Author {
            get { return "Wigglez & AknA (Special thanks to Mjj23)"; }
        }

        public override Version Version {
            get { return new Version(1, 0); }
        }

        public override bool WantButton {
            get { return true; }
        }

        public override string ButtonText {
            get { return "User Interface"; }
        }

        public override void OnButtonPress() {
            var gui = new DisenchantrixGUI();
            gui.ShowDialog();
        }

        public override void OnEnable() {
            try {
                _root = CreateBehaviorLogic();
                _root.Start(null);
                CustomBlacklist.SweepTimer();
                Lua.Events.AttachEvent("UI_ERROR_MESSAGE", HandleErrorMessage);
                base.OnEnable();
            } catch(Exception e) {
                CustomNormalLog("Could not initialize. Message = " + e.Message + " Stacktrace = " + e.StackTrace);
            } finally {
                CustomNormalLog("Initialization complete.");
            }
        }

        public override void OnDisable() {
            try {
                CustomBlacklist.RemoveAllGUID();
                CustomBlacklist.RemoveAllEntries();
                Lua.Events.DetachEvent("UI_ERROR_MESSAGE", HandleErrorMessage);

                // stop the behavior tree and release
                _root.Stop(null);
                _root = null;

                base.OnDisable();
            } catch(Exception e) {
                CustomNormalLog("Could not dispose. Message = " + e.Message + " Stacktrace = " + e.StackTrace);
            } finally {
                CustomNormalLog("Shutdown complete.");
            }
        }

        public override void Pulse() {
            if(!PulseTimer.IsRunning) {
                PulseTimer.Start();
            }

            if(PulseTimer.ElapsedMilliseconds < 200) {
                return;
            }

            PulseTimer.Restart();

            if(StyxWoW.Me.IsCasting) {
                return;
            }

            FindDisenchantables();

            if(IsDone()) {
                if(StyxWoW.Me.CurrentPendingCursorSpell != null && StyxWoW.Me.CurrentPendingCursorSpell.Name == "Disenchant") {
                    SpellManager.StopCasting();
                }

                return;
            }

            try {
                _root.Tick(null);

                if(_root.LastStatus == RunStatus.Running) {
                    return;
                }

                _root.Stop(null);
                _root.Start(null);
            } catch(Exception e) {
                // Restart on any exception.
                CustomDiagnosticLog(e.StackTrace);

                _root.Stop(null);
                _root.Start(null);
                throw;
            }
        }

        // ===========================================================
        // Methods
        // ===========================================================

        public static void CustomNormalLog(string message, params object[] args) {
            Logging.Write(Colors.DeepSkyBlue, "[Disenchantrix]: " + message, args);
        }

        public static void CustomDiagnosticLog(string message, params object[] args) {
            Logging.WriteDiagnostic(Colors.DeepSkyBlue, "[Disenchantrix Diag]: " + message, args);
        }

        public static bool CanDisenchant() {
            if(!SpellManager.HasSpell(13262)) {
                return false;
            }

            if(!StyxWoW.Me.IsValid) {
                return false;
            }

            if(StyxWoW.Me.IsActuallyInCombat || StyxWoW.Me.Combat) {
                return false;
            }

            if(StyxWoW.Me.Mounted || StyxWoW.Me.IsFlying) {
                return false;
            }

            if(StyxWoW.Me.IsStealthed) {
                return false;
            }

            return !StyxWoW.Me.IsDead && !StyxWoW.Me.IsGhost;
        }

        public static void FindDisenchantables() {
            if(DisenchantGreenList == null) {
                DisenchantGreenList = new List<WoWItem>();
            }

            if(DisenchantBlueList == null) {
                DisenchantBlueList = new List<WoWItem>();
            }

            if(DisenchantPurpleList == null) {
                DisenchantPurpleList = new List<WoWItem>();
            }

            FindDisenchantableGreens();
            FindDisenchantableBlues();
            FindDisenchantablePurples();
        }

        public static void FindDisenchantableGreens() {
            if(!ItemSettings.Instance.DisenchantGreen) {
                CustomDiagnosticLog("Not disenchanting greens.");
                return;
            }

            var disenchantableGreens = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Uncommon && BlacklistDoesNotContain(de));

            if(!ItemSettings.Instance.DisenchantSoulbound) {
                disenchantableGreens = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Uncommon && BlacklistDoesNotContain(de) && !de.IsSoulbound);

                if(!ItemSettings.Instance.DisenchantWeapon) {
                    disenchantableGreens = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Uncommon && BlacklistDoesNotContain(de) && !de.IsSoulbound && !de.ItemInfo.IsWeapon);
                }
            } else {
                if(!ItemSettings.Instance.DisenchantWeapon) {
                    disenchantableGreens = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Uncommon && BlacklistDoesNotContain(de) && !de.ItemInfo.IsWeapon);
                }
            }

            //var count = 0;

            foreach(var item in disenchantableGreens) {
                //CustomDiagnosticLog("Added green item #{0} to disenchant list", count + 1);
                DisenchantGreenList.Add(item);

                //count++;
            }
        }

        public static void FindDisenchantableBlues() {
            if(!ItemSettings.Instance.DisenchantBlue) {
                CustomDiagnosticLog("Not disenchanting blues.");
                return;
            }

            var disenchantableBlues = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Rare && BlacklistDoesNotContain(de));

            if(!ItemSettings.Instance.DisenchantSoulbound) {
                disenchantableBlues = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Rare && BlacklistDoesNotContain(de) && !de.IsSoulbound);

                if(!ItemSettings.Instance.DisenchantWeapon) {
                    disenchantableBlues = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Rare && BlacklistDoesNotContain(de) && !de.IsSoulbound && !de.ItemInfo.IsWeapon);
                }
            } else {
                if(!ItemSettings.Instance.DisenchantWeapon) {
                    disenchantableBlues = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Rare && BlacklistDoesNotContain(de) && !de.ItemInfo.IsWeapon);
                }
            }

            //var count = 0;

            foreach(var item in disenchantableBlues) {
                //CustomDiagnosticLog("Added blue item #{0} to disenchant list", count + 1);
                DisenchantBlueList.Add(item);

                //count++;
            }
        }

        public static void FindDisenchantablePurples() {
            if(!ItemSettings.Instance.DisenchantPurple) {
                CustomDiagnosticLog("Not disenchanting purples.");
                return;
            }

            var disenchantablePurples = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Epic && BlacklistDoesNotContain(de));

            if(!ItemSettings.Instance.DisenchantSoulbound) {
                disenchantablePurples = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Epic && BlacklistDoesNotContain(de) && !de.IsSoulbound);

                if(!ItemSettings.Instance.DisenchantWeapon) {
                    disenchantablePurples = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Epic && BlacklistDoesNotContain(de) && !de.IsSoulbound && !de.ItemInfo.IsWeapon);
                }
            } else {
                if(!ItemSettings.Instance.DisenchantWeapon) {
                    disenchantablePurples = StyxWoW.Me.BagItems.Where(de => de.IsValid && !de.IsOpenable && de.Quality == WoWItemQuality.Epic && BlacklistDoesNotContain(de) && !de.ItemInfo.IsWeapon);
                }
            }

            //var count = 0;

            foreach(var item in disenchantablePurples) {
                //CustomDiagnosticLog("Added purple item #{0} to disenchant list", count + 1);
                DisenchantPurpleList.Add(item);

                //count++;
            }
        }

        public static void DisenchantGreens() {
            foreach(var greenItem in DisenchantGreenList) {
                StoredItem = greenItem;

                CustomNormalLog("Disenchanting green item: {0}", greenItem.Name);
                greenItem.Use();

                break;
            }
        }

        public static void DisenchantBlues() {
            foreach(var blueItem in DisenchantBlueList) {
                StoredItem = blueItem;

                CustomNormalLog("Disenchanting blue item: {0}", blueItem.Name);
                blueItem.Use();

                break;
            }
        }

        public static void DisenchantPurples() {
            foreach(var purpleItem in DisenchantPurpleList) {
                StoredItem = purpleItem;

                CustomNormalLog("Disenchanting purple item: {0}", purpleItem.Name);
                purpleItem.Use();

                break;
            }
        }

        public static void CastDisenchant() {
            if(StyxWoW.Me.CurrentPendingCursorSpell != null) {
                return;
            }

            if(SpellManager.CanCast(DisenchantId)) {
                SpellManager.Cast(DisenchantId);
            }
        }

        public static void DisenchantItem() {
            if(StyxWoW.Me.CurrentPendingCursorSpell == null || StyxWoW.Me.CurrentPendingCursorSpell.Name != "Disenchant") {
                return;
            }

            DisenchantGreens();
            DisenchantBlues();
            DisenchantPurples();

            ClearLists();
        }

        public static void ClearLists() {
            CustomDiagnosticLog("Clearing lists");
            DisenchantGreenList.Clear();
            DisenchantBlueList.Clear();
            DisenchantPurpleList.Clear();
        }

        public static void HandleErrorMessage(object sender, LuaEventArgs args) {
            var errorMessage = args.Args[0].ToString();

            var localizedCantBeDisenchanted = Lua.GetReturnVal<string>("return SPELL_FAILED_CANT_BE_DISENCHANTED", 0);
            var localizedItemLocked = Lua.GetReturnVal<string>("return ERR_ITEM_LOCKED", 0);
            var localizedLowEnchantingSkill = Lua.GetReturnVal<string>("return SPELL_FAILED_CANT_BE_DISENCHANTED_SKILL", 0);

            if(!errorMessage.Equals(localizedLowEnchantingSkill) && !errorMessage.Equals(localizedCantBeDisenchanted) &&
                !errorMessage.Equals(localizedItemLocked)) {
                return;
            }

            if(errorMessage.Equals(localizedLowEnchantingSkill)) {
                CustomNormalLog("Added " + StoredItem.Name + " to the blacklist for this session.");
                CustomBlacklist.AddEntry((int)StoredItem.Entry, TimeSpan.FromDays(365));
            } else {
                var cantBeDisenchanted = new BlacklistEntry {
                    Entry = StoredItem.Entry,
                    Name = StoredItem.Name
                };

                CustomNormalLog("Added " + StoredItem.Name + " to the blacklist permanently.");
                BlacklistDatabase.Instance.BlacklistedItems.Add(cantBeDisenchanted);
                BlacklistDatabase.Save();
            }
        }

        public static bool BlacklistDoesNotContain(WoWItem i) {
            return !CustomBlacklist.ContainsEntry((int)i.Entry) && BlacklistDatabase.Instance.BlacklistedItems.All(b => b.Entry != i.Entry);
        }

        public static bool IsDone() {
            return DisenchantGreenList.Count == 0 && DisenchantBlueList.Count == 0 && DisenchantPurpleList.Count == 0;
        }

        // ===========================================================
        // Inner and Anonymous Classes
        // ===========================================================

        private static Composite CreateBehaviorLogic() {
            return new Decorator(ctx => CanDisenchant(),
                new Sequence(
                    new Action(ctx => CastDisenchant()),
                    new WaitContinue(TimeSpan.FromMilliseconds(50), ret => false, new ActionAlwaysSucceed()),
                    new Action(ctx => DisenchantItem()),
                    new WaitContinue(MaxDelayForCastingComplete, ret => false, new ActionAlwaysSucceed())
                )
            );
        }
    }
}