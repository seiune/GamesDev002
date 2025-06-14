namespace Game
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    // Handles unit movement and combat on the hex grid
    public class UnitsMovement : MonoBehaviour
    {
        // Current hex position
        public Vector2Int hexPosition;
        // Is this an enemy unit?
        public bool isEnemy;
        // Has the unit moved this turn?
        public bool hasMoved;
        // Is the unit locked from acting?
        public bool IsLocked { get { return false; } }
        // Is the unit currently moving?
        public bool IsMoving { get; private set; }
        // Unit component
        private Unit unit;
        // References to managers
        private UnitManger unitManger;
        private HexGrid hexGrid;
        // Duration for movement animation
        private float moveDuration = 0.5f;

        // Initialize references
        private void Awake()
        {
            unit = GetComponent<Unit>();
            unitManger = Object.FindFirstObjectByType<UnitManger>();
            hexGrid = Object.FindFirstObjectByType<HexGrid>();
            if (unit == null) Debug.LogError($"Unit component missing on {gameObject.name}.");
            if (unitManger == null) Debug.LogError("UnitManger not found in scene.");
            if (hexGrid == null) Debug.LogError("HexGrid not found in scene.");
        }

        // Reset unit state for bench
        public void ResetUnit()
        {
            hasMoved = false;
            IsMoving = false;
            hexPosition = new Vector2Int(-1, -1);
            gameObject.SetActive(true);
            Debug.Log($"Reset unit {(unit != null ? unit.name : gameObject.name)} state for bench.");
        }

        // Perform unit's turn (attack or move)
        public void TakeTurn()
        {
            if (hasMoved || IsMoving) return;

            // Check for adjacent enemy to attack
            UnitsMovement neighborTarget = FindNeighborTarget();
            if (neighborTarget != null)
            {
                Unit targetUnit = neighborTarget.GetComponent<Unit>();
                targetUnit.health -= unit.damage;
                Debug.Log($"{unit.name} attacked {targetUnit.name} for {unit.damage} damage.");
                if (targetUnit.health <= 0)
                {
                    hexGrid.RemoveUnitFromHex(neighborTarget.hexPosition);
                    unitManger.GetAllUnits().Remove(neighborTarget);
                    Destroy(neighborTarget.gameObject);
                    Debug.Log($"{targetUnit.name} destroyed.");
                }
                hasMoved = true;
            }
            else
            {
                // Find closest enemy to attack or move toward
                UnitsMovement target = FindTarget();
                if (target != null && Vector3.Distance(transform.position, target.transform.position) <= unit.attackRange)
                {
                    Unit targetUnit = target.GetComponent<Unit>();
                    targetUnit.health -= unit.damage;
                    Debug.Log($"{unit.name} attacked {targetUnit.name} for {unit.damage} damage.");
                    if (targetUnit.health <= 0)
                    {
                        hexGrid.RemoveUnitFromHex(target.hexPosition);
                        unitManger.GetAllUnits().Remove(target);
                        Destroy(target.gameObject);
                        Debug.Log($"{targetUnit.name} destroyed.");
                    }
                    hasMoved = true;
                }
                else
                    StartCoroutine(MoveTowardsTarget(target));
            }
        }

        // Find adjacent enemy unit
        private UnitsMovement FindNeighborTarget()
        {
            List<Vector2Int> neighbors = new List<Vector2Int>
            {
                hexPosition + new Vector2Int(1, 0),
                hexPosition + new Vector2Int(-1, 0),
                hexPosition + new Vector2Int(0, 1),
                hexPosition + new Vector2Int(0, -1),
                hexPosition + new Vector2Int(1, -1),
                hexPosition + new Vector2Int(-1, 1)
            };

            foreach (var neighbor in neighbors)
                if (neighbor.x >= 0 && neighbor.x < hexGrid.Width && neighbor.y >= 0 && neighbor.y < hexGrid.Height)
                {
                    UnitsMovement unitAtHex = hexGrid.GetUnitAtHex(neighbor);
                    if (unitAtHex != null && unitAtHex.isEnemy != isEnemy)
                        return unitAtHex;
                }
            return null;
        }

        // Find closest enemy unit
        private UnitsMovement FindTarget()
        {
            List<UnitsMovement> units = unitManger.GetAllUnits();
            UnitsMovement closest = null;
            float minDist = float.MaxValue;
            foreach (var u in units)
                if (u != null && u.isEnemy != isEnemy)
                {
                    float dist = Vector3.Distance(transform.position, u.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = u;
                    }
                }
            return closest;
        }

        // Move toward the target unit
        private IEnumerator MoveTowardsTarget(UnitsMovement target)
        {
            if (target == null)
            {
                hasMoved = true;
                yield break;
            }

            Vector2Int targetPos = target.hexPosition;
            List<Vector2Int> neighbors = new List<Vector2Int>
            {
                hexPosition + new Vector2Int(1, 0),
                hexPosition + new Vector2Int(-1, 0),
                hexPosition + new Vector2Int(0, 1),
                hexPosition + new Vector2Int(0, -1),
                hexPosition + new Vector2Int(1, -1),
                hexPosition + new Vector2Int(-1, 1)
            };
            Vector2Int nextHex = hexPosition;
            float minDist = Vector3.Distance(transform.position, hexGrid.GetWorldPositionFromHex(targetPos));
            foreach (var neighbor in neighbors)
                if (neighbor.x >= 0 && neighbor.x < hexGrid.Width && neighbor.y >= 0 && neighbor.y < hexGrid.Height && !hexGrid.IsHexOccupied(neighbor))
                {
                    float dist = Vector3.Distance(hexGrid.GetWorldPositionFromHex(neighbor), hexGrid.GetWorldPositionFromHex(targetPos));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nextHex = neighbor;
                    }
                }

            if (nextHex != hexPosition)
            {
                IsMoving = true;
                hexGrid.RemoveUnitFromHex(hexPosition);
                yield return StartCoroutine(MoveToHex(nextHex));
                if (hexGrid.PlaceUnitOnHex(unit, nextHex))
                {
                    hexPosition = nextHex;
                    Debug.Log($"{unit.name} moved to hex {nextHex}.");
                }
                else
                {
                    Debug.LogError($"Failed to place {unit.name} at hex {nextHex}.");
                    hexGrid.PlaceUnitOnHex(unit, hexPosition);
                }
                IsMoving = false;
                hasMoved = true;
            }
            else
                hasMoved = true;
        }

        // Animate movement to a hex
        private IEnumerator MoveToHex(Vector2Int targetHex)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = hexGrid.GetWorldPositionFromHex(targetHex);
            float elapsedTime = 0f;

            while (elapsedTime < moveDuration)
            {
                transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.position = endPos;
        }
    }
}