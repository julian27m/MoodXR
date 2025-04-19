using Oculus.Interaction.HandGrab;
using System.Collections;
using UnityEngine;
using static Oculus.Interaction.AudioPhysics;

namespace Oculus.Interaction.Samples
{
    public class SquashRacketPhysics : MonoBehaviour, ITransformer
    {
        [SerializeField]
        private HandGrabInteractable _leftHandGrabInteractable;
        [SerializeField]
        private HandGrabInteractable _rightHandGrabInteractable;

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private AnimationCurve _collisionStrength;

        [SerializeField]
        private float _maxHapticStrength = 0.7f;

        [SerializeField]
        private float _hapticDuration = 0.1f;

        // Factores para ajustar la física
        [SerializeField]
        private float _velocityFactor = 1.0f;

        [SerializeField]
        private float _minCollisionMagnitude = 0.1f;

        private const float _timeBetweenCollisions = 0.05f;
        private WaitForSeconds _hapticsWait;

        private CollisionEvents _collisionEvents;
        private float _timeAtLastCollision = 0f;

        protected bool _started = false;

        private OVRInput.Controller _activeController;
        private IGrabbable _grabbable;
        private Pose _grabDeltaInLocalSpace;


        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_rigidbody, nameof(_rigidbody));

            // Asegúrate de que el rigidbody tenga las propiedades adecuadas para una raqueta
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            _collisionEvents = _rigidbody.gameObject.AddComponent<CollisionEvents>();
            _hapticsWait = new WaitForSeconds(_hapticDuration);

            this.EndStart(ref _started);
        }


        protected virtual void OnEnable()
        {
            if (_started)
            {
                _collisionEvents.WhenCollisionEnter += HandleCollisionEnter;
                _leftHandGrabInteractable.WhenStateChanged += HandleLeftHandGrabInteractableStateChanged;
                _rightHandGrabInteractable.WhenStateChanged += HandleRightHandGrabInteractableStateChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _collisionEvents.WhenCollisionEnter -= HandleCollisionEnter;
                _leftHandGrabInteractable.WhenStateChanged -= HandleLeftHandGrabInteractableStateChanged;
                _rightHandGrabInteractable.WhenStateChanged -= HandleRightHandGrabInteractableStateChanged;
            }
        }

        private void HandleLeftHandGrabInteractableStateChanged(InteractableStateChangeArgs stateChange)
        {
            if (stateChange.NewState == InteractableState.Select)
            {
                _activeController |= OVRInput.Controller.LTouch;
            }
            else if (stateChange.PreviousState == InteractableState.Select)
            {
                _activeController &= ~OVRInput.Controller.LTouch;
            }
        }

        private void HandleRightHandGrabInteractableStateChanged(InteractableStateChangeArgs stateChange)
        {
            if (stateChange.NewState == InteractableState.Select)
            {
                _activeController |= OVRInput.Controller.RTouch;
            }
            else if (stateChange.PreviousState == InteractableState.Select)
            {
                _activeController &= ~OVRInput.Controller.RTouch;
            }
        }

        private void HandleCollisionEnter(Collision collision)
        {
            // Verificar si estamos colisionando con la pelota de squash
            if (collision.gameObject.CompareTag("SquashBall") || collision.gameObject.CompareTag("Ball"))
            {
                ProcessBallCollision(collision);
            }
            else
            {
                // Otras colisiones (pared, suelo, etc.)
                TryPlayCollisionAudio(collision);
            }
        }

        private void ProcessBallCollision(Collision collision)
        {
            float collisionMagnitude = collision.relativeVelocity.sqrMagnitude;

            if (collisionMagnitude < _minCollisionMagnitude)
            {
                return;
            }

            // Aplicar fuerza a la pelota basada en la velocidad de la raqueta
            Rigidbody ballRigidbody = collision.rigidbody;
            if (ballRigidbody != null)
            {
                Vector3 direction = collision.contacts[0].point - transform.position;
                direction.Normalize();

                // Calcular la velocidad del impacto considerando tanto la dirección como la fuerza
                Vector3 racketVelocity = _rigidbody.velocity;
                float impactForce = racketVelocity.magnitude * _velocityFactor;

                // Aplicar la fuerza a la pelota
                ballRigidbody.AddForce(direction * impactForce, ForceMode.Impulse);
            }

            // Reproducir haptics para el impacto
            TryPlayCollisionAudio(collision);
        }

        private void TryPlayCollisionAudio(Collision collision)
        {
            float collisionMagnitude = collision.relativeVelocity.sqrMagnitude;

            if (collision.collider.gameObject == null)
            {
                return;
            }

            float deltaTime = Time.time - _timeAtLastCollision;
            if (_timeBetweenCollisions > deltaTime)
            {
                return;
            }

            _timeAtLastCollision = Time.time;

            PlayCollisionHaptics(collisionMagnitude);
        }

        private void PlayCollisionHaptics(float strength)
        {
            float pitch = _collisionStrength.Evaluate(strength);
            // Limitar la intensidad máxima de los haptics para la raqueta de squash
            pitch = Mathf.Min(pitch, _maxHapticStrength);

            StartCoroutine(HapticsRoutine(pitch, _activeController));
        }

        private IEnumerator HapticsRoutine(float pitch, OVRInput.Controller controller)
        {
            OVRInput.SetControllerVibration(pitch * 0.5f, pitch * 0.2f, controller);
            yield return _hapticsWait;
            OVRInput.SetControllerVibration(0, 0, controller);
        }

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void BeginTransform()
        {
            Pose grabPoint = _grabbable.GrabPoints[0];
            Transform targetTransform = _rigidbody.transform;
            _grabDeltaInLocalSpace = new Pose(targetTransform.InverseTransformVector(grabPoint.position - targetTransform.position),
                                            Quaternion.Inverse(grabPoint.rotation) * targetTransform.rotation);
        }

        public void UpdateTransform()
        {
            Pose grabPoint = _grabbable.GrabPoints[0];
            _rigidbody.MoveRotation(grabPoint.rotation * _grabDeltaInLocalSpace.rotation);
            _rigidbody.MovePosition(grabPoint.position - _rigidbody.transform.TransformVector(_grabDeltaInLocalSpace.position));
        }

        public void EndTransform() { }
    }
}