using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Range(2, 6)]
    public uint NumberOfPlayers = 6;

    public int PlayerTurn;



    public LayerMask MouseLayerMask;
    public float MouseRaycastDistance = 100.0f;

    public bool IsHoveringOverCell { get; protected set; } = false;
    public Vector3Int CellHoveringOver { get; protected set; } = default;

    public Transform HoverCellPreview;

    public CameraManager CameraManager;

    [Header("Terrain Stuff")]
    public TerrainGenerator TerrainGenerator;
    public HexMap HexMap;
    public Transform GameObjectPrent;
    public GameObject CityPrefab;

    [Header("Players")]
    public PlayerSettings PlayerSettings;
    List<Player> Players = new List<Player>();

    Player currentPlayer;

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
        StartCoroutine(PlayGame());
    }

    private IEnumerator PlayGame()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("HUD", LoadSceneMode.Additive);

        // Wait for HUD to load
        while (!load.isDone)
        {
            yield return null;
        }

        TerrainGenerator.Generate();

        while (TerrainGenerator.IsGenerating)
        {
            yield return null;
        }

        System.Random r = new System.Random(TerrainGenerator.Seed);

        List<Vector3Int> cities = new List<Vector3Int>();
        List<Vector3> cameraCityPositions = new List<Vector3>();

        foreach (KeyValuePair<Vector3Int, HexMap.Hexagon> hex in HexMap.Hexagons)
        {
            // Instantiate city prefabs
            if (hex.Value.Biome is Biome.PlayerCity)
            {
                cities.Add(hex.Key);
                GameObjects[hex.Key] = Instantiate(CityPrefab, hex.Value.CentreOfFaceWorld, Quaternion.identity, GameObjectPrent);

                cameraCityPositions.Add(HexMap.Hexagons[hex.Key].CentreOfFaceWorld);
            }
            else if (hex.Value.Biome is Biome.EnemyCity)
            {
                EnemyCities.Add(hex.Key);
                GameObjects[hex.Key] = Instantiate(CityPrefab, hex.Value.CentreOfFaceWorld, Quaternion.identity, GameObjectPrent);
            }
        }




        CameraManager.SetupCameras(cameraCityPositions);

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


        while (true)
        {
            foreach (Player p in Players)
            {
                // Do player turn
                currentPlayer = p;
                PlayerTurn = (int)p.ID;

                HUD.Instance.PlayerTurnText.text = $"Current turn: player {PlayerTurn}";
                HUD.Instance.PlayerTurnText.color = p.Colour;

                // Set the position that the camera should try and move to
                CameraManager.CameraFollow.position = HexMap.Hexagons[p.CurrentCell].CentreOfFaceWorld;

                // Wait here while it is this players turn
                while (currentPlayer == p)
                {
                    yield return null;
                }
            }

            // Do enemy turn
        }
    }


    private void Update()
    {
        if (currentPlayer != null)
        {
            UpdateHighlight();

            if (Input.GetButtonDown("Fire1"))
            {
                currentPlayer = null;
            }
        }
    }

    private void UpdateHighlight()
    {
        IsHoveringOverCell = false;

        if (CameraManager.IsHoveringMouseOverTerrain(MouseRaycastDistance, MouseLayerMask, out Vector3 position))
        {
            Vector3Int cell = HexMap.Grid.WorldToCell(new Vector3(position.x, 0, position.z));
            if (HexMap.Hexagons.ContainsKey(cell) && CanMoveToCell(currentPlayer, cell))
            {
                IsHoveringOverCell = true;
                CellHoveringOver = cell;

                Vector3 previewPosition = HexMap.Hexagons[cell].CentreOfFaceWorld;
                previewPosition.y += 0.001f;
                HoverCellPreview.position = previewPosition;
            }
        }

        HoverCellPreview.gameObject.SetActive(IsHoveringOverCell);
    }

    private bool CanMoveToCell(Player p, Vector3Int cell)
    {
        if (HexMap.Hexagons[cell].Biome != Biome.None && cell != p.CurrentCell)
        {
            return true;
        }

        return false;
    }

    public class Player
    {
        public readonly uint ID;
        public readonly Color Colour;

        public Vector3Int CurrentCell;

        public readonly List<Vector3Int> ControlledCities = new List<Vector3Int>();

        public Player(uint id, Color colour, Vector3Int startingCity)
        {
            ID = id;
            Colour = colour;

            CurrentCell = startingCity;
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
