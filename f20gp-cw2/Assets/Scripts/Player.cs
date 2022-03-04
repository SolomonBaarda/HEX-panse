using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


[RequireComponent(typeof(Renderer))]
public class Player : MonoBehaviour
{
    public uint ID;
    public Color Colour;
    public Vector3Int CurrentCell;
    public int Strength;

    public HashSet<Vector3Int> ValidMovesThisTurn;

    new Renderer renderer;

    [Space]
    public TMP_Text StrengthText;

    private void Awake()
    {
        renderer = GetComponent<Renderer>();
    }

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

        renderer.material.color = colour;
    }
}
