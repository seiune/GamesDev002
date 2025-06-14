namespace Game
{
    using System.Collections.Generic;
    using UnityEngine;

    // Manages the player's bench for storing and placing units
    public class PlayerBenchManger : MonoBehaviour
    {
        // List of bench slots
        public List<BenchSlot> benchSlots;
        // Available unit prefabs
        public List<Unit> availableUnits;
        // References to managers
        private BattleManager battleManager;
        private HexGrid hexGrid;
        private UnitManger unitManger;
        // Selected unit for placement
        private Unit selectedUnit;
        // Max units allowed on bench
        private const int MAX_UNITS = 12;

        // Set up bench and references
        private void Awake()
        {
            if (availableUnits == null) availableUnits = new List<Unit>();
            if (benchSlots == null) benchSlots = new List<BenchSlot>();
            battleManager = FindObjectOfType<BattleManager>();
            hexGrid = FindObjectOfType<HexGrid>();
            unitManger = FindFirstObjectByType<UnitManger>();
            if (battleManager == null) Debug.LogError("BattleManager not found in scene.");
            if (hexGrid == null) Debug.LogError("HexGrid not found in scene.");
            if (unitManger == null) Debug.LogError("UnitManger not found in scene.");
            if (benchSlots.Count > MAX_UNITS) Debug.LogWarning($"Bench has {benchSlots.Count} slots, capping at {MAX_UNITS}.");
            Debug.Log($"Awake: AvailableUnits count: {availableUnits.Count}, BenchSlots count: {benchSlots.Count}, GameObject active: {gameObject.activeInHierarchy}");
            for (int i = 0; i < availableUnits.Count; i++)
                Debug.Log($"AvailableUnit[{i}]: {(availableUnits[i] != null ? availableUnits[i].name : "null")}, unitName: {(availableUnits[i] != null ? availableUnits[i].unitName : "null")}, Has UnitsMovement: {(availableUnits[i] != null && availableUnits[i].GetComponent<UnitsMovement>() != null)}");
        }

        // Initialize bench slots
        private void Start()
        {
            if (benchSlots == null || benchSlots.Count == 0)
            {
                Debug.LogError("BenchSlots list is null or empty. Please assign slots in the Inspector.");
                return;
            }
            foreach (var slot in benchSlots)
            {
                if (slot == null) Debug.LogError("Null BenchSlot found in benchSlots list.");
                else
                {
                    slot.ClearSlot();
                    Debug.Log($"Cleared slot {slot.name}, IsOccupied: {slot.IsOccupied}");
                }
            }
            Debug.Log($"Start: Initialized bench with {benchSlots.Count} slots. AvailableUnits: {availableUnits.Count}");
            AddTwoRandomUnitsToBench();
        }

        // Handle unit placement clicks
        private void Update()
        {
            if (selectedUnit != null && Input.GetMouseButtonDown(0))
            {
                Vector2Int hex = GetClickedHex();
                if (hex != new Vector2Int(-1, -1) && IsValidPlayerHex(hex))
                    TryPlaceUnit(selectedUnit, hex);
            }
        }

        // Add two random units to bench at start
        private void AddTwoRandomUnitsToBench()
        {
            if (availableUnits == null || availableUnits.Count < 2 || GetOccupiedSlotCount() >= MAX_UNITS)
            {
                Debug.LogWarning($"Cannot add units. AvailableUnits: {(availableUnits == null ? "null" : availableUnits.Count.ToString())}, Occupied slots: {GetOccupiedSlotCount()}, Max: {MAX_UNITS}");
                return;
            }
            if (benchSlots == null || benchSlots.Count < 2)
            {
                Debug.LogError($"Bench has {benchSlots?.Count ?? 0} slots, need at least 2. Please assign BenchSlot GameObjects in the Inspector.");
                return;
            }

            List<Unit> shuffledUnits = new List<Unit>();
            foreach (var unit in availableUnits)
                if (unit != null)
                    shuffledUnits.Add(unit);

            if (shuffledUnits.Count < 2)
            {
                Debug.LogWarning($"Not enough valid units to add. Found: {shuffledUnits.Count}");
                return;
            }

            // Shuffle units
            for (int i = 0; i < shuffledUnits.Count; i++)
            {
                Unit temp = shuffledUnits[i];
                int randomIndex = Random.Range(i, shuffledUnits.Count);
                shuffledUnits[i] = shuffledUnits[randomIndex];
                shuffledUnits[randomIndex] = temp;
            }

            // Add two units
            int unitsAdded = 0;
            for (int i = 0; i < 2 && i < shuffledUnits.Count && GetOccupiedSlotCount() < MAX_UNITS; i++)
            {
                if (AddUnitToBench(shuffledUnits[i]))
                    unitsAdded++;
            }
            Debug.Log($"Added {unitsAdded} random units to bench. Total occupied: {GetOccupiedSlotCount()}");
            if (unitsAdded == 0)
                Debug.LogError("Failed to add any units. Check unit prefabs and bench slots.");
        }

        // Add a unit to the bench
        public bool AddUnitToBench(Unit unit)
        {
            if (unit == null)
            {
                Debug.LogError("Unit is null.");
                return false;
            }
            if (GetOccupiedSlotCount() >= MAX_UNITS)
            {
                Debug.LogWarning($"Bench is full (max {MAX_UNITS} units). Cannot add unit {unit.unitName}.");
                return false;
            }
            BenchSlot emptySlot = benchSlots.Find(slot => slot != null && !slot.IsOccupied);
            if (emptySlot != null)
            {
                UnitsMovement movement = unit.GetComponent<UnitsMovement>();
                if (movement != null && unit.gameObject.scene.IsValid())
                    movement.ResetUnit();
                emptySlot.SetUnit(unit);
                if (!availableUnits.Contains(unit))
                    availableUnits.Add(unit);
                Debug.Log($"Added unit {unit.name} to slot {emptySlot.name}. IsOccupied: {emptySlot.IsOccupied}, Total occupied: {GetOccupiedSlotCount()}");
                return true;
            }
            Debug.LogWarning($"No empty slot found for {unit.name}. Total slots: {benchSlots.Count}, Occupied: {GetOccupiedSlotCount()}");
            return false;
        }

        // Remove a unit from the bench
        public void RemoveUnitFromBench(Unit unit)
        {
            BenchSlot slot = benchSlots.Find(s => s.IsOccupied && s.Unit == unit);
            if (slot != null)
            {
                slot.ClearSlot();
                availableUnits.Remove(unit);
                Debug.Log($"Removed unit {unit.unitName} from bench. Total occupied: {GetOccupiedSlotCount()}");
            }
        }

        // Count occupied slots
        public int GetOccupiedSlotCount()
        {
            int count = 0;
            foreach (var slot in benchSlots)
                if (slot != null && slot.IsOccupied)
                    count++;
            return count;
        }

        // Move grid units back to bench
        public void CollectUnitsToBench()
        {
            if (hexGrid == null || unitManger == null)
            {
                Debug.LogError("HexGrid or UnitManger is null, cannot collect units.");
                return;
            }
            List<UnitsMovement> playerUnits = unitManger.GetAllUnits().FindAll(u => u != null && !u.isEnemy);
            Debug.Log($"Collecting {playerUnits.Count} player units to bench.");
            foreach (UnitsMovement unit in playerUnits)
            {
                if (unit != null && unit.gameObject.scene.IsValid())
                {
                    Unit unitComponent = unit.GetComponent<Unit>();
                    if (unitComponent != null)
                    {
                        hexGrid.RemoveUnitFromHex(unit.hexPosition);
                        unitManger.GetAllUnits().Remove(unit);
                        if (AddUnitToBench(unitComponent))
                            Debug.Log($"Moved unit {unitComponent.unitName} to bench.");
                        else
                        {
                            Debug.LogWarning($"Failed to move unit {unitComponent.unitName} to bench. Bench full?");
                            Destroy(unit.gameObject);
                        }
                    }
                }
            }
            Debug.Log($"Finished collecting units. Bench occupied: {GetOccupiedSlotCount()}");
        }

        // Select a unit for placement
        public void OnSelectUnit(Unit unit)
        {
            if (unit != null)
            {
                selectedUnit = unit;
                battleManager.SetSelectedUnit(unit);
                Debug.Log($"Selected unit {unit.unitName}, Name: {unit.name}, IsPrefab: {!unit.gameObject.scene.IsValid()}, Active: {unit.gameObject.activeInHierarchy}, Scene valid: {unit.gameObject.scene.IsValid()}");
            }
            else
            {
                selectedUnit = null;
                Debug.LogError($"Selected unit is null.");
            }
        }

        // Try to place a unit on the grid
        private bool TryPlaceUnit(Unit unit, Vector2Int hex)
        {
            if (unit == null)
            {
                Debug.LogError($"Unit is null.");
                selectedUnit = null;
                return false;
            }
            if (unit.gameObject.scene.IsValid() && !unit.gameObject.activeInHierarchy)
            {
                Debug.LogError($"Unit is inactive: {unit.unitName}");
                selectedUnit = null;
                return false;
            }
            if (!IsValidPlayerHex(hex))
            {
                Debug.LogWarning($"Hex {hex} is not in player's area (x=5-9).");
                return false;
            }
            UnitsMovement movement = unit.GetComponent<UnitsMovement>();
            if (movement == null)
            {
                Debug.LogError($"Unit {unit.unitName} missing UnitsMovement component.");
                return false;
            }
            Unit instance = unit.gameObject.scene.IsValid() ? unit : Instantiate(unit, hexGrid.GetWorldPositionFromHex(hex), Quaternion.identity);
            movement = instance.GetComponent<UnitsMovement>();
            movement.hexPosition = hex;
            movement.isEnemy = false;
            movement.ResetUnit();
            if (hexGrid.PlaceUnitOnHex(instance, hex))
            {
                unitManger.GetAllUnits().Add(movement);
                RemoveUnitFromBench(unit);
                Debug.Log($"Placed unit {instance.unitName} on hex {hex}");
                selectedUnit = null;
                return true;
            }
            else
            {
                Debug.LogError($"Failed to place unit {instance.unitName} on hex {hex}.");
                if (instance != unit) Destroy(instance.gameObject);
                return false;
            }
        }

        // Get hex coordinates from mouse click
        private Vector2Int GetClickedHex()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Vector3 worldPos = hit.point;
                Vector2Int hex = hexGrid.GetHexCoordinatesFromPosition(worldPos);
                if (hex.x >= 0 && hex.x < hexGrid.Width && hex.y >= 0 && hex.y < hexGrid.Height)
                {
                    Debug.Log($"Clicked hex {hex}");
                    return hex;
                }
            }
            Debug.Log("Clicked outside valid hex grid.");
            return new Vector2Int(-1, -1);
        }

        // Check if hex is valid for player placement
        private bool IsValidPlayerHex(Vector2Int hex)
        {
            return hex.x >= 5 && hex.x < hexGrid.Width && hex.y >= 0 && hex.y < hexGrid.Height && !hexGrid.IsHexOccupied(hex);
        }
    }
}