using UnityEngine;
using System.Collections;

namespace NeuralDraft.Unity {
    public class FighterController : MonoBehaviour {
        public int playerIndex;
        public CharacterDef characterDef;
        public SpriteRenderer spriteRenderer;
        public Animator animator;
        public Transform hitboxVisual;
        public Transform hurtboxVisual;
        private PlayerState _engineState;
        private int _facing = 1;
        private bool _isAttacking;
        private InputBits _inputBits;
        
        void Awake() {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
        }
        
        public void Initialize(int index, CharacterDef def) {
            playerIndex = index;
            characterDef = def;
            float startX = index == 0 ? -2f : 2f;
            transform.position = new Vector3(startX, 0, 0);
            _engineState = new PlayerState();
            _engineState.posX = (int)(startX * 1000);
            _engineState.posY = 0;
            _engineState.health = def.baseHealth;
            _engineState.grounded = 1;
            _facing = index == 0 ? 1 : -1;
            spriteRenderer.color = index == 0 ? Color.blue : Color.red;
        }
        
        void Update() {
            if (EngineBridge.Instance == null || !EngineBridge.Instance.runEngine) return;
            CollectInput();
            UpdateAnimation();
            UpdateFacing();
        }
        
        void CollectInput() {
            _inputBits = 0;
            if (playerIndex == 0) {
                if (Input.GetKey(KeyCode.A)) _inputBits |= InputBits.LEFT;
                if (Input.GetKey(KeyCode.D)) _inputBits |= InputBits.RIGHT;
                if (Input.GetKey(KeyCode.W)) _inputBits |= InputBits.UP;
                if (Input.GetKey(KeyCode.S)) _inputBits |= InputBits.DOWN;
                if (Input.GetKey(KeyCode.J)) _inputBits |= InputBits.ATTACK;
                if (Input.GetKey(KeyCode.K)) _inputBits |= InputBits.SPECIAL;
                if (Input.GetKey(KeyCode.L)) _inputBits |= InputBits.DEFEND;
                if (Input.GetKey(KeyCode.Space)) _inputBits |= InputBits.JUMP;
            } else {
                if (Input.GetKey(KeyCode.LeftArrow)) _inputBits |= InputBits.LEFT;
                if (Input.GetKey(KeyCode.RightArrow)) _inputBits |= InputBits.RIGHT;
                if (Input.GetKey(KeyCode.UpArrow)) _inputBits |= InputBits.UP;
                if (Input.GetKey(KeyCode.DownArrow)) _inputBits |= InputBits.DOWN;
                if (Input.GetKey(KeyCode.Keypad1)) _inputBits |= InputBits.ATTACK;
                if (Input.GetKey(KeyCode.Keypad2)) _inputBits |= InputBits.SPECIAL;
                if (Input.GetKey(KeyCode.Keypad3)) _inputBits |= InputBits.DEFEND;
                if (Input.GetKey(KeyCode.Keypad0)) _inputBits |= InputBits.JUMP;
            }
        }
        
        public ushort GetInputBits() { return (ushort)_inputBits; }
        public void SetState(PlayerState state) { _engineState = state; }
        public void SetFacing(int facing) { _facing = facing; }
        
        void UpdateFacing() {
            Vector3 scale = transform.localScale;
            scale.x = _facing > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        
        void UpdateAnimation() {
            if (animator == null) return;
            if (_engineState.hitstunRemaining > 0) {
                animator.Play("Hit");
            } else if ((_inputBits & InputBits.ATTACK) != 0 && !_isAttacking) {
                StartCoroutine(PlayAttack());
            } else if ((_inputBits & InputBits.LEFT) != 0 || (_inputBits & InputBits.RIGHT) != 0) {
                animator.Play("Walk");
            } else {
                animator.Play("Idle");
            }
        }
        
        IEnumerator PlayAttack() {
            _isAttacking = true;
            if (hitboxVisual != null) hitboxVisual.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            if (hitboxVisual != null) hitboxVisual.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.3f);
            _isAttacking = false;
        }
        
        public void UpdateVisuals() {
            if (hurtboxVisual != null) {
                float w = characterDef.hitboxWidth / 1000f;
                float h = characterDef.hitboxHeight / 1000f;
                hurtboxVisual.localScale = new Vector3(w, h, 1);
                hurtboxVisual.localPosition = new Vector3(0, h / 2, 0);
            }
        }
    }
}
