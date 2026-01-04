using UnityEngine;

namespace NeuralDraft.Unity {
    public class StageController : MonoBehaviour {
        [Header("Stage Settings")]
        public int stageWidth = 1000;
        public int stageHeight = 500;
        public Transform floorTransform;
        public Transform leftWall;
        public Transform rightWall;
        public Camera mainCamera;
        
        [Header("Background")]
        public SpriteRenderer background;
        public Transform[] cloudObjects;
        
        private Vector3 _cameraOffset = new Vector3(0, 2, -10);
        private float _cameraSize = 6f;
        
        void Start() {
            if (mainCamera != null) {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = _cameraSize;
            }
        }
        
        void LateUpdate() {
            // Smooth camera follow between players
            if (EngineBridge.Instance != null) {
                var state = EngineBridge.Instance.GetCurrentState();
                if (state != null && state.players != null) {
                    float midX = (state.players[0].posX + state.players[1].posX) / (2f * Fx.SCALE);
                    Vector3 target = new Vector3(midX, _cameraOffset.y, _cameraOffset.z);
                    mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, target, Time.deltaTime * 5f);
                }
            }
        }
        
        public void LoadStage(int stageId) {
            // Would load stage by ID
            switch (stageId) {
                case 0: LoadDojo(); break;
                case 1: LoadArena(); break;
                case 2: LoadRooftop(); break;
            }
        }
        
        void LoadDojo() {
            if (background != null) background.color = new Color(0.8f, 0.7f, 0.5f);
            stageWidth = 1000;
            stageHeight = 500;
        }
        
        void LoadArena() {
            if (background != null) background.color = new Color(0.5f, 0.5f, 0.6f);
            stageWidth = 1200;
            stageHeight = 600;
        }
        
        void LoadRooftop() {
            if (background != null) background.color = new Color(0.3f, 0.3f, 0.5f);
            stageWidth = 800;
            stageHeight = 400;
        }
        
        public void ScreenShake(float intensity) {
            if (mainCamera != null) {
                StartCoroutine(Shake(intensity));
            }
        }
        
        System.Collections.IEnumerator Shake(float intensity) {
            Vector3 original = mainCamera.transform.position;
            float duration = 0.2f;
            float elapsed = 0;
            
            while (elapsed < duration) {
                float x = Random.Range(-intensity, intensity);
                float y = Random.Range(-intensity, intensity);
                mainCamera.transform.position = original + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            mainCamera.transform.position = original;
        }
    }
}
