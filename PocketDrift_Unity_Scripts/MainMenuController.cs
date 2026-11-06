using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Loading")]
    public string gameSceneName = "WorkbenchArena";
    public GameObject loadingOverlay;
    public Slider loadingProgressBar;

    [Header("Menu Panels")]
    public GameObject mainButtonsPanel;
    public GameObject garagePanel;
    public GameObject settingsPanel;
    public GameObject quitConfirmPanel;

    [Header("Default Selected Buttons (Controller Focus)")]
    public Button playButton;
    public Button garageFirstButton;
    public Button settingsFirstButton;
    public Button quitConfirmYesButton;

    [Header("Gamepad & Input Prompts")]
    public Text controllerStatusText;
    public Text inputPromptText;

    [Header("Audio SFX (Optional)")]
    public AudioSource audioSource;
    public AudioClip buttonHoverSfx;
    public AudioClip buttonClickSfx;

    [Header("Background 3D Turntable (Optional)")]
    public Transform vehicleTurntable;
    public float turntableRotationSpeed = 22f;

    private GameObject currentActivePanel;

    private void Start()
    {
        // Default to Main Buttons Panel
        ShowPanel(mainButtonsPanel);

        // Ensure EventSystem focuses first button for Razer Wolverine TE / Gamepad
        StartCoroutine(FocusButtonNextFrame(playButton));

        // Update Controller Status
        DetectActiveController();

        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
        }
    }

    private void Update()
    {
        // Rotate 3D vehicle on turntable if present
        if (vehicleTurntable != null)
        {
            vehicleTurntable.Rotate(Vector3.up, turntableRotationSpeed * Time.deltaTime, Space.World);
        }

        // Razer Wolverine TE / Gamepad Back Button (Button B / JoystickButton1 or Escape)
        if (Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackNavigation();
        }

        // Periodic check for controller connect/disconnect
        if (Time.frameCount % 120 == 0)
        {
            DetectActiveController();
        }

        // Keep selection active if controller is being used and nothing is selected
        EnsureGamepadSelection();
    }

    private void EnsureGamepadSelection()
    {
        if (EventSystem.current == null) return;

        // If user touches stick or d-pad and nothing is focused, restore focus
        float stickX = Input.GetAxisRaw("Horizontal");
        float stickY = Input.GetAxisRaw("Vertical");

        if ((Mathf.Abs(stickX) > 0.2f || Mathf.Abs(stickY) > 0.2f) &&
            (EventSystem.current.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy))
        {
            if (currentActivePanel == mainButtonsPanel && playButton != null)
            {
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
            }
            else if (currentActivePanel == garagePanel && garageFirstButton != null)
            {
                EventSystem.current.SetSelectedGameObject(garageFirstButton.gameObject);
            }
            else if (currentActivePanel == settingsPanel && settingsFirstButton != null)
            {
                EventSystem.current.SetSelectedGameObject(settingsFirstButton.gameObject);
            }
        }
    }

    private void DetectActiveController()
    {
        string[] joysticks = Input.GetJoystickNames();
        bool hasController = false;
        string controllerName = "Gamepad";

        if (joysticks != null)
        {
            for (int i = 0; i < joysticks.Length; i++)
            {
                if (!string.IsNullOrEmpty(joysticks[i]))
                {
                    hasController = true;
                    string rawName = joysticks[i].ToLower();
                    if (rawName.Contains("razer") || rawName.Contains("wolverine"))
                    {
                        controllerName = "Razer Wolverine TE";
                    }
                    else if (rawName.Contains("xbox") || rawName.Contains("xinput"))
                    {
                        controllerName = "Xbox / Wolverine Gamepad";
                    }
                    else
                    {
                        controllerName = joysticks[i];
                    }
                    break;
                }
            }
        }

        if (controllerStatusText != null)
        {
            if (hasController)
            {
                controllerStatusText.text = $"<color=#00FFAA>[CONNECTED]</color> {controllerName}";
            }
            else
            {
                controllerStatusText.text = "<color=#FFFF00>[STANDBY]</color> Keyboard / Touch Controls Active";
            }
        }

        if (inputPromptText != null)
        {
            if (hasController)
            {
                inputPromptText.text = "[A] Select   [B] Back   [D-Pad / L-Stick] Navigate   [RT] Gas in Race";
            }
            else
            {
                inputPromptText.text = "[ENTER / SPACE] Select   [ESC] Back   [WASD / ARROWS] Navigate";
            }
        }
    }

    public void PlayGame()
    {
        PlaySound(buttonClickSfx);
        StartCoroutine(LoadSceneAsync(gameSceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"[Pocket Drift] Failed to load scene '{sceneName}'. Check Build Settings!");
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            }
            yield return null;
        }
    }

    public void OpenGarage()
    {
        PlaySound(buttonClickSfx);
        ShowPanel(garagePanel);
        StartCoroutine(FocusButtonNextFrame(garageFirstButton));
    }

    public void OpenSettings()
    {
        PlaySound(buttonClickSfx);
        ShowPanel(settingsPanel);
        StartCoroutine(FocusButtonNextFrame(settingsFirstButton));
    }

    public void OpenQuitConfirmation()
    {
        PlaySound(buttonClickSfx);
        ShowPanel(quitConfirmPanel);
        StartCoroutine(FocusButtonNextFrame(quitConfirmYesButton));
    }

    public void CloseSubPanel()
    {
        PlaySound(buttonClickSfx);
        ShowPanel(mainButtonsPanel);
        StartCoroutine(FocusButtonNextFrame(playButton));
    }

    private void HandleBackNavigation()
    {
        if (currentActivePanel == garagePanel || currentActivePanel == settingsPanel)
        {
            CloseSubPanel();
        }
        else if (currentActivePanel == quitConfirmPanel)
        {
            CloseSubPanel();
        }
        else if (currentActivePanel == mainButtonsPanel)
        {
            OpenQuitConfirmation();
        }
    }

    public void ConfirmQuit()
    {
        PlaySound(buttonClickSfx);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowPanel(GameObject panelToShow)
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (garagePanel != null) garagePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            currentActivePanel = panelToShow;
        }
        else if (mainButtonsPanel != null)
        {
            mainButtonsPanel.SetActive(true);
            currentActivePanel = mainButtonsPanel;
        }
    }

    private IEnumerator FocusButtonNextFrame(Button button)
    {
        yield return null; // Wait one frame for UI hierarchy to update
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    public void OnButtonHover()
    {
        PlaySound(buttonHoverSfx);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
