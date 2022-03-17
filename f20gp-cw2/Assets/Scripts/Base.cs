using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Base : MonoBehaviour
{
    public int MaxStrength;
    public int Strength;
    public Vector3Int Cell;

    public Player OwnedBy;

    [Space]
    public TMP_Text StrengthText;
    public TMP_Text StrengthChangeText;

    [Space]
    public Color DefaultUnclaimedColour = Color.white;
    public Color DefaultColour = Color.black;

    [Space]
    public ParticleSystem Particle;

    public Renderer Renderer;


    private void Awake()
    {
        StrengthChangeText.enabled = false;
    }

    private void Update()
    {
        StrengthText.transform.rotation = Camera.main.transform.rotation;
        StrengthChangeText.transform.rotation = Camera.main.transform.rotation;
    }

    public void UpdateBase()
    {
        StrengthText.text = Strength > 0 ? $"health: {Strength}" : "";

        if (Strength <= 0)
        {
            OwnedBy = null;
        }

        if (OwnedBy != null)
        {
            Renderer.material.color = OwnedBy.Colour;
        }
        else
        {
            Renderer.material.color = Strength > 0 ? DefaultColour : DefaultUnclaimedColour;
        }

        if(Particle != null)
        {
            if (Strength > 0)
            {
                Particle.Play();
            }
            else
            {
                Particle.Stop();
            }
        }
    }

    public void Init(Vector3Int cell, int strength, int maxStrength)
    {
        Cell = cell;
        Strength = strength;
        MaxStrength = maxStrength;

        if (OwnedBy != null)
        {
            Renderer.material.color = OwnedBy.Colour;
        }

        UpdateBase();
    }

    public void PlayerCaptureBase(Player player)
    {
        OwnedBy = player;
        Strength += player.Strength;

        player.Strength = 0;
        player.gameObject.SetActive(false);

        UpdateBase();
    }

    public void PlayerLeaveBase(Player player, int strengthToLeave)
    {
        // Player keeps control of the city 
        if (strengthToLeave < Strength)
        {
            player.Strength = Strength - strengthToLeave;
            Strength = strengthToLeave;
            OwnedBy = player;
        }
        // Player loses control of the city
        else if (strengthToLeave >= Strength)
        {
            player.Strength = Strength;
            Strength = 0;
            OwnedBy = null;
        }

        player.gameObject.SetActive(true);

        UpdateBase();
        player.UpdatePlayer();
    }

    public void DisplayStrengthChangeText(int damage, float seconds)
    {
        IEnumerator Display()
        {
            StrengthChangeText.text = damage <= 0 ? damage.ToString() : $"+{damage}";
            StrengthChangeText.color = damage < 0 ? Color.red : Color.green;
            StrengthChangeText.enabled = true;

            float timer = 0;

            while (timer < seconds)
            {
                yield return null;
                timer += Time.deltaTime;
            }

            StrengthChangeText.enabled = false;
        }

        StartCoroutine(Display());
    }
}
