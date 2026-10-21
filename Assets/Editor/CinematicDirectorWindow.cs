using MythMayhem.Cinematic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MythMayhem.EditorTools
{
    public sealed class CinematicDirectorWindow : EditorWindow
    {
        private CinematicCameraDirector director;
        private Vector2 scrollPosition;
        private int selectedShot = -1;

        [MenuItem("Myth Mayhem/Cinematic Director")]
        private static void Open()
        {
            CinematicDirectorWindow window = GetWindow<CinematicDirectorWindow>();
            window.titleContent = new GUIContent("Cinematic Director");
            window.minSize = new Vector2(360f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshDirector();
            EditorApplication.hierarchyChanged += RefreshDirector;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= RefreshDirector;
        }

        private void OnSelectionChange()
        {
            RefreshDirector();
            Repaint();
        }

        private void OnGUI()
        {
            if (director == null)
                RefreshDirector();

            EditorGUILayout.LabelField("Cinematic Director", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Saved shots live on the director component in this scene. Save the scene to keep changes between Editor sessions.",
                MessageType.Info);

            if (director == null)
            {
                EditorGUILayout.HelpBox("Add a director to the PlayWithAI Main Camera to get started.", MessageType.Warning);
                if (GUILayout.Button("Add Director to PlayWithAI Main Camera", GUILayout.Height(32f)))
                    AddDirectorToMainCamera();
                return;
            }

            DrawPlaybackControls();
            EditorGUILayout.Space(6f);
            DrawShots();
        }

        private void DrawPlaybackControls()
        {
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play Sequence"))
                    director.PlaySequence();
                if (GUILayout.Button("Stop / Restore Camera"))
                    director.StopPlayback();
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Preview Selected Shot"))
                {
                    if (selectedShot >= 0 && selectedShot < director.Shots.Count)
                        director.PreviewShot(selectedShot);
                    else
                        Debug.LogWarning("[Cinematic Director] Select a shot to preview.", director);
                }
            }

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to preview shots or run a sequence.", MessageType.None);
        }

        private void DrawShots()
        {
            EditorGUILayout.LabelField("Gameplay Camera", EditorStyles.boldLabel);
            SerializedObject serializedDirector = new SerializedObject(director);
            serializedDirector.Update();
            EditorGUILayout.PropertyField(serializedDirector.FindProperty("gameplayCamera"));
            EditorGUILayout.PropertyField(serializedDirector.FindProperty("gameplayCanvas"));
            EditorGUILayout.PropertyField(serializedDirector.FindProperty("playSequenceOnSceneLoad"),
                new GUIContent("Play Intro On Scene Load", "Play the sequence once when PlayWithAI loads, then restore gameplay controls."));
            serializedDirector.ApplyModifiedProperties();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sequence", EditorStyles.boldLabel);
            if (GUILayout.Button("Save Scene View Angle", GUILayout.Width(155f)))
                SaveSceneViewAngle();
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < director.Shots.Count; i++)
                DrawShot(serializedDirector, i);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Shot"))
                ShowAddShotMenu();
            using (new EditorGUI.DisabledScope(director.Shots.Count > 0))
            {
                if (GUILayout.Button("Create Arena Intro"))
                    CreateArenaIntro();
            }
            using (new EditorGUI.DisabledScope(selectedShot < 0 || selectedShot >= director.Shots.Count))
            {
                if (GUILayout.Button("Duplicate"))
                    DuplicateShot();
                if (GUILayout.Button("Remove"))
                    RemoveShot();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawShot(SerializedObject serializedDirector, int index)
        {
            SerializedProperty shots = serializedDirector.FindProperty("shots");
            if (shots == null || index >= shots.arraySize)
                return;

            SerializedProperty shot = shots.GetArrayElementAtIndex(index);
            SerializedProperty name = shot.FindPropertyRelative("shotName");
            SerializedProperty type = shot.FindPropertyRelative("type");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string displayName = string.IsNullOrWhiteSpace(name.stringValue) ? "Shot " + (index + 1) : name.stringValue;
            bool expanded = EditorGUILayout.Foldout(selectedShot == index, displayName, true);
            if (GUILayout.Button("Select", GUILayout.Width(52f)))
                selectedShot = index;
            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUILayout.Button("↑", GUILayout.Width(26f)))
                    MoveShot(index, index - 1);
            }
            using (new EditorGUI.DisabledScope(index >= shots.arraySize - 1))
            {
                if (GUILayout.Button("↓", GUILayout.Width(26f)))
                    MoveShot(index, index + 1);
            }
            EditorGUILayout.EndHorizontal();

            if (expanded)
            {
                selectedShot = index;
                DrawShotProperties(shot, type);
            }

            EditorGUILayout.EndVertical();
            serializedDirector.ApplyModifiedProperties();
        }

        private static void DrawShotProperties(SerializedProperty shot, SerializedProperty type)
        {
            EditorGUILayout.PropertyField(shot.FindPropertyRelative("shotName"));
            EditorGUILayout.PropertyField(shot.FindPropertyRelative("enabled"));
            EditorGUILayout.PropertyField(type);
            EditorGUILayout.PropertyField(shot.FindPropertyRelative("duration"));
            EditorGUILayout.PropertyField(shot.FindPropertyRelative("hardCut"));
            if (!shot.FindPropertyRelative("hardCut").boolValue)
                EditorGUILayout.PropertyField(shot.FindPropertyRelative("transitionDuration"));
            EditorGUILayout.PropertyField(shot.FindPropertyRelative("easing"));
            EditorGUILayout.PropertyField(shot.FindPropertyRelative("fieldOfView"));

            CinematicShotType shotType = (CinematicShotType)type.enumValueIndex;
            if (shotType == CinematicShotType.Orbit || shotType == CinematicShotType.Aerial ||
                shotType == CinematicShotType.CloseUp || shotType == CinematicShotType.Custom ||
                shotType == CinematicShotType.Reveal || shotType == CinematicShotType.Establishing)
            {
                EditorGUILayout.PropertyField(shot.FindPropertyRelative("target"));
                EditorGUILayout.PropertyField(shot.FindPropertyRelative("targetOffset"));
            }

            switch (shotType)
            {
                case CinematicShotType.Orbit:
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("orbitRadius"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("orbitHeight"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("orbitStartAngle"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("orbitEndAngle"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("orbitClockwise"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("changeOrbitHeight"));
                    if (shot.FindPropertyRelative("changeOrbitHeight").boolValue)
                        EditorGUILayout.PropertyField(shot.FindPropertyRelative("orbitEndHeight"));
                    break;
                case CinematicShotType.Aerial:
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("aerialStartHeight"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("aerialEndHeight"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("aerialViewAngle"));
                    break;
                case CinematicShotType.Establishing:
                case CinematicShotType.Reveal:
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("startPosition"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("startEulerAngles"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("endPosition"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("endEulerAngles"));
                    if (shotType == CinematicShotType.Establishing)
                        EditorGUILayout.PropertyField(shot.FindPropertyRelative("moveToEndPose"));
                    break;
                case CinematicShotType.CloseUp:
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("closeUpDistance"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("closeUpHeight"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("closeUpOrbitDegrees"));
                    break;
                case CinematicShotType.Custom:
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("customPosition"));
                    EditorGUILayout.PropertyField(shot.FindPropertyRelative("customEulerAngles"));
                    break;
            }
        }

        private void SaveSceneViewAngle()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                Debug.LogWarning("[Cinematic Director] No active Scene view camera is available.");
                return;
            }

            Undo.RecordObject(director, "Save Cinematic Scene View Angle");
            Transform cameraTransform = sceneView.camera.transform;
            CinematicShot shot = new CinematicShot
            {
                shotName = "Scene View " + (director.Shots.Count + 1),
                type = CinematicShotType.Custom,
                customPosition = cameraTransform.position,
                customEulerAngles = cameraTransform.eulerAngles,
                fieldOfView = sceneView.camera.fieldOfView
            };
            director.AddShot(shot);
            selectedShot = director.Shots.Count - 1;
            MarkDirectorDirty();
        }

        private void ShowAddShotMenu()
        {
            GenericMenu menu = new GenericMenu();
            foreach (CinematicShotType type in System.Enum.GetValues(typeof(CinematicShotType)))
            {
                CinematicShotType selectedType = type;
                menu.AddItem(new GUIContent(type.ToString()), false, () => AddShot(selectedType));
            }
            menu.ShowAsContext();
        }

        private void AddShot(CinematicShotType type)
        {
            Undo.RecordObject(director, "Add Cinematic Shot");
            director.AddShot(new CinematicShot
            {
                shotName = type + " " + (director.Shots.Count + 1),
                type = type
            });
            selectedShot = director.Shots.Count - 1;
            MarkDirectorDirty();
        }

        private void CreateArenaIntro()
        {
            GameObject battlefield = GameObject.Find("Battlefield3D");
            if (battlefield == null || battlefield.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene())
            {
                Debug.LogError("[Cinematic Director] Battlefield3D was not found in the active scene.");
                return;
            }

            Camera gameplayCamera = director.GetComponent<Camera>();
            if (gameplayCamera == null)
                gameplayCamera = Camera.main;
            if (gameplayCamera == null)
            {
                Debug.LogError("[Cinematic Director] The gameplay camera was not found.");
                return;
            }

            Vector3 cameraStart = gameplayCamera.transform.position;
            Vector3 cameraEuler = gameplayCamera.transform.eulerAngles;
            Vector3 walkEnd = new Vector3(0f, 1.7f, 4f);
            Vector3 walkEndEuler = new Vector3(8f, 0f, 0f);
            Vector3 turnBackEuler = new Vector3(8f, 180f, 0f);
            Vector3 aerialEuler = new Vector3(90f, 180f, 0f);
            Vector3 oppositeEnd = new Vector3(0f, 1.7f, -4.5f);
            Vector3 boardCenter = new Vector3(0f, 1.7f, 0f);

            Undo.RecordObject(director, "Create Arena Intro Sequence");
            director.AddShot(new CinematicShot
            {
                shotName = "Walk Forward Across Arena",
                type = CinematicShotType.Reveal,
                duration = 5f,
                transitionDuration = 1.25f,
                fieldOfView = 60f,
                startPosition = cameraStart,
                startEulerAngles = cameraEuler,
                endPosition = walkEnd,
                endEulerAngles = walkEndEuler
            });
            director.AddShot(new CinematicShot
            {
                shotName = "Turn Toward Opposite End",
                type = CinematicShotType.Reveal,
                duration = 1.5f,
                transitionDuration = 1f,
                fieldOfView = 55f,
                startPosition = walkEnd,
                startEulerAngles = walkEndEuler,
                endPosition = walkEnd,
                endEulerAngles = turnBackEuler
            });
            director.AddShot(new CinematicShot
            {
                shotName = "Walk Forward To Opposite End",
                type = CinematicShotType.Reveal,
                duration = 4f,
                transitionDuration = 1.25f,
                fieldOfView = 55f,
                startPosition = walkEnd,
                startEulerAngles = turnBackEuler,
                endPosition = oppositeEnd,
                endEulerAngles = turnBackEuler
            });
            director.AddShot(new CinematicShot
            {
                shotName = "Pull Back To Board Middle",
                type = CinematicShotType.Reveal,
                duration = 3f,
                transitionDuration = 1.25f,
                fieldOfView = 55f,
                startPosition = oppositeEnd,
                startEulerAngles = turnBackEuler,
                endPosition = boardCenter,
                endEulerAngles = turnBackEuler
            });
            director.AddShot(new CinematicShot
            {
                shotName = "Tilt Down At Board Center",
                type = CinematicShotType.Reveal,
                duration = 1.5f,
                transitionDuration = 1.5f,
                fieldOfView = 55f,
                startPosition = boardCenter,
                startEulerAngles = turnBackEuler,
                endPosition = boardCenter,
                endEulerAngles = aerialEuler
            });
            director.AddShot(new CinematicShot
            {
                shotName = "Rise Above The Board",
                type = CinematicShotType.Reveal,
                duration = 2.5f,
                transitionDuration = 1.5f,
                fieldOfView = 55f,
                startPosition = boardCenter,
                startEulerAngles = aerialEuler,
                endPosition = new Vector3(0f, 13f, 0f),
                endEulerAngles = aerialEuler
            });
            director.AddShot(new CinematicShot
            {
                shotName = "Rotate Arena Overhead",
                type = CinematicShotType.Reveal,
                duration = 2f,
                transitionDuration = 1.5f,
                fieldOfView = 55f,
                startPosition = new Vector3(0f, 13f, 0f),
                startEulerAngles = aerialEuler,
                endPosition = new Vector3(0f, 13f, 0f),
                endEulerAngles = new Vector3(90f, 0f, 0f)
            });
            director.AddShot(new CinematicShot
            {
                shotName = "Circle The Arena",
                type = CinematicShotType.Orbit,
                duration = 12f,
                transitionDuration = 2f,
                target = battlefield.transform,
                targetOffset = Vector3.up,
                fieldOfView = 55f,
                orbitRadius = 14f,
                orbitHeight = 4f,
                orbitStartAngle = 0f,
                orbitEndAngle = 360f,
                orbitClockwise = true
            });
            director.PlaySequenceOnSceneLoad = true;

            selectedShot = 0;
            MarkDirectorDirty();
        }

        private static CinematicShot CreatePanShot(
            string shotName,
            Transform target,
            Vector3 startPosition,
            Vector3 endPosition,
            Vector3 eulerAngles)
        {
            return new CinematicShot
            {
                shotName = shotName,
                type = CinematicShotType.Establishing,
                duration = 5f,
                transitionDuration = 0.75f,
                target = target,
                targetOffset = Vector3.up,
                fieldOfView = 58f,
                startPosition = startPosition,
                startEulerAngles = eulerAngles,
                endPosition = endPosition,
                endEulerAngles = eulerAngles
            };
        }

        private void DuplicateShot()
        {
            if (selectedShot < 0 || selectedShot >= director.Shots.Count)
                return;

            Undo.RecordObject(director, "Duplicate Cinematic Shot");
            CinematicShot duplicate = director.Shots[selectedShot].Clone();
            director.AddShot(duplicate);
            selectedShot = director.Shots.Count - 1;
            MarkDirectorDirty();
        }

        private void RemoveShot()
        {
            if (selectedShot < 0 || selectedShot >= director.Shots.Count)
                return;

            Undo.RecordObject(director, "Remove Cinematic Shot");
            director.RemoveShotAt(selectedShot);
            selectedShot = Mathf.Min(selectedShot, director.Shots.Count - 1);
            MarkDirectorDirty();
        }

        private void MoveShot(int from, int to)
        {
            SerializedObject serializedDirector = new SerializedObject(director);
            SerializedProperty shots = serializedDirector.FindProperty("shots");
            if (shots == null || from < 0 || from >= shots.arraySize || to < 0 || to >= shots.arraySize)
                return;

            Undo.RecordObject(director, "Reorder Cinematic Shots");
            shots.MoveArrayElement(from, to);
            serializedDirector.ApplyModifiedProperties();
            selectedShot = to;
            MarkDirectorDirty();
        }

        private void AddDirectorToMainCamera()
        {
            Camera camera = FindMainCamera();
            if (camera == null)
            {
                Debug.LogError("[Cinematic Director] The active scene has no Main Camera.");
                return;
            }

            director = camera.GetComponent<CinematicCameraDirector>();
            if (director == null)
            {
                Undo.AddComponent<CinematicCameraDirector>(camera.gameObject);
                director = camera.GetComponent<CinematicCameraDirector>();
            }

            if (director != null)
            {
                SerializedObject serializedDirector = new SerializedObject(director);
                serializedDirector.FindProperty("gameplayCamera").objectReferenceValue = camera;
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    if (canvas != null && canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                        canvas.gameObject.scene == camera.gameObject.scene)
                    {
                        serializedDirector.FindProperty("gameplayCanvas").objectReferenceValue = canvas;
                        break;
                    }
                }
                serializedDirector.ApplyModifiedProperties();
                Selection.activeGameObject = camera.gameObject;
                MarkDirectorDirty();
            }
        }

        private static Camera FindMainCamera()
        {
            if (Selection.activeGameObject != null)
            {
                Camera selectedCamera = Selection.activeGameObject.GetComponent<Camera>();
                if (selectedCamera != null && selectedCamera.CompareTag("MainCamera"))
                    return selectedCamera;
            }

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera camera in cameras)
            {
                if (camera.CompareTag("MainCamera") && camera.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene())
                    return camera;
            }
            return null;
        }

        private void RefreshDirector()
        {
            if (Selection.activeGameObject != null)
            {
                CinematicCameraDirector selected = Selection.activeGameObject.GetComponent<CinematicCameraDirector>();
                if (selected != null)
                {
                    director = selected;
                    return;
                }
            }

            CinematicCameraDirector[] directors = FindObjectsByType<CinematicCameraDirector>(FindObjectsSortMode.None);
            foreach (CinematicCameraDirector candidate in directors)
            {
                if (candidate.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene())
                {
                    director = candidate;
                    return;
                }
            }
            director = null;
        }

        private void MarkDirectorDirty()
        {
            EditorUtility.SetDirty(director);
            if (director.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
            Repaint();
        }
    }
}
