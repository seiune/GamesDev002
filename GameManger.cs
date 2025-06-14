using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game
{
    // Manages game flow, unit placement, rounds, and timer
    public class GameManger : MonoBehaviour
    {
        // Hex grid for unit placement
        public HexGrid hexGrid;
        // Enemy unit prefabs
        public List<Unit> enemyPrefabs;
        // Tank and archer prefabs for initial units
        public Unit tankPrefab, archerPrefab;
        // Number of enemies per round
        public int enemyCount = 1;
        // Buttons for starting timer and next round
        public Button startTimerButton, nextRoundButton;
        // UI text for timer and gold
        public TextMeshProUGUI timerText, goldText;
        // Shop UI panel
        public GameObject shopOverlay;

        // Selected unit for placement
        private Unit selectedUnit;
        // Preview of unit being placed
        private GameObject previewInstance;
        // Flags for placement and combat state
        private bool isPlacingUnit = false, placementLock = false, turnsRunning = false, enemiesSpawned = false, roundEnded = false;
        // Timer settings
        private float timerDuration = 20f, timer;
        private bool timerRunning = false;
        // Game state
        private int currentRound = 0, gold = 5;
        // References to managers
        private UnitManger unitManger;
        private PlayerBenchManger benchManger;
        private ShopManager shopManager;

        // Set up initial state and references
        private void Start()
        {
            // Find required managers
            unitManger = FindObjectOfType<UnitManger>();
            benchManger = FindObjectOfType<PlayerBenchManger>();
            shopManager = FindObjectOfType<ShopManager>();
            if (unitManger == null || benchManger == null || shopManager == null)
                Debug.LogError($"Missing: UnitManger={unitManger == null}, PlayerBenchManger={benchManger == null}, ShopManager={shopManager == null}");

            // Set up button listeners
            startTimerButton?.onClick.AddListener(OnStartButtonPressed);
            nextRoundButton?.onClick.AddListener(OnNextRoundButtonPressed);

            // Hide shop at start
            shopOverlay.SetActive(false);

            // Add initial units to bench
            if (tankPrefab != null && archerPrefab != null)
            {
                benchManger.AddUnitToBench(tankPrefab);
                benchManger.AddUnitToBench(archerPrefab);
                Debug.Log("Added Tank and Archer to bench.");
            }
            else
                Debug.LogError($"TankPrefab={tankPrefab == null}, ArcherPrefab={archerPrefab == null}");

            // Initialize UI
            UpdateGoldText();
            StartNewRound();
            UpdateTimerText(timerDuration);
        }

        // Handle placement, pickup, and timer updates
        private void Update()
        {
            HandleUnitPlacement();
            HandleUnitPickup();
            UpdateTimer();
        }

        // Handle unit placement on grid
        private void HandleUnitPlacement()
        {
            if (!isPlacingUnit || selectedUnit == null || placementLock || turnsRunning) return;

            // Get mouse position and convert to hex coordinates
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                Vector2Int hexCoords = hexGrid.GetHexCoordinatesFromPosition(hitPoint);
                Vector3 closestHexCenter = hexGrid.GetWorldPositionFromHex(hexCoords);
                ShowUnitPreview(closestHexCenter);

                // Place unit on left-click
                if (Input.GetMouseButtonDown(0))
                {
                    placementLock = true;
                    // Check if hex is in player area
                    if (hexCoords.x < 5 || hexCoords.x >= hexGrid.Width || hexCoords.y < 0 || hexCoords.y >= hexGrid.Height)
                    {
                        Debug.LogWarning($"Cannot place at hex ({hexCoords.x}, {hexCoords.y}): outside player area.");
                        placementLock = false;
                        return;
                    }

                    // Place unit if hex is empty
                    if (!hexGrid.IsHexOccupied(hexCoords))
                    {
                        GameObject unitGO = Instantiate(selectedUnit.gameObject, closestHexCenter, Quaternion.identity);
                        UnitsMovement movement = unitGO.GetComponent<UnitsMovement>();
                        Unit unit = unitGO.GetComponent<Unit>();
                        if (movement != null && unit != null)
                        {
                            if (hexGrid.PlaceUnitOnHex(unit, hexCoords))
                            {
                                movement.hexPosition = hexCoords;
                                movement.isEnemy = false;
                                unitManger.AddUnit(movement);
                                Debug.Log($"Placed unit {unit.unitName} at hex ({hexCoords.x}, {hexCoords.y})");
                                ClearSelectedUnit();
                            }
                            else
                            {
                                Debug.LogError($"Failed to place unit at hex ({hexCoords.x}, {hexCoords.y})");
                                Destroy(unitGO);
                            }
                        }
                        else
                        {
                            Debug.LogError($"Unit missing components: UnitsMovement={movement == null}, Unit={unit == null}");
                            Destroy(unitGO);
                        }
                    }
                    placementLock = false;
                }
            }
        }

        // Pick up units from grid
        private void HandleUnitPickup()
        {
            if (turnsRunning || isPlacingUnit || !Input.GetMouseButtonDown(0)) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                UnitsMovement movement = hit.collider.GetComponent<UnitsMovement>();
                if (movement != null && !movement.isEnemy)
                {
                    Unit unit = movement.GetComponent<Unit>();
                    if (unit != null)
                    {
                        Vector2Int hexCoords = movement.hexPosition;
                        if (benchManger.AddUnitToBench(unit))
                        {
                            hexGrid.RemoveUnitFromHex(hexCoords);
                            unitManger.GetAllUnits().Remove(movement);
                            Destroy(movement.gameObject);
                            Debug.Log($"Picked up unit {unit.unitName} from hex ({hexCoords.x}, {hexCoords.y})");
                        }
                    }
                }
            }
        }

        // Show a semi-transparent preview of the unit being placed
        private void ShowUnitPreview(Vector3 position)
        {
            if (selectedUnit == null) return;
            if (previewInstance != null) Destroy(previewInstance);

            previewInstance = Instantiate(selectedUnit.gameObject, position, Quaternion.identity);
            Collider col = previewInstance.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            foreach (var renderer in previewInstance.GetComponentsInChildren<Renderer>())
            {
                Material mat = renderer.material;
                Color c = mat.color;
                c.a = 0.5f;
                mat.color = c;
            }
        }

        // Set unit for placement
        public void SetSelectedUnit(Unit unit)
        {
            ClearSelectedUnit();
            selectedUnit = unit;
            isPlacingUnit = unit != null;
            Debug.Log($"Selected unit: {unit?.unitName}");
        }

        // Clear selected unit and preview
        public void ClearSelectedUnit()
        {
            selectedUnit = null;
            isPlacingUnit = false;
            placementLock = false;
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }
        }

        // Spawn enemies for the round
        private void SpawnEnemies()
        {
            if (enemiesSpawned || enemyPrefabs == null || enemyPrefabs.Count == 0) return;

            Unit meleePrefab = enemyPrefabs.Find(p => p.unitType == UnitType.Melee);
            if (meleePrefab != null)
            {
                Vector2Int hexCoords = new Vector2Int(0, 4);
                if (!hexGrid.IsHexOccupied(hexCoords))
                {
                    GameObject unitGO = Instantiate(meleePrefab.gameObject, hexGrid.GetWorldPositionFromHex(hexCoords), Quaternion.identity);
                    UnitsMovement movement = unitGO.GetComponent<UnitsMovement>();
                    Unit unit = unitGO.GetComponent<Unit>();
                    if (movement != null && unit != null)
                    {
                        if (hexGrid.PlaceUnitOnHex(unit, hexCoords))
                        {
                            movement.hexPosition = hexCoords;
                            movement.isEnemy = true;
                            unitManger.AddUnit(movement);
                            Debug.Log($"Spawned Melee enemy at ({hexCoords.x},{hexCoords.y})");
                        }
                        else Destroy(unitGO);
                    }
                    else Destroy(unitGO);
                }
            }
            enemiesSpawned = true;
        }

        // Start a new round
        private void StartNewRound()
        {
            currentRound++;
            enemiesSpawned = false;
            enemyCount = currentRound;
            unitManger.ClearUnits();
            // Clear hex grid units via reflection
            hexGrid.GetType()
                .GetField("hexUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(hexGrid, new Dictionary<Vector2Int, Unit>());
            SpawnEnemies();
            UpdateGoldText();
            turnsRunning = false;
            shopOverlay.SetActive(false);
            if (nextRoundButton != null)
                nextRoundButton.interactable = false;
        }

        // Start combat and timer
        public void OnStartButtonPressed()
        {
            if (!turnsRunning)
            {
                turnsRunning = true;
                StartTimer();
                StartCoroutine(RunTurns());
            }
        }

        // Move to next round
        public void OnNextRoundButtonPressed()
        {
            if (roundEnded)
            {
                StartNewRound();
                roundEnded = false;
                nextRoundButton.interactable = false;
            }
        }

        // Start the round timer
        private void StartTimer()
        {
            timer = timerDuration;
            timerRunning = true;
            startTimerButton.interactable = false;
            shopOverlay.SetActive(false);
        }

        // Update timer and UI
        private void UpdateTimer()
        {
            if (!timerRunning) return;
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timerRunning = false;
                timer = 0f;
                startTimerButton.interactable = true;
                shopOverlay.SetActive(true);
            }
            UpdateTimerText(timer);
        }

        // Update timer display
        private void UpdateTimerText(float time)
        {
            if (timerText != null)
                timerText.text = $"{Mathf.FloorToInt(time / 60):00}:{Mathf.FloorToInt(time % 60):00}";
        }

        // Update gold display
        private void UpdateGoldText()
        {
            if (goldText != null)
                goldText.text = $"Gold: {gold}";
        }

        // Add gold to player
        public void AddGold(int amount)
        {
            gold += amount;
            UpdateGoldText();
        }

        // Get current gold
        public int GetGold()
        {
            return gold;
        }

        // Run combat turns
        private IEnumerator RunTurns()
        {
            while (turnsRunning)
            {
                List<UnitsMovement> units = unitManger.GetAllUnits();
                bool allUnitsStopped = true;

                foreach (UnitsMovement unit in units)
                {
                    if (unit != null && !unit.IsLocked && !unit.hasMoved)
                    {
                        allUnitsStopped = false;
                        unit.TakeTurn();
                        yield return new WaitUntil(() => unit.hasMoved || unit.IsLocked);
                        yield return new WaitForSeconds(0.5f);
                    }
                }

                // Check if enemies are defeated or units can't move
                bool enemiesAlive = units.Exists(u => u != null && u.isEnemy);
                if (!enemiesAlive || allUnitsStopped)
                {
                    turnsRunning = false;
                    roundEnded = true;
                    shopOverlay.SetActive(true);
                    if (nextRoundButton != null)
                        nextRoundButton.interactable = true;
                    yield break;
                }

                // Reset movement flags
                foreach (var unit in units)
                    if (unit != null)
                        unit.hasMoved = false;

                yield return null;
            }
        }
    }
}