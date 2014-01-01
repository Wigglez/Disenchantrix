using System;
using System.Collections.Generic;
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

        private static readonly HashSet<uint> Rods = new HashSet<uint>
        {
            6218, // Runed Copper Rod
            6339, // Runed Silver Rod
            11130, // Runed Golden Rod
            11145, // Runed Truesilver Rod
            16207, // Runed Arcanite Rod
            22461, // Runed Fel Iron Rod
            22462, // Runed Adamantite Rod
            22463, // Runed Eternium Rod
            44452, // Runed Titanium Rod
            52723, // Runed Elementium Rod
        };

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

            if(PulseTimer.ElapsedMilliseconds < 500) {
                return;
            }

            PulseTimer.Restart();

            if(Me.IsCasting) {
                return;
            }

            if(!IsDone() && _root == null) {
                try {
                    _root = CreateBehaviorLogic();
                    TreeHooks.Instance.InsertHook("Combat_OOC", 0, _root);
                    _root.Start(null);
                    _root.Tick(null);

                    // If the last status wasn't running, stop the tree, and restart it.
                    if(_root.LastStatus != RunStatus.Running) {
                        _root.Stop(null);
                        _root.Start(null);
                    }
                } catch(Exception e) {
                    // Restart on any exception.
                    Logging.WriteException(e);

                    if(_root == null) {
                        throw;
                    }

                    _root.Stop(null);
                    _root.Start(null);
                    throw;
                }
            }

            if(_root == null || !IsDone()) {
                return;
            }

            try {
                TreeHooks.Instance.RemoveHook("Combat_OOC", _root);
                _root = null;
            } catch(Exception e) {
                // Restart on any exception.
                Logging.WriteException(e);

                if(_root == null) {
                    throw;
                }

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

        public static bool CanDisenchant() {
            if(!SpellManager.HasSpell(13262)) {
                return false;
            }

            if(Me.BagItems.FirstOrDefault(i => Rods.Contains(i.Entry)) == null) {
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

            DisenchantableItem = FindDisenchantableItem();

            if(DisenchantableItem == null) {
                return false;
            }

            if(LootFrame.Instance.IsVisible) {
                return false;
            }

            return true;
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

            if(ItemSettings.Instance.DisenchantPurple) {
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

        private static Composite CreateBehaviorLogic() {
            return new PrioritySelector(
                new Decorator(ctx => CanDisenchant() && Me.CurrentPendingCursorSpell == null, 
                    new Sequence(
                        new Action(r => WoWMovement.MoveStop()),
                        new Action(r => CastDisenchant()),
                        new WaitContinue(TimeSpan.FromMilliseconds(500), ret => false, new ActionAlwaysSucceed())
                    )
                ),
                new Decorator(ctx =>Me.CurrentPendingCursorSpell != null && Me.CurrentPendingCursorSpell.Name == "Disenchant" && DisenchantableItem != null, 
                    new Sequence(
                        new Action(r => DisenchantableItem.Use()),
                        new Action(r => CustomNormalLog("Disenchanting {0}", DisenchantableItem.Name)),
                        new WaitContinue(MaxDelayForCastingComplete, ret => false, new ActionAlwaysSucceed())
                    )
                ),
                new Action(delegate { 
                    if(Me.CurrentPendingCursorSpell != null && Me.CurrentPendingCursorSpell.Name == "Disenchant" && DisenchantableItem == null) {
                        SpellManager.StopCasting();
                        return RunStatus.Success;
                    }

                    return RunStatus.Failure;
                })
            );
        }
    }
}