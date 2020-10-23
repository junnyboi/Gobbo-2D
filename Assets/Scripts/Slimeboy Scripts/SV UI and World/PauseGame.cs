using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseGame : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel = null;
    void Start()
    {
        pausePanel.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!pausePanel.activeInHierarchy)
            {
                WorldManager.Instance.Print("Paused! Press Esc to continue.");
                Pause();
            }
            else if (pausePanel.activeInHierarchy)
            {
                WorldManager.Instance.Print("Unpaused! Press Esc to pause.");
                ContinueGame();
            }
        }
    }
    private void Pause()
    {
        Time.timeScale = 0;
        pausePanel.SetActive(true);
        //Disable scripts that still work while timescale is set to 0
    }
    private void ContinueGame()
    {
        Time.timeScale = 1;
        pausePanel.SetActive(false);
        //enable the scripts again
    }
}
