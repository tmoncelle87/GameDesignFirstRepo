using UnityEngine;
using com.pyroduck.eggheadslite.Runtime.Scripts.Combat; 
using com.pyroduck.eggheadslite.Runtime.Scripts.Enums;
using com.pyroduck.eggheadslite.Runtime.Scripts.Events;
using com.pyroduck.eggheadslite.Runtime.Scripts.Utils;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Character
{
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(WeaponLoadoutController))]
    public class EggHeadController : BaseCharacterController, IDamageable
    {
        [Header("Movement Settings")] [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField] private float crouchMultiplier = 0.5f;
        [SerializeField] private float runSpeedMultiplier = 1.5f;

        private CharacterState _characterState;

        private HealthComponent _health;
        private WeaponLoadoutController _weaponLoadout;
        private HealthComponent health => _health != null ? _health : (_health = GetComponent<HealthComponent>());
        public CharacterVisualController CurrentVisualController => characterVisualController;
        public bool IsAlive => _characterState == CharacterState.Alive;
        public GameObject CharacterBasePrefab => characterBasePrefab;
 

        protected override void Awake()
        {
            base.Awake();
            _weaponLoadout = GetComponent<WeaponLoadoutController>();
            _characterState = CharacterState.Alive;
        }

        protected override void Start()
        {
            base.Start();

            if (characterBasePrefab != null)
                ApplyCharacterPrefab(characterBasePrefab);
        }

        public void SetCharacterPrefab(GameObject prefab)
        {
            ApplyCharacterPrefab(prefab);
        }

        private void ApplyCharacterPrefab(GameObject prefab)
        {
            if (prefab == null) return;

            characterBasePrefab = prefab;

            if (characterVisualController != null)
            {
                Destroy(characterVisualController.gameObject);
            }

            var obj = Instantiate(prefab, characterVisualParentTransform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            characterVisualController = obj.GetComponent<CharacterVisualController>()
                                        ?? obj.GetComponentInChildren<CharacterVisualController>(true);
            if (characterVisualController != null)
            {
                characterVisualController.SetAnimatedBody(animatedBodyParent);
                characterVisualController.SetRotatingBody(rotatingBodyParent);
            }

            _weaponLoadout?.BindCurrentWeaponOwner();
        }
        
        public GameObject GetRootOwner()
        {
            return transform.root != null ? transform.root.gameObject : gameObject;
        }
        
        protected override void OnEnable()
        {
            EventManager.Subscribe<TriggerJumpEvent>(OnTriggerJumpEvent);
            health.OnHealthUpdated += SyncCharacterStateFromHealth;
            SyncCharacterStateFromHealth(health.Health); 
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (_health != null)
                _health.OnHealthUpdated -= SyncCharacterStateFromHealth;
            EventManager.Unsubscribe<TriggerJumpEvent>(OnTriggerJumpEvent); 
            base.OnDisable();
        }

        private void SyncCharacterStateFromHealth(float currentHealth)
        {
            bool wasAlive = _characterState == CharacterState.Alive;
            bool wasDead  = _characterState == CharacterState.Dead;
            _characterState = currentHealth > 0f ? CharacterState.Alive : CharacterState.Dead;

            if (wasAlive && _characterState == CharacterState.Dead)
            {
                EventManager.Publish(new SetMovementEvent
                {
                    Direction   = 0f,
                    IsWalking   = false,
                    IsCrouching = false,
                    IsRunning   = false,
                });
            }
            else if (wasDead && _characterState == CharacterState.Alive)
            {
                EventManager.Publish(new CharacterRevivedEvent { Source = gameObject });
            }
        }

        // ── Update ────────────────────────────────────────────────────────────

        protected override void Update()
        {
            if (_characterState != CharacterState.Alive)
                return;

            base.Update();
            _weaponLoadout?.TickInput();
        }

        private void FixedUpdate()
        {
            if (eggRigidBody2D == null) return;

            Vector2 v = eggRigidBody2D.GetLinearVelocity();

            if (_characterState != CharacterState.Alive)
            {
                eggRigidBody2D.SetLinearVelocity(new Vector2(0f, v.y));
                return;
            }

            eggRigidBody2D.SetLinearVelocity(new Vector2(
                (IsRunning ? runSpeedMultiplier : 1f) *
                (IsCrouching ? crouchMultiplier : 1f) *
                Move.x * moveSpeed,
                v.y));
        }

        // ── Jump ──────────────────────────────────────────────────────────────

        private void OnTriggerJumpEvent(TriggerJumpEvent obj)
        {
            if (_characterState != CharacterState.Alive)
                return;

            if (eggRigidBody2D != null)
            {
                eggRigidBody2D.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                isJumping = true;
                isGrounded = false;
                jumpGroundCooldown = JumpGroundCooldownDuration;
            }
        }

        // ── Damage ────────────────────────────────────────────────────────────

        public void TakeDamage(float amount, GameObject source, Vector2 hitPoint)
        {
            if (ApplyDamage(amount, source, hitPoint))
                animationsController?.OnTakeDamageButtonDown();
        }

        /// <summary>
        /// Called from <see cref="EggHeadPhysicsContactRelay"/> on the collider object; hazard
        /// callbacks do not reach this script when the capsule lives on a child GameObject.
        /// </summary>
        public void TryApplyHazardDamage(Collider2D other)
        { 
            if (other == null) return;

            var hazard = other.GetComponent<SpikeHazard>()
                         ?? other.GetComponentInParent<SpikeHazard>();
            if (hazard == null) return;

            hazard.TryDamage(GetOwnDamageCollider(), other.ClosestPoint(transform.position));
        }

        private Collider2D GetOwnDamageCollider()
        {
            return GetComponent<Collider2D>() ?? GetComponentInChildren<Collider2D>();
        }

        private bool ApplyDamage(float amount, GameObject source, Vector2 hitPoint = default)
        { 
            if (amount <= 0f) return false;
            return health.ApplyDamage(amount, source, hitPoint);
        }
    }
}
