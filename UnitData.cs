using UnityEngine;

[CreateAssetMenu(menuName = "Unit/UnitData")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public Sprite icon;
    public int attack;
    public int health;
}
