using QuestKiosk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QuestKioskEditor
{
    /// <summary>
    /// Menu "Quest Kiosk" in the Unity toolbar.
    /// - Build UI in open scene: creates the kiosk Canvas with the nine screens as real objects, so they can be edited.
    /// - Show screen: turns on one screen and turns off the others, to edit them one at a time.
    /// </summary>
    public static class KioskSceneBuilder
    {
        [MenuItem("Quest Kiosk/Build UI in open scene", false, 0)]
        static void BuildUi()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Quest Kiosk", "Stop Play mode first.", "OK");
                return;
            }

            var existing = FindCanvas();
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog("Quest Kiosk",
                    "This scene already has the kiosk UI (" + KioskLayout.CanvasName + ").\n\n" +
                    "Replacing it deletes every visual change made to it. Replace it?",
                    "Replace", "Cancel");
                if (!replace) return;

                Undo.DestroyObjectImmediate(existing);
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = KioskLayout.CreateEventSystem();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            var canvas = KioskLayout.Build();
            canvas.AddComponent<KioskApp>();
            Undo.RegisterCreatedObjectUndo(canvas, "Build Quest Kiosk UI");

            Selection.activeGameObject = canvas;
            EditorSceneManager.MarkSceneDirty(canvas.scene);

            Debug.Log("[Quest Kiosk] UI created. Save the scene (Ctrl+S) to keep it.");
        }

        [MenuItem("Quest Kiosk/Show screen/1 Main", false, 20)] static void ShowMain() { Show(KioskPage.Main); }
        [MenuItem("Quest Kiosk/Show screen/2 Devices", false, 21)] static void ShowDevices() { Show(KioskPage.Devices); }
        [MenuItem("Quest Kiosk/Show screen/3 Loan Time", false, 22)] static void ShowLoanTime() { Show(KioskPage.LoanTime); }
        [MenuItem("Quest Kiosk/Show screen/4 Verify", false, 23)] static void ShowVerify() { Show(KioskPage.Verify); }
        [MenuItem("Quest Kiosk/Show screen/5 Review", false, 24)] static void ShowReview() { Show(KioskPage.Review); }
        [MenuItem("Quest Kiosk/Show screen/6 Edit", false, 25)] static void ShowEdit() { Show(KioskPage.Edit); }
        [MenuItem("Quest Kiosk/Show screen/7 Checkout", false, 26)] static void ShowCheckout() { Show(KioskPage.Checkout); }
        [MenuItem("Quest Kiosk/Show screen/8 Availability", false, 27)] static void ShowAvailability() { Show(KioskPage.Availability); }
        [MenuItem("Quest Kiosk/Show screen/9 Reservation Saved", false, 28)] static void ShowReservationSaved() { Show(KioskPage.ReservationSaved); }

        static void Show(KioskPage page)
        {
            var canvas = FindCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Quest Kiosk", "There is no kiosk UI in this scene. Use Quest Kiosk > Build UI in open scene first.", "OK");
                return;
            }

            GameObject shown = null;
            foreach (KioskPage p in System.Enum.GetValues(typeof(KioskPage)))
            {
                var t = UiKit.FindDeep(canvas.transform, KioskLayout.ScreenName(p));
                if (t == null) continue;

                bool on = p == page;
                if (t.gameObject.activeSelf != on)
                {
                    Undo.RecordObject(t.gameObject, "Show kiosk screen");
                    t.gameObject.SetActive(on);
                }
                if (on) shown = t.gameObject;
            }

            if (shown != null)
            {
                Selection.activeGameObject = shown;
                EditorSceneManager.MarkSceneDirty(shown.scene);
            }
        }

        static GameObject FindCanvas()
        {
            var app = Object.FindFirstObjectByType<KioskApp>(FindObjectsInactive.Include);
            if (app != null) return app.gameObject;
            return GameObject.Find(KioskLayout.CanvasName);
        }
    }
}
