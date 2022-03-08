using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Base : MonoBehaviour
{
    public int Strength;
    public Vector3Int Cell;

    public Player OwnedBy;

    [Space]
    public TMP_Text StrengthText;

    [Space]
    public Color DefaultColour = Color.white;


    public Renderer Renderer;

    private void Update()
    {
        StrengthText.transform.rotation = Camera.main.transform.rotation;
    }

    public void UpdateCity()
    {
        StrengthText.text = $"strength: {Strength}";
        Renderer.material.color = OwnedBy != null ? OwnedBy.Colour : DefaultColour;
    }

    public void Init(Vector3Int cell, int strength)
    {
        Cell = cell;
        Strength = strength;

        if (OwnedBy != null)
        {
            Renderer.material.color = OwnedBy.Colour;
        }

        UpdateCity();
    }

    public void PlayerCaptureCity(Player player)
    {
        OwnedBy = player;
        Strength += player.Strength;

        player.Strength = 0;
        player.gameObject.SetActive(false); 

        UpdateCity();
    }

    public void PlayerLeaveCity(Player player, int strengthToLeave)
    {
        // Player keeps control of the city 
        if(strengthToLeave < Strength)
        {
            player.Strength = Strength - strengthToLeave;
            Strength = strengthToLeave;
            OwnedBy = player;
        }
        // Player loses control of the city
        else if(strengthToLeave >= Strength)
        {
            player.Strength = Strength;
            Strength = 0;
            OwnedBy = null;
        }

        player.gameObject.SetActive(true);

        UpdateCity();
        player.UpdatePlayer();
    }
}