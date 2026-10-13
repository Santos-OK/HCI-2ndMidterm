using UnityEngine;
using UnityEngine.UI;

namespace QuestKiosk
{
    // Hamburger menu on the Main screen with a "Reset availability" button.
    // This file only adds things: it does not change any other script or the look of the existing screens.
    //
    // Objects (all inside Screen_Main). Keep these names, the code finds them by name:
    //   Btn_Menu                         hamburger button, top right
    //     Bar1, Bar2, Bar3               the three lines of the icon
    //   Menu                             panel that opens/closes
    //     Btn_ResetAvailability          the reset button (with a child Txt_Label)
    //
    // If Screen_Main does not have them, they are created when the app starts (grey, unstyled).
    // To get them into the scene so they can be styled: Quest Kiosk > Add reset menu (hamburger).
    public partial class KioskApp
    {
        void Start()
        {
            Transform main;
            if (!screens.TryGetValue(KioskPage.Main, out main)) return;

            if (UiKit.FindDeep(main, "Btn_Menu") == null) BuildResetMenu(main);

            var menu = UiKit.FindDeep(main, "Menu");
            if (menu != null) menu.gameObject.SetActive(false);

            // Tap the hamburger: open. Tap it again: close without doing anything.
            UiKit.OnClick(main, "Btn_Menu", () =>
            {
                var m = UiKit.FindDeep(main, "Menu");
                if (m != null) m.gameObject.SetActive(!m.gameObject.activeSelf);
            });

            UiKit.OnClick(main, "Btn_ResetAvailability", ResetAllKits);

            // Leave the menu closed every time the user comes back to Main
            if (main.GetComponent<ResetMenuCloser>() == null) main.gameObject.AddComponent<ResetMenuCloser>();
        }

        /// <summary>Every kit goes back to Available. Reservations, holds and the current request are cleared.</summary>
        void ResetAllKits()
        {
            foreach (var kit in service.State.kits)
            {
                kit.status = KitStatus.Available;
                kit.busyUntilTicks = 0;
                kit.reservedByUser = false;
                kit.holdActive = false;
            }

            ResetFlow();

            Transform main;
            if (screens.TryGetValue(KioskPage.Main, out main))
            {
                var menu = UiKit.FindDeep(main, "Menu");
                if (menu != null) menu.gameObject.SetActive(false);
            }

            Go(KioskPage.Main); // refreshes the "N of 6 kits available" text
            ShowToast("All " + KioskService.KitCount + " kits are available again.");
        }

        /// <summary>Creates the hamburger button and the menu inside the Main screen (top right corner).</summary>
        public static void BuildResetMenu(Transform main)
        {
            var burger = UiKit.Panel(main, "Btn_Menu", 340, 12, 48, 48, UiKit.ButtonFill);
            UiKit.MakeTappable(burger);

            // The three lines of the icon are plain rectangles (no font or image needed)
            for (int i = 0; i < 3; i++)
            {
                UiKit.Panel(burger.transform, "Bar" + (i + 1), 11, 14 + i * 8, 26, 4, UiKit.TextColor);
            }

            var menu = UiKit.Panel(main, "Menu", 166, 66, 222, 64, UiKit.Card);
            UiKit.MakeButton(menu.transform, "Btn_ResetAvailability", "Reset availability", 8, 8, 206, 48, 18);
            menu.gameObject.SetActive(false);
        }
    }

    /// <summary>Sits on Screen_Main. When the screen is shown again, the menu is closed.</summary>
    public class ResetMenuCloser : MonoBehaviour
    {
        bool closeOnNextFrame;

        void OnDisable()
        {
            closeOnNextFrame = true;
        }

        void Update()
        {
            if (!closeOnNextFrame) return;
            closeOnNextFrame = false;

            var menu = UiKit.FindDeep(transform, "Menu");
            if (menu != null) menu.gameObject.SetActive(false);
        }
    }
}
