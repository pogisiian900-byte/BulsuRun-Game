using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseContainer;

    private MobileInput mobileInput;
    private bool isPaused;
    private bool pauseInputBlocked;

    private void Awake()
    {
        // auto-find MobileInput in scene
        mobileInput = FindFirstObjectByType<MobileInput>();
    }

    private void Start()
    {
        if (pauseContainer != null)
            pauseContainer.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    private void Update()
    {
        if (pauseInputBlocked)
        {
            if (mobileInput != null)
                mobileInput.ConsumePausePressed();

            return;
        }

        // ✅ Mobile pause button
        if (mobileInput != null && mobileInput.ConsumePausePressed())
        {
            if (isPaused) ResumeButton();
            else Pause();
        }

        // ✅ Optional: still allow ESC on PC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeButton();
            else Pause();
        }
    }

    private void Pause()
    {
        if (pauseContainer != null)
            pauseContainer.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeButton()
    {
        if (pauseContainer != null)
            pauseContainer.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void BacktoMainMenu()
    {
        Time.timeScale = 1f;
        if (SinglePlayerSaveSystem.IsCheckpointRestoreQueued)
            SinglePlayerSaveSystem.RestoreCheckpoint();
        else
            SinglePlayerSaveSystem.SaveCheckpoint();

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("Worlds");
            }

            return;
        }

        SceneManager.LoadScene("Worlds");
    }

    public void SetPauseInputBlocked(bool isBlocked)
    {
        pauseInputBlocked = isBlocked;
    }
}
