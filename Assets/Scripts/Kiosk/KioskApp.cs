using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuestKiosk
{
    /// <summary>
    /// Entry point of the Quest 3 loaning kiosk. It creates itself when any scene starts,
    /// builds the Canvas UI from code and drives the screen flow.
    /// Screens are in KioskApp.Screens.cs.
    /// </summary>
    public partial class KioskApp : MonoBehaviour
    {
        public const float FrameWidth = 402f;
        public const float FrameHeight = 874f;

        enum Page
        {
            Main,
            Devices,
            LoanTime,
            Verify,
            Review,
            Edit,
            Checkout,
            Availability,
            ReservationSaved
        }

        KioskService service;

        RectTransform content;   // the current screen is built in here
        RectTransform overlay;   // messages that survive screen changes

        Page current = Page.Main;

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
            new GameObject("QuestKioskApp").AddComponent<KioskApp>();
        }

        void Awake()
        {
            service = new KioskService();
            service.Changed += OnServiceChanged;

            EnsureEventSystem();
            BuildCanvas();
            Go(Page.Main);
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
                if (current == Page.Devices || current == Page.Availability) Rebuild();
            }
        }

        void OnServiceChanged()
        {
            // Do not rebuild in the middle of a click handler. Update() does it next frame.
            pendingRebuild = true;
        }

        // ------------------------------------------------------------------ setup

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        void BuildCanvas()
        {
            var canvasGo = new GameObject("KioskCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(FrameWidth, FrameHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // the 402x874 frame always fits
            scaler.referencePixelsPerUnit = 100f;

            // Background for the whole window (also the side bars on a wide screen)
            var bgRt = UiKit.Node("PageBackground", canvasGo.transform);
            UiKit.Stretch(bgRt);
            var bg = bgRt.gameObject.AddComponent<Image>();
            bg.color = UiKit.Background;

            // The phone sized frame, same size as the Figma frames
            var frame = UiKit.Node("KioskFrame", canvasGo.transform);
            frame.anchorMin = new Vector2(0.5f, 0.5f);
            frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            frame.sizeDelta = new Vector2(FrameWidth, FrameHeight);
            frame.gameObject.AddComponent<RectMask2D>();
            var frameBg = frame.gameObject.AddComponent<Image>();
            frameBg.color = UiKit.Background;

            content = UiKit.Node("Content", frame);
            UiKit.Stretch(content);

            overlay = UiKit.Node("Overlay", frame);
            UiKit.Stretch(overlay);
        }

        // ------------------------------------------------------------------ navigation

        void Go(Page page)
        {
            current = page;
            dropdownOpen = false;
            Rebuild();
        }

        void Rebuild()
        {
            ClearChildren(content);

            switch (current)
            {
                case Page.Main: BuildMain(); break;
                case Page.Devices: BuildDevices(); break;
                case Page.LoanTime: BuildLoanTime(); break;
                case Page.Verify: BuildVerify(); break;
                case Page.Review: BuildReview(); break;
                case Page.Edit: BuildEdit(); break;
                case Page.Checkout: BuildCheckout(); break;
                case Page.Availability: BuildAvailability(); break;
                case Page.ReservationSaved: BuildReservationSaved(); break;
            }
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false); // hide right now, Destroy only happens at the end of the frame
                Destroy(child);
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

        GameObject toast;
        Coroutine toastRoutine;

        /// <summary>Short message at the bottom, used to explain why something was blocked.</summary>
        void ShowToast(string message)
        {
            if (toast != null) Destroy(toast);
            if (toastRoutine != null) StopCoroutine(toastRoutine);

            var bg = UiKit.Panel(overlay, "Toast", 24, 768, 354, 88, UiKit.Card);
            UiKit.Label(bg.transform, message, 16, 8, 322, 72, 16, FontStyle.Bold, TextAnchor.MiddleLeft);

            toast = bg.gameObject;
            toastRoutine = StartCoroutine(HideToastLater(toast, 4.5f));
        }

        IEnumerator HideToastLater(GameObject target, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (target != null) Destroy(target);
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
