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
        if(HUD.Instance == null)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("HUD", LoadSceneMode.Additive);

            // Wait for HUD to load
            while (!load.isDone)
            {
                yield return null;
            }
        }

        TerrainGenerator.Generate();

        // Wait for terrain to generate
        while (TerrainGenerator.IsGenerating)
        {
            yield return null;
        }

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
            p.name = $"Player {i}";
            Player player = p.GetComponent<Player>();
            player.Init(i, PlayerSettings.GetPlayerColour(i), cityCell, TerrainGenerator.TerrainSettings.InitialPlayerStrength);
            Players.Add(player);

            // Init player city
            GameObject c = Instantiate(CityPrefab, HexMap.Hexagons[cityCell].CentreOfFaceWorld, Quaternion.identity, GameObjectParent);
            City city = c.GetComponent<City>();
            city.Init(cityCell, 0);
            Cities.Add(city);

            // Move the player into that city
            city.PlayerCaptureCity(player);
        }

        enemyCities.AddRange(playerCities);

        // Instantiate enemies
        foreach (Vector3Int cell in enemyCities)
        {
            GameObject c = Instantiate(CityPrefab, HexMap.Hexagons[cell].CentreOfFaceWorld, Quaternion.identity, GameObjectParent);
            City city = c.GetComponent<City>();
            city.Init(cell, r.Next(TerrainGenerator.TerrainSettings.InitialEnemyStrengthMin, TerrainGenerator.TerrainSettings.InitialEnemyStrengthMax));
            Cities.Add(city);
        }

        Debug.Log($"Playing with {Players.Count} players and {enemyCities.Count} enemies");


        while (true)
        {
            foreach (Player p in Players)
            {
                // Set the position that the camera should try and move to
                Vector3 playerPositionWorld = HexMap.Hexagons[p.CurrentCell].CentreOfFaceWorld;
                CameraManager.CameraFollow.position = playerPositionWorld;
                CameraManager.CameraLookAtPlayer.position = playerPositionWorld;
                CameraManager.SetCameraModeAutomatic(true);


                yield return new WaitForSeconds(2.0f);

                // Update player turn
                currentPlayer = p;
                PlayerTurn = (int)p.ID;

                currentPlayer.ValidMovesThisTurn = CalculateAllValidMovesForPlayer(currentPlayer);
                UpdateValidMovesHighlight();

                HUD.Instance.PlayerTurnText.text = $"Current turn: player {PlayerTurn}";
                HUD.Instance.PlayerTurnText.color = p.Colour;

                if(currentPlayer.ValidMovesThisTurn.Count == 0)
                {
                    Debug.LogError($"Player {currentPlayer.ID} can't make any moves. Skipping turn");
                    currentPlayer = null;
                }

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

    private void MakeMove(Player player, Vector3Int destination)
    {
        // Leave a city
        if((HexMap.Hexagons[player.CurrentCell].Biome == Biome.PlayerCity || HexMap.Hexagons[player.CurrentCell].Biome == Biome.EnemyCity) && HexMap.Hexagons[destination].Biome != Biome.PlayerCity)
        {
            foreach (City city in Cities)
            {
                if (city.Cell == player.CurrentCell && city.OwnedBy == player)
                {
                    city.PlayerLeaveCity(player, 1);
                    break;
                }
            }
        }
        // Enter a city
        else if (HexMap.Hexagons[destination].Biome == Biome.PlayerCity || HexMap.Hexagons[destination].Biome == Biome.EnemyCity)
        {
            foreach(City city in Cities)
            {
                if(city.Cell == destination)
                {
                    // Owned by another player
                    if(city.OwnedBy != null && city.OwnedBy != player)
                    {
                        // TODO FIGHTING LOGIC
                        //city.PlayerCaptureCity(player);
                    }
                    // Owned by an enemy
                    else if(city.Strength > 0)
                    {
                        // TODO FIGHTING LOGIC
                        //city.PlayerCaptureCity(player);
                    }
                    // Unowned or owned by this player
                    else
                    {
                        city.PlayerCaptureCity(player);
                    }

                    // Exit the for loop
                    break;
                }
            }
        }

        player.transform.position = HexMap.Hexagons[destination].CentreOfFaceWorld;
        player.CurrentCell = destination;

        currentPlayer = null;

        // Hide previews etc
        UpdateValidMovesHighlight();
        UpdateHoverHighlight();
        HUD.Instance.PlayerTurnText.text = "";
    }

    private void FightForCity(uint cityStrength, uint attackerStrength, out uint newCityStrength, out uint newAttackerStrength)
    {
        int difference = Mathf.Abs((int)(cityStrength - attackerStrength));

        // Fair fight
        if(cityStrength == attackerStrength)
        {

        }

        newCityStrength = 0;
        newAttackerStrength = 0;
    }


    private HashSet<Vector3Int> CalculateAllValidMovesForPlayer(Player current)
    {
        IEnumerable<Vector3Int> GetMoves(Vector3Int cell)
        {
            return HexMap.CalculateAllExistingNeighbours(cell)
                .Where((x) => 
                    HexMap.Hexagons[x].Biome != Biome.None && 
                    x != cell &&
                    Mathf.Abs(Mathf.Abs(HexMap.Hexagons[cell].Height) - Mathf.Abs(HexMap.Hexagons[x].Height)) <= TerrainGenerator.HeightBetweenEachTerrace + (TerrainGenerator.HeightBetweenEachTerrace / 2.0f) &&
                    !Players.Any(player => player.CurrentCell == x && player.gameObject.activeSelf)
                );
        }

        HashSet<Vector3Int> all = new HashSet<Vector3Int>(GetMoves(current.CurrentCell));

        for (int i = 1; i < TerrainGenerator.TerrainSettings.MaxPlayerMovementPerTurn; i++)
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
