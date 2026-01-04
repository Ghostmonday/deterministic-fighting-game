using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuralDraft.Unity {
    public class UIManager : MonoBehaviour {
        [Header("HUD")]
        public Slider p1HealthBar;
        public Slider p2HealthBar;
        public TextMeshProUGUI p1HealthText;
        public TextMeshProUGUI p2HealthText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI frameCounterText;
        
        [Header("Character Select")]
        public GameObject characterSelectPanel;
        public Button[] p1CharacterButtons;
        public Button[] p2CharacterButtons;
        
        [Header("Match End")]
        public GameObject matchEndPanel;
        public TextMeshProUGUI winnerText;
        public Button restartButton;
        public Button mainMenuButton;
        
        [Header("Skins Store")]
        public GameObject storePanel;
        public Transform skinGrid;
        public TextMeshProUGUI walletBalanceText;
        
        [Header("Combo Display")]
        public TextMeshProUGUI comboText;
        public TextMeshProUGUI damageText;
        
        private int _timer = 99;
        private int _currentFrame;
        private float _timerAccumulator;
        
        void Awake() {
            if (matchEndPanel != null) matchEndPanel.SetActive(false);
            if (storePanel != null) storePanel.SetActive(false);
            if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
        }
        
        void Start() {
            if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
        }
        
        void Update() {
            if (EngineBridge.Instance == null || !EngineBridge.Instance.runEngine) return;
            
            UpdateTimer();
            UpdateFrameCounter();
        }
        
        public void UpdateHUD(int p1Health, int p2Health, int frame) {
            if (p1HealthBar != null) p1HealthBar.value = Mathf.Max(0, p1Health / 100f);
            if (p2HealthBar != null) p2HealthBar.value = Mathf.Max(0, p2Health / 100f);
            if (p1HealthText != null) p1HealthText.text = p1Health.ToString();
            if (p2HealthText != null) p2HealthText.text = p2Health.ToString();
        }
        
        void UpdateTimer() {
            _timerAccumulator += Time.deltaTime;
            if (_timerAccumulator >= 1f) {
                _timerAccumulator -= 1f;
                _timer--;
                if (_timer < 0) _timer = 99;
                if (timerText != null) timerText.text = _timer.ToString("D2");
            }
        }
        
        void UpdateFrameCounter() {
            if (EngineBridge.Instance != null) {
                _currentFrame = EngineBridge.Instance.GetFrameCount();
                if (frameCounterText != null) frameCounterText.text = $"FRAME {_currentFrame}";
            }
        }
        
        public void ShowMatchEnd(int winner) {
            if (matchEndPanel != null) {
                matchEndPanel.SetActive(true);
                if (winnerText != null) {
                    if (winner == -1) winnerText.text = "DOUBLE KO!";
                    else winnerText.text = $"PLAYER {winner + 1} WINS!";
                }
            }
        }
        
        public void HideMatchEnd() {
            if (matchEndPanel != null) matchEndPanel.SetActive(false);
        }
        
        public void ShowStore() {
            if (storePanel != null) storePanel.SetActive(true);
            UpdateWalletBalance();
        }
        
        public void HideStore() {
            if (storePanel != null) storePanel.SetActive(false);
        }
        
        public void UpdateWalletBalance(string balance) {
            if (walletBalanceText != null) walletBalanceText.text = balance;
        }
        
        void UpdateWalletBalance() {
            // Would call Solana wallet
            if (walletBalanceText != null) walletBalanceText.text = "Connecting...";
        }
        
        public void UpdateCombo(int comboNumber, int damage) {
            if (comboText != null) {
                comboText.text = comboNumber > 0 ? $"{comboNumber} HIT COMBO!" : "";
                comboText.gameObject.SetActive(comboNumber > 0);
            }
            if (damageText != null) {
                damageText.text = comboNumber > 0 ? $"{damage} DAMAGE" : "";
            }
        }
        
        public void SelectCharacter(int player, int characterId) {
            Debug.Log($"Player {player} selected character {characterId}");
            // Would apply character selection
        }
        
        void OnRestart() {
            HideMatchEnd();
            if (EngineBridge.Instance != null) {
                EngineBridge.Instance.ResetGame();
            }
        }
        
        void OnMainMenu() {
            HideMatchEnd();
            if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
        }
    }
}
