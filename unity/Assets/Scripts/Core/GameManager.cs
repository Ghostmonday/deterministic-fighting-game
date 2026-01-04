using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeuralDraft.Unity {
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }
        
        [Header("Game State")]
        public GameState gameState = GameState.MainMenu;
        public int selectedStage = 0;
        public int p1Character = 0;
        public int p2Character = 1;
        
        [Header("References")]
        public EngineBridge engineBridge;
        public UIManager ui;
        public StageController stage;
        
        // Game session data
        private int _p1Wins;
        private int _p2Wins;
        private int _currentRound;
        private int _roundsToWin = 2;
        
        void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
                return;
            }
        }
        
        void Start() {
            if (ui != null) ui.ShowStore();
        }
        
        public void StartMatch() {
            if (engineBridge == null) {
                Debug.LogError("[GameManager] EngineBridge not found!");
                return;
            }
            
            gameState = GameState.Playing;
            _currentRound = 1;
            _p1Wins = 0;
            _p2Wins = 0;
            
            if (ui != null) {
                ui.HideMatchEnd();
                ui.HideStore();
            }
            
            if (stage != null) {
                stage.LoadStage(selectedStage);
            }
            
            engineBridge.ResetGame();
            Debug.Log($"[GameManager] Match started: P1({p1Character}) vs P2({p2Character})");
        }
        
        public void OnMatchEnd(int winner) {
            gameState = GameState.MatchEnd;
            
            if (winner == 0) _p1Wins++;
            else if (winner == 1) _p2Wins++;
            
            Debug.Log($"[GameManager] Round {_currentRound} winner: P{winner + 1}");
            
            // Check for match winner
            if (_p1Wins >= _roundsToWin) {
                ShowMatchWinner(0);
            } else if (_p2Wins >= _roundsToWin) {
                ShowMatchWinner(1);
            } else {
                // Next round
                _currentRound++;
                if (ui != null) ui.ShowMatchEnd(winner);
            }
        }
        
        void ShowMatchWinner(int winner) {
            gameState = GameState.MatchEnd;
            if (ui != null) ui.ShowMatchEnd(winner);
            Debug.Log($"[GameManager] Match winner: P{winner + 1} ({(_winner == 0 ? _p1Wins : _p2Wins)}-{(_winner == 0 ? _p2Wins : _p1Wins)})");
        }
        
        public void SelectCharacter(int player, int characterId) {
            if (player == 0) p1Character = characterId;
            else p2Character = characterId;
            Debug.Log($"[GameManager] P{player + 1} selected character {characterId}");
        }
        
        public void SelectStage(int stageId) {
            selectedStage = stageId;
            Debug.Log($"[GameManager] Selected stage {stageId}");
        }
        
        public void ReturnToMenu() {
            gameState = GameState.MainMenu;
            if (ui != null) {
                ui.HideMatchEnd();
                ui.characterSelectPanel.SetActive(true);
            }
        }
        
        public void QuitGame() {
            Debug.Log("[GameManager] Quitting...");
            Application.Quit();
        }
        
        // Scene management
        public void LoadScene(string sceneName) {
            SceneManager.LoadScene(sceneName);
        }
        
        public void LoadMainScene() {
            SceneManager.LoadScene("GameScene");
        }
    }
    
    public enum GameState {
        MainMenu,
        CharacterSelect,
        Playing,
        Paused,
        MatchEnd,
        Store
    }
}
