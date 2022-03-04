using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    float heightBetweenEachTerrace;

    [Header("Game Objects")]
    public Transform GameObjectParent;
    public GameObject PlayerPrefab;
    public GameObject CityPrefab;
    public Transform HoverPreviewParent;
    public GameObject ValidMovePrefab;
    List<GameObject> AllValidMovePreviews = new List<GameObject>();

    [Header("Players")]
    public PlayerSettings PlayerSettings;

    List<Player> Players = new List<Player>();
    Player currentPlayer;
    List<City> Cities = new List<City>();

    private void Start()
    {
        StartGame();
    }

    public void Clear()
    {
        StopAllCoroutines();

        foreach (Player p in Players)
        {
            Destroy(p.gameObject);
        }

        foreach (City c in Cities)
        {
            Destroy(c.gameObject);
        }

        HexMap.Clear();
        Players.Clear();
        Cities.Clear();

        foreach (GameObject g in AllValidMovePreviews)
        {
            Destroy(g.gameObject);
        }

        AllValidMovePreviews.Clear();
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

        // Wait for terrain to generate
        while (TerrainGenerator.IsGenerating)
        {
            yield return null;
        }

        heightBetweenEachTerrace = 1.0f / TerrainGenerator.NumberOfTerraces;

        System.Random r = new System.Random(TerrainGenerator.Seed);

        List<Vector3Int> playerCities = new List<Vector3Int>();
        List<Vector3Int> enemyCities = new List<Vector3Int>();
        List<Vector3> cameraCityPositions = new List<Vector3>();

        foreach (KeyValuePair<Vector3Int, HexMap.Hexagon> hex in HexMap.Hexagons)
        {
            if (hex.Value.Biome is Biome.PlayerCity)
            {
                playerCities.Add(hex.Key);
                cameraCityPositions.Add(HexMap.Hexagons[hex.Key].CentreOfFaceWorld);
            }
            else if (hex.Value.Biome is Biome.EnemyCity)
            {
                enemyCities.Add(hex.Key);
            }
        }

        CameraManager.SetupCameras(cameraCityPositions);

        // Choose player starting positions
        switch (playerCities.Count)
        {
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
            default:
                // Shuffle the list of player cities
                int n = playerCities.Count;
                while (n > 1)
                {
                    n--;
                    int k = r.Next(n + 1);
                    Vector3Int value = playerCities[k];
                    playerCities[k] = playerCities[n];
                    playerCities[n] = value;
                }
                break;
        }




        // Instantiate players and their city
        for (uint i = 0; i < NumberOfPlayers; i++)
        {
            Vector3Int cityCell = playerCities[0];
            playerCities.RemoveAt(0);

            // Init player
            GameObject p = Instantiate(PlayerPrefab, HexMap.Hexagons[cityCell].CentreOfFaceWorld, Quaternion.identity, GameObjectParent);
            Player player = p.GetComponent<Player>();
            player.Init(i, PlayerSettings.GetPlayerColour(i), cityCell);
            Players.Add(player);

            // Init player city
            GameObject c = Instantiate(CityPrefab, HexMap.Hexagons[cityCell].CentreOfFaceWorld, Quaternion.identity, GameObjectParent);
            City city = c.GetComponent<City>();
            city.Init(cityCell, City.CityType.Player, player);
            Cities.Add(city);
        }

        enemyCities.AddRange(playerCities);

        // Instantiate enemies
        foreach (Vector3Int cell in enemyCities)
        {
            GameObject c = Instantiate(CityPrefab, HexMap.Hexagons[cell].CentreOfFaceWorld, Quaternion.identity, GameObjectParent);
            City city = c.GetComponent<City>();
            city.Init(cell, City.CityType.Enemy, null);
            Cities.Add(city);
        }

        Debug.Log($"Playing with {Players.Count} players and {enemyCities.Count} enemies");
        Debug.Log($"Height between each terrace {heightBetweenEachTerrace}");


        while (true)
        {
            foreach (Player p in Players)
            {
                // Set the position that the camera should try and move to
                Vector3 playerPositionWorld = HexMap.Hexagons[p.CurrentCell].CentreOfFaceWorld;
                CameraManager.CameraFollow.position = playerPositionWorld;
                CameraManager.CameraLookAtPlayer.position = playerPositionWorld;

                yield return new WaitForSeconds(2.0f);

                // Update player turn
                currentPlayer = p;
                PlayerTurn = (int)p.ID;

                currentPlayer.ValidMovesThisTurn = CalculateAllValidMovesForPlayer(currentPlayer);
                UpdateValidMovesHighlight();

                HUD.Instance.PlayerTurnText.text = $"Current turn: player {PlayerTurn}";
                HUD.Instance.PlayerTurnText.color = p.Colour;

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
            UpdateHoverHighlight();

            if (Input.GetButtonDown("Fire1") && IsHoveringOverCell && currentPlayer.ValidMovesThisTurn.Contains(CellHoveringOver))
            {
                // Make the move
                MakeMove(currentPlayer, CellHoveringOver);
            }
        }
    }

    private void MakeMove(Player player, Vector3Int cell)
    {
        player.transform.position = HexMap.Hexagons[cell].CentreOfFaceWorld;
        player.CurrentCell = cell;

        currentPlayer = null;

        // Hide previews etc
        UpdateValidMovesHighlight();
        UpdateHoverHighlight();
        HUD.Instance.PlayerTurnText.text = "";
    }


    private HashSet<Vector3Int> CalculateAllValidMovesForPlayer(Player current)
    {
        IEnumerable<Vector3Int> GetMoves(Vector3Int cell)
        {
            return HexMap.CalculateAllExistingNeighbours(cell)
                .Where((x) => HexMap.Hexagons[x].Biome != Biome.None && x != cell &&
                Mathf.Abs(HexMap.Hexagons[cell].Height - HexMap.Hexagons[x].Height) <= heightBetweenEachTerrace &&
                !Players.Any(player => player.CurrentCell == x));
        }

        HashSet<Vector3Int> all = new HashSet<Vector3Int>(GetMoves(current.CurrentCell));

        for (int i = 1; i < Player.MaxMovementPerTurn; i++)
        {
            HashSet<Vector3Int> allCopy = new HashSet<Vector3Int>(all);

            foreach (Vector3Int move in all)
            {
                allCopy.UnionWith(GetMoves(move));
            }

            all = allCopy;
        }

        return new HashSet<Vector3Int>(all);
    }

    private void UpdateHoverHighlight()
    {
        IsHoveringOverCell = false;

        if (currentPlayer != null && CameraManager.IsHoveringMouseOverTerrain(MouseRaycastDistance, MouseLayerMask, out Vector3 position))
        {
            Vector3Int cell = HexMap.Grid.WorldToCell(new Vector3(position.x, 0, position.z));
            if (HexMap.Hexagons.ContainsKey(cell) && currentPlayer.ValidMovesThisTurn.Contains(cell))
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

    private void UpdateValidMovesHighlight()
    {
        foreach (GameObject g in AllValidMovePreviews)
        {
            g.SetActive(false);
        }

        if (currentPlayer != null)
        {
            foreach (Vector3Int move in currentPlayer.ValidMovesThisTurn)
            {
                GameObject preview = AllValidMovePreviews.Find(x => !x.activeSelf);

                // No disabled previews that we can use
                if (preview == null)
                {
                    preview = Instantiate(ValidMovePrefab, HoverPreviewParent);
                    AllValidMovePreviews.Add(preview);
                }

                Vector3 previewPosition = HexMap.Hexagons[move].CentreOfFaceWorld;
                previewPosition.y += 0.001f;
                preview.transform.position = previewPosition;

                preview.SetActive(true);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            if (IsHoveringOverCell)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(HexMap.Hexagons[CellHoveringOver].CentreOfFaceWorld, 0.25f);
            }

            foreach (Player p in Players)
            {
                Gizmos.color = p.Colour;
                Gizmos.DrawRay(HexMap.Hexagons[p.CurrentCell].CentreOfFaceWorld, Vector3.up * 10);
            }
        }
    }
}
