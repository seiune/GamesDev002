namespace Game
{
    using System.Collections.Generic;
    using UnityEngine;

    // Tracks all active units in the game
    public class UnitManger : MonoBehaviour
    {
        // List of all units
        private List<UnitsMovement> units = new List<UnitsMovement>();

        // Add a unit to the manager
        public void AddUnit(UnitsMovement unit)
        {
            if (unit != null && !units.Contains(unit))
            {
                units.Add(unit);
                Debug.Log($"UnitManger: Added unit at hex ({unit.hexPosition.x}, {unit.hexPosition.y}).");
            }
        }

        // Remove a unit from the manager
        public void RemoveUnit(UnitsMovement unit)
        {
            if (unit != null && units.Contains(unit))
            {
                units.Remove(unit);
                Debug.Log($"UnitManger: Removed unit at hex ({unit.hexPosition.x}, {unit.hexPosition.y}).");
            }
        }

        // Get all active units
        public List<UnitsMovement> GetAllUnits()
        {
            return units;
        }

        // Clear all units
        public void ClearUnits()
        {
            units.Clear();
            Debug.Log("UnitManger: Cleared all units.");
        }
    }
}