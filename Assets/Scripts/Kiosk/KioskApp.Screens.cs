using System;
using UnityEngine;
using UnityEngine.UI;

namespace QuestKiosk
{
    // Behavior of every screen: which button goes where, and the data written into the scene objects.
    // The look of the screens lives in the scene, not here.
    public partial class KioskApp
    {
        const string CloneName = "Kit_";

        // ------------------------------------------------------------------ buttons (wired once)

        void WireButtons()
        {
            // 1. Main
            OnClick(KioskPage.Main, "Btn_Check", () =>
            {
                ResetFlow();
                Go(KioskPage.Devices);
            });

            // 2. Device list (and Scenario B)
            OnClick(KioskPage.Devices, "Btn_Back", () =>
            {
                if (devicesPickingForEdit)
                {
                    devicesPickingForEdit = false;
                    Go(KioskPage.Edit);
                }
                else
                {
                    ResetFlow();
                    Go(KioskPage.Main);
                }
            });
            OnClick(KioskPage.Devices, "Btn_ViewAvailability", () => Go(KioskPage.Availability));
            OnClick(KioskPage.Devices, "Btn_Exit", () =>
            {
                ResetFlow();
                Go(KioskPage.Main);
            });

            // 3. Loan time
            OnClick(KioskPage.LoanTime, "Btn_Back", () => Go(KioskPage.Devices));
            OnClick(KioskPage.LoanTime, "Btn_Verify", () => Go(KioskPage.Verify));
            WireDurationPicker(KioskPage.LoanTime);

            // 4. Verify
            OnClick(KioskPage.Verify, "Btn_Back", () => Go(KioskPage.LoanTime));
            OnClick(KioskPage.Verify, "Btn_Review", () => Go(KioskPage.Review));
            OnClick(KioskPage.Verify, "Btn_ChooseAnother", () =>
            {
                selectedKit = 0;
                Go(KioskPage.Devices);
            });

            // 5. Review
            OnClick(KioskPage.Review, "Btn_Back", () => Go(KioskPage.Verify));
            OnClick(KioskPage.Review, "Btn_Edit", () =>
            {
                editKit = selectedKit;
                editMinutes = selectedMinutes;
                Go(KioskPage.Edit);
            });
            OnClick(KioskPage.Review, "Btn_Confirm", ConfirmCheckout);

            // 6. Edit
            OnClick(KioskPage.Edit, "KitCard", () =>
            {
                devicesPickingForEdit = true;
                Go(KioskPage.Devices);
            });
            OnClick(KioskPage.Edit, "Btn_BackToReview", ApplyEdit);
            WireDurationPicker(KioskPage.Edit);

            // 7. Checkout
            OnClick(KioskPage.Checkout, "Btn_Done", () =>
            {
                ResetFlow();
                Go(KioskPage.Main);
            });

            // 8. Availability
            OnClick(KioskPage.Availability, "Btn_Back", () => Go(KioskPage.Devices));

            // 9. Reservation saved
            OnClick(KioskPage.ReservationSaved, "Btn_ViewAvailability", () => Go(KioskPage.Availability));
            OnClick(KioskPage.ReservationSaved, "Btn_Done", () =>
            {
                ResetFlow();
                Go(KioskPage.Main);
            });
        }

        void WireDurationPicker(KioskPage page)
        {
            var picker = Get(page, "DurationPicker");
            if (picker == null) return;

            UiKit.OnClick(picker, "Box", () =>
            {
                dropdownOpen = true;
                Rebuild();
            });

            foreach (int option in KioskService.DurationOptions)
            {
                int minutes = option;
                UiKit.OnClick(picker, "Option_" + minutes, () =>
                {
                    if (current == KioskPage.Edit) editMinutes = minutes;
                    else selectedMinutes = minutes;

                    dropdownOpen = false;
                    Rebuild();
                });
            }
        }

        // ------------------------------------------------------------------ shared pieces

        /// <summary>Writes a kit into a kit card. Unavailable kits are dimmed but can still be tapped to see why.</summary>
        void FillKitCard(Transform card, KitRecord kit, bool dim, bool tall)
        {
            if (card == null || kit == null) return;

            var group = card.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = dim ? 0.5f : 1f;

            var dot = UiKit.Find(card, "StatusDot");
            if (dot != null)
            {
                var img = dot.GetComponent<Image>();
                if (img != null) img.color = StatusColor(kit);
            }

            UiKit.SetText(card, "Txt_Status", StatusLabel(kit));
            UiKit.SetText(card, "Txt_Number", "#" + kit.id.ToString("00"));

            if (!tall)
            {
                string when = AvailabilityText(kit);
                UiKit.SetText(card, "Txt_When", when);
                UiKit.SetShown(card, "Txt_When", when.Length > 0);
            }
        }

        /// <summary>Makes one copy of the template per kit, inside the same list.</summary>
        void FillList(GameObject template, Action<Transform, KitRecord> fill)
        {
            if (template == null) return;

            var parent = template.transform.parent;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (!child.name.StartsWith(CloneName)) continue;
                child.gameObject.SetActive(false); // hide now, Destroy happens at the end of the frame
                Destroy(child.gameObject);
            }

            foreach (var kit in service.State.kits)
            {
                var copy = Instantiate(template, parent, false);
                copy.name = CloneName + kit.id.ToString("00");
                copy.SetActive(true);
                fill(copy.transform, kit);
            }
        }

        static void SetButton(Transform button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            var btn = button.GetComponent<Button>();
            if (btn == null) return;

            btn.onClick.RemoveAllListeners();
            if (action != null) btn.onClick.AddListener(action);
            btn.interactable = action != null;
        }

        void FillDurationPicker(KioskPage page, int minutes)
        {
            var picker = Get(page, "DurationPicker");
            if (picker == null) return;

            UiKit.SetShown(picker, "Box", !dropdownOpen);
            UiKit.SetShown(picker, "Options", dropdownOpen);
            UiKit.SetText(picker, "Txt_Value", minutes > 0 ? minutes + " MIN" : "Select duration...");

            foreach (int option in KioskService.DurationOptions)
            {
                UiKit.SetShown(UiKit.Find(picker, "Option_" + option), "Mark", option == minutes);
            }
        }

        void FillItems(KioskPage page, ContentCheck check)
        {
            string[] ids = { "Item_Headset", "Item_Controllers", "Item_Charger" };
            bool[] oks = { check.HeadsetOk, check.ControllersOk, check.ChargerOk };
            int[] found = { check.HeadsetsFound, check.ControllersFound, check.ChargersFound };

            for (int i = 0; i < 3; i++)
            {
                string text = oks[i] ? "Verified" : (found[i] <= 0 ? "MISSING" : "Only " + found[i] + " found");
                UiKit.SetText(Get(page, ids[i]), "Txt_Result", text);
            }
        }

        // ------------------------------------------------------------------ 1. Main

        void ShowMain()
        {
            SetText(KioskPage.Main, "Txt_Count", service.AvailableCount() + " of " + KioskService.KitCount + " kits available");
        }

        // ------------------------------------------------------------------ 2. Device list (and Scenario B)

        void ShowDevices()
        {
            const KioskPage P = KioskPage.Devices;

            bool none = service.AvailableCount() == 0;
            bool footer = none && !devicesPickingForEdit;

            SetText(P, "Txt_Title", devicesPickingForEdit ? "Change kit" : "Choose a kit");

            SetShown(P, "Banner_NoKits", none);
            if (none)
            {
                string why = "All " + KioskService.KitCount + " kits are loaned, reserved or charging.";
                var next = service.NextToFree();
                if (next != null)
                {
                    why += "\nThe next one, " + KitName(next.id) + ", is back at " + TimeText(next.busyUntilTicks) + ".";
                }
                why += "\nTap View Availability to reserve one.";
                UiKit.SetText(Get(P, "Banner_NoKits"), "Txt_Reason", why);
            }

            SetShown(P, "Footer", footer);
            KeepListAboveFooter(footer);

            FillList(devicesTemplate, (card, kit) =>
            {
                int id = kit.id;
                FillKitCard(card, kit, !service.IsSelectable(kit), false);
                SetButton(card, () => OnKitTapped(id));
            });
        }

        /// <summary>When the footer is shown, extra space at the end of the list lets the last kit scroll above it.</summary>
        void KeepListAboveFooter(bool footerShown)
        {
            if (devicesTemplate == null) return;

            var layout = devicesTemplate.transform.parent.GetComponent<VerticalLayoutGroup>();
            if (layout == null) return;

            int bottom = 10;
            var footer = Get(KioskPage.Devices, "Footer") as RectTransform;
            if (footerShown && footer != null) bottom += Mathf.CeilToInt(footer.rect.height);

            var p = layout.padding;
            if (p.bottom != bottom) layout.padding = new RectOffset(p.left, p.right, p.top, bottom);
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
                Go(KioskPage.Edit);
                return;
            }

            selectedKit = id;
            Go(KioskPage.LoanTime);
        }

        // ------------------------------------------------------------------ 3. Loan time

        void ShowLoanTime()
        {
            var kit = service.GetKit(selectedKit);
            if (kit == null)
            {
                Go(KioskPage.Devices);
                return;
            }

            FillKitCard(Get(KioskPage.LoanTime, "KitCard"), kit, false, false);
            FillDurationPicker(KioskPage.LoanTime, selectedMinutes);
            SetShown(KioskPage.LoanTime, "Btn_Verify", !dropdownOpen && selectedMinutes > 0);
        }

        // ------------------------------------------------------------------ 4. Verify kit

        void ShowVerify()
        {
            const KioskPage P = KioskPage.Verify;

            var kit = service.GetKit(selectedKit);
            if (kit == null)
            {
                Go(KioskPage.Devices);
                return;
            }

            var check = service.VerifyContents(kit);

            SetText(P, "Txt_KitNumber", "#" + kit.id.ToString("00"));
            FillItems(P, check);

            // Invalid kit: explain and block. The only way forward is another kit.
            SetShown(P, "Btn_Review", check.AllOk);
            SetShown(P, "Warning", !check.AllOk);
            SetShown(P, "Btn_ChooseAnother", !check.AllOk);

            if (!check.AllOk)
            {
                UiKit.SetText(Get(P, "Warning"), "Txt_Message",
                    KitName(kit.id) + " is incomplete. Missing: " + check.MissingSummary() + ". It can't be borrowed.");
            }
        }

        // ------------------------------------------------------------------ 5. Review request

        void ShowReview()
        {
            var kit = service.GetKit(selectedKit);
            if (kit == null || selectedMinutes <= 0)
            {
                Go(KioskPage.Devices);
                return;
            }

            SetText(KioskPage.Review, "Txt_KitNumber", "#" + kit.id.ToString("00"));
            SetText(KioskPage.Review, "Txt_Time", "Time selected: " + selectedMinutes + " MIN");
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
                Go(KioskPage.Checkout);
                return;
            }

            // Something changed while the person was deciding. Keep the duration, ask for another kit.
            ShowToast(error + " Pick another kit, your time choice was kept.");
            selectedKit = 0;
            Go(KioskPage.Devices);
        }

        // ------------------------------------------------------------------ 6. Edit

        void ShowEdit()
        {
            var shownKit = service.GetKit(editKit);
            if (shownKit == null)
            {
                editKit = selectedKit;
                shownKit = service.GetKit(editKit);
                if (shownKit == null)
                {
                    Go(KioskPage.Devices);
                    return;
                }
            }

            FillKitCard(Get(KioskPage.Edit, "KitCard"), shownKit, false, false);
            FillDurationPicker(KioskPage.Edit, editMinutes);
            SetShown(KioskPage.Edit, "Btn_BackToReview", !dropdownOpen);
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
            Go(KioskPage.Review);
        }

        // ------------------------------------------------------------------ 7. Checkout done

        void ShowCheckout()
        {
            const KioskPage P = KioskPage.Checkout;

            if (lastLoan == null)
            {
                Go(KioskPage.Main);
                return;
            }

            SetText(P, "Txt_Locker", "Go to: " + lastLoan.locker);
            SetText(P, "Txt_Deadline", TimeText(lastLoan.dueTicks));
            SetText(P, "Txt_Summary", KitName(lastLoan.kitId) + " - " + lastLoan.minutes + " min");
        }

        // ------------------------------------------------------------------ 8. Availability (Scenario B next step)

        void ShowAvailability()
        {
            FillList(availabilityTemplate, (card, kit) =>
            {
                int id = kit.id;
                FillKitCard(card, kit, false, true);

                var button = UiKit.Find(card, "Btn_Action");
                UiKit.SetShown(card, "Btn_Action", true);

                if (service.IsSelectable(kit))
                {
                    UiKit.SetText(card, "Txt_When", kit.holdActive ? AvailabilityText(kit) : "Available now");
                    UiKit.SetText(button, "Txt_Label", "Borrow now");
                    SetButton(button, () =>
                    {
                        devicesPickingForEdit = false;
                        selectedKit = id;
                        Go(KioskPage.LoanTime);
                    });
                    return;
                }

                UiKit.SetText(card, "Txt_When", AvailabilityText(kit));

                if (kit.reservedByUser)
                {
                    UiKit.SetText(button, "Txt_Label", "Reserved for you");
                    SetButton(button, null); // shown, but can't be pressed again
                }
                else if (kit.busyUntilTicks > 0)
                {
                    UiKit.SetText(button, "Txt_Label", "Reserve at " + TimeText(kit.busyUntilTicks));
                    SetButton(button, () => OnReserve(id));
                }
                else
                {
                    UiKit.SetShown(card, "Btn_Action", false);
                }
            });
        }

        void OnReserve(int id)
        {
            DateTime ready;
            string error;

            if (service.TryReserve(id, out ready, out error))
            {
                lastReservedKit = id;
                lastReservedReady = ready;
                Go(KioskPage.ReservationSaved);
            }
            else
            {
                ShowToast(error);
            }
        }

        // ------------------------------------------------------------------ 9. Reservation saved

        void ShowReservationSaved()
        {
            const KioskPage P = KioskPage.ReservationSaved;

            SetText(P, "Txt_Kit", KitName(lastReservedKit));
            SetText(P, "Txt_Ready", "Ready at " + KioskService.FormatTime(lastReservedReady));
            SetText(P, "Txt_Locker", KioskService.LockerFor(lastReservedKit));
            SetText(P, "Txt_Info",
                "Come back to this kiosk at that time. We will hold the kit for you for " + KioskService.HoldMinutes + " minutes.");
        }
    }
}
