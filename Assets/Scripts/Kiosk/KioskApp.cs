using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuestKiosk
{
    /// <summary>
    /// Controller of the Quest 3 loaning kiosk. It is on the KioskCanvas object of the scene
    /// (created with the menu "Quest Kiosk > Build UI in open scene").
    /// The look of every screen is edited in the scene. This script only shows the right screen,
    /// writes the data (kits, times, lockers) into it and reacts to taps. See KioskLayout for the object names.
    /// Screen logic is in KioskApp.Screens.cs.
    /// </summary>
    public partial class KioskApp : MonoBehaviour
    {
        KioskService service;

        readonly Dictionary<KioskPage, Transform> screens = new Dictionary<KioskPage, Transform>();
        GameObject devicesTemplate;
        GameObject availabilityTemplate;
        GameObject toast;
        Coroutine toastRoutine;

        KioskPage current = KioskPage.Main;

        // Current request (kept while the user moves around so valid choices are never lost)
        int selectedKit;
        int selectedMinutes;
        bool dropdownOpen;

        // Edit screen
        int editKit;
        int editMinutes;
        bool devicesPickingForEdit;

        // Results shown on the last screens
        LoanRecord lastLoan;
        int lastReservedKit;
        DateTime lastReservedReady;

        float refreshTimer;
        bool pendingRebuild;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<KioskApp>() != null) return;

            // The open scene has no kiosk UI: build the default one so the app still runs.
            KioskLayout.Build().AddComponent<KioskApp>();
        }

        void Awake()
        {
            service = new KioskService();
            service.Changed += OnServiceChanged;

            if (FindFirstObjectByType<EventSystem>() == null) KioskLayout.CreateEventSystem();

            FindScreens();
            WireButtons();
            Go(KioskPage.Main);
        }

        void OnDestroy()
        {
            if (service != null) service.Changed -= OnServiceChanged;
        }

        void Update()
        {
            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer >= 1f)
            {
                refreshTimer = 0f;
                service.Refresh();
            }

            if (pendingRebuild)
            {
                pendingRebuild = false;
                if (current == KioskPage.Devices || current == KioskPage.Availability) Rebuild();
            }
        }

        void OnServiceChanged()
        {
            // Do not rebuild in the middle of a click handler. Update() does it next frame.
            pendingRebuild = true;
        }

        // ------------------------------------------------------------------ scene objects

        void FindScreens()
        {
            foreach (KioskPage page in Enum.GetValues(typeof(KioskPage)))
            {
                var s = UiKit.FindDeep(transform, KioskLayout.ScreenName(page));
                if (s == null)
                {
                    Debug.LogError("[Quest Kiosk] " + KioskLayout.ScreenName(page) + " is missing under " + name +
                        ". Rebuild the UI with Quest Kiosk > Build UI in open scene.");
                    continue;
                }
                screens[page] = s;
            }

            // The kit card templates are copied once per kit, the originals stay hidden.
            devicesTemplate = HideTemplate(KioskPage.Devices);
            availabilityTemplate = HideTemplate(KioskPage.Availability);

            var t = UiKit.FindDeep(transform, "Toast");
            if (t != null)
            {
                toast = t.gameObject;
                toast.SetActive(false);
            }
        }

        GameObject HideTemplate(KioskPage page)
        {
            var t = Get(page, "KitCardTemplate");
            if (t == null) return null;
            t.gameObject.SetActive(false);
            return t.gameObject;
        }

        Transform ScreenOf(KioskPage page)
        {
            Transform s;
            return screens.TryGetValue(page, out s) ? s : null;
        }

        Transform Get(KioskPage page, string objectName)
        {
            return UiKit.Find(ScreenOf(page), objectName);
        }

        void SetText(KioskPage page, string objectName, string text)
        {
            UiKit.SetText(ScreenOf(page), objectName, text);
        }

        void SetShown(KioskPage page, string objectName, bool shown)
        {
            UiKit.SetShown(ScreenOf(page), objectName, shown);
        }

        void OnClick(KioskPage page, string objectName, UnityEngine.Events.UnityAction action)
        {
            UiKit.OnClick(ScreenOf(page), objectName, action);
        }

        // ------------------------------------------------------------------ navigation

        void Go(KioskPage page)
        {
            current = page;
            dropdownOpen = false;
            Rebuild();
        }

        /// <summary>Shows only the current screen and writes the current data into it.</summary>
        void Rebuild()
        {
            foreach (var pair in screens)
            {
                bool on = pair.Key == current;
                if (pair.Value.gameObject.activeSelf != on) pair.Value.gameObject.SetActive(on);
            }

            switch (current)
            {
                case KioskPage.Main: ShowMain(); break;
                case KioskPage.Devices: ShowDevices(); break;
                case KioskPage.LoanTime: ShowLoanTime(); break;
                case KioskPage.Verify: ShowVerify(); break;
                case KioskPage.Review: ShowReview(); break;
                case KioskPage.Edit: ShowEdit(); break;
                case KioskPage.Checkout: ShowCheckout(); break;
                case KioskPage.Availability: ShowAvailability(); break;
                case KioskPage.ReservationSaved: ShowReservationSaved(); break;
            }
        }

        void ResetFlow()
        {
            selectedKit = 0;
            selectedMinutes = 0;
            editKit = 0;
            editMinutes = 0;
            dropdownOpen = false;
            devicesPickingForEdit = false;
            lastLoan = null;
        }

        // ------------------------------------------------------------------ message

        /// <summary>Short message at the bottom, used to explain why something was blocked.</summary>
        void ShowToast(string message)
        {
            if (toast == null)
            {
                Debug.Log("[Quest Kiosk] " + message);
                return;
            }

            UiKit.SetText(toast.transform, "Txt_Message", message);
            toast.SetActive(true);

            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(HideToastLater(4.5f));
        }

        IEnumerator HideToastLater(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (toast != null) toast.SetActive(false);
            toastRoutine = null;
        }

        // ------------------------------------------------------------------ shared helpers

        static Color StatusColor(KitRecord kit)
        {
            if (kit.holdActive) return UiKit.StatusAvailable;
            switch (kit.status)
            {
                case KitStatus.Available: return UiKit.StatusAvailable;
                case KitStatus.Reserved: return UiKit.StatusReserved;
                case KitStatus.Loaned: return UiKit.StatusLoaned;
                default: return UiKit.StatusCharging;
            }
        }

        static string StatusLabel(KitRecord kit)
        {
            if (kit.holdActive) return "Reserved for you";
            return kit.status.ToString();
        }

        string TimeText(long utcTicks)
        {
            return KioskService.FormatTime(service.ToLocal(utcTicks));
        }

        /// <summary>"Available at 1:45 pm", "Held for you until 2:00 pm" or "" for a free kit.</summary>
        string AvailabilityText(KitRecord kit)
        {
            if (kit.holdActive) return "Held for you until " + TimeText(kit.busyUntilTicks);
            if (service.IsSelectable(kit)) return "";
            if (kit.busyUntilTicks <= 0) return "No return time yet";
            return "Available at " + TimeText(kit.busyUntilTicks);
        }

        static string KitName(int id)
        {
            return "Kit #" + id.ToString("00");
        }
    }
}
