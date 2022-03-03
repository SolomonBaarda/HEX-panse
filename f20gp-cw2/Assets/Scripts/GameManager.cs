using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Range(2, 6)]
    public uint NumberOfPlayers = 6;

    public int PlayerTurn;


    [Header("Camera Stuff")]
    public Camera Camera;
    public LayerMask MouseLayerMask;
    public float MouseRaycastDistance = 100.0f;

    public bool IsHoveringOverCell { get; protected set; } = false;
    public Vector3Int CellHoveringOver { get; protected set; } = default;

    public Transform HoverCellPreview;

    [Header("Terrain Stuff")]
    public TerrainGenerator TerrainGenerator;
    public HexMap HexMap;
    public Transform GameObjectPrent;
    public GameObject CityPrefab;

    [Header("Players")]
    public List<Player> Players = new List<Player>();
    public PlayerSettings PlayerSettings;

    private void Start()
    {
        StartCoroutine(StartGame());
    }

    public IEnumerator StartGame()
    {
        TerrainGenerator.Generate();

        while(TerrainGenerator.IsGenerating)
        {
            yield return null;
        }

        List<Vector3Int> cities = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, HexMap.Hexagon> hex in HexMap.Hexagons)
        {
            if (hex.Value.Biome is Biome.PlayerCity)
            {
                cities.Add(hex.Key);
                //Instantiate(CityPrefab, hex.Value.CentreOfFaceWorld, Quaternion.identity, GameObjectPrent);
            }
            else if(hex.Value.Biome is Biome.EnemyCity)
            {
                //Instantiate(CityPrefab, hex.Value.CentreOfFaceWorld, Quaternion.identity, GameObjectPrent);
            }
        }

        for (uint i = 0; i < NumberOfPlayers; i++)
        {
            Players.Add(new Player(i, PlayerSettings.GetPlayerColour(i)));
        }
    }

    private void Update()
    {
        IsHoveringOverCell = false;
        Vector3 viewport = Camera.ScreenToViewportPoint(Input.mousePosition);

        // Mouse within window
        if (viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1)
        {
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, MouseRaycastDistance, MouseLayerMask))
            {
                Vector3Int cell = HexMap.Grid.WorldToCell(new Vector3(hit.point.x, 0, hit.point.z));
                if (HexMap.Hexagons.ContainsKey(cell) && HexMap.Hexagons[cell].Biome != Biome.None)
                {
                    IsHoveringOverCell = true;
                    CellHoveringOver = cell;

                    Vector3 previewPosition = HexMap.Hexagons[cell].CentreOfFaceWorld;
                    previewPosition.y += 0.001f;
                    HoverCellPreview.position = previewPosition;
                }
            }
        }

        HoverCellPreview.gameObject.SetActive(IsHoveringOverCell);
    }

    public class Player
    {
        public readonly uint ID;
        public Color Colour;

        public Player(uint id, Color colour)
        {
            ID = id;
            Colour = colour;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (IsHoveringOverCell)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(HexMap.Hexagons[CellHoveringOver].CentreOfFaceWorld, 0.25f);
        }
    }
}
