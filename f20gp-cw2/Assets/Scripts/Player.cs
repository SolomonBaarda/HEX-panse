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

    public HashSet<Vector3Int> ValidMovesThisTurn;

    public Renderer Renderer;

    [Space]
    public TMP_Text StrengthText;

    [Space]
    public ParticleSystem Particle;

    private void Update()
    {
        StrengthText.transform.rotation = Camera.main.transform.rotation;
    }

    public void UpdatePlayer()
    {
        StrengthText.text = $"strength: {Strength}";
    }

    public void Init(uint id, Color colour, Vector3Int startingCity, int strength)
    {
        ID = id;
        Colour = colour;
        CurrentCell = startingCity;
        Strength = strength;

        Renderer.material.color = colour;
    }

    public void MoveToPosition(Vector3 start, Vector3 destination, float timeSeconds)
    {
        StartCoroutine(MoveThroughPositions(start, destination, timeSeconds));
    }

    private IEnumerator MoveThroughPositions(Vector3 start, Vector3 destination, float timeSeconds)
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
}
