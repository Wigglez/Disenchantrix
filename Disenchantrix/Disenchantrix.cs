using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Frames;
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
        private static readonly Stopwatch PulseTimer = new Stopwatch();

        // ===========================================================
        // Fields
        // ===========================================================

        // root of our plugins behavior tree
        private static Composite _root;

        public static LocalPlayer Me = StyxWoW.Me;
        public static ItemSettings Item = new ItemSettings();

        public static WoWItem DisenchantableItem;

        // ===========================================================
        // Constructors
        // ===========================================================

        // ===========================================================
        // Getter & Setter
        // ===========================================================

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

            if(Me.IsCasting) {
                return;
            }

            if(IsDone()) return;

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

            if(!Me.IsValid) {
                return false;
            }

            if(Me.IsActuallyInCombat || Me.Combat) {
                return false;
            }

            if(Me.Mounted || Me.IsFlying) {
                return false;
            }

            if(Me.IsStealthed) {
                return false;
            }

            if(Me.IsDead || Me.IsGhost) {
                return false;
            }

            if(LootFrame.Instance.IsVisible) {
                return false;
            }

            DisenchantableItem = FindDisenchantableItem();

            return DisenchantableItem != null;
        }

        public static WoWItem FindDisenchantableItem() {
            if(Me.CarriedItems == null) {
                return null;
            }

            if(ItemSettings.Instance.DisenchantGreen) {
                if(ItemSettings.Instance.DisenchantSoulbound) {
                    var carriedGreens = Me.CarriedItems.FirstOrDefault(i => i.IsValid && i.Quality == WoWItemQuality.Uncommon && BlacklistDoesNotContain(i));

                    if(carriedGreens != null && Me.BagItems.Contains(carriedGreens)) {
                        return carriedGreens;
                    }
                } else {
                    var carriedGreens = Me.CarriedItems.FirstOrDefault(i => i != null && i.IsValid && i.Quality == WoWItemQuality.Uncommon && !i.IsSoulbound && BlacklistDoesNotContain(i));

                    if(carriedGreens != null && Me.BagItems.Contains(carriedGreens)) {
                        return carriedGreens;
                    }
                }
            }

            if(ItemSettings.Instance.DisenchantBlue) {
                if(ItemSettings.Instance.DisenchantSoulbound) {
                    var carriedBlues = Me.CarriedItems.FirstOrDefault(i => i.IsValid && i.Quality == WoWItemQuality.Rare && BlacklistDoesNotContain(i));

                    if(carriedBlues != null && Me.BagItems.Contains(carriedBlues)) {
                        return carriedBlues;
                    }
                } else {
                    var carriedBlues = Me.CarriedItems.FirstOrDefault(i => i != null && i.IsValid && i.Quality == WoWItemQuality.Rare && !i.IsSoulbound && BlacklistDoesNotContain(i));

                    if(carriedBlues != null && Me.BagItems.Contains(carriedBlues)) {
                        return carriedBlues;
                    }
                }
            }

            if(!ItemSettings.Instance.DisenchantPurple) { return null; }

            if(ItemSettings.Instance.DisenchantSoulbound) {
                var carriedPurples = Me.CarriedItems.FirstOrDefault(i => i.IsValid && i.Quality == WoWItemQuality.Epic && BlacklistDoesNotContain(i));

                if(carriedPurples != null && Me.BagItems.Contains(carriedPurples)) {
                    return carriedPurples;
                }
            } else {
                var carriedPurples = Me.CarriedItems.FirstOrDefault(i => i != null && i.IsValid && i.Quality == WoWItemQuality.Epic && !i.IsSoulbound && BlacklistDoesNotContain(i));

                if(carriedPurples != null && Me.BagItems.Contains(carriedPurples)) {
                    return carriedPurples;
                }
            }

            return null;
        }

        public static void HandleErrorMessage(object sender, LuaEventArgs args) {
            var errorMessage = args.Args[0].ToString();

            var localizedCantBeDisenchanted = Lua.GetReturnVal<string>("return SPELL_FAILED_CANT_BE_DISENCHANTED", 0);
            var localizedItemLocked = Lua.GetReturnVal<string>("return ERR_ITEM_LOCKED", 0);
            var localizedLowEnchantingSkill = Lua.GetReturnVal<string>(
                "return SPELL_FAILED_CANT_BE_DISENCHANTED_SKILL", 0);

            if(!errorMessage.Equals(localizedLowEnchantingSkill) && !errorMessage.Equals(localizedCantBeDisenchanted) &&
                !errorMessage.Equals(localizedItemLocked)) {
                return;
            }

            if(errorMessage.Equals(localizedLowEnchantingSkill)) {
                CustomNormalLog("Added " + DisenchantableItem.Name + " to the blacklist for this session.");
                CustomBlacklist.AddEntry((int)DisenchantableItem.Entry, TimeSpan.FromDays(365));
            } else {
                var cantBeDisenchanted = new BlacklistEntry {
                    Entry = DisenchantableItem.Entry,
                    Name = DisenchantableItem.Name
                };
                CustomNormalLog("Added " + DisenchantableItem.Name + " to the blacklist permanently.");
                BlacklistDatabase.Instance.BlacklistedItems.Add(cantBeDisenchanted);
                BlacklistDatabase.Save();
            }
        }

        public static bool BlacklistDoesNotContain(WoWItem i) {
            return !CustomBlacklist.ContainsEntry((int)i.Entry) &&
                   BlacklistDatabase.Instance.BlacklistedItems.All(b => b.Entry != i.Entry);
        }

        public static void CastDisenchant() {
            if(SpellManager.CanCast(DisenchantId)) {
                SpellManager.Cast(DisenchantId);
            }
        }

        public static bool IsDone() {
            return FindDisenchantableItem() == null;
        }

        // ===========================================================
        // Inner and Anonymous Classes
        // ===========================================================

        private static Composite CreateBehaviorLogic() {
            return new PrioritySelector(
                new Decorator(ctx => CanDisenchant(),
                    new Sequence(
                        new Action(ctx => CustomDiagnosticLog("CreateBehaviorLogic")),
                        NoDisenchantables(),
                        Disenchantables()
                    )
                )
            );
        }

        private static Composite NoDisenchantables() {
            return new Decorator(ctx => DisenchantableItem == null,
                new Sequence(
                    new Action(ctx => CustomDiagnosticLog("NoDisenchantables")),
                    CursorActiveNoItems(),
                    CursorInactiveNoItems()
                )
            );
        }

        private static Composite CursorActiveNoItems() {
            return new Decorator(ctx => Me.CurrentPendingCursorSpell.Name == "Disenchant",
                new Sequence(
                    new Action(r => CustomDiagnosticLog("No more items to disenchant, cancel pending cursor spell.")),
                    new Action(r => SpellManager.StopCasting()),
                    new Action(r => RunStatus.Success)
                )
            );
        }

        private static Composite CursorInactiveNoItems() {
            return new Decorator(ctx => Me.CurrentPendingCursorSpell == null,
                new Sequence(
                    new Action(r => CustomDiagnosticLog("CursorInactiveNoItems")),
                    new Action(r => RunStatus.Failure)
                )
            );
        }

        private static Composite Disenchantables() {
            return new Decorator(ctx => DisenchantableItem != null,
                new Sequence(
                    new Action(r => CustomDiagnosticLog("Disenchantables")),
                    CursorInactiveItems(),
                    CursorActiveItems()
                )
            );
        }

        private static Composite CursorInactiveItems() {
            return new Decorator(ctx => Me.CurrentPendingCursorSpell == null || Me.CurrentPendingCursorSpell.Name != "Disenchant",
                new Sequence(
                    new Action(r => CustomDiagnosticLog("CursorInactiveItems")),
                    new Action(r => CastDisenchant()),
                    new WaitContinue(TimeSpan.FromMilliseconds(50), ret => false, new ActionAlwaysSucceed())
                )
            );
        }
     
        private static Composite CursorActiveItems() {
            return new Decorator(ctx => Me.CurrentPendingCursorSpell.Name == "Disenchant",
                new Sequence(
                    new Action(r => CustomNormalLog("Disenchanting {0}", DisenchantableItem.Name)),
                    new Action(r => WoWMovement.MoveStop()),
                    new Action(r => DisenchantableItem.Use()),
                    new WaitContinue(MaxDelayForCastingComplete, ret => false, new ActionAlwaysSucceed())
                )
            );
        }
    }
}