using System.IO;
using QuestKiosk;
using UnityEditor;
using UnityEngine;

namespace QuestKioskEditor
{
    /// <summary>
    /// Quest Kiosk > Saved data: helpers to test persistence from the editor.
    /// </summary>
    public static class KioskSaveMenu
    {
        [MenuItem("Quest Kiosk/Saved data/Delete saved data (start from scratch)")]
        static void DeleteSavedData()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Quest Kiosk", "Stop Play mode first, otherwise the running app writes the file again.", "OK");
                return;
            }

            string path = KioskPersistence.SavePath;
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Quest Kiosk", "There is no saved data.", "OK");
                return;
            }

            File.Delete(path);
            Debug.Log("[Quest Kiosk] Saved data deleted. The next Play starts with the default inventory.");
        }

        [MenuItem("Quest Kiosk/Saved data/Show saved data folder")]
        static void ShowFolder()
        {
            string path = KioskPersistence.SavePath;
            if (File.Exists(path)) EditorUtility.RevealInFinder(path);
            else EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}
