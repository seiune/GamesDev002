namespace Game
{
    using UnityEngine;

    public class GameManager : MonoBehaviour
    {
        private BattleManager battleManager;

        private void Awake()
        {
            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null) Debug.LogError("BattleManager not found in scene.");
        }
    }
}