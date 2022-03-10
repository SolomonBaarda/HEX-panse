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
    public TMP_InputField seedInput;
    public TMP_InputField playerInput;
    public int seed;

    [Range(2, 6)]
    public uint NumberOfPlayers;

    void Start()
    {
        createGameButton.onClick.AddListener(CreateGamePressed);
        helpButton.onClick.AddListener(HelpPressed);
    }
    void CreateGamePressed()
    {
        StartCoroutine(LoadGame());
    }

    void HelpPressed()
    {
        helpMenu.SetActive(true);
    }

    IEnumerator LoadGame()
    {
        if(int.TryParse(seedInput.text, out int seedOut))
        {
            seed = seedOut; 
        }
        else
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
        }
        if(uint.TryParse(playerInput.text, out uint playerOut))
        {
            if(playerOut >=2 && playerOut <= 6)
            {
                NumberOfPlayers = playerOut; 
            }
            else
            {
                NumberOfPlayers = 3;
            }
        }
        else
        {
            NumberOfPlayers = 3;
        }
        AsyncOperation load = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
        while(!load.isDone)
        {
            yield return null;
        }
        GameManager gameManager = GameObject.FindObjectOfType<GameManager>();
        gameManager.StartGame(seed);
        gameManager.NumberOfPlayers = NumberOfPlayers;
    }
}
