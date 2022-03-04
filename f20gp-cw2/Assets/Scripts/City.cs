using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class City : MonoBehaviour
{
    public int Strength;
    public CityType Type;
    public Vector3Int Cell;

    public Player OwnedBy;


    new Renderer renderer;

    private void Awake()
    {
        renderer = GetComponent<Renderer>();
    }

    public void Init(Vector3Int cell, CityType type, Player owner)
    {
        Cell = cell;
        Type = type;
        OwnedBy = owner;

        if (OwnedBy != null)
        {
            renderer.material.color = OwnedBy.Colour;
        }
    }

    public void PlayerCaptureCity(Player player)
    {
        Type = CityType.Player;
        OwnedBy = player;

        renderer.material.color = OwnedBy.Colour;
    }

    public enum CityType
    {
        Empty,
        Enemy,
        Player
    }
}
