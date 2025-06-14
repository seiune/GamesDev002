namespace Game
{
    using System.Collections.Generic;
    using UnityEngine;

    // Manages the hex grid for unit placement and movement
    public class HexGrid : MonoBehaviour
    {
        // Grid dimensions
        public int Width = 10, Height = 6;
        // Size of each hex
        public float hexSize = 5f;
        // Tracks units on the grid
        private Dictionary<Vector2Int, Unit> hexUnits = new Dictionary<Vector2Int, Unit>();

        // Initialize the grid
        private void Awake()
        {
            hexUnits.Clear();
            Debug.Log($"HexGrid initialized with {Width}x{Height} grid, hexSize={hexSize}.");
        }

        // Place a unit on a hex
        public bool PlaceUnitOnHex(Unit unit, Vector2Int hex)
        {
            if (unit == null)
            {
                Debug.LogError($"Attempted to place null unit at hex {hex}.");
                return false;
            }
            if (hex.x < 0 || hex.x >= Width || hex.y < 0 || hex.y >= Height)
            {
                Debug.LogError($"Invalid hex coordinates {hex} for unit {unit.unitName}.");
                return false;
            }
            if (IsHexOccupied(hex))
            {
                Debug.LogWarning($"Hex {hex} already occupied for unit {unit.unitName}.");
                return false;
            }
            hexUnits[hex] = unit;
            unit.transform.position = GetWorldPositionFromHex(hex);
            UnitsMovement movement = unit.GetComponent<UnitsMovement>();
            if (movement != null)
                movement.hexPosition = hex;
            else
                Debug.LogWarning($"Unit {unit.unitName} at hex {hex} missing UnitsMovement component.");
            Debug.Log($"Placed unit {unit.unitName} at hex {hex}");
            return true;
        }

        // Remove a unit from a hex
        public void RemoveUnitFromHex(Vector2Int hex)
        {
            if (hexUnits.ContainsKey(hex))
            {
                Debug.Log($"Removing unit from hex {hex}.");
                hexUnits.Remove(hex);
            }
        }

        // Check if a hex is occupied
        public bool IsHexOccupied(Vector2Int hex)
        {
            bool occupied = hexUnits.ContainsKey(hex) && hexUnits[hex] != null;
            Debug.Log($"Checking hex {hex}: {(occupied ? "Occupied" : "Empty")}");
            return occupied;
        }

        // Get the unit at a hex
        public UnitsMovement GetUnitAtHex(Vector2Int hex)
        {
            if (hexUnits.TryGetValue(hex, out Unit unit) && unit != null)
            {
                UnitsMovement movement = unit.GetComponent<UnitsMovement>();
                if (movement == null)
                    Debug.LogWarning($"Unit at hex {hex} missing UnitsMovement component.");
                return movement;
            }
            return null;
        }

        // Convert hex coordinates to world position
        public Vector3 GetWorldPositionFromHex(Vector2Int hex)
        {
            float xOffset = (hex.y % 2 == 0) ? 0f : hexSize * Mathf.Sqrt(3f) / 2f;
            float x = hex.x * hexSize * Mathf.Sqrt(3f) + xOffset;
            float z = hex.y * hexSize * 1.5f;
            return new Vector3(x, 0f, z);
        }

        // Convert world position to hex coordinates
        public Vector2Int GetHexCoordinatesFromPosition(Vector3 position)
        {
            float z = position.z / (hexSize * 1.5f);
            int y = Mathf.RoundToInt(z);
            float xOffset = (y % 2 == 0) ? 0f : hexSize * Mathf.Sqrt(3f) / 2f;
            float x = (position.x - xOffset) / (hexSize * Mathf.Sqrt(3f));
            int xInt = Mathf.RoundToInt(x);
            return new Vector2Int(xInt, y);
        }

        // Visualize hex grid in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    Vector3 center = GetWorldPositionFromHex(new Vector2Int(x, y));
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = 60f * i - 30f;
                        Vector3 p1 = center + new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle), 0f, Mathf.Sin(Mathf.Deg2Rad * angle)) * hexSize;
                        Vector3 p2 = center + new Vector3(Mathf.Cos(Mathf.Deg2Rad * (angle + 60f)), 0f, Mathf.Sin(Mathf.Deg2Rad * (angle + 60f))) * hexSize;
                        Gizmos.DrawLine(p1, p2);
                    }
                }
        }
    }
}