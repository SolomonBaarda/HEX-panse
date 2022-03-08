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
    public bool GameTurn = false;

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
    public GameObject PlayerBasePrefab;
    public GameObject EnemyBasePrefab;


    public Transform HoverPreviewParent;
    public GameObject ValidMovePrefab;
    List<GameObject> AllValidMovePreviews = new List<GameObject>();

    [Header("Players")]
    public PlayerSettings PlayerSettings;

    List<Player> Players = new List<Player>();
    Player currentPlayer;
    List<Base> Cities = new List<Base>();

    Vector3 centreOfMap;

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

        foreach (Base c in Cities)
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

            HUD.Instance.PlayerTurnText.text = "";
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

        foreach (KeyValuePair<Vector3Int, HexMap.Hexagon> hex in HexMap.Hexagons)
        {
            if (hex.Value.IsCity is CityType.Player)
            {
                playerCities.Add(hex.Key);
            }
            else if (hex.Value.IsCity is CityType.Enemy)
            {
                enemyCities.Add(hex.Key);
            }
        }

        Vector3Int centre = new Vector3Int();
        foreach (Vector3Int pos in playerCities)
        {
            centre += pos;
        }
        centre /= playerCities.Count;

        // Sort points so that thay are in anti clockwise order
        playerCities.Sort((x, y) => -Clockwise.Compare(HexMap.Hexagons[x].CentreOfFaceWorld, HexMap.Hexagons[y].CentreOfFaceWorld, centre));

        centreOfMap = HexMap.Hexagons[centre].CentreOfFaceWorld;

        List<Vector3> cameraCityPositions = new List<Vector3>();
        foreach(Vector3Int pos in playerCities)
        {
            cameraCityPositions.Add(HexMap.Hexagons[pos].CentreOfFaceWorld);
        }

        CameraManager.SetupCameras(cameraCityPositions, centreOfMap);

        HexMap.GenerateMeshFromHexagons();
        yield return null;

        if(playerCities.Count != 6)
        {
            Debug.LogError("There are not 6 available player cities on the map");
        }

        switch (NumberOfPlayers)
        {
            // Ensure that players are on opposite sides of the board
            case 2:
                Vector3Int opposite0 = playerCities[3];
                playerCities.RemoveAt(3);
                playerCities.Insert(1, opposite0);
                break;
            // Ensure that all three players have their own corner
            case 3:
                Vector3Int player1 = playerCities[1];
                Vector3Int player3 = playerCities[3];
                playerCities.RemoveAt(3);
                playerCities.RemoveAt(1);

                playerCities.Add(player1);
                playerCities.Add(player3);

                break;
            // Ensure it's two players to a side
            case 4:
                Vector3Int player2 = playerCities[2];
                playerCities.RemoveAt(2);
                playerCities.Add(player2);
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
            GameObject b = Instantiate(PlayerBasePrefab, HexMap.Hexagons[cityCell].CentreOfFaceWorld, Quaternion.identity, GameObjectParent);
            Base city = b.GetComponent<Base>();
            city.Init(cityCell, 0);
            Cities.Add(city);

            // Move the player into that city
            city.PlayerCaptureCity(player);
        }

        enemyCities.AddRange(playerCities);

        // Instantiate enemies
        foreach (Vector3Int cell in enemyCities)
        {
            GameObject c = Instantiate(EnemyBasePrefab, HexMap.Hexagons[cell].CentreOfFaceWorld, Quaternion.identity, GameObjectParent);
            Base city = c.GetComponent<Base>();
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

                // Wait for the camera to move there
                yield return new WaitForSeconds(2.0f);

                // Update player turn
                currentPlayer = p;
                PlayerTurn = (int)p.ID;

                // Reinforce each city
                foreach(Base c in Cities.Where(city => city.OwnedBy == p))
                {
                    c.Strength += TerrainGenerator.TerrainSettings.ReinforcementStrengthPerCityPerTurn;
                    c.UpdateCity();
                }

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
        if (currentPlayer != null && !GameTurn)
        {
            UpdateHoverHighlight();

            if (Input.GetButtonDown("Fire1") && IsHoveringOverCell && currentPlayer.ValidMovesThisTurn.Contains(CellHoveringOver))
            {
                // Make the move
                StartCoroutine(MakeMove(currentPlayer, CellHoveringOver, 1.0f));
            }
        }
    }

    private IEnumerator MakeMove(Player player, Vector3Int destinationCell, float turnDuration)
    {
        Vector3 originalPlayerPosition = player.transform.position;

        void move()
        {
            GameTurn = true;

            Vector3 destination = HexMap.Hexagons[destinationCell].CentreOfFaceWorld;
            Vector3 facing = destination - originalPlayerPosition;
            facing.y = 0;

            player.transform.forward = facing;
            player.CurrentCell = destinationCell;

            player.gameObject.SetActive(true);
            player.MoveToPosition(originalPlayerPosition, destination, turnDuration);

            // Hide previews etc
            UpdateValidMovesHighlight();
            UpdateHoverHighlight();
            HUD.Instance.PlayerTurnText.text = "";
        }

        // Leave a city
        if((HexMap.Hexagons[player.CurrentCell].IsCity == CityType.Player || HexMap.Hexagons[player.CurrentCell].IsCity == CityType.Enemy) && HexMap.Hexagons[destinationCell].IsCity != CityType.Player)
        {
            foreach (Base city in Cities)
            {
                if (city.Cell == player.CurrentCell && city.OwnedBy == player)
                {
                    city.PlayerLeaveCity(player, 1);
                    move();
                    yield return new WaitForSeconds(turnDuration);
                    GameTurn = false;
                    break;
                }
            }
        }
        // Enter a city
        else if (HexMap.Hexagons[destinationCell].IsCity == CityType.Player || HexMap.Hexagons[destinationCell].IsCity == CityType.Enemy)
        {
            foreach(Base city in Cities)
            {
                if(city.Cell == destinationCell)
                {
                    // Owned by an enemy or another player
                    if(city.Strength > 0 && (city.OwnedBy == null || city.OwnedBy != player))
                    {
                        if(player.Strength > 1)
                        {
                            FightCity(city, player);

                            if (city.Strength == 0)
                            {
                                move();
                                yield return new WaitForSeconds(turnDuration);
                                GameTurn = false;
                                city.PlayerCaptureCity(player);
                            }
                            else
                            {
                                GameTurn = true;
                                yield return new WaitForSeconds(turnDuration);
                                GameTurn = false;
                            }
                        }
                    }
                    // Empty or owned by this player
                    else
                    {
                        move();
                        yield return new WaitForSeconds(turnDuration);
                        GameTurn = false;
                        city.PlayerCaptureCity(player);
                    }

                    // Exit the for loop
                    break;
                }
            }
        }
        else
        {
            move();
            yield return new WaitForSeconds(turnDuration);
            GameTurn = false;
        }

        currentPlayer = null;
    }

    private void FightCity(Base city, Player player)
    {
        int difference = Mathf.Abs(city.Strength - player.Strength);
        float differencePercentage = (float)difference / Mathf.Max(city.Strength, player.Strength);

        // Small difference so fair fight
        if (differencePercentage < 0.1f)
        {

        }
        // Medium difference so ok fight
        else if(differencePercentage < 0.5f)
        {

        }
        // Large difference so unfair fight
        else
        {

        }

        while(player.Strength > 0 && city.Strength > 0)
        {
            player.Strength--;
            city.Strength--;
        }
    }


    private HashSet<Vector3Int> CalculateAllValidMovesForPlayer(Player current)
    {
        IEnumerable<Vector3Int> GetMoves(Vector3Int cell)
        {
            return HexMap.CalculateAllExistingNeighbours(cell)
                .Where((x) => 
                    // Ensure it is a valid biome
                    HexMap.Hexagons[x].Biome != Biome.None && 
                    // Don't add our current position
                    x != cell && 
                    // Move up or down only one step
                    Mathf.Abs(Mathf.Abs(HexMap.Hexagons[cell].Height) - Mathf.Abs(HexMap.Hexagons[x].Height)) <= TerrainGenerator.HeightBetweenEachTerrace + (TerrainGenerator.HeightBetweenEachTerrace / 2.0f) &&
                    // Ensure there are no players in that cell (either other players or us but in a city)
                    !Players.Any(player => player.CurrentCell == x && (player.gameObject.activeSelf || player == current))
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

        if (currentPlayer != null && !GameTurn && CameraManager.IsHoveringMouseOverTerrain(MouseRaycastDistance, MouseLayerMask, out Vector3 position))
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

        if (currentPlayer != null && !GameTurn)
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

            Gizmos.color = Color.green;
            Gizmos.DrawRay(centreOfMap, Vector3.up * 25);
        }
    }
}
