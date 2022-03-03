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
    public PlayerSettings PlayerSettings;
    List<Player> Players = new List<Player>();

    List<Vector3Int> EnemyCities = new List<Vector3Int>();

    Dictionary<Vector3Int, GameObject> GameObjects = new Dictionary<Vector3Int, GameObject>();

    private void Start()
    {
        StartGame();
    }

    public void Clear()
    {
        StopAllCoroutines();

        foreach (KeyValuePair<Vector3Int, GameObject> pair in GameObjects)
        {
            Destroy(pair.Value);
        }

        HexMap.Clear();
        GameObjects.Clear();
        EnemyCities.Clear();
        Players.Clear();
    }

    public void StartGame()
    {
        StartCoroutine(WaitForStartGame());
    }

    private IEnumerator WaitForStartGame()
    {
        TerrainGenerator.Generate();

        while (TerrainGenerator.IsGenerating)
        {
            yield return null;
        }

        System.Random r = new System.Random(TerrainGenerator.Seed);

        List<Vector3Int> cities = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, HexMap.Hexagon> hex in HexMap.Hexagons)
        {
            // Instantiate city prefabs
            if (hex.Value.Biome is Biome.PlayerCity)
            {
                cities.Add(hex.Key);
                GameObjects[hex.Key] = Instantiate(CityPrefab, hex.Value.CentreOfFaceWorld, Quaternion.identity, GameObjectPrent);
            }
            else if (hex.Value.Biome is Biome.EnemyCity)
            {
                EnemyCities.Add(hex.Key);
                GameObjects[hex.Key] = Instantiate(CityPrefab, hex.Value.CentreOfFaceWorld, Quaternion.identity, GameObjectPrent);
            }
        }

        // Choose player starting positions
        switch (cities.Count)
        {
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
            default:
                // Shuffle the list of player cities
                int n = cities.Count;
                while (n > 1)
                {
                    n--;
                    int k = r.Next(n + 1);
                    Vector3Int value = cities[k];
                    cities[k] = cities[n];
                    cities[n] = value;
                }
                break;
        }

        for (uint i = 0; i < NumberOfPlayers; i++)
        {
            Vector3Int city = cities[0];
            cities.RemoveAt(0);
            Player player = new Player(i, PlayerSettings.GetPlayerColour(i), city);
            Players.Add(player);

            GameObject model = GameObjects[city];
            model.GetComponent<MeshRenderer>().material.color = player.Colour;
        }

        EnemyCities.AddRange(cities);

        Debug.Log($"Playing with {Players.Count} players and {EnemyCities.Count} enemies");
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
        public readonly Color Colour;

        public readonly List<Vector3Int> ControlledCities = new List<Vector3Int>();

        public Player(uint id, Color colour, Vector3Int startingCity)
        {
            ID = id;
            Colour = colour;

            ControlledCities.Add(startingCity);
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
