using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MythMayhem.EditorTools
{
    // Turns the imported Meshy character FBXs into spawnable, animated creature prefabs.
    //
    // The Meshy FBX references its texture as "texture_0.png", but the file kept in
    // the project is named "Meshy_AI_<Creature>_..._texture_0.png", so Unity's model
    // importer cannot link the albedo automatically. Instead we build a URP/Lit
    // material per creature from the albedo, drop the model into a small wrapper
    // prefab (normalised so the feet sit at the prefab origin and the height matches
    // the creature), build an AnimatorController from the exported clips, and assign
    // that prefab to the matching CreatureDefinition.
    //
    // Clip mapping (from the "_Meshy_Merged_Animations" FBX):
    //   Spawn  -> "Arise"
    //   Attack -> the creature's attack take (Slash / Spell Cast / Weapon Combo)
    //   Death  -> "dying_backwards"
    //   Idle   -> a single-frame pose baked from the end of "Arise" (no idle take exists)
    //
    // Safe to re-run: existing materials/prefabs/controllers are reused and overwritten.
    public static class CreatureSetupTool
    {
        private const string CharactersRoot = "Assets/Art/Characters";
        private const string CardsRoot = "Assets/Data/Cards";
        private const string PrefabsRoot = "Assets/Prefabs";
        private const string CreaturePrefabsFolder = PrefabsRoot + "/Creatures";
        private const string MaterialsRoot = "Assets/Materials";
        private const string CreatureMaterialsFolder = MaterialsRoot + "/Creatures";
        private const string AnimationsRoot = "Assets/Animations";
        private const string CreatureAnimationsFolder = AnimationsRoot + "/Creatures";

        private static readonly string[] SpawnKeywords = { "arise", "spawn", "rise" };
        private static readonly string[] DeathKeywords = { "dying", "death", "die", "dead" };
        private static readonly string[] AttackKeywords = { "slash", "spell", "weapon", "combo", "attack", "cast", "punch", "kick", "strike" };

        // Each entry maps a CreatureDefinition asset to its rigged model and textures.
        private struct CreatureSpec
        {
            public string Name;
            public string Folder;
            public string ModelFile;
            public string TexturePrefix;
            public float TargetHeight;
            public bool UseTexture0Suffix;

            public CreatureSpec(string name, string folder, string modelFile, string texturePrefix, float targetHeight, bool useTexture0Suffix = true)
            {
                Name = name;
                Folder = folder;
                ModelFile = modelFile;
                TexturePrefix = texturePrefix;
                TargetHeight = targetHeight;
                UseTexture0Suffix = useTexture0Suffix;
            }
        }

        private static readonly CreatureSpec[] Specs =
        {
            new CreatureSpec("Aswang", "Aswang", "Meshy_AI_Aswang_Rigged_biped_Meshy_Merged_Animations", "Meshy_AI_Aswang_Rigged_biped", 1.8f),
            new CreatureSpec("Diwata", "Diwata", "Meshy_AI_Diwata_Rigged_biped_Meshy_Merged_Animations", "Meshy_AI_Diwata_Rigged_biped", 1.9f),
            new CreatureSpec("Tiyanak", "Tiyanak", "Meshy_AI_Tiyanak_Rigged_biped_Meshy_Merged_Animations", "Meshy_AI_Tiyanak_Rigged_biped", 1.1f),
            new CreatureSpec("Kapre", "Kapre", "Meshy_AI_Ancient_Troll_Rigged_biped_Meshy_Merged_Animations", "Meshy_AI_Ancient_Troll_Rigged_biped", 2.2f),
            new CreatureSpec("Tikbalang", "Tikbalang", "Meshy_AI_Tikbalang_Rigged_biped_Meshy_Merged_Animations", "Meshy_AI_Tikbalang_Rigged_biped", 2.0f),
            new CreatureSpec("Manananggal", "Manananggal", "Meshy_AI_Manananggal_3D_biped_Meshy_Merged_Animations", "Meshy_AI_Manananggal_3D_biped", 1.9f),
            new CreatureSpec("Bakunawa", "Bakunawa", "Meshy_AI_Bakunawa_LowPoly_Opti_1016031641_texture", "Meshy_AI_Bakunawa_LowPoly_Opti_1016031641", 4.55f, false),
        };

        public static void SetupCreatures()
        {
            SetupCreatures(null);
        }

        // Rebuilds a single creature without touching the others.
        public static void SetupBakunawa()
        {
            SetupCreatures("Bakunawa");
        }

        public static void SetupCreatures(string onlyName)
        {
            EnsureFolder(PrefabsRoot);
            EnsureFolder(CreaturePrefabsFolder);
            EnsureFolder(MaterialsRoot);
            EnsureFolder(CreatureMaterialsFolder);
            EnsureFolder(AnimationsRoot);
            EnsureFolder(CreatureAnimationsFolder);

            int built = 0;
            int considered = 0;

            foreach (CreatureSpec spec in Specs)
            {
                if (!string.IsNullOrEmpty(onlyName) && !string.Equals(spec.Name, onlyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                considered++;
                try
                {
                    if (BuildCreature(spec))
                        built++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[Creatures] Failed to build " + spec.Name + ": " + e.Message);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Creatures] Finished. Built " + built + " of " + considered +
                      " creature prefabs. Fine-tune scale/rotation in the Inspector if needed.");
        }

        private static bool BuildCreature(CreatureSpec spec)
        {
            string modelPath = CharactersRoot + "/" + spec.Folder + "/" + spec.ModelFile + ".fbx";
            string albedoPath = spec.UseTexture0Suffix
                ? CharactersRoot + "/" + spec.Folder + "/" + spec.TexturePrefix + "_texture_0.png"
                : CharactersRoot + "/" + spec.Folder + "/" + spec.TexturePrefix + "_texture.png";
            string definitionPath = CardsRoot + "/" + spec.Name + ".asset";

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError("[Creatures] Model not imported yet at " + modelPath +
                               ". Wait for Unity to finish importing the FBX, then run again.");
                return false;
            }

            EnsureAlbedoImport(albedoPath);
            Material material = CreateMaterial(spec.Name, albedoPath);
            RuntimeAnimatorController controller = BuildAnimations(spec, modelPath);

            GameObject wrapper = new GameObject(spec.Name);
            GameObject instance = UnityEngine.Object.Instantiate(model);
            instance.name = spec.Name;
            instance.transform.SetParent(wrapper.transform, false);

            ApplyMaterial(instance, material);
            StripNonVisualComponents(instance);
            Normalize(instance, spec.TargetHeight);
            AssignAnimator(instance, controller);

            string prefabPath = CreaturePrefabsFolder + "/" + spec.Name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
            UnityEngine.Object.DestroyImmediate(wrapper);

            AssignDefinitionPrefab(definitionPath, spec.Name, prefab);
            Debug.Log("[Creatures] Built " + spec.Name + " prefab at " + prefabPath + ".");
            return true;
        }

        // --- Animations --------------------------------------------------------------

        private static RuntimeAnimatorController BuildAnimations(CreatureSpec spec, string modelPath)
        {
            AnimationClip[] clips = LoadClips(modelPath);
            if (clips.Length == 0)
            {
                Debug.Log("[Creatures] " + spec.Name + " is a static model with no imported clips; " +
                          "the procedural spawn/attack/death fallback will drive it.");
                return null;
            }

            AnimationClip spawn = FindClip(clips, SpawnKeywords);
            AnimationClip attack = FindClip(clips, AttackKeywords);
            AnimationClip death = FindClip(clips, DeathKeywords);

            if (spawn == null && clips.Length > 0)
                spawn = clips[0];

            if (attack == null || death == null || spawn == null)
            {
                Debug.LogWarning("[Creatures] " + spec.Name + " is missing one of spawn/attack/death clips; " +
                                 "found " + clips.Length + " clip(s). Animator will use what is available.");
            }

            AnimationClip idle = BakeIdle(spawn, CreatureAnimationsFolder + "/" + spec.Name + "_Idle.anim", spec.Name + "_Idle");
            if (idle == null)
                idle = spawn;
            if (death == null)
                death = spawn;

            string controllerPath = CreatureAnimationsFolder + "/" + spec.Name + ".controller";
            return CreateController(controllerPath, spawn, idle, attack, death);
        }

        private static AnimationClip[] LoadClips(string fbxPath)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null)
                return clips.ToArray();

            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is AnimationClip clip && !string.IsNullOrEmpty(clip.name) && !clip.name.StartsWith("__preview__"))
                    clips.Add(clip);
            }

            return clips.ToArray();
        }

        private static AnimationClip FindClip(AnimationClip[] clips, string[] keywords)
        {
            foreach (AnimationClip clip in clips)
            {
                string name = clip.name.ToLowerInvariant();
                foreach (string keyword in keywords)
                {
                    if (name.Contains(keyword))
                        return clip;
                }
            }

            return null;
        }

        // Builds a static one-frame clip from the final pose of the source clip. Meshy
        // exports have no dedicated idle take, so the creature simply holds its
        // standing pose (the end of "Arise") between actions.
        private static AnimationClip BakeIdle(AnimationClip source, string path, string name)
        {
            if (source == null)
                return null;

            AnimationClip idle = new AnimationClip { name = name, frameRate = source.frameRate, wrapMode = WrapMode.Loop };
            float time = Mathf.Max(0f, source.length);

            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve == null)
                    continue;

                float value = curve.Evaluate(time);
                AnimationUtility.SetEditorCurve(idle, binding, new AnimationCurve(new Keyframe(0f, value)));
            }

            ReplaceAsset(idle, path);
            return idle;
        }

        private static AnimatorController CreateController(string path, AnimationClip spawn, AnimationClip idle, AnimationClip attack, AnimationClip death)
        {
            ReplaceAsset(path);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState spawnState = machine.AddState("Spawn");
            spawnState.motion = spawn;
            AnimatorState idleState = machine.AddState("Idle");
            idleState.motion = idle;
            AnimatorState deathState = machine.AddState("Death");
            deathState.motion = death;

            AnimatorState attackState = null;
            if (attack != null)
            {
                attackState = machine.AddState("Attack");
                attackState.motion = attack;
            }

            machine.defaultState = spawnState;

            AnimatorStateTransition spawnToIdle = spawnState.AddTransition(idleState);
            spawnToIdle.hasExitTime = true;
            spawnToIdle.exitTime = 1f;
            spawnToIdle.duration = 0.1f;
            spawnToIdle.hasFixedDuration = false;

            if (attack != null)
            {
                AnimatorStateTransition idleToAttack = idleState.AddTransition(attackState);
                idleToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
                idleToAttack.hasExitTime = false;
                idleToAttack.duration = 0.1f;
                idleToAttack.hasFixedDuration = false;

                AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
                attackToIdle.hasExitTime = true;
                attackToIdle.exitTime = 1f;
                attackToIdle.duration = 0.15f;
                attackToIdle.hasFixedDuration = false;
            }
            AnimatorStateTransition anyToDeath = machine.AddAnyStateTransition(deathState);
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Death");
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.1f;
            anyToDeath.canTransitionToSelf = false;
            anyToDeath.hasFixedDuration = false;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AssignAnimator(GameObject model, RuntimeAnimatorController controller)
        {
            if (controller == null)
                return;

            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                Animator child = model.GetComponentInChildren<Animator>();
                animator = child != null ? child : model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        // --- Materials / model -------------------------------------------------------

        private static Material CreateMaterial(string name, string albedoPath)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            string path = CreatureMaterialsFolder + "/" + name + "_Mat.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            Texture albedo = AssetDatabase.LoadAssetAtPath<Texture>(albedoPath);

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", albedo);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", albedo);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.35f);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.35f);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureAlbedoImport(string albedoPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(albedoPath) as TextureImporter;
            if (importer == null)
                return;

            bool changed = false;
            if (!importer.sRGBTexture)
            {
                importer.sRGBTexture = true;
                changed = true;
            }

            if (importer.mipmapEnabled == false)
            {
                importer.mipmapEnabled = true;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        private static void StripNonVisualComponents(GameObject root)
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                UnityEngine.Object.DestroyImmediate(camera);

            foreach (Light light in root.GetComponentsInChildren<Light>(true))
                UnityEngine.Object.DestroyImmediate(light);
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                int slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                Material[] materials = new Material[slotCount];
                for (int i = 0; i < slotCount; i++)
                    materials[i] = material;

                renderer.sharedMaterials = materials;
            }
        }

        // Scales the model so it stands TargetHeight units tall and re-seats it so the
        // base of its bounds sits on the wrapper origin (feet on the ground, centred).
        // The model's own root rotation is left untouched (it can carry the FBX axis
        // conversion); only the uniform scale and position are overridden.
        private static void Normalize(GameObject model, float targetHeight)
        {
            if (!TryGetBounds(model, out Bounds bounds))
                return;

            float height = bounds.size.y;
            if (height > 0.0001f)
                model.transform.localScale = Vector3.one * (targetHeight / height);

            if (!TryGetBounds(model, out bounds))
                return;

            Vector3 offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            model.transform.position += offset;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }

        private static void AssignDefinitionPrefab(string definitionPath, string name, GameObject prefab)
        {
            ScriptableObject definition = AssetDatabase.LoadAssetAtPath<ScriptableObject>(definitionPath);
            if (definition == null)
            {
                Debug.LogWarning("[Creatures] No CreatureDefinition found at " + definitionPath + " for " + name + ".");
                return;
            }

            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty property = serialized.FindProperty("prefab");
            if (property == null)
            {
                Debug.LogWarning("[Creatures] " + definitionPath + " has no 'prefab' field.");
                return;
            }

            property.objectReferenceValue = prefab;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
        }

        private static void ReplaceAsset(UnityEngine.Object asset, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(asset, path);
        }

        private static void ReplaceAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
