using System;
using System.Globalization;

namespace QuestKiosk
{
    /// <summary>
    /// Rules of the loaning system: availability, content verification, checkout and reservations.
    /// No UI code in here. The state lives in memory; listen to <see cref="Changed"/> to react to updates.
    /// </summary>
    public sealed class KioskService
    {
        public const int KitCount = 6;
        public const int RequiredHeadsets = 1;
        public const int RequiredControllers = 2;
        public const int RequiredChargers = 1;

        /// <summary>How long a kit that the user reserved is held once it becomes free.</summary>
        public const int HoldMinutes = 15;

        public static readonly int[] DurationOptions = { 30, 60, 90 };

        // Starting inventory (same as the Figma device list).
        // Offsets are "minutes from now until the kit is free again".
        static readonly KitStatus[] StartStatus =
        {
            KitStatus.Reserved, KitStatus.Available, KitStatus.Loaned,
            KitStatus.Loaned, KitStatus.Charging, KitStatus.Reserved
        };
        static readonly int[] StartMinutes = { 30, 0, 45, 75, 120, 60 };

        public KioskState State { get; private set; }

        /// <summary>Raised whenever the inventory changes (loan, reservation, expiry).</summary>
        public event Action Changed;

        public KioskService()
        {
            State = BuildStartState();
        }

        // ------------------------------------------------------------------ time

        public DateTime Now
        {
            get { return DateTime.UtcNow; }
        }

        public DateTime ToLocal(long utcTicks)
        {
            return new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime();
        }

        /// <summary>Formats a local time like "5:30 pm".</summary>
        public static string FormatTime(DateTime local)
        {
            return local.ToString("h:mm tt", CultureInfo.InvariantCulture).ToLowerInvariant();
        }

        public static string LockerFor(int kitId)
        {
            return "Locker #" + kitId.ToString("00");
        }

        // ------------------------------------------------------------------ queries

        public KitRecord GetKit(int id)
        {
            return State.kits.Find(k => k.id == id);
        }

        /// <summary>A kit can be borrowed when it is Available, or when it is being held for the user.</summary>
        public bool IsSelectable(KitRecord kit)
        {
            if (kit == null) return false;
            if (kit.status == KitStatus.Available) return true;
            return kit.status == KitStatus.Reserved && kit.holdActive;
        }

        public int AvailableCount()
        {
            int n = 0;
            foreach (var k in State.kits)
            {
                if (IsSelectable(k)) n++;
            }
            return n;
        }

        /// <summary>The unavailable kit that will be free first, or null if everything is free.</summary>
        public KitRecord NextToFree()
        {
            KitRecord best = null;
            foreach (var k in State.kits)
            {
                if (IsSelectable(k) || k.busyUntilTicks <= 0) continue;
                if (best == null || k.busyUntilTicks < best.busyUntilTicks) best = k;
            }
            return best;
        }

        public ContentCheck VerifyContents(KitRecord kit)
        {
            var check = new ContentCheck();
            if (kit == null) return check;

            check.HeadsetsFound = kit.headsets;
            check.ControllersFound = kit.controllers;
            check.ChargersFound = kit.chargers;
            check.HeadsetOk = kit.headsets >= RequiredHeadsets;
            check.ControllersOk = kit.controllers >= RequiredControllers;
            check.ChargerOk = kit.chargers >= RequiredChargers;
            return check;
        }

        // ------------------------------------------------------------------ operations

        /// <summary>Validates everything again and, if valid, records the loan. Never double-loans a kit.</summary>
        public bool TryCheckout(int kitId, int minutes, out LoanRecord loan, out string error)
        {
            loan = null;
            error = null;

            Refresh();

            var kit = GetKit(kitId);
            if (kit == null)
            {
                error = "That kit number doesn't exist.";
                return false;
            }

            if (Array.IndexOf(DurationOptions, minutes) < 0)
            {
                error = "Choose a loan duration first.";
                return false;
            }

            if (!IsSelectable(kit))
            {
                error = "Kit #" + kit.id.ToString("00") + " is no longer available (" + kit.status + ").";
                return false;
            }

            var check = VerifyContents(kit);
            if (!check.AllOk)
            {
                error = "Kit #" + kit.id.ToString("00") + " is incomplete. Missing: " + check.MissingSummary() + ".";
                return false;
            }

            DateTime now = Now;
            DateTime due = now.AddMinutes(minutes);

            kit.status = KitStatus.Loaned;
            kit.busyUntilTicks = due.Ticks;
            kit.reservedByUser = false;
            kit.holdActive = false;

            loan = new LoanRecord
            {
                loanId = Guid.NewGuid().ToString("N").Substring(0, 8),
                kitId = kit.id,
                minutes = minutes,
                startTicks = now.Ticks,
                dueTicks = due.Ticks,
                locker = LockerFor(kit.id)
            };

            State.loans.Add(loan);
            if (State.loans.Count > 50) State.loans.RemoveAt(0);

            RaiseChanged();
            return true;
        }

        /// <summary>Scenario B next step: ask to get a kit that is busy right now.</summary>
        public bool TryReserve(int kitId, out DateTime readyLocal, out string error)
        {
            readyLocal = DateTime.MinValue;
            error = null;

            Refresh();

            var kit = GetKit(kitId);
            if (kit == null)
            {
                error = "That kit number doesn't exist.";
                return false;
            }

            if (IsSelectable(kit))
            {
                error = "Kit #" + kit.id.ToString("00") + " is available now. You can borrow it.";
                return false;
            }

            if (kit.reservedByUser)
            {
                error = "You already reserved Kit #" + kit.id.ToString("00") + ".";
                return false;
            }

            if (kit.busyUntilTicks <= 0)
            {
                error = "Kit #" + kit.id.ToString("00") + " has no return time yet, so it can't be reserved.";
                return false;
            }

            kit.reservedByUser = true;
            readyLocal = ToLocal(kit.busyUntilTicks);

            RaiseChanged();
            return true;
        }

        /// <summary>Releases loans / charges / holds whose time has passed. Returns true if anything changed.</summary>
        public bool Refresh()
        {
            bool changed = false;
            long now = Now.Ticks;

            foreach (var k in State.kits)
            {
                if (k.status == KitStatus.Available) continue;
                if (k.busyUntilTicks <= 0 || now < k.busyUntilTicks) continue;

                if (k.reservedByUser && !k.holdActive)
                {
                    // The kit is free and the user asked for it: hold it for them.
                    k.status = KitStatus.Reserved;
                    k.holdActive = true;
                    k.busyUntilTicks = now + TimeSpan.FromMinutes(HoldMinutes).Ticks;
                }
                else
                {
                    k.status = KitStatus.Available;
                    k.busyUntilTicks = 0;
                    k.reservedByUser = false;
                    k.holdActive = false;
                }

                changed = true;
            }

            if (changed) RaiseChanged();
            return changed;
        }

        // ------------------------------------------------------------------ helpers

        static KioskState BuildStartState()
        {
            var state = new KioskState();
            DateTime now = DateTime.UtcNow;

            for (int i = 0; i < KitCount; i++)
            {
                var kit = new KitRecord { id = i + 1, status = StartStatus[i] };
                if (StartStatus[i] != KitStatus.Available)
                {
                    kit.busyUntilTicks = RoundUpToFiveMinutes(now.AddMinutes(StartMinutes[i])).Ticks;
                }
                state.kits.Add(kit);
            }

            return state;
        }

        static DateTime RoundUpToFiveMinutes(DateTime utc)
        {
            DateTime local = utc.ToLocalTime();
            int add = (5 - local.Minute % 5) % 5;
            var rounded = new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, local.Kind).AddMinutes(add);
            return rounded.ToUniversalTime();
        }

        void RaiseChanged()
        {
            var handler = Changed;
            if (handler != null) handler();
        }
    }
}
