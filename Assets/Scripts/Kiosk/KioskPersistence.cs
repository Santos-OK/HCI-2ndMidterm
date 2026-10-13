using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QuestKiosk
{
    /// <summary>
    /// Keeps the inventory (the 6 kits and the loan list) between runs of the app.
    ///
    /// - When the app starts it reads kiosk_state.json and puts it into the KioskService.
    /// - While the app runs it saves that file whenever something changed (checked every second),
    ///   and once more when the app is closed or paused.
    /// - Times are stored as absolute dates, so a loan that ended while the app was closed is released
    ///   by the normal expiry check right after loading.
    ///
    /// The file lives in Application.persistentDataPath (outside the project, so it is never committed).
    /// It creates itself at startup; it needs no object in the scene.
    /// </summary>
    public class KioskPersistence : MonoBehaviour
    {
        const string FileName = "kiosk_state.json";
        const float SaveInterval = 1f;

        public static string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        KioskApp app;
        KioskService service;
        string lastSaved = "";
        float timer;
        bool warnedSave;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<KioskPersistence>() != null) return;
            new GameObject("KioskPersistence").AddComponent<KioskPersistence>();
        }

        void Start()
        {
            app = FindFirstObjectByType<KioskApp>();
            service = app == null ? null : app.PersistenceService;

            if (service == null)
            {
                Debug.LogWarning("[Quest Kiosk] Persistence is off: KioskApp or its service was not found.");
                enabled = false;
                return;
            }

            if (TryLoad())
            {
                service.Refresh();            // release anything whose time passed while the app was closed
                app.PersistenceRefreshView(); // show the loaded data on the current screen
            }

            SaveIfChanged(); // first run: writes the starting inventory
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < SaveInterval) return;

            timer = 0f;
            SaveIfChanged();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveIfChanged();
        }

        void OnApplicationQuit()
        {
            SaveIfChanged();
        }

        // ------------------------------------------------------------------ load

        bool TryLoad()
        {
            string path = SavePath;
            if (!File.Exists(path)) return false;

            try
            {
                var loaded = JsonUtility.FromJson<KioskState>(File.ReadAllText(path));

                if (loaded == null || loaded.kits == null || loaded.kits.Count != KioskService.KitCount)
                {
                    Debug.LogWarning("[Quest Kiosk] Saved data is not valid, starting with the default inventory.");
                    return false;
                }

                var ids = new HashSet<int>();
                foreach (var kit in loaded.kits)
                {
                    if (kit == null || kit.id < 1 || kit.id > KioskService.KitCount || !ids.Add(kit.id))
                    {
                        Debug.LogWarning("[Quest Kiosk] Saved data has a wrong kit list, starting with the default inventory.");
                        return false;
                    }
                }

                if (loaded.loans == null) loaded.loans = new List<LoanRecord>();

                // The service object stays the same, only its content is replaced.
                service.State.kits.Clear();
                service.State.kits.AddRange(loaded.kits);
                service.State.loans.Clear();
                service.State.loans.AddRange(loaded.loans);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Quest Kiosk] Could not read saved data (" + e.Message + "). Starting with the default inventory.");
                return false;
            }
        }

        // ------------------------------------------------------------------ save

        void SaveIfChanged()
        {
            if (service == null || service.State == null) return;

            string json = JsonUtility.ToJson(service.State, true);
            if (json == lastSaved) return;

            try
            {
                string path = SavePath;
                string temp = path + ".tmp";

                // Write to a temporary file first so a crash never leaves half a file.
                File.WriteAllText(temp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);

                lastSaved = json;
                warnedSave = false;
            }
            catch (Exception e)
            {
                if (!warnedSave)
                {
                    Debug.LogWarning("[Quest Kiosk] Could not save data (" + e.Message + ").");
                    warnedSave = true;
                }
            }
        }
    }

    // Small bridge so the persistence component can reach the service and refresh the screen.
    public partial class KioskApp
    {
        internal KioskService PersistenceService
        {
            get { return service; }
        }

        internal void PersistenceRefreshView()
        {
            Rebuild();
        }
    }
}
