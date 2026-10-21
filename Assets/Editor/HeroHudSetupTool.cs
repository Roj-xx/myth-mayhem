using System;
using MythMayhem.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythMayhem.EditorTools
{
    // Bakes the battle HUD (HP bars, game-over panel, victory/defeat banners,
    // turn/summon label backgrounds and the end-turn button art) into the
    // PlayWithAI scene so every element is a real scene object you can select,
    // drag and fine-tune in the Scene view.
    //
    // Safe to re-run: it replaces any previously baked "HeroHud" object.
    public static class HeroHudSetupTool
    {
        private const string ScenePath = "Assets/Scenes/PlayWithAI.unity";
        private const string HudObjectName = "HeroHud";

        public static void SetupHud()
        {
            Scene scene = MythMayhemUi.OpenForScopedEditing(ScenePath, ScenePath);
            try
            {
                Canvas canvas = MythMayhemUi.FindCanvasInScene(scene);
                if (canvas == null)
                    throw new Exception("No Canvas found in " + ScenePath);

                // Drop any previous bake so re-running is idempotent.
                Transform previous = canvas.transform.Find(HudObjectName);
                if (previous != null)
                    UnityEngine.Object.DestroyImmediate(previous.gameObject);

                GameObject hudObject = new GameObject(HudObjectName, typeof(RectTransform));
                hudObject.transform.SetParent(canvas.transform, false);
                MythMayhemUi.SetStretch(hudObject.GetComponent<RectTransform>());

                HeroHud hud = hudObject.AddComponent<HeroHud>();
                hud.EditorBuild();

                EditorUtility.SetDirty(hud);
                EditorSceneManager.MarkSceneDirty(scene);
                MythMayhemUi.FinishScopedScene(scene, ScenePath);

                Debug.Log("[HUD] Battle HUD baked into " + ScenePath +
                          ". Select the 'HeroHud' object to move/scale the bars, banners and backgrounds.");
            }
            catch (Exception)
            {
                MythMayhemUi.DiscardScopedScene(scene);
                throw;
            }
        }
    }
}
