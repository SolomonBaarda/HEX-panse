using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOver : MonoBehaviour
{
    public Player winner;
    public TextMeshProUGUI header;

    public void SetWinner(Player player)
    {
        winner = player;
        UpdateText();
    }

    void UpdateText()
    {
        header.text = $"Congratulations Team {winner.Nickname}";
        header.color = winner.Colour;
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
