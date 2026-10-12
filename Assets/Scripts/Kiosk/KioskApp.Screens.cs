using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QuestKiosk
{
    // All screens of the flow. Coordinates are copied from the Figma frames (402 x 874, top-left origin).
    // The visuals are plain on purpose; only the layout and the behavior are implemented here.
    public partial class KioskApp
    {
        // ------------------------------------------------------------------ small shared pieces

        void BuildMainPanel()
        {
            // Same area as the big panel of the Figma screens.
            UiKit.Panel(content, "MainPanel", 40, 0, 322, 661, UiKit.Surface);
        }

        void AddBack(float x, UnityAction onClick)
        {
            UiKit.MakeButton(content, "< Back", x, 12, 96, 40, onClick, 18);
        }

        /// <summary>The kit card used on the device list, duration, edit and availability screens.</summary>
        RectTransform BuildKitCard(Transform parent, KitRecord kit, float x, float y, bool tall, bool dim, UnityAction onTap)
        {
            var card = UiKit.Panel(parent, "Kit" + kit.id.ToString("00"), x, y, 306, tall ? 244 : 192, UiKit.Card);
            if (onTap != null) UiKit.MakeTappable(card, onTap);

            // Unavailable kits are dimmed, but can still be tapped to see why they are blocked.
            var group = card.gameObject.AddComponent<CanvasGroup>();
            group.alpha = dim ? 0.5f : 1f;

            UiKit.Panel(card.transform, "StatusDot", 15, 12, 11, 11, StatusColor(kit));
            UiKit.Label(card.transform, StatusLabel(kit), 33, 7, 240, 24, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(card.transform, "Complete Set", 15, 60, 170, 30, 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(card.transform, "1 Headset\n2 Controllers\n1 Charger", 15, 92, 170, 56, 14, FontStyle.Normal);
            UiKit.Label(card.transform, "#" + kit.id.ToString("00"), 190, 130, 110, 48, 40, FontStyle.Bold, TextAnchor.MiddleCenter);

            if (!tall)
            {
                string when = AvailabilityText(kit);
                if (when.Length > 0)
                {
                    UiKit.Label(card.transform, when, 15, 152, 185, 30, 14, FontStyle.Normal);
                }
            }

            return card.rectTransform;
        }

        /// <summary>Closed: a box showing the choice. Open: the three duration options.</summary>
        void BuildDurationPicker(float x, float y, int currentMinutes, Action<int> onPick)
        {
            if (!dropdownOpen)
            {
                var box = UiKit.Panel(content, "DurationBox", x, y, 273, 75, UiKit.Card);
                UiKit.MakeTappable(box, () => { dropdownOpen = true; Rebuild(); });

                string text = currentMinutes > 0 ? currentMinutes + " MIN" : "Select duration...";
                var style = currentMinutes > 0 ? FontStyle.Bold : FontStyle.Normal;
                UiKit.Label(box.transform, text, 30, 24, 200, 28, 22, style, TextAnchor.MiddleLeft);
                UiKit.Label(box.transform, "v", 235, 24, 30, 28, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
                return;
            }

            for (int i = 0; i < KioskService.DurationOptions.Length; i++)
            {
                int minutes = KioskService.DurationOptions[i];
                bool selected = minutes == currentMinutes;

                var row = UiKit.Panel(content, "Option" + minutes, x, y + i * 75, 273, 71, selected ? UiKit.ButtonFill : UiKit.Card);
                UiKit.MakeTappable(row, () => onPick(minutes));
                UiKit.Label(row.transform, minutes + " MIN", 30, 22, 200, 28, 22, FontStyle.Bold, TextAnchor.MiddleLeft);
            }
        }

        /// <summary>The three item rows of the Verify and Review screens.</summary>
        void BuildContentRows(ContentCheck check)
        {
            string[] names = { "1 Headset", "2 Controllers", "1 Charger" };
            float[] ys = { 234f, 358f, 482f };

            for (int i = 0; i < 3; i++)
            {
                UiKit.Label(content, names[i], 70, ys[i], 160, 30, 24, FontStyle.Bold, TextAnchor.MiddleLeft);
            }

            if (check == null) return;

            bool[] oks = { check.HeadsetOk, check.ControllersOk, check.ChargerOk };
            int[] found = { check.HeadsetsFound, check.ControllersFound, check.ChargersFound };

            for (int i = 0; i < 3; i++)
            {
                string text = oks[i] ? "Verified" : (found[i] <= 0 ? "MISSING" : "Only " + found[i] + " found");
                UiKit.Label(content, text, 70, ys[i] + 32, 200, 22, 15, FontStyle.Bold, TextAnchor.MiddleLeft);
            }
        }

        // ------------------------------------------------------------------ 1. Main

        void BuildMain()
        {
            BuildMainPanel();

            UiKit.Label(content, "META QUEST 3\nLOANING SYSTEM", 40, 82, 322, 90, 30, FontStyle.Bold, TextAnchor.UpperCenter);

            int free = service.AvailableCount();
            UiKit.Label(content, free + " of " + KioskService.KitCount + " kits available", 40, 672, 322, 26, 17,
                FontStyle.Bold, TextAnchor.UpperCenter);

            UiKit.MakeButton(content, "Check Availability!", 64, 707, 275, 91, () =>
            {
                ResetFlow();
                Go(Page.Devices);
            }, 26);
        }

        // ------------------------------------------------------------------ 2. Device list (and Scenario B)

        void BuildDevices()
        {
            bool none = service.AvailableCount() == 0;
            bool footer = none && !devicesPickingForEdit;

            UiKit.MakeButton(content, "< Back", 16, 12, 96, 40, () =>
            {
                if (devicesPickingForEdit)
                {
                    devicesPickingForEdit = false;
                    Go(Page.Edit);
                }
                else
                {
                    ResetFlow();
                    Go(Page.Main);
                }
            }, 18);

            UiKit.Label(content, devicesPickingForEdit ? "Change kit" : "Choose a kit", 120, 12, 162, 40, 22,
                FontStyle.Bold, TextAnchor.MiddleCenter);

            float viewTop = 64f;
            float viewBottom = footer ? 572f : FrameHeight;
            float bannerHeight = none ? 190f : 0f;
            float contentHeight = 5f + bannerHeight + KioskService.KitCount * 219f + 10f;

            var list = UiKit.ScrollArea(content, 0, viewTop, FrameWidth, viewBottom - viewTop, contentHeight);

            float y = 5f;
            if (none)
            {
                BuildNoKitsBanner(list, 48, y);
                y += bannerHeight;
            }

            foreach (var kit in service.State.kits)
            {
                int id = kit.id;
                bool selectable = service.IsSelectable(kit);
                BuildKitCard(list, kit, 48, y, false, !selectable, () => OnKitTapped(id));
                y += 219f;
            }

            if (footer)
            {
                UiKit.MakeButton(content, "View Availability", 63, 584, 275, 80, () => Go(Page.Availability), 26);
                UiKit.MakeButton(content, "Exit", 63, 676, 275, 80, () =>
                {
                    ResetFlow();
                    Go(Page.Main);
                }, 26);
            }
        }

        void BuildNoKitsBanner(Transform parent, float x, float y)
        {
            var card = UiKit.Panel(parent, "NoKitsBanner", x, y, 306, 170, UiKit.Card);

            UiKit.Panel(card.transform, "StatusDot", 15, 14, 11, 11, UiKit.StatusLoaned);
            UiKit.Label(card.transform, "No Quest 3 available right now", 33, 8, 265, 24, 19, FontStyle.Bold, TextAnchor.MiddleLeft);

            string why = "All " + KioskService.KitCount + " kits are loaned, reserved or charging.";
            var next = service.NextToFree();
            if (next != null)
            {
                why += "\nThe next one, " + KitName(next.id) + ", is back at " + TimeText(next.busyUntilTicks) + ".";
            }
            why += "\nTap View Availability to reserve one.";

            UiKit.Label(card.transform, why, 15, 48, 280, 115, 15, FontStyle.Normal);
        }

        void OnKitTapped(int id)
        {
            var kit = service.GetKit(id);

            if (!service.IsSelectable(kit))
            {
                string label = kit == null ? "not a valid kit" : StatusLabel(kit).ToLowerInvariant();
                string when = kit == null ? "" : AvailabilityText(kit);
                string message = KitName(id) + " is " + label + " and can't be borrowed right now.";
                if (when.Length > 0) message += " " + when + ".";
                ShowToast(message);
                return;
            }

            if (devicesPickingForEdit)
            {
                editKit = id;
                devicesPickingForEdit = false;
                Go(Page.Edit);
                return;
            }

            selectedKit = id;
            Go(Page.LoanTime);
        }

        // ------------------------------------------------------------------ 3. Loan time

        void BuildLoanTime()
        {
            var kit = service.GetKit(selectedKit);
            if (kit == null)
            {
                Go(Page.Devices);
                return;
            }

            BuildMainPanel();
            AddBack(52, () => Go(Page.Devices));

            BuildKitCard(content, kit, 48, 69, false, false, null);

            UiKit.Label(content, "Loaning duration", 63, 311, 280, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);

            BuildDurationPicker(65, 380, selectedMinutes, minutes =>
            {
                selectedMinutes = minutes;
                dropdownOpen = false;
                Rebuild();
            });

            if (!dropdownOpen && selectedMinutes > 0)
            {
                UiKit.MakeButton(content, "Verify Kit", 63, 711, 275, 91, () => Go(Page.Verify), 26);
            }
        }

        // ------------------------------------------------------------------ 4. Verify kit

        void BuildVerify()
        {
            var kit = service.GetKit(selectedKit);
            if (kit == null)
            {
                Go(Page.Devices);
                return;
            }

            var check = service.VerifyContents(kit);

            BuildMainPanel();
            AddBack(52, () => Go(Page.LoanTime));

            UiKit.Label(content, "#" + kit.id.ToString("00"), 232, 18, 100, 44, 36, FontStyle.Bold, TextAnchor.MiddleRight);
            UiKit.Panel(content, "Card", 48, 69, 306, 550, UiKit.Card);
            UiKit.Label(content, "Kit Includes ...", 63, 85, 270, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);

            BuildContentRows(check);

            if (check.AllOk)
            {
                UiKit.MakeButton(content, "Review Request", 64, 730, 275, 91, () => Go(Page.Review), 26);
                return;
            }

            // Invalid kit: explain and block. The only way forward is another kit.
            var warning = UiKit.Panel(content, "Warning", 63, 625, 273, 90, UiKit.ErrorFill);
            UiKit.Label(warning.transform, KitName(kit.id) + " is incomplete. Missing: " + check.MissingSummary() + ". It can't be borrowed.",
                14, 6, 245, 78, 16, FontStyle.Bold, TextAnchor.MiddleLeft);

            UiKit.MakeButton(content, "Choose another kit", 64, 730, 275, 91, () =>
            {
                selectedKit = 0;
                Go(Page.Devices);
            }, 24);
        }

        // ------------------------------------------------------------------ 5. Review request

        void BuildReview()
        {
            var kit = service.GetKit(selectedKit);
            if (kit == null || selectedMinutes <= 0)
            {
                Go(Page.Devices);
                return;
            }

            BuildMainPanel();
            AddBack(52, () => Go(Page.Verify));

            UiKit.Label(content, "#" + kit.id.ToString("00"), 232, 18, 100, 44, 36, FontStyle.Bold, TextAnchor.MiddleRight);
            UiKit.Panel(content, "Card", 48, 69, 306, 550, UiKit.Card);
            UiKit.Label(content, "Your request...", 63, 85, 210, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);

            UiKit.MakeButton(content, "Edit", 278, 88, 64, 34, () =>
            {
                editKit = selectedKit;
                editMinutes = selectedMinutes;
                Go(Page.Edit);
            }, 16);

            BuildContentRows(null);

            var pill = UiKit.Panel(content, "TimeSelected", 63, 625, 273, 42, UiKit.Card);
            UiKit.Label(pill.transform, "Time selected: " + selectedMinutes + " MIN", 0, 0, 273, 42, 20, FontStyle.Normal, TextAnchor.MiddleCenter);

            UiKit.MakeButton(content, "Confirm Checkout", 64, 730, 275, 91, ConfirmCheckout, 24);
        }

        void ConfirmCheckout()
        {
            LoanRecord loan;
            string error;

            if (service.TryCheckout(selectedKit, selectedMinutes, out loan, out error))
            {
                lastLoan = loan;
                selectedKit = 0;
                selectedMinutes = 0;
                Go(Page.Checkout);
                return;
            }

            // Something changed while the person was deciding. Keep the duration, ask for another kit.
            ShowToast(error + " Pick another kit, your time choice was kept.");
            selectedKit = 0;
            Go(Page.Devices);
        }

        // ------------------------------------------------------------------ 6. Edit

        void BuildEdit()
        {
            var shownKit = service.GetKit(editKit);
            if (shownKit == null)
            {
                editKit = selectedKit;
                shownKit = service.GetKit(editKit);
                if (shownKit == null)
                {
                    Go(Page.Devices);
                    return;
                }
            }

            BuildMainPanel();

            UiKit.Label(content, "Change set", 61, 86, 260, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);

            BuildKitCard(content, shownKit, 48, 138, false, false, () =>
            {
                devicesPickingForEdit = true;
                Go(Page.Devices);
            });
            UiKit.Label(content, "Tap the kit to choose a different one", 63, 336, 280, 24, 14, FontStyle.Normal);

            UiKit.Label(content, "Change time", 63, 397, 260, 40, 30, FontStyle.Bold, TextAnchor.MiddleLeft);

            BuildDurationPicker(65, 449, editMinutes, minutes =>
            {
                editMinutes = minutes;
                dropdownOpen = false;
                Rebuild();
            });

            if (!dropdownOpen)
            {
                UiKit.MakeButton(content, "Back to Review", 63, 711, 275, 91, ApplyEdit, 24);
            }
        }

        void ApplyEdit()
        {
            if (editKit != selectedKit)
            {
                var kit = service.GetKit(editKit);

                if (!service.IsSelectable(kit))
                {
                    ShowToast(KitName(editKit) + " is not available any more. Your previous choice (" + KitName(selectedKit) + ") was kept.");
                    editKit = selectedKit;
                    Rebuild();
                    return;
                }

                var check = service.VerifyContents(kit);
                if (!check.AllOk)
                {
                    ShowToast(KitName(editKit) + " is incomplete (missing " + check.MissingSummary() + "). Your previous choice (" + KitName(selectedKit) + ") was kept.");
                    editKit = selectedKit;
                    Rebuild();
                    return;
                }

                selectedKit = editKit;
            }

            selectedMinutes = editMinutes;
            Go(Page.Review);
        }

        // ------------------------------------------------------------------ 7. Checkout done

        void BuildCheckout()
        {
            if (lastLoan == null)
            {
                Go(Page.Main);
                return;
            }

            BuildMainPanel();

            UiKit.Label(content, "Pick up your request", 40, 72, 322, 40, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiKit.Panel(content, "Card", 48, 143, 306, 476, UiKit.Card);
            UiKit.Label(content, "Go to: " + lastLoan.locker, 56, 160, 290, 40, 28, FontStyle.Bold, TextAnchor.MiddleLeft);

            UiKit.Label(content, "Return deadline:", 40, 685, 257, 40, 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiKit.Label(content, TimeText(lastLoan.dueTicks), 45, 723, 170, 40, 30, FontStyle.Normal, TextAnchor.MiddleLeft);
            UiKit.Label(content, KitName(lastLoan.kitId) + " - " + lastLoan.minutes + " min", 45, 765, 190, 24, 16,
                FontStyle.Normal, TextAnchor.MiddleLeft);

            UiKit.MakeButton(content, "Done", 230, 712, 140, 64, () =>
            {
                ResetFlow();
                Go(Page.Main);
            }, 24);
        }

        // ------------------------------------------------------------------ 8. Availability (Scenario B next step)

        void BuildAvailability()
        {
            UiKit.MakeButton(content, "< Back", 16, 12, 96, 40, () => Go(Page.Devices), 18);
            UiKit.Label(content, "Kit availability", 120, 12, 162, 40, 22, FontStyle.Bold, TextAnchor.MiddleCenter);

            float contentHeight = 5f + KioskService.KitCount * 271f + 10f;
            var list = UiKit.ScrollArea(content, 0, 64, FrameWidth, FrameHeight - 64f, contentHeight);

            float y = 5f;
            foreach (var kit in service.State.kits)
            {
                int id = kit.id;
                var card = BuildKitCard(list, kit, 48, y, true, false, null);

                if (service.IsSelectable(kit))
                {
                    string text = kit.holdActive ? AvailabilityText(kit) : "Available now";
                    UiKit.Label(card, text, 15, 150, 172, 36, 15, FontStyle.Bold, TextAnchor.MiddleLeft);
                    UiKit.MakeButton(card, "Borrow now", 28, 188, 250, 44, () =>
                    {
                        devicesPickingForEdit = false;
                        selectedKit = id;
                        Go(Page.LoanTime);
                    }, 20);
                }
                else
                {
                    UiKit.Label(card, AvailabilityText(kit), 15, 150, 172, 36, 15, FontStyle.Bold, TextAnchor.MiddleLeft);

                    if (kit.reservedByUser)
                    {
                        var done = UiKit.MakeButton(card, "Reserved for you", 28, 188, 250, 44, null, 20);
                        done.interactable = false;
                    }
                    else if (kit.busyUntilTicks > 0)
                    {
                        UiKit.MakeButton(card, "Reserve at " + TimeText(kit.busyUntilTicks), 28, 188, 250, 44, () => OnReserve(id), 20);
                    }
                }

                y += 271f;
            }
        }

        void OnReserve(int id)
        {
            DateTime ready;
            string error;

            if (service.TryReserve(id, out ready, out error))
            {
                lastReservedKit = id;
                lastReservedReady = ready;
                Go(Page.ReservationSaved);
            }
            else
            {
                ShowToast(error);
            }
        }

        void BuildReservationSaved()
        {
            BuildMainPanel();

            UiKit.Label(content, "Reservation saved", 40, 87, 322, 50, 32, FontStyle.Bold, TextAnchor.UpperCenter);
            UiKit.Panel(content, "Card", 48, 190, 306, 400, UiKit.Card);

            UiKit.Label(content, KitName(lastReservedKit), 48, 215, 306, 60, 44, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiKit.Label(content, "Ready at " + KioskService.FormatTime(lastReservedReady), 48, 285, 306, 40, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            UiKit.Label(content, KioskService.LockerFor(lastReservedKit), 48, 330, 306, 30, 20, FontStyle.Normal, TextAnchor.MiddleCenter);
            UiKit.Label(content,
                "Come back to this kiosk at that time. We will hold the kit for you for " + KioskService.HoldMinutes + " minutes.",
                72, 395, 258, 130, 18, FontStyle.Normal, TextAnchor.UpperCenter);

            UiKit.MakeButton(content, "View Availability", 63, 683, 275, 76, () => Go(Page.Availability), 24);
            UiKit.MakeButton(content, "Done", 63, 775, 275, 76, () =>
            {
                ResetFlow();
                Go(Page.Main);
            }, 24);
        }
    }
}
