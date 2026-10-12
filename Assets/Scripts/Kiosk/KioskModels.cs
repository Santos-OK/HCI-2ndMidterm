using System;
using System.Collections.Generic;

namespace QuestKiosk
{
    /// <summary>Status of a Quest 3 kit as shown on the kiosk.</summary>
    public enum KitStatus
    {
        Available = 0,
        Reserved = 1,
        Loaned = 2,
        Charging = 3
    }

    /// <summary>One of the six physical kits.</summary>
    [Serializable]
    public class KitRecord
    {
        public int id;
        public KitStatus status = KitStatus.Available;

        // Contents currently inside the kit. A complete kit has 1 / 2 / 1.
        public int headsets = 1;
        public int controllers = 2;
        public int chargers = 1;

        // UTC ticks when the current status ends (loan due, charge done, reservation released). 0 = none.
        public long busyUntilTicks;

        // True when the person at this kiosk asked to be notified / held this kit (Scenario B next step).
        public bool reservedByUser;

        // True while a kit that was reserved by the user is waiting for them to pick it up.
        public bool holdActive;
    }

    /// <summary>A confirmed loan.</summary>
    [Serializable]
    public class LoanRecord
    {
        public string loanId;
        public int kitId;
        public int minutes;
        public long startTicks;
        public long dueTicks;
        public string locker;
    }

    /// <summary>The whole inventory and loan list the kiosk works with.</summary>
    [Serializable]
    public class KioskState
    {
        public int version = 1;

        public List<KitRecord> kits = new List<KitRecord>();
        public List<LoanRecord> loans = new List<LoanRecord>();
    }

    /// <summary>Result of checking the contents of one kit.</summary>
    public sealed class ContentCheck
    {
        public int HeadsetsFound;
        public int ControllersFound;
        public int ChargersFound;

        public bool HeadsetOk;
        public bool ControllersOk;
        public bool ChargerOk;

        public bool AllOk
        {
            get { return HeadsetOk && ControllersOk && ChargerOk; }
        }

        /// <summary>Human readable list of what is missing, e.g. "1 Controller, 1 Charger".</summary>
        public string MissingSummary()
        {
            var parts = new List<string>();

            if (!HeadsetOk)
            {
                int n = KioskService.RequiredHeadsets - HeadsetsFound;
                parts.Add(n + (n == 1 ? " Headset" : " Headsets"));
            }

            if (!ControllersOk)
            {
                int n = KioskService.RequiredControllers - ControllersFound;
                parts.Add(n + (n == 1 ? " Controller" : " Controllers"));
            }

            if (!ChargerOk)
            {
                int n = KioskService.RequiredChargers - ChargersFound;
                parts.Add(n + (n == 1 ? " Charger" : " Chargers"));
            }

            return string.Join(", ", parts.ToArray());
        }
    }
}
