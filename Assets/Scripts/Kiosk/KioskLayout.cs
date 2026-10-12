using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuestKiosk
{
    /// <summary>The screens of the kiosk. Each one is a GameObject called "Screen_" + name in the scene.</summary>
    public enum KioskPage
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

    /// <summary>
    /// Creates the default kiosk UI: one Canvas with the nine screens as normal GameObjects.
    /// It is used once from the menu "Quest Kiosk > Build UI in open scene" so the UI is saved in the scene
    /// and can be edited there. If a scene has no kiosk UI, KioskApp also calls it at Play so the app still runs.
    ///
    /// Coordinates are the ones of the Figma frames (402 x 874, top-left origin).
    /// KioskApp finds the objects by name: everything named Screen_, Btn_, Txt_, KitCard, Banner_NoKits,
    /// Footer, Warning, DurationPicker, Box, Options, Option_, Mark, Item_, StatusDot and Toast
    /// must keep its name. Everything else (position, size, color, sprite, font, extra decoration) is free to change.
    /// </summary>
    public static class KioskLayout
    {
        public const float FrameWidth = 402f;
        public const float FrameHeight = 874f;
        public const string CanvasName = "KioskCanvas";

        public static string ScreenName(KioskPage page)
        {
            return "Screen_" + page;
        }

        public static GameObject CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            return go;
        }

        public static GameObject Build()
        {
            var canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(FrameWidth, FrameHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // the 402 x 874 frame always fits
            scaler.referencePixelsPerUnit = 100f;

            // Background of the whole window (also the side bars on a wide screen)
            var bg = UiKit.Node("PageBackground", canvasGo.transform);
            UiKit.Stretch(bg);
            bg.gameObject.AddComponent<Image>().color = UiKit.Background;

            // Phone sized frame, same size as the Figma frames
            var frame = UiKit.Node("KioskFrame", canvasGo.transform);
            frame.anchorMin = new Vector2(0.5f, 0.5f);
            frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            frame.sizeDelta = new Vector2(FrameWidth, FrameHeight);
            frame.gameObject.AddComponent<RectMask2D>();
            frame.gameObject.AddComponent<Image>().color = UiKit.Background;

            var screens = UiKit.Node("Screens", frame);
            UiKit.Stretch(screens);

            foreach (KioskPage page in Enum.GetValues(typeof(KioskPage)))
            {
                var s = UiKit.Node(ScreenName(page), screens);
                UiKit.Stretch(s);

                switch (page)
                {
                    case KioskPage.Main: BuildMain(s); break;
                    case KioskPage.Devices: BuildDevices(s); break;
                    case KioskPage.LoanTime: BuildLoanTime(s); break;
                    case KioskPage.Verify: BuildVerify(s); break;
                    case KioskPage.Review: BuildReview(s); break;
                    case KioskPage.Edit: BuildEdit(s); break;
                    case KioskPage.Checkout: BuildCheckout(s); break;
                    case KioskPage.Availability: BuildAvailability(s); break;
                    case KioskPage.ReservationSaved: BuildReservationSaved(s); break;
                }

                // Only one screen is visible while editing. Turn screens on/off in the Hierarchy to edit them.
                s.gameObject.SetActive(page == KioskPage.Main);
            }

            var overlay = UiKit.Node("Overlay", frame);
            UiKit.Stretch(overlay);
            BuildToast(overlay);

            return canvasGo;
        }

        // ------------------------------------------------------------------ shared pieces

        static void MainPanel(Transform s)
        {
            UiKit.Panel(s, "MainPanel", 40, 0, 322, 661, UiKit.Surface);
        }

        static void BackButton(Transform s, float x)
        {
            UiKit.MakeButton(s, "Btn_Back", "< Back", x, 12, 96, 40, 18);
        }

        /// <summary>Kit card. Code fills StatusDot (color), Txt_Status, Txt_Number and Txt_When.</summary>
        static void KitCard(Transform parent, string name, float x, float y, bool tall, bool tappable)
        {
            var card = UiKit.Panel(parent, name, x, y, 306, tall ? 244 : 192, UiKit.Card);
            card.gameObject.AddComponent<CanvasGroup>(); // used to dim kits that can't be borrowed
            if (tappable) UiKit.MakeTappable(card);

            var t = card.transform;
            UiKit.Panel(t, "StatusDot", 15, 12, 11, 11, UiKit.StatusAvailable);
            UiKit.Label(t, "Txt_Status", "Available", 33, 7, 240, 24, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(t, "Txt_Set", "Complete Set", 15, 60, 170, 30, 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(t, "Txt_Contents", "1 Headset\n2 Controllers\n1 Charger", 15, 92, 170, 56, 14, FontStyle.Normal);
            UiKit.Label(t, "Txt_Number", "#01", 190, 130, 110, 48, 40, FontStyle.Bold, TextAnchor.MiddleCenter);

            if (!tall)
            {
                UiKit.Label(t, "Txt_When", "Available at 5:30 pm", 15, 152, 185, 30, 14, FontStyle.Normal);
            }
            else
            {
                UiKit.Label(t, "Txt_When", "Available now", 15, 150, 172, 36, 15, FontStyle.Bold, TextAnchor.MiddleLeft);
                UiKit.MakeButton(t, "Btn_Action", "Borrow now", 28, 188, 250, 44, 20);
            }
        }

        /// <summary>Box shows the choice; Options holds the three durations (hidden until the box is tapped).</summary>
        static void DurationPicker(Transform s, float x, float y)
        {
            var picker = UiKit.Node("DurationPicker", s);
            UiKit.TopLeft(picker, x, y, 273, 300);

            var box = UiKit.Panel(picker, "Box", 0, 0, 273, 75, UiKit.Card);
            UiKit.MakeTappable(box);
            UiKit.Label(box.transform, "Txt_Value", "Select duration...", 30, 24, 200, 28, 22, FontStyle.Normal, TextAnchor.MiddleLeft);
            UiKit.Label(box.transform, "Txt_Arrow", "v", 235, 24, 30, 28, 22, FontStyle.Bold, TextAnchor.MiddleCenter);

            var options = UiKit.Node("Options", picker);
            UiKit.TopLeft(options, 0, 0, 273, 225);

            for (int i = 0; i < KioskService.DurationOptions.Length; i++)
            {
                int minutes = KioskService.DurationOptions[i];
                var row = UiKit.Panel(options, "Option_" + minutes, 0, i * 75, 273, 71, UiKit.Card);
                UiKit.MakeTappable(row);
                UiKit.Panel(row.transform, "Mark", 0, 0, 8, 71, UiKit.ButtonFill); // shown on the selected option
                UiKit.Label(row.transform, "Txt_Label", minutes + " MIN", 30, 22, 200, 28, 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            }

            options.gameObject.SetActive(false);
        }

        /// <summary>The three item rows of the Verify and Review screens.</summary>
        static void Items(Transform s, bool withResult)
        {
            string[] ids = { "Item_Headset", "Item_Controllers", "Item_Charger" };
            string[] names = { "1 Headset", "2 Controllers", "1 Charger" };
            float[] ys = { 234f, 358f, 482f };

            for (int i = 0; i < 3; i++)
            {
                var item = UiKit.Node(ids[i], s);
                UiKit.TopLeft(item, 70, ys[i], 260, 60);
                UiKit.Label(item, "Txt_Name", names[i], 0, 0, 160, 30, 24, FontStyle.Bold, TextAnchor.MiddleLeft);
                if (withResult)
                {
                    UiKit.Label(item, "Txt_Result", "Verified", 0, 32, 200, 22, 15, FontStyle.Bold, TextAnchor.MiddleLeft);
                }
            }
        }

        // ------------------------------------------------------------------ screens

        static void BuildMain(Transform s)
        {
            MainPanel(s);
            UiKit.Label(s, "Txt_Title", "META QUEST 3\nLOANING SYSTEM", 40, 82, 322, 90, 30, FontStyle.Bold, TextAnchor.UpperCenter);
            UiKit.Label(s, "Txt_Count", "3 of 6 kits available", 40, 672, 322, 26, 17, FontStyle.Bold, TextAnchor.UpperCenter);
            UiKit.MakeButton(s, "Btn_Check", "Check Availability!", 64, 707, 275, 91, 26);
        }

        static void BuildDevices(Transform s)
        {
            BackButton(s, 16);
            UiKit.Label(s, "Txt_Title", "Choose a kit", 120, 12, 162, 40, 22, FontStyle.Bold, TextAnchor.MiddleCenter);

            var list = UiKit.ScrollList(s, "List", 0, 64, FrameWidth, FrameHeight - 64f, 48, 5, 27f);

            // Scenario B: only visible when no kit can be borrowed
            var banner = UiKit.Panel(list, "Banner_NoKits", 0, 0, 306, 170, UiKit.Card);
            UiKit.Panel(banner.transform, "StatusDot", 15, 14, 11, 11, UiKit.StatusLoaned);
            UiKit.Label(banner.transform, "Txt_Heading", "No Quest 3 available right now", 33, 8, 265, 24, 19, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(banner.transform, "Txt_Reason",
                "All 6 kits are loaned, reserved or charging.\nThe next one, Kit #01, is back at 5:30 pm.\nTap View Availability to reserve one.",
                15, 48, 280, 115, 15, FontStyle.Normal);

            // Copied once per kit at Play. Edit this one and all six cards change.
            KitCard(list, "KitCardTemplate", 0, 0, false, true);

            // Scenario B: only visible when no kit can be borrowed
            var footer = UiKit.Panel(s, "Footer", 0, 572, FrameWidth, FrameHeight - 572f, UiKit.Background);
            footer.raycastTarget = true; // taps on the footer don't reach the list behind it
            UiKit.MakeButton(footer.transform, "Btn_ViewAvailability", "View Availability", 63, 12, 275, 80, 26);
            UiKit.MakeButton(footer.transform, "Btn_Exit", "Exit", 63, 104, 275, 80, 26);
        }

        static void BuildLoanTime(Transform s)
        {
            MainPanel(s);
            BackButton(s, 52);
            KitCard(s, "KitCard", 48, 69, false, false);
            UiKit.Label(s, "Txt_Heading", "Loaning duration", 63, 311, 280, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.MakeButton(s, "Btn_Verify", "Verify Kit", 63, 711, 275, 91, 26);
            DurationPicker(s, 65, 380);
        }

        static void BuildVerify(Transform s)
        {
            MainPanel(s);
            BackButton(s, 52);
            UiKit.Label(s, "Txt_KitNumber", "#01", 232, 18, 100, 44, 36, FontStyle.Bold, TextAnchor.MiddleRight);
            UiKit.Panel(s, "Card", 48, 69, 306, 550, UiKit.Card);
            UiKit.Label(s, "Txt_Heading", "Kit Includes ...", 63, 85, 270, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);
            Items(s, true);

            UiKit.MakeButton(s, "Btn_Review", "Review Request", 64, 730, 275, 91, 26);

            // Incomplete kit: hidden in the scene, shown by code. Turn them on to edit them.
            var warning = UiKit.Panel(s, "Warning", 63, 625, 273, 90, UiKit.ErrorFill);
            UiKit.Label(warning.transform, "Txt_Message", "Kit #01 is incomplete. Missing: 1 Controller. It can't be borrowed.",
                14, 6, 245, 78, 16, FontStyle.Bold, TextAnchor.MiddleLeft);
            warning.gameObject.SetActive(false);

            var another = UiKit.MakeButton(s, "Btn_ChooseAnother", "Choose another kit", 64, 730, 275, 91, 24);
            another.gameObject.SetActive(false);
        }

        static void BuildReview(Transform s)
        {
            MainPanel(s);
            BackButton(s, 52);
            UiKit.Label(s, "Txt_KitNumber", "#01", 232, 18, 100, 44, 36, FontStyle.Bold, TextAnchor.MiddleRight);
            UiKit.Panel(s, "Card", 48, 69, 306, 550, UiKit.Card);
            UiKit.Label(s, "Txt_Heading", "Your request...", 63, 85, 210, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.MakeButton(s, "Btn_Edit", "Edit", 278, 88, 64, 34, 16);
            Items(s, false);

            var pill = UiKit.Panel(s, "TimeSelected", 63, 625, 273, 42, UiKit.Card);
            UiKit.Label(pill.transform, "Txt_Time", "Time selected: 60 MIN", 0, 0, 273, 42, 20, FontStyle.Normal, TextAnchor.MiddleCenter);

            UiKit.MakeButton(s, "Btn_Confirm", "Confirm Checkout", 64, 730, 275, 91, 24);
        }

        static void BuildEdit(Transform s)
        {
            MainPanel(s);
            UiKit.Label(s, "Txt_ChangeSet", "Change set", 61, 86, 260, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);
            KitCard(s, "KitCard", 48, 138, false, true);
            UiKit.Label(s, "Txt_Hint", "Tap the kit to choose a different one", 63, 336, 280, 24, 14, FontStyle.Normal);
            UiKit.Label(s, "Txt_ChangeTime", "Change time", 63, 397, 260, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.MakeButton(s, "Btn_BackToReview", "Back to Review", 63, 711, 275, 91, 24);
            DurationPicker(s, 65, 449);
        }

        static void BuildCheckout(Transform s)
        {
            MainPanel(s);
            UiKit.Label(s, "Txt_Title", "Pick up your request", 40, 72, 322, 40, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiKit.Panel(s, "Card", 48, 143, 306, 476, UiKit.Card);
            UiKit.Label(s, "Txt_Locker", "Go to: Locker #02", 56, 160, 290, 40, 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(s, "Txt_DeadlineLabel", "Return deadline:", 40, 685, 257, 40, 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(s, "Txt_Deadline", "6:30 pm", 45, 723, 170, 40, 30, FontStyle.Normal, TextAnchor.MiddleLeft);
            UiKit.Label(s, "Txt_Summary", "Kit #02 - 60 min", 45, 765, 190, 24, 16, FontStyle.Normal, TextAnchor.MiddleLeft);
            UiKit.MakeButton(s, "Btn_Done", "Done", 230, 712, 140, 64, 24);
        }

        static void BuildAvailability(Transform s)
        {
            BackButton(s, 16);
            UiKit.Label(s, "Txt_Title", "Kit availability", 120, 12, 162, 40, 22, FontStyle.Bold, TextAnchor.MiddleCenter);

            var list = UiKit.ScrollList(s, "List", 0, 64, FrameWidth, FrameHeight - 64f, 48, 5, 27f);

            // Copied once per kit at Play. Edit this one and all six cards change.
            KitCard(list, "KitCardTemplate", 0, 0, true, false);
        }

        static void BuildReservationSaved(Transform s)
        {
            MainPanel(s);
            UiKit.Label(s, "Txt_Title", "Reservation saved", 40, 87, 322, 50, 32, FontStyle.Bold, TextAnchor.UpperCenter);
            UiKit.Panel(s, "Card", 48, 190, 306, 400, UiKit.Card);
            UiKit.Label(s, "Txt_Kit", "Kit #03", 48, 215, 306, 60, 44, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiKit.Label(s, "Txt_Ready", "Ready at 5:45 pm", 48, 285, 306, 40, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiKit.Label(s, "Txt_Locker", "Locker #03", 48, 330, 306, 30, 20, FontStyle.Normal, TextAnchor.MiddleCenter);
            UiKit.Label(s, "Txt_Info",
                "Come back to this kiosk at that time. We will hold the kit for you for " + KioskService.HoldMinutes + " minutes.",
                72, 395, 258, 130, 18, FontStyle.Normal, TextAnchor.UpperCenter);
            UiKit.MakeButton(s, "Btn_ViewAvailability", "View Availability", 63, 683, 275, 76, 24);
            UiKit.MakeButton(s, "Btn_Done", "Done", 63, 775, 275, 76, 24);
        }

        static void BuildToast(Transform overlay)
        {
            var toast = UiKit.Panel(overlay, "Toast", 24, 768, 354, 88, UiKit.Card);
            UiKit.Label(toast.transform, "Txt_Message", "Kit #03 is loaned and can't be borrowed right now. Available at 6:15 pm.",
                16, 8, 322, 72, 16, FontStyle.Bold, TextAnchor.MiddleLeft);
            toast.gameObject.SetActive(false);
        }
    }
}
