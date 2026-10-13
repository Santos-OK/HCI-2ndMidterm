using QuestKiosk;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuestKioskEditor
{
    /// <summary>
    /// Quest Kiosk > Add reset menu (hamburger): puts the hamburger button and its menu inside Screen_Main
    /// of the open scene, so they can be styled there. Nothing else in the scene is touched.
    /// </summary>
    public static class KioskResetMenuEditor
    {
        [MenuItem("Quest Kiosk/Add reset menu (hamburger)")]
        static void AddResetMenu()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Quest Kiosk", "Stop Play mode first.", "OK");
                return;
            }

            var app = Object.FindFirstObjectByType<KioskApp>(FindObjectsInactive.Include);
            var main = app == null ? null : UiKit.FindDeep(app.transform, "Screen_Main");
            if (main == null)
            {
                EditorUtility.DisplayDialog("Quest Kiosk",
                    "Screen_Main was not found. Open Main_Screen.unity first (the one with KioskCanvas).", "OK");
                return;
            }

            if (UiKit.FindDeep(main, "Btn_Menu") != null)
            {
                EditorUtility.DisplayDialog("Quest Kiosk", "Screen_Main already has the reset menu.", "OK");
                return;
            }

            KioskApp.BuildResetMenu(main);

            var burger = UiKit.FindDeep(main, "Btn_Menu");
            var menu = UiKit.FindDeep(main, "Menu");
            if (burger != null) Undo.RegisterCreatedObjectUndo(burger.gameObject, "Add reset menu");
            if (menu != null) Undo.RegisterCreatedObjectUndo(menu.gameObject, "Add reset menu");

            EditorSceneManager.MarkSceneDirty(main.gameObject.scene);
            Debug.Log("[Quest Kiosk] Reset menu added to Screen_Main. Save the scene (Ctrl+S) to keep it.");
        }
    }
}
