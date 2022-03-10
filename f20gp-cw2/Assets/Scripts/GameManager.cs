using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Range(2, 6)]
    public uint NumberOfPlayers = 6;
    public int CurrentTurn;
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
    public List<BiomeSettings> BiomeSettings = new List<BiomeSettings>();

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
    List<Base> Bases = new List<Base>();

    Vector3 centreOfMap;

    bool gameOver = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

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

        foreach (Base c in Bases)
        {
            Destroy(c.gameObject);
        }

        HexMap.Clear();
        Players.Clear();
        Bases.Clear();

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
        if (HUD.Instance == null)
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
        BiomeSettings biomes = BiomeSettings[r.Next(0, BiomeSettings.Count)];

        List<Vector3Int> playerCities = new List<Vector3Int>();
        List<Vector3Int> enemyCities = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, HexMap.Hexagon> hex in HexMap.Hexagons)
        {
            if (hex.Value.IsCity is BaseType.Player)
            {
                playerCities.Add(hex.Key);
            }
            else if (hex.Value.IsCity is BaseType.Enemy)
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
        foreach (Vector3Int pos in playerCities)
        {
            cameraCityPositions.Add(HexMap.Hexagons[pos].CentreOfFaceWorld);
        }

        CameraManager.SetupCameras(cameraCityPositions, centreOfMap);

        HexMap.GenerateMeshFromHexagons(biomes);
        yield return null;

        if (playerCities.Count != 6)
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
            city.Init(cityCell, 0, int.MaxValue);
            Bases.Add(city);

            // Move the player into that city
            city.PlayerCaptureCity(player);
        }

        enemyCities.AddRange(playerCities);

        // Instantiate enemies
        foreach (Vector3Int cell in enemyCities)
        {
            GameObject c = Instantiate(EnemyBasePrefab, HexMap.Hexagons[cell].CentreOfFaceWorld, Quaternion.identity, GameObjectParent);
            Base city = c.GetComponent<Base>();
            int maxStrength = r.Next(TerrainGenerator.TerrainSettings.MaxEnemyStrengthMin, TerrainGenerator.TerrainSettings.MaxEnemyStrengthMax);
            city.Init(cell, r.Next(TerrainGenerator.TerrainSettings.InitialEnemyStrengthMin, TerrainGenerator.TerrainSettings.InitialEnemyStrengthMax), maxStrength);
            Bases.Add(city);
        }

        Debug.Log($"Playing with {Players.Count} players and {enemyCities.Count} enemies on seed {TerrainGenerator.Seed} with biomes {biomes.name}");


        while (!gameOver)
        {
            foreach (Player p in Players)
            {
                if (gameOver)
                {
                    yield break;
                }

                if (!p.IsDead)
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
                    CurrentTurn = (int)p.ID;

                    // Reinforce each city
                    foreach (Base c in Bases.Where(city => city.OwnedBy == p))
                    {
                        c.Strength += TerrainGenerator.TerrainSettings.ReinforcementStrengthPerCityPerTurn;
                        c.UpdateCity();
                    }

                    currentPlayer.ValidMovesThisTurn = CalculateAllValidMovesForPlayer(currentPlayer);
                    UpdateValidMovesHighlight();

                    HUD.Instance.PlayerTurnText.text = $"Current turn: player {CurrentTurn}";
                    HUD.Instance.PlayerTurnText.color = p.Colour;

                    if (currentPlayer.ValidMovesThisTurn.Count == 0)
                    {
                        Debug.LogError($"Player {currentPlayer.ID} can't make any moves. Skipping turn");
                        currentPlayer = null;
                    }

                    // Wait here while it is this players turn
                    while (currentPlayer != null && currentPlayer == p)
                    {
                        yield return null;
                    }
                }
            }

            foreach (Player p in Players)
            {
                if (p.IsDead)
                {
                    Destroy(p.gameObject);
                }
            }

            Players.RemoveAll(p => p.IsDead);

            // Do enemy turn
            foreach (Base b in Bases)
            {
                if (b.OwnedBy == null && b.Strength > 0)
                {
                    b.Strength += TerrainGenerator.TerrainSettings.ReinforcementStrengthPerCityPerTurn;
                    b.Strength = Mathf.Min(b.Strength, b.MaxStrength);
                    b.UpdateCity();
                }
            }
        }
    }


    private void Update()
    {
        if (currentPlayer != null && !GameTurn && !gameOver)
        {
            UpdateHoverHighlight();

            if (Input.GetButtonDown("Fire1") && IsHoveringOverCell && currentPlayer.ValidMovesThisTurn.Contains(CellHoveringOver))
            {
                // Make the move
                StartCoroutine(MakeMove(currentPlayer, CellHoveringOver, 1.0f));
            }
        }
    }

    private void TryRespawnPlayer(Player player)
    {
        Base x = Bases.Find(x => x.OwnedBy == player);

        // Still alive
        if (x != null)
        {
            Debug.Log($"Player {player.ID} respawned at base");

            // Respawn at an owned base
            player.CurrentCell = x.Cell;
            player.transform.position = HexMap.Hexagons[player.CurrentCell].CentreOfFaceWorld;
            x.PlayerCaptureCity(player);
        }
        // Player dies
        else
        {
            Debug.Log($"Player {player.ID} has died");
            player.Kill();

            if (Players.Where(p => !p.IsDead).Count() == 1)
            {
                Debug.Log($"Player {Players[0].ID} has won");
                gameOver = true;
            }
        }
    }

    private IEnumerator MakeMove(Player player, Vector3Int destinationCell, float turnDuration)
    {
        Vector3 originalPlayerPosition = player.transform.position;
        GameTurn = true;

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
        if ((HexMap.Hexagons[player.CurrentCell].IsCity == BaseType.Player || HexMap.Hexagons[player.CurrentCell].IsCity == BaseType.Enemy) && HexMap.Hexagons[destinationCell].IsCity != BaseType.Player)
        {
            foreach (Base city in Bases)
            {
                if (city.Cell == player.CurrentCell && city.OwnedBy == player)
                {
                    city.PlayerLeaveCity(player, 1);
                    move();
                    yield return new WaitForSeconds(turnDuration);
                    break;
                }
            }
        }
        // Enter a city
        else if (HexMap.Hexagons[destinationCell].IsCity == BaseType.Player || HexMap.Hexagons[destinationCell].IsCity == BaseType.Enemy)
        {
            foreach (Base b in Bases)
            {
                if (b.Cell == destinationCell)
                {
                    // Owned by an enemy or another player
                    if (b.Strength > 0 && (b.OwnedBy == null || b.OwnedBy != player))
                    {
                        if (player.Strength >= 1)
                        {
                            Fight(ref b.Strength, ref player.Strength);

                            b.UpdateCity();
                            player.UpdatePlayer();

                            Vector3 destination = HexMap.Hexagons[destinationCell].CentreOfFaceWorld;
                            Vector3 facing = destination - originalPlayerPosition;
                            facing.y = 0;

                            player.transform.forward = facing;

                            // Player won
                            if (b.Strength == 0)
                            {
                                move();
                                yield return new WaitForSeconds(turnDuration);
                                b.PlayerCaptureCity(player);
                            }
                            // City won
                            else if (player.Strength == 0)
                            {
                                TryRespawnPlayer(player);

                                yield return new WaitForSeconds(turnDuration);
                            }
                            // Fight not over yet
                            else
                            {
                                yield return new WaitForSeconds(turnDuration);
                            }
                        }
                    }
                    // Empty or owned by this player
                    else
                    {
                        move();
                        yield return new WaitForSeconds(turnDuration);
                        b.PlayerCaptureCity(player);
                    }

                    // Exit the for loop
                    break;
                }
            }
        }
        else
        {
            Player defending = Players.Find(p => !p.IsDead && p.CurrentCell == destinationCell);

            // Attacking another player
            if (defending != null)
            {
                Vector3 destination = HexMap.Hexagons[destinationCell].CentreOfFaceWorld;
                Vector3 facing = destination - originalPlayerPosition;
                facing.y = 0;

                player.transform.forward = facing;
                defending.transform.forward = -facing;

                Fight(ref defending.Strength, ref player.Strength);

                defending.UpdatePlayer();
                player.UpdatePlayer();

                // Attacking won
                if (defending.Strength == 0)
                {
                    yield return new WaitForSeconds(turnDuration);
                    move();
                    TryRespawnPlayer(defending);
                }
                // Defending won
                else if (player.Strength == 0)
                {
                    yield return new WaitForSeconds(turnDuration);

                    TryRespawnPlayer(player);
                }
                // Fight not over yet
                else
                {
                    yield return new WaitForSeconds(turnDuration);
                }

            }
            // Just move
            else
            {
                move();
                yield return new WaitForSeconds(turnDuration);
            }
        }

        GameTurn = false;
        currentPlayer = null;
    }

    private void Fight(ref int defendingStrength, ref int attackingStrength)
    {
        int defenders = Mathf.Min(defendingStrength, TerrainGenerator.TerrainSettings.MaxNumberAttackersPerFight);
        int attackers = Mathf.Min(attackingStrength, TerrainGenerator.TerrainSettings.MaxNumberAttackersPerFight);

        List<int> attackStrengths = new List<int>();
        List<int> defendStrengths = new List<int>();

        for (int i = 0; i < attackers; i++)
        {
            attackStrengths.Add(Random.Range(0, TerrainGenerator.TerrainSettings.NumberSidedDice + 1));
        }

        for (int i = 0; i < defenders; i++)
        {
            defendStrengths.Add(Random.Range(0, TerrainGenerator.TerrainSettings.NumberSidedDice + 1));
        }

        attackStrengths.Sort((x, y) => -x.CompareTo(y));
        defendStrengths.Sort((x, y) => -x.CompareTo(y));

        for (int i = 0; i < Mathf.Min(attackers, defenders); i++)
        {
            // Attacker wins
            if (attackStrengths[i] > defendStrengths[i])
            {
                defendingStrength--;
                Debug.Log("attack won");
            }
            // Defenders win
            else
            {
                attackingStrength--;
                Debug.Log("defenders won");
            }
        }
    }


    private HashSet<Vector3Int> CalculateAllValidMovesForPlayer(Player player)
    {
        IEnumerable<Vector3Int> GetMoves(Vector3Int cell, Vector3Int startingCell)
        {
            return HexMap.CalculateAllExistingNeighbours(cell)
                .Where((x) =>
                    // Ensure it is a valid biome
                    HexMap.Hexagons[x].Biome != Biome.None &&
                    // Don't add our current position
                    x != cell &&
                    // Move up or down only one step
                    Mathf.Abs(Mathf.Abs(HexMap.Hexagons[cell].Height) - Mathf.Abs(HexMap.Hexagons[x].Height)) <= TerrainGenerator.HeightBetweenEachTerrace + (TerrainGenerator.HeightBetweenEachTerrace / 2.0f)

                );
        }

        HashSet<Vector3Int> all = new HashSet<Vector3Int>(GetMoves(player.CurrentCell, player.CurrentCell).Where(x => x != player.CurrentCell));

        for (int i = 1; i < TerrainGenerator.TerrainSettings.MaxPlayerMovementPerTurn; i++)
        {
            HashSet<Vector3Int> allCopy = new HashSet<Vector3Int>(all);

            foreach (Vector3Int move in all)
            {
                IEnumerable<Vector3Int> validMoves = GetMoves(move, player.CurrentCell)
                    .Where(x =>
                        // Ensure that players can only move through their own cities and attack neighbour tiles
                        !Bases.Any(b => b.Cell == x && b.Strength > 0 && b.OwnedBy != player) &&
                        // Ensure there are no players in that cell (either other players or us but in a city)
                        !Players.Any(p => p.CurrentCell == x && (p.gameObject.activeSelf || p == player)));

                allCopy.UnionWith(validMoves);
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
