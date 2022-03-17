using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    public uint ID;
    public Color Colour;
    public Vector3Int CurrentCell;
    public int Strength;
    public string Nickname;

    public HashSet<Vector3Int> ValidMovesThisTurn;

    public Renderer Renderer;

    [Space]
    public TMP_Text StrengthText;
    public TMP_Text StrengthChangeText;

    [Space]
    public ParticleSystem Particle;

    [Space]
    public bool IsDead = false;

    private void Awake()
    {
        StrengthChangeText.enabled = false;
    }

    private void Update()
    {
        StrengthText.transform.rotation = Camera.main.transform.rotation;
        StrengthChangeText.transform.rotation = Camera.main.transform.rotation;
    }

    public void UpdatePlayer()
    {
        StrengthText.text = $"health: {Strength}";
    }

    public void Init(uint id, string nickname, Color colour, Vector3Int startingCity, int strength)
    {
        ID = id;
        Nickname = nickname;
        Colour = colour;
        CurrentCell = startingCity;
        Strength = strength;

        Renderer.material.color = colour;
    }

    public void MoveToPosition(Vector3 start, Vector3 destination, float timeSeconds)
    {
        IEnumerator MoveThroughPositions()
        {
            Particle.Play();
            float timer = 0;
            Vector3 direction = destination - start;

            while (timer < timeSeconds)
            {
                transform.position = start + direction * timer / timeSeconds;

                yield return null;
                timer += Time.deltaTime;
            }

            Particle.Stop();
        }

        StartCoroutine(MoveThroughPositions());
    }

    public void Kill()
    {
        IsDead = true;
        gameObject.SetActive(false);
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
