using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{

    public Button createGameButton;
    public Button helpButton;
    public GameObject helpMenu;

    void Start()
    {
        createGameButton.onClick.AddListener(CreateGamePressed);
        helpButton.onClick.AddListener(HelpPressed);
    }
    void CreateGamePressed()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Additive);
    }

    void HelpPressed()
    {
        helpMenu.SetActive(true);
    }
}
