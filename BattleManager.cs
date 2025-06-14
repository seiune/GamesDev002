namespace Game
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    // Manages combat, enemy spawning, and round progression
    public class BattleManager : MonoBehaviour
    {
        // References to managers and UI
        private UnitManger unitManger;
        private Unit selectedUnit;
        private int gold = 100;
        public Unit enemyUnitPrefab;
        private HexGrid hexGrid;
        private bool levelWon = false, combatStarted = false;
        public event Action OnLevelWon;
        private int enemyUnitCount = 1;
        private PlayerBenchManger benchManger;
        private UIManager uiManager;
        public Button startButton;

        // Initialize references
        private void Awake()
        {
            // Find required components
            unitManger = FindFirstObjectByType<UnitManger>();
            hexGrid = FindFirstObjectByType<HexGrid>();
            benchManger = FindObjectOfType<PlayerBenchManger>();
            uiManager = FindObjectOfType<UIManager>();
            if (unitManger == null) Debug.LogError("UnitManger not found in scene.");
            if (hexGrid == null) Debug.LogError("HexGrid not found in scene.");
            if (benchManger == null) Debug.LogError("PlayerBenchManger not found in scene.");
            if (uiManager == null) Debug.LogError("UIManager not found in scene.");
            if (startButton == null) Debug.LogError("StartButton not assigned in Inspector.");
            Debug.Log("BattleManager Awake, OnLevelWon subscribers: " + (OnLevelWon != null ? OnLevelWon.GetInvocationList().Length.ToString() : "0"));
        }

        // Start enemy spawning
        private void Start()
        {
            Debug.Log("BattleManager initializing enemy spawn.");
            SpawnEnemyUnits();
        }

        // Start combat phase
        public void StartCombat()
        {
            if (!combatStarted)
            {
                combatStarted = true;
                if (startButton != null)
                {
                    startButton.interactable = false;
                    Debug.Log("Start button disabled during combat.");
                }
                Debug.Log("Combat phase started.");
                StartCoroutine(RunTurns());
            }
            else
                Debug.LogWarning("Combat already started.");
        }

        // Prepare for the next round
        public void PrepareNextRound()
        {
            levelWon = false;
            combatStarted = false;
            enemyUnitCount++;
            Debug.Log($"Preparing round with {enemyUnitCount} enemy units.");

            // Move player units back to bench
            if (benchManger != null)
                benchManger.CollectUnitsToBench();
            else
                Debug.LogError("PlayerBenchManger is null, cannot collect units.");

            // Spawn new enemies
            SpawnEnemyUnits();

            // Enable start button for placement
            if (startButton != null)
            {
                startButton.interactable = true;
                startButton.gameObject.SetActive(true);
                Debug.Log("Start button enabled for placement phase.");
            }
            else
                Debug.LogError("StartButton is null, cannot enable for placement.");
        }

        // Spawn enemy units on the grid
        private void SpawnEnemyUnits()
        {
            if (enemyUnitPrefab == null)
            {
                Debug.LogError("Enemy unit prefab not assigned in BattleManager.");
                return;
            }

            // Clear existing enemies
            var units = FindObjectsOfType<UnitsMovement>();
            foreach (var unit in units)
            {
                if (unit != null && unit.isEnemy)
                {
                    hexGrid.RemoveUnitFromHex(unit.hexPosition);
                    unitManger.GetAllUnits().Remove(unit);
                    Destroy(unit.gameObject);
                }
            }

            // Spawn new enemies
            for (int i = 0; i < enemyUnitCount; i++)
            {
                Vector2Int randomHex = GetRandomEnemyHex();
                if (randomHex != new Vector2Int(-1, -1))
                {
                    Unit enemy = Instantiate(enemyUnitPrefab, hexGrid.GetWorldPositionFromHex(randomHex), Quaternion.identity);
                    UnitsMovement enemyMovement = enemy.GetComponent<UnitsMovement>();
                    if (enemyMovement == null)
                    {
                        Debug.LogError($"Enemy prefab {enemy.name} missing UnitsMovement component.");
                        Destroy(enemy);
                        continue;
                    }
                    enemyMovement.isEnemy = true;
                    enemyMovement.hexPosition = randomHex;
                    if (hexGrid.PlaceUnitOnHex(enemy, randomHex))
                    {
                        unitManger.GetAllUnits().Add(enemyMovement);
                        Debug.Log($"Spawned enemy {enemy.unitName} at hex {randomHex}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to place enemy {enemy.name} at hex {randomHex}");
                        Destroy(enemy);
                    }
                }
                else
                    Debug.LogWarning("No valid hex found for enemy spawn.");
            }
        }

        // Find a random hex for enemy spawning
        private Vector2Int GetRandomEnemyHex()
        {
            List<Vector2Int> enemyHexes = new List<Vector2Int>();
            for (int x = 0; x <= 4; x++)
                for (int y = 0; y < hexGrid.Height; y++)
                    if (!hexGrid.IsHexOccupied(new Vector2Int(x, y)))
                        enemyHexes.Add(new Vector2Int(x, y));
            Debug.Log($"Found {enemyHexes.Count} valid hexes for enemy spawn.");
            return enemyHexes.Count > 0 ? enemyHexes[UnityEngine.Random.Range(0, enemyHexes.Count)] : new Vector2Int(-1, -1);
        }

        // Set the selected unit
        public void SetSelectedUnit(Unit unit)
        {
            selectedUnit = unit;
            Debug.Log($"Selected unit: {(unit != null ? unit.unitName : "None")}");
        }

        // Get current gold
        public int GetGold()
        {
            return gold;
        }

        // Add gold to player
        public void AddGold(int amount)
        {
            gold += amount;
            Debug.Log($"Added {amount} gold. Total gold: {gold}");
        }

        // Run combat turns
        private IEnumerator RunTurns()
        {
            while (!levelWon)
            {
                List<UnitsMovement> units = new List<UnitsMovement>(unitManger.GetAllUnits());
                Debug.Log($"Running turn with {units.Count} units.");
                foreach (var unit in units)
                {
                    if (unit != null && !unit.IsLocked)
                    {
                        unit.TakeTurn();
                        while (unit.IsMoving)
                            yield return null;
                    }
                }
                foreach (var unit in units)
                    if (unit != null) unit.hasMoved = false;

                // Check if enemies are defeated
                if (!AnyEnemiesRemain())
                {
                    Debug.Log("Level Won! All enemy units defeated.");
                    levelWon = true;
                    if (OnLevelWon != null)
                    {
                        OnLevelWon.Invoke();
                        Debug.Log("OnLevelWon event invoked.");
                    }
                    else
                        Debug.LogWarning("OnLevelWon has no subscribers.");
                    yield break;
                }
            }
        }

        // Check if any enemies remain
        private bool AnyEnemiesRemain()
        {
            foreach (var unit in unitManger.GetAllUnits())
                if (unit != null && unit.isEnemy)
                    return true;
            return false;
        }
    }
}