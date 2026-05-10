using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures game starts from MainMenu and cleans stale gameplay UI in Main.
/// </summary>
public static class BootFlowGuard
{
    const string MainScene = "Main";
    const string MenuScene = "MainMenu";
    const string StartedFromMenuKey = "StartedFromMenu";
    const string StartedFromMenuAtUtcTicksKey = "StartedFromMenuAtUtcTicks";
    static readonly long StartedFromMenuFreshWindowTicks = System.TimeSpan.FromSeconds(20).Ticks;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad()
    {
        Scene s = SceneManager.GetActiveScene();
        if (!s.IsValid()) return;

        if (s.name == MainScene)
        {
            int startedFromMenu = PlayerPrefs.GetInt(StartedFromMenuKey, 0);
            long atTicks;
            long.TryParse(PlayerPrefs.GetString(StartedFromMenuAtUtcTicksKey, "0"), out atTicks);

            bool freshFromMenu = startedFromMenu == 1 &&
                                  atTicks > 0 &&
                                  (System.DateTime.UtcNow.Ticks - atTicks) <= StartedFromMenuFreshWindowTicks;

            if (!freshFromMenu && Application.CanStreamedLevelBeLoaded(MenuScene))
            {
                // stale key -> force menu
                PlayerPrefs.SetInt(StartedFromMenuKey, 0);
                PlayerPrefs.SetString(StartedFromMenuAtUtcTicksKey, "0");
                PlayerPrefs.Save();
                SceneManager.LoadScene(MenuScene);
                return;
            }

            // consume flag once entering gameplay
            PlayerPrefs.SetInt(StartedFromMenuKey, 0);
            PlayerPrefs.SetString(StartedFromMenuAtUtcTicksKey, "0");
            PlayerPrefs.Save();
            CleanupMainUiState();
        }
    }

    static void CleanupMainUiState()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = true;
        }

        var map = Object.FindObjectOfType<MapUIController>(true);
        if (map != null && map.panel != null) map.panel.SetActive(false);

        var inv = Object.FindObjectOfType<InventoryToggle>(true);
        if (inv != null && inv.inventoryPanel != null) inv.inventoryPanel.SetActive(false);

        var ending = Object.FindObjectOfType<EndingManager>(true);
        if (ending != null) ending.ResetUiForNewRun();

        var flow = Object.FindObjectOfType<PrologueFlowManager>(true);
        if (flow != null) flow.ResetForNewRun();

        var monsters = Object.FindObjectsOfType<MonsterAI>(true);
        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] != null) monsters[i].ResetForNewRun();
        }
    }
}
