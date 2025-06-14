using UnityEngine;

// Configuration for battle setup, stored as a ScriptableObject
[CreateAssetMenu(fileName = "BattleSettings", menuName = "ScriptableObjects/BattleSettings")]
public class BattleSettings : ScriptableObject
{
    // Width of the battle grid
    public int boardWidth = 10;
    // Height of the battle grid
    public int boardHeight = 6;
    // Number of starting units for the player
    public int initialPlayerUnits = 2;
    // Start of enemy area (column index)
    public int enemyColumnsStart = 5;
    // End of enemy area (column index)
    public int enemyColumnsEnd = 9;
}