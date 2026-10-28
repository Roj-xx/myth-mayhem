using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using MythMayhem.Gameplay;

namespace MythMayhem.Cinematic
{
    public enum CinematicShotType
    {
        Orbit,
        Aerial,
        Establishing,
        Reveal,
        CloseUp,
        Custom
    }

    [Serializable]
    public sealed class CinematicShot
    {
        public string shotName = "New Shot";
        public bool enabled = true;
        public CinematicShotType type;
        [Min(0.1f)] public float duration = 4f;
        [Min(0f)] public float transitionDuration = 1f;
        public bool hardCut;
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public Transform target;
        public Vector3 targetOffset = Vector3.up;
        [Range(1f, 179f)] public float fieldOfView = 50f;

        [Header("Orbit")]
        [Min(0.1f)] public float orbitRadius = 7f;
        public float orbitHeight = 3f;
        public float orbitStartAngle;
        public float orbitEndAngle = 360f;
        public bool orbitClockwise = true;
        public bool changeOrbitHeight;
        public float orbitEndHeight = 5f;

        [Header("Aerial")]
        [Min(0.1f)] public float aerialStartHeight = 14f;
        [Min(0.1f)] public float aerialEndHeight = 9f;
        [Range(5f, 89f)] public float aerialViewAngle = 60f;

        [Header("Establishing / Reveal")]
        public Vector3 startPosition;
        public Vector3 startEulerAngles;
        public Vector3 endPosition;
        public Vector3 endEulerAngles;
        public bool moveToEndPose = true;

        [Header("Close-up")]
        [Min(0.1f)] public float closeUpDistance = 3f;
        public float closeUpHeight = 1.5f;
        public float closeUpOrbitDegrees = 0f;

        [Header("Custom")]
        public Vector3 customPosition;
        public Vector3 customEulerAngles;

        public CinematicShot Clone()
        {
            return new CinematicShot
            {
                shotName = shotName + " Copy",
                enabled = enabled,
                type = type,
                duration = duration,
                transitionDuration = transitionDuration,
                hardCut = hardCut,
                easing = easing != null ? new AnimationCurve(easing.keys) : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                target = target,
                targetOffset = targetOffset,
                fieldOfView = fieldOfView,
                orbitRadius = orbitRadius,
                orbitHeight = orbitHeight,
                orbitStartAngle = orbitStartAngle,
                orbitEndAngle = orbitEndAngle,
                orbitClockwise = orbitClockwise,
                changeOrbitHeight = changeOrbitHeight,
                orbitEndHeight = orbitEndHeight,
                aerialStartHeight = aerialStartHeight,
                aerialEndHeight = aerialEndHeight,
                aerialViewAngle = aerialViewAngle,
                startPosition = startPosition,
                startEulerAngles = startEulerAngles,
                endPosition = endPosition,
                endEulerAngles = endEulerAngles,
                moveToEndPose = moveToEndPose,
                closeUpDistance = closeUpDistance,
                closeUpHeight = closeUpHeight,
                closeUpOrbitDegrees = closeUpOrbitDegrees,
                customPosition = customPosition,
                customEulerAngles = customEulerAngles,
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class CinematicCameraDirector : MonoBehaviour
    {
        private static readonly RaycastHit[] CameraObstructionHits = new RaycastHit[16];

        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Canvas gameplayCanvas;
        [SerializeField] private bool playSequenceOnSceneLoad = true;
        [SerializeField] private GameObject skipHintRoot;
        [SerializeField] private List<CinematicShot> shots = new List<CinematicShot>();

        private readonly List<CinemachineCamera> shotCameras = new List<CinemachineCamera>();
        private Coroutine introRoutine;
        private CinemachineBrain brain;
        private CinemachineBrain createdBrain;
        private CinemachineCamera restoreCamera;
        private CinemachineCamera previousCamera;
        private Coroutine playback;
        private bool skipRequested;
        private bool originalCameraEnabled;
        private bool originalBrainEnabled;
        private bool originalBrainIgnoreTimeScale;
        private bool originalBrainCaptured;
        private bool cameraStateCaptured;
        private CinemachineBlendDefinition originalBlend;
        private CanvasGroup gameplayCanvasGroup;
        private SummonController summonController;
        private bool originalCanvasGroupCaptured;
        private bool createdCanvasGroup;
        private float originalCanvasAlpha;
        private bool originalCanvasInteractable;
        private bool originalCanvasBlocksRaycasts;
        private bool originalSummonControllerEnabled;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float originalFieldOfView;
        private int nextPriority;

        public IReadOnlyList<CinematicShot> Shots => shots;
        public bool IsPlaying => playback != null;
        public bool PlaySequenceOnSceneLoad
        {
            get => playSequenceOnSceneLoad;
            set => playSequenceOnSceneLoad = value;
        }

        public void AddShot(CinematicShot shot)
        {
            if (shot == null)
                throw new ArgumentNullException(nameof(shot));

            shots.Add(shot);
        }

        public void RemoveShotAt(int index)
        {
            if (shots != null && index >= 0 && index < shots.Count)
                shots.RemoveAt(index);
        }

        private void Awake()
        {
            if (gameplayCamera == null)
                gameplayCamera = GetComponent<Camera>();
            if (gameplayCamera == null)
                gameplayCamera = Camera.main;

            if (gameplayCanvas == null)
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    if (canvas != null && canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                        canvas.gameObject.scene == gameObject.scene)
                    {
                        gameplayCanvas = canvas;
                        break;
                    }
                }
            }
        }

        private void Start()
        {
            skipRequested = false;
            if (playSequenceOnSceneLoad && !NetworkSession.IsNetworked && shots != null && shots.Count > 0)
                introRoutine = StartCoroutine(PlayIntroOnSceneLoad());
        }

        private void OnDisable()
        {
            StopPlayback();
        }

        private void OnDestroy()
        {
            StopPlayback();
        }

        public void PreviewShot(int index)
        {
            StartPlayback(index, true);
        }

        public void PlaySequence()
        {
            StartPlayback(0, false);
        }

        public void StopPlayback()
        {
            skipRequested = true;

            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
                introRoutine = null;
            }

            if (playback != null)
            {
                StopCoroutine(playback);
                playback = null;
            }

            SetSkipHintVisible(false);
            RestoreCameraState();
            RestoreGameplayInput();
        }

        private void StartPlayback(int startIndex, bool previewOnly)
        {
            StopPlayback();

            if (gameplayCamera == null)
            {
                Debug.LogError("[Cinematic Director] Assign a gameplay camera before previewing shots.", this);
                return;
            }

            if (shots == null || shots.Count == 0 || startIndex < 0 || startIndex >= shots.Count)
            {
                Debug.LogWarning("[Cinematic Director] Add a shot before starting playback.", this);
                return;
            }

            CaptureCameraState();
            PrepareCinemachine();
            playback = StartCoroutine(PlaybackRoutine(startIndex, previewOnly));
        }

        private IEnumerator PlayIntroOnSceneLoad()
        {
            skipRequested = false;
            SetGameplayInputLocked(true);
            yield return null;
            introRoutine = null;

            if (shots == null || shots.Count == 0)
            {
                RestoreGameplayInput();
                yield break;
            }

            if (gameplayCamera == null)
            {
                Debug.LogError("[Cinematic Director] Cannot play the scene intro because no gameplay camera is assigned.", this);
                RestoreGameplayInput();
                yield break;
            }

            CaptureCameraState();
            PrepareCinemachine();
            SetSkipHintVisible(true);
            playback = StartCoroutine(PlaybackRoutine(0, false));
        }

        private void SetSkipHintVisible(bool visible)
        {
            if (skipHintRoot != null && skipHintRoot.activeSelf != visible)
                skipHintRoot.SetActive(visible);
        }

        private void SetGameplayInputLocked(bool locked)
        {
            if (locked)
            {
                if (gameplayCanvas != null)
                {
                    gameplayCanvasGroup = gameplayCanvas.GetComponent<CanvasGroup>();
                    if (gameplayCanvasGroup == null)
                    {
                        gameplayCanvasGroup = gameplayCanvas.gameObject.AddComponent<CanvasGroup>();
                        createdCanvasGroup = true;
                    }

                    originalCanvasAlpha = gameplayCanvasGroup.alpha;
                    originalCanvasInteractable = gameplayCanvasGroup.interactable;
                    originalCanvasBlocksRaycasts = gameplayCanvasGroup.blocksRaycasts;
                    originalCanvasGroupCaptured = true;
                    gameplayCanvasGroup.alpha = 0f;
                    gameplayCanvasGroup.interactable = false;
                    gameplayCanvasGroup.blocksRaycasts = false;
                }

                summonController = FindFirstObjectByType<SummonController>();
                if (summonController != null)
                {
                    originalSummonControllerEnabled = summonController.enabled;
                    summonController.enabled = false;
                }
                return;
            }

            RestoreGameplayInput();
        }

        private void RestoreGameplayInput()
        {
            if (originalCanvasGroupCaptured && gameplayCanvasGroup != null)
            {
                gameplayCanvasGroup.alpha = originalCanvasAlpha;
                gameplayCanvasGroup.interactable = originalCanvasInteractable;
                gameplayCanvasGroup.blocksRaycasts = originalCanvasBlocksRaycasts;
            }

            if (createdCanvasGroup && gameplayCanvasGroup != null)
                Destroy(gameplayCanvasGroup);

            if (summonController != null)
                summonController.enabled = originalSummonControllerEnabled;

            gameplayCanvasGroup = null;
            summonController = null;
            originalCanvasGroupCaptured = false;
            createdCanvasGroup = false;
        }

        private void CaptureCameraState()
        {
            originalCameraEnabled = gameplayCamera.enabled;
            originalPosition = gameplayCamera.transform.position;
            originalRotation = gameplayCamera.transform.rotation;
            originalFieldOfView = gameplayCamera.fieldOfView;
            cameraStateCaptured = true;

            brain = gameplayCamera.GetComponent<CinemachineBrain>();
            originalBrainCaptured = brain != null;
            if (brain != null)
            {
                originalBrainEnabled = brain.enabled;
                originalBrainIgnoreTimeScale = brain.IgnoreTimeScale;
                originalBlend = brain.DefaultBlend;
            }
        }

        private void PrepareCinemachine()
        {
            if (brain == null)
            {
                createdBrain = gameplayCamera.gameObject.AddComponent<CinemachineBrain>();
                brain = createdBrain;
            }

            brain.enabled = true;
            brain.IgnoreTimeScale = true;
            gameplayCamera.enabled = true;
            restoreCamera = CreateVirtualCamera("Cinematic Restore Camera", originalPosition, originalRotation, originalFieldOfView);
            restoreCamera.Priority = ++nextPriority;
        }

        private IEnumerator PlaybackRoutine(int startIndex, bool previewOnly)
        {
            yield return null;

            int endIndex = previewOnly ? startIndex + 1 : shots.Count;
            float finalBlendDuration = 0f;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (skipRequested)
                    break;

                CinematicShot shot = shots[i];
                if (shot == null || !shot.enabled)
                    continue;

                if (!TryGetShotPose(shot, 0f, out Vector3 startPosition, out Quaternion startRotation))
                {
                    Debug.LogWarning("[Cinematic Director] Skipping '" + shot.shotName + "': its target is missing.", this);
                    continue;
                }

                CinemachineCamera active = CreateVirtualCamera(
                    "Cinematic Shot - " + shot.shotName,
                    startPosition,
                    startRotation,
                    shot.fieldOfView);

                SetBlend(shot);
                active.Priority = ++nextPriority;

                float elapsed = 0f;
                float duration = Mathf.Max(0.1f, shot.duration);
                while (elapsed < duration && active != null)
                {
                    if (skipRequested)
                        break;

                    bool skipped = false;
                    if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                        skipped = true;
                    if (!skipped && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                        skipped = true;
                    if (!skipped && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                        skipped = true;
                    if (skipped)
                    {
                        skipRequested = true;
                        break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    float normalized = Mathf.Clamp01(elapsed / duration);
                    float eased = EvaluateEasing(shot.easing, normalized);
                    if (TryGetShotPose(shot, eased, out Vector3 position, out Quaternion rotation))
                    {
                        active.transform.SetPositionAndRotation(position, rotation);
                        SetFieldOfView(active, shot.fieldOfView);
                    }

                    yield return null;
                }

                if (previousCamera != null)
                {
                    previousCamera.Priority = 0;
                    shotCameras.Remove(previousCamera);
                    Destroy(previousCamera.gameObject);
                }

                previousCamera = active;
                finalBlendDuration = GetTransitionDuration(shot);
            }

            if (restoreCamera != null && !skipRequested)
            {
                SetBlend(finalBlendDuration);
                restoreCamera.Priority = ++nextPriority;
                if (previousCamera != null)
                    previousCamera.Priority = 0;
                if (finalBlendDuration > 0f)
                    yield return new WaitForSecondsRealtime(finalBlendDuration);
            }

            playback = null;
            StopPlayback();
        }

        private CinemachineCamera CreateVirtualCamera(string cameraName, Vector3 position, Quaternion rotation, float fieldOfView)
        {
            GameObject cameraObject = new GameObject(cameraName);
            CinemachineCamera virtualCamera = cameraObject.AddComponent<CinemachineCamera>();
            virtualCamera.transform.SetPositionAndRotation(position, rotation);
            SetFieldOfView(virtualCamera, fieldOfView);
            shotCameras.Add(virtualCamera);
            return virtualCamera;
        }

        private bool TryGetShotPose(CinematicShot shot, float progress, out Vector3 position, out Quaternion rotation)
        {
            progress = Mathf.Clamp01(progress);
            position = shot.customPosition;
            rotation = Quaternion.Euler(shot.customEulerAngles);

            switch (shot.type)
            {
                case CinematicShotType.Orbit:
                {
                    if (shot.target == null)
                        return false;

                    float direction = shot.orbitClockwise ? 1f : -1f;
                    float angle = Mathf.Lerp(shot.orbitStartAngle, shot.orbitEndAngle, progress) * direction;
                    float height = shot.changeOrbitHeight
                        ? Mathf.Lerp(shot.orbitHeight, shot.orbitEndHeight, progress)
                        : shot.orbitHeight;
                    Vector3 center = shot.target.position + shot.targetOffset;
                    Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.back * Mathf.Max(0.1f, shot.orbitRadius);
                    position = center + offset + Vector3.up * height;
                    KeepCameraClearOfGeometry(shot.target, center, ref position);
                    rotation = LookAt(position, center);
                    return true;
                }
                case CinematicShotType.Aerial:
                {
                    if (shot.target == null)
                        return false;

                    Vector3 center = shot.target.position + shot.targetOffset;
                    float height = Mathf.Lerp(shot.aerialStartHeight, shot.aerialEndHeight, progress);
                    float viewAngle = Mathf.Clamp(shot.aerialViewAngle, 5f, 89f) * Mathf.Deg2Rad;
                    float horizontalDistance = height / Mathf.Tan(viewAngle);
                    position = center + new Vector3(0f, height, -horizontalDistance);
                    KeepCameraClearOfGeometry(shot.target, center, ref position);
                    rotation = LookAt(position, center);
                    return true;
                }
                case CinematicShotType.Establishing:
                    if (!shot.moveToEndPose)
                        progress = 0f;
                    goto case CinematicShotType.Reveal;
                case CinematicShotType.Reveal:
                    position = Vector3.Lerp(shot.startPosition, shot.endPosition, progress);
                    rotation = Quaternion.Slerp(
                        Quaternion.Euler(shot.startEulerAngles),
                        Quaternion.Euler(shot.endEulerAngles),
                        progress);
                    if (shot.target != null)
                        rotation = LookAt(position, shot.target.position + shot.targetOffset);
                    return true;
                case CinematicShotType.CloseUp:
                {
                    if (shot.target == null)
                        return false;

                    Vector3 center = shot.target.position + shot.targetOffset;
                    float angle = shot.closeUpOrbitDegrees * progress;
                    Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.back * Mathf.Max(0.1f, shot.closeUpDistance);
                    position = center + offset + Vector3.up * shot.closeUpHeight;
                    KeepCameraClearOfGeometry(shot.target, center, ref position);
                    rotation = LookAt(position, center);
                    return true;
                }
                case CinematicShotType.Custom:
                    position = shot.customPosition;
                    rotation = Quaternion.Euler(shot.customEulerAngles);
                    if (shot.target != null)
                        rotation = LookAt(position, shot.target.position + shot.targetOffset);
                    return true;
                default:
                    return false;
            }
        }

        private static Quaternion LookAt(Vector3 position, Vector3 target)
        {
            Vector3 direction = target - position;
            return direction.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
        }

        private static void KeepCameraClearOfGeometry(Transform target, Vector3 center, ref Vector3 position)
        {
            Vector3 offset = position - center;
            float distance = offset.magnitude;
            if (distance <= 0.1f)
                return;

            Vector3 direction = offset / distance;
            int hitCount = Physics.SphereCastNonAlloc(
                center,
                0.2f,
                direction,
                CameraObstructionHits,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = distance;
            for (int i = 0; i < hitCount; i++)
            {
                Transform hitTransform = CameraObstructionHits[i].transform;
                if (hitTransform == target || hitTransform.IsChildOf(target))
                    continue;

                nearestDistance = Mathf.Min(nearestDistance, CameraObstructionHits[i].distance);
            }

            if (nearestDistance < distance)
                position = center + direction * Mathf.Max(0.1f, nearestDistance - 0.15f);
        }

        private static float EvaluateEasing(AnimationCurve curve, float progress)
        {
            return curve != null && curve.length > 0
                ? Mathf.Clamp01(curve.Evaluate(progress))
                : progress;
        }

        private static float GetTransitionDuration(CinematicShot shot)
        {
            return shot == null || shot.hardCut ? 0f : Mathf.Max(0f, shot.transitionDuration);
        }

        private void SetBlend(CinematicShot shot)
        {
            SetBlend(GetTransitionDuration(shot));
        }

        private void SetBlend(float duration)
        {
            if (brain == null)
                return;

            CinemachineBlendDefinition.Styles style = duration <= 0f
                ? CinemachineBlendDefinition.Styles.Cut
                : CinemachineBlendDefinition.Styles.EaseInOut;
            brain.DefaultBlend = new CinemachineBlendDefinition(style, duration);
        }

        private static void SetFieldOfView(CinemachineCamera virtualCamera, float fieldOfView)
        {
            LensSettings lens = virtualCamera.Lens;
            lens.FieldOfView = Mathf.Clamp(fieldOfView, 1f, 179f);
            virtualCamera.Lens = lens;
        }

        private void RestoreCameraState()
        {
            for (int i = shotCameras.Count - 1; i >= 0; i--)
            {
                if (shotCameras[i] != null)
                    Destroy(shotCameras[i].gameObject);
            }
            shotCameras.Clear();
            restoreCamera = null;
            previousCamera = null;
            nextPriority = 0;

            if (!cameraStateCaptured)
                return;

            if (gameplayCamera == null)
            {
                brain = null;
                createdBrain = null;
                originalBrainCaptured = false;
                cameraStateCaptured = false;
                return;
            }

            if (createdBrain != null)
            {
                createdBrain.enabled = false;
                Destroy(createdBrain);
            }
            else if (originalBrainCaptured && brain != null)
            {
                brain.DefaultBlend = originalBlend;
                brain.IgnoreTimeScale = originalBrainIgnoreTimeScale;
                brain.enabled = originalBrainEnabled;
            }

            gameplayCamera.transform.SetPositionAndRotation(originalPosition, originalRotation);
            gameplayCamera.fieldOfView = originalFieldOfView;
            gameplayCamera.enabled = originalCameraEnabled;

            brain = null;
            createdBrain = null;
            originalBrainCaptured = false;
            cameraStateCaptured = false;
        }
    }
}
