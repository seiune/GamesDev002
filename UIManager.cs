namespace Game
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    // Manages the game's UI, including timer and shop
    public class UIManager : MonoBehaviour
    {
        // UI elements
        public Button startButton;
        public TextMeshProUGUI timerText;
        public GameObject shopPanel;
        // References to managers
        public BattleManager battleManager;
        private ShopManager shopManager;
        private PlayerBenchManger benchManger;
        // Timer state
        private float timeRemaining = 20f;
        private bool isTimerRunning = false;

        // Initialize UI and references
        private void Awake()
        {
            if (battleManager == null)
            {
                battleManager = UnityEngine.Object.FindObjectOfType<BattleManager>();
                if (battleManager == null) Debug.LogError("BattleManager not found in scene during Awake. Please assign manually in Inspector.");
                else Debug.Log("BattleManager found during Awake via FindObjectOfType.");
            }
            else
                Debug.Log("BattleManager assigned manually in Inspector.");
            shopManager = UnityEngine.Object.FindObjectOfType<ShopManager>();
            benchManger = UnityEngine.Object.FindObjectOfType<PlayerBenchManger>();
            if (shopManager == null) Debug.LogError("ShopManager not found in scene.");
            if (benchManger == null) Debug.LogError("PlayerBenchManger not found in scene.");
            if (startButton == null) Debug.LogError("Start button not assigned in UIManager.");
            if (timerText == null) Debug.LogError("Timer text not assigned in UIManager.");
            if (shopPanel == null) Debug.LogError("Shop panel not assigned in UIManager.");
        }

        // Set up UI and event listeners
        private void Start()
        {
            startButton.onClick.AddListener(OnStartButtonPressed);
            UpdateTimerDisplay();
            EnsureImageComponent();
            Debug.Log("Shop panel initialized in UIManager.");
            if (battleManager == null)
            {
                battleManager = UnityEngine.Object.FindObjectOfType<BattleManager>();
                if (battleManager == null)
                    Debug.LogError("BattleManager still not found in Start, check scene hierarchy.");
                else
                    Debug.Log("BattleManager found in Start fallback.");
            }
            if (battleManager != null)
            {
                battleManager.OnLevelWon += ShowShop;
                Debug.Log("Subscribed to BattleManager.OnLevelWon.");
            }
            else
                Debug.LogError("Cannot subscribe to OnLevelWon: BattleManager is null.");
        }

        // Clean up event subscriptions
        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.OnLevelWon -= ShowShop;
                Debug.Log("Unsubscribed from BattleManager.OnLevelWon.");
            }
        }

        // Update timer and check for game over
        private void Update()
        {
            if (isTimerRunning)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay();
                if (timeRemaining <= 0)
                {
                    isTimerRunning = false;
                    GameOver();
                }
            }
        }

        // Start combat when button is pressed
        private void OnStartButtonPressed()
        {
            isTimerRunning = true;
            startButton.interactable = false;
            timeRemaining = 20f;
            UpdateTimerDisplay();
            if (battleManager != null)
            {
                battleManager.StartCombat();
                Debug.Log("Start button pressed, timer started.");
            }
            else
                Debug.LogError("Cannot start combat: BattleManager is null.");
        }

        // Update timer display
        private void UpdateTimerDisplay()
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0, timeRemaining));
            timerText.text = $"00:{seconds:00}";
        }

        // Ensure shop panel has an image component
        private void EnsureImageComponent()
        {
            Image image = shopPanel.GetComponent<Image>();
            if (image == null)
            {
                image = shopPanel.AddComponent<Image>();
                image.color = new Color(1, 1, 1, 0.8f);
            }
            else if (image.color.a < 0.1f)
                image.color = new Color(1, 1, 1, 0.8f);
            Debug.Log($"ShopPanel Image: Color {image.color}, Enabled: {image.enabled}");
        }

        // Show shop when level is won
        private void ShowShop()
        {
            isTimerRunning = false;
            if (shopManager != null)
                shopManager.OpenShop();
            else
                Debug.LogError("ShopManager is null. Cannot open shop.");
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
                shopPanel.transform.localScale = Vector3.one;
                shopPanel.transform.localPosition = Vector3.zero;
                Canvas canvas = shopPanel.GetComponentInParent<Canvas>();
                Debug.Log($"Shop panel activated. Active: {shopPanel.activeSelf}, Position: {shopPanel.transform.localPosition}, Scale: {shopPanel.transform.localScale}, Canvas Active: {(canvas != null ? canvas.isActiveAndEnabled : false)}, Child Count: {shopPanel.transform.childCount}, Parent: {(shopPanel.transform.parent != null ? shopPanel.transform.parent.name : "None")}");
                EnsureImageComponent();
            }
            else
                Debug.LogError("ShopPanel is null in UIManager.");
        }

        // Handle game over conditions
        private void GameOver()
        {
            Debug.Log("Game Over: Time limit exceeded.");
            if (battleManager != null && benchManger != null)
            {
                var units = FindObjectsOfType<UnitsMovement>();
                bool allPlayerUnitsDead = true;
                foreach (var unit in units)
                    if (unit != null && !unit.isEnemy && unit.GetComponent<Unit>().health > 0)
                    {
                        allPlayerUnitsDead = false;
                        break;
                    }
                if (allPlayerUnitsDead || benchManger.GetOccupiedSlotCount() >= 12)
                {
                    Debug.Log($"Game Over: All player units dead or bench limit (12) reached. Occupied slots: {benchManger.GetOccupiedSlotCount()}");
                    isTimerRunning = false;
                    startButton.interactable = true;
                }
            }
        }
    }
}