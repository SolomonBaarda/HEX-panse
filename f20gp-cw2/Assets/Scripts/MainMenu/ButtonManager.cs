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
    public Button randomSeedButton;
    public GameObject helpMenu;
    public TMP_InputField seedInput;
    public int seed;

    public Slider NumberPlayersSlider;
    public TMP_Text NumberPlayersText;

    [Range(2, 6)]
    public uint NumberOfPlayers;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        seedInput.text = Noise.RandomSeed.ToString();
    }

    void Start()
    {
        createGameButton.onClick.AddListener(CreateGamePressed);
        helpButton.onClick.AddListener(HelpPressed);
        randomSeedButton.onClick.AddListener(() => seedInput.text = Noise.RandomSeed.ToString());
        NumberPlayersSlider.onValueChanged.AddListener((value) => NumberPlayersText.text = uint.Parse(value.ToString()).ToString());
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
        if (int.TryParse(seedInput.text, out int seedOut))
        {
            seed = seedOut;
        }
        else
        {
            seed = Noise.RandomSeed;
        }

        if (uint.TryParse(NumberPlayersSlider.value.ToString(), out uint playerOut))
        {
            if (playerOut >= 2 && playerOut <= 6)
            {
                NumberOfPlayers = playerOut;
            }
            else
            {
                NumberOfPlayers = 2;
            }
        }
        else
        {
            NumberOfPlayers = 2;
        }

        Scene current = SceneManager.GetActiveScene();

        AsyncOperation load = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
        while (!load.isDone)
        {
            yield return null;
        }
        GameManager gameManager = GameObject.FindObjectOfType<GameManager>();
        gameManager.StartGame(seed);
        gameManager.NumberOfPlayers = NumberOfPlayers;

        SceneManager.UnloadSceneAsync(current);
    }
}
