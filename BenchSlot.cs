namespace Game
{
    using UnityEngine;
    using UnityEngine.UI;

    // Manages a single slot on the player's bench
    public class BenchSlot : MonoBehaviour
    {
        private Unit unit; // Unit in this slot
        private Image slotImage; // Image for slot visuals
        private PlayerBenchManger benchManger; // Ref to bench manager
        public bool IsOccupied { get { return unit != null; } } // Check if slot has unit
        public Unit Unit { get { return unit; } } // Get unit in slot

        // Setup slot components
        private void Awake()
        {
            slotImage = GetComponent<Image>();
            benchManger = FindObjectOfType<PlayerBenchManger>();
            // Add click listener to button
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnSlotClicked);
                Debug.Log($"BenchSlot {name}: Button listener set to OnSlotClicked.");
            }
            else
            {
                Debug.LogError($"BenchSlot {name}: No Button component found.");
            }
            if (benchManger == null) Debug.LogError($"BenchManger not found for slot {name}.");
            if (slotImage == null) Debug.LogError($"BenchSlot {name}: No Image component found.");
        }

        // Put a unit in this slot
        public void SetUnit(Unit newUnit)
        {
            unit = newUnit;
            if (unit != null && slotImage != null)
            {
                // Color slot based on unit type
                switch (unit.unitType)
                {
                    case UnitType.King:
                        slotImage.color = new Color(1f, 0.84f, 0f); // Gold
                        break;
                    case UnitType.Warrior:
                        slotImage.color = Color.red; // Red
                        break;
                    case UnitType.Adventurer:
                        slotImage.color = Color.blue; // Blue
                        break;
                    case UnitType.Knight:
                        slotImage.color = new Color(0.75f, 0.75f, 0.75f); // Silver
                        break;
                    default:
                        slotImage.color = Color.grey; // Fallback
                        Debug.LogWarning($"Unknown UnitType {unit.unitType} for unit {unit.unitName}, using grey.");
                        break;
                }
            }
            else
            {
                slotImage.color = Color.grey; // Empty slot color
            }
            Debug.Log($"SetUnit in slot {name}: {(unit != null ? unit.unitName : "null")}, IsOccupied: {IsOccupied}, Color: {slotImage.color}");
        }

        // Clear unit from slot
        public void ClearSlot()
        {
            unit = null;
            if (slotImage != null)
            {
                slotImage.color = Color.grey; // Reset to empty color
            }
            Debug.Log($"Cleared slot {name}, IsOccupied: {IsOccupied}");
        }

        // Handle slot click
        private void OnSlotClicked()
        {
            if (name.Contains("NextRound"))
            {
                Debug.LogWarning($"Slot {name} is incorrectly configured as a BenchSlot. Remove BenchSlot component or reassign button.");
                return;
            }
            if (unit != null)
            {
                if (!unit.gameObject.scene.IsValid() || unit.gameObject.activeInHierarchy)
                {
                    benchManger.OnSelectUnit(unit); // Select unit for placement
                    Debug.Log($"Clicked slot {name}, selected unit: {unit.unitName}, IsPrefab: {!unit.gameObject.scene.IsValid()}, Active: {unit.gameObject.activeInHierarchy}");
                }
                else
                {
                    Debug.LogWarning($"Unit in slot {name} is inactive: {unit.unitName}");
                }
            }
            else
            {
                Debug.LogWarning($"No valid unit in slot {name}.");
            }
        }
    }
}