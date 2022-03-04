using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Renderer))]
public class Player : MonoBehaviour
{
    public uint ID;
    public Color Colour;

    public Vector3Int CurrentCell;

    public readonly List<Vector3Int> ControlledCities = new List<Vector3Int>();


    new Renderer renderer;

    public HashSet<Vector3Int> ValidMovesThisTurn;

    private void Awake()
    {
        renderer = GetComponent<Renderer>();
    }

    public void Init(uint id, Color colour, Vector3Int startingCity)
    {
        ID = id;
        Colour = colour;

        CurrentCell = startingCity;
        ControlledCities.Add(startingCity);

        renderer.material.color = colour;
    }
}
