using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Range(2, 6)]
    public uint NumberOfPlayers;
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

    bool gameOver = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        //StartGame(0);
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

    public void StartGame(int seed)
    {
        StartCoroutine(PlayGame(seed));
    }

    private IEnumerator PlayGame(int seed)
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

        TerrainGenerator.Generate(seed);

        // Wait for terrain to generate
        while (TerrainGenerator.IsGenerating)
        {
            yield return null;
        }

        System.Random r = new System.Random(seed);
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

        Vector3 centreOfMap = HexMap.Hexagons[centre].CentreOfFaceWorld;

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
            city.PlayerCaptureBase(player);
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

        Debug.Log($"Playing with {Players.Count} players and {enemyCities.Count} enemies on seed {seed} with biomes {biomes.name}");


        // PLAY GAME HERE

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

            // Remove all dead players
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
                if(b.Strength > 0)
                {
                    if(b.OwnedBy == null)
                    {
                        b.Strength += TerrainGenerator.TerrainSettings.ReinforcementStrengthPerCityPerTurn;
                        b.Strength = Mathf.Min(b.Strength, b.MaxStrength);
                    }
                    else
                    {
                        b.Strength += TerrainGenerator.TerrainSettings.ReinforcementStrengthPerCityPerTurn;
                    }

                    b.UpdateBase();
                }
            }
        }
    }

    private void Update()
    {
        if (currentPlayer != null && !GameTurn && !gameOver)
        {
            UpdateHoverHighlight();
            HUD.Instance.UsingManualDollyText.enabled = CameraManager.IsManualDollyCamera;

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
            x.PlayerCaptureBase(player);
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
                StartCoroutine(LoadGameOver(Players[0]));
            }
        }
    }

     IEnumerator LoadGameOver(Player p)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("GameOver", LoadSceneMode.Additive);
        while(!load.isDone)
        {
            yield return null;
        }
        GameOver gameOver = GameObject.FindObjectOfType<GameOver>();
        gameOver.SetWinner(p);    
    }

    private IEnumerator MakeMove(Player player, Vector3Int destinationCell, float turnDuration)
    {
        Vector3 originalPlayerPosition = player.transform.position;
        GameTurn = true;

        // Hide previews etc
        UpdateValidMovesHighlight();
        UpdateHoverHighlight();
        HUD.Instance.PlayerTurnText.text = "";
        HUD.Instance.UsingManualDollyText.enabled = false;

        // ---------- LEAVE TILE ----------

        Base currentBase = Bases.Find(x => x.Cell == player.CurrentCell && x.OwnedBy == player);
        Player defendingPlayer = Players.Find(p => !p.IsDead && p.CurrentCell == destinationCell);
        Base defendingBase = Bases.Find(x => x.Cell == destinationCell);


        // Trying to leave a base
        if (currentBase != null && defendingPlayer == null && (defendingBase == null || defendingBase.Strength <= 0 || defendingBase.OwnedBy == player))
        {
            currentBase.PlayerLeaveBase(player, 1);
        }

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
        }

        // ---------- MOVE TO NEXT TILE ----------


        // Trying to enter a base
        if (defendingBase != null)
        {
            // Owned by an enemy or another player
            // Attack the city
            if (defendingBase.Strength > 0 && (defendingBase.OwnedBy == null || defendingBase.OwnedBy != player))
            {
                // Base to base combat
                if(currentBase != null)
                {
                    GameLogic.Fight(ref defendingBase.Strength, ref currentBase.Strength, TerrainGenerator.TerrainSettings.MaxNumberAttackersPerFight, TerrainGenerator.TerrainSettings.MaxNumberDefendersPerFight);

                    defendingBase.UpdateBase();
                    currentBase.UpdateBase();

                    // Player won
                    if (defendingBase.Strength == 0)
                    {
                        currentBase.PlayerLeaveBase(player, 1);
                        move();
                        yield return new WaitForSeconds(turnDuration);
                        defendingBase.PlayerCaptureBase(player);
                    }
                    // Other base won won
                    else if (currentBase.Strength == 0)
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
                // Normal base attacking
                else
                {
                    if (player.Strength <= 0 || defendingBase.Strength <= 0)
                    {
                        Debug.LogError("Player or base has strength 0");
                    }

                    GameLogic.Fight(ref defendingBase.Strength, ref player.Strength, TerrainGenerator.TerrainSettings.MaxNumberAttackersPerFight, TerrainGenerator.TerrainSettings.MaxNumberDefendersPerFight);

                    defendingBase.UpdateBase();
                    player.UpdatePlayer();

                    // Make the player face the city
                    Vector3 facing = HexMap.Hexagons[destinationCell].CentreOfFaceWorld - originalPlayerPosition;
                    player.transform.forward = new Vector3(facing.x, 0, facing.z);

                    // Player won
                    if (defendingBase.Strength == 0)
                    {
                        move();
                        yield return new WaitForSeconds(turnDuration);
                        defendingBase.PlayerCaptureBase(player);
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
            // Otherwise just capture it
            else
            {
                move();
                yield return new WaitForSeconds(turnDuration);
                defendingBase.PlayerCaptureBase(player);
            }
        }
        // Trying to attack a player
        else if (defendingPlayer != null)
        {
            Vector3 facing = HexMap.Hexagons[destinationCell].CentreOfFaceWorld - originalPlayerPosition;
            facing.y = 0;

            // If attacking a player while inside a base
            if (currentBase != null)
            {
                defendingPlayer.transform.forward = -facing;

                GameLogic.Fight(ref defendingPlayer.Strength, ref currentBase.Strength, TerrainGenerator.TerrainSettings.MaxNumberAttackersPerFight, TerrainGenerator.TerrainSettings.MaxNumberDefendersPerFight);
                defendingPlayer.UpdatePlayer();
                currentBase.UpdateBase();

                // Attacking won
                if (defendingPlayer.Strength == 0)
                {
                    yield return new WaitForSeconds(turnDuration);
                    TryRespawnPlayer(defendingPlayer);
                }
                // Defending won
                else if (currentBase.Strength == 0)
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
            // Otherwise normal fight
            else
            {
                // Make players face each other
                player.transform.forward = facing;
                defendingPlayer.transform.forward = -facing;

                GameLogic.Fight(ref defendingPlayer.Strength, ref player.Strength, TerrainGenerator.TerrainSettings.MaxNumberAttackersPerFight, TerrainGenerator.TerrainSettings.MaxNumberDefendersPerFight);
                defendingPlayer.UpdatePlayer();
                player.UpdatePlayer();

                // Attacking won
                if (defendingPlayer.Strength == 0)
                {
                    yield return new WaitForSeconds(turnDuration);
                    move();
                    TryRespawnPlayer(defendingPlayer);
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
        }
        // Just move
        else
        {
            move();
            yield return new WaitForSeconds(turnDuration);
        }


        // ---------- END OF MOVE ----------

        GameTurn = false;
        currentPlayer = null;
    }

    private HashSet<Vector3Int> CalculateAllValidMovesForPlayer(Player player)
    {
        IEnumerable<Vector3Int> GetAllMovesForCell(Vector3Int cell)
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

        // Calculate the number of tiles the player can move
        int maxPlayerMovement = TerrainGenerator.TerrainSettings.MaxPlayerMovementPerTurn;

        if (Bases.Find(x => x.Cell == player.CurrentCell && x.OwnedBy == player) != null)
        {
            maxPlayerMovement = TerrainGenerator.TerrainSettings.MaxPlayerMovementPerTurnInBase;
        }

        // Get the initial moves (can move to any cell, this allows us to attack things)
        HashSet<Vector3Int> all = new HashSet<Vector3Int>(GetAllMovesForCell(player.CurrentCell)
            .Where(x =>
                // Don't allow the player to move to the cell they are currently on
                x != player.CurrentCell));

        for (int i = 1; i < maxPlayerMovement; i++)
        {
            HashSet<Vector3Int> allCopy = new HashSet<Vector3Int>(all);

            foreach (Vector3Int move in all)
            {
                IEnumerable<Vector3Int> validMoves = GetAllMovesForCell(move)
                    .Where(x =>
                        // Don't allow the player to move to the cell they are currently on
                        x != player.CurrentCell &&
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
        }
    }
}
