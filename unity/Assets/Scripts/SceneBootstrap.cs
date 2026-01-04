using UnityEngine;

namespace NeuralDraft.Unity {
    /// <summary>
    /// Auto-creates the fighting game scene setup
    /// Attach this to an empty GameObject in the scene
    /// </summary>
    public class SceneBootstrap : MonoBehaviour {
        void Awake() {
            SetupScene();
        }
        
        void SetupScene() {
            // Create main camera
            var mainCamera = GameObject.Find("Main Camera");
            if (mainCamera == null) {
                mainCamera = new GameObject("Main Camera");
                mainCamera.AddComponent<Camera>();
                mainCamera.tag = "MainCamera";
            }
            var cam = mainCamera.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
            cam.transform.position = new Vector3(0, 2, -10);
            
            // Create lighting
            var lightGO = GameObject.Find("Directional Light");
            if (lightGO == null) {
                lightGO = new GameObject("Directional Light");
                lightGO.AddComponent<Light>();
            }
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            
            // Create floor
            var floor = GameObject.Find("Floor");
            if (floor == null) {
                floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Floor";
            }
            floor.transform.position = new Vector3(0, -1.5f, 0);
            floor.transform.localScale = new Vector3(20, 1, 5);
            var floorRenderer = floor.GetComponent<Renderer>();
            floorRenderer.material.color = new Color(0.3f, 0.3f, 0.35f);
            
            // Create background
            var bg = GameObject.Find("Background");
            if (bg == null) {
                bg = new GameObject("Background");
            }
            bg.transform.position = new Vector3(0, 5, 5);
            var bgSprite = bg.AddComponent<SpriteRenderer>();
            bgSprite.color = new Color(0.15f, 0.15f, 0.25f);
            bgSprite.size = new Vector2(25, 15);
            
            // Create game manager
            var gameManager = GameObject.Find("GameManager");
            if (gameManager == null) {
                gameManager = new GameObject("GameManager");
            }
            if (gameManager.GetComponent<GameManager>() == null) {
                gameManager.AddComponent<GameManager>();
            }
            
            // Create engine bridge
            var engineBridge = GameObject.Find("EngineBridge");
            if (engineBridge == null) {
                engineBridge = new GameObject("EngineBridge");
            }
            if (engineBridge.GetComponent<EngineBridge>() == null) {
                engineBridge.AddComponent<EngineBridge>();
            }
            
            // Create UI
            var uiGO = GameObject.Find("UIManager");
            if (uiGO == null) {
                uiGO = new GameObject("UIManager");
            }
            if (uiGO.GetComponent<UIManager>() == null) {
                uiGO.AddComponent<UIManager>();
            }
            
            // Create stage controller
            var stageGO = GameObject.Find("StageController");
            if (stageGO == null) {
                stageGO = new GameObject("StageController");
            }
            var stageCtrl = stageGO.GetComponent<StageController>();
            if (stageCtrl == null) {
                stageCtrl = stageGO.AddComponent<StageController>();
            }
            stageCtrl.mainCamera = cam;
            stageCtrl.background = bgSprite;
            
            // Create player 1
            CreatePlayer(0, new Vector3(-3, 0, 0), Color.blue);
            
            // Create player 2
            CreatePlayer(1, new Vector3(3, 0, 0), Color.red);
            
            Debug.Log("[SceneBootstrap] Scene setup complete");
        }
        
        void CreatePlayer(int index, Vector3 position, Color color) {
            var playerGO = GameObject.Find($"Player{index + 1}");
            if (playerGO == null) {
                playerGO = new GameObject($"Player{index + 1}");
            }
            
            playerGO.transform.position = position;
            
            // Add sprite renderer
            var sprite = playerGO.GetComponent<SpriteRenderer>();
            if (sprite == null) {
                sprite = playerGO.AddComponent<SpriteRenderer>();
            }
            sprite.color = color;
            sprite.size = new Vector2(1f, 2f);
            
            // Add fighter controller
            var fighter = playerGO.GetComponent<FighterController>();
            if (fighter == null) {
                fighter = playerGO.AddComponent<FighterController>();
            }
            fighter.playerIndex = index;
            fighter.spriteRenderer = sprite;
            
            // Create hurtbox visual
            var hurtbox = new GameObject("Hurtbox");
            hurtbox.transform.SetParent(playerGO.transform);
            var hurtboxSprite = hurtbox.AddComponent<SpriteRenderer>();
            hurtboxSprite.color = new Color(0, 1, 0, 0.3f);
            fighter.hurtboxVisual = hurtbox.transform;
            
            // Create hitbox visual
            var hitbox = new GameObject("Hitbox");
            hitbox.transform.SetParent(playerGO.transform);
            var hitboxSprite = hitbox.AddComponent<SpriteRenderer>();
            hitboxSprite.color = new Color(1, 0, 0, 0.3f);
            hitbox.SetActive(false);
            fighter.hitboxVisual = hitbox.transform;
            
            // Link to engine bridge
            var bridge = EngineBridge.Instance;
            if (bridge != null) {
                if (index == 0) bridge.player1 = fighter;
                else bridge.player2 = fighter;
            }
        }
    }
}
