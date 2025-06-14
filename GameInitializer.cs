namespace Game
{
    using UnityEngine;

    // Sets up game on start
    public class GameInitializer : MonoBehaviour
    {
        // Reset time scale
        private void Awake()
        {
            Time.timeScale = 1f; // Make sure game runs normally
            Debug.Log($"GameInitializer.Awake: Time.timeScale = {Time.timeScale}, GameObject = {gameObject.name}");
        }
    }
}