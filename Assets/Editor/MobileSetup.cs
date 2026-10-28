using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

// One-off project setup used to apply the mobile player settings through the
// Unity API so the serialized project files stay canonical.
public static class MobileSetup
{
    private const string AdaptiveIconPath = "Assets/Art/Branding/AppIcon_Adaptive.png";
    private const string LegacyIconPath = "Assets/Art/Branding/AppIcon_Legacy.png";
    private const string ApplicationId = "com.elemento.mythmayhem";

    public static void Apply()
    {
        try
        {
            // Icon textures must be imported before they can be referenced.
            AssetDatabase.ImportAsset(AdaptiveIconPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(LegacyIconPath, ImportAssetOptions.ForceUpdate);

            Texture2D adaptive = AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptiveIconPath);
            Texture2D legacy = AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyIconPath);
            Debug.Log($"[MobileSetup] textures adaptive={adaptive} legacy={legacy}");
            if (adaptive == null || legacy == null)
                throw new Exception("Icon textures failed to load.");

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ApplicationId);
            PlayerSettings.Android.forceInternetPermission = true;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            var kinds = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android);
            Debug.Log("[MobileSetup] supported icon kinds: " + string.Join(", ", kinds.Select(k => k.ToString())));

            foreach (var kind in kinds)
            {
                var slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                if (slots.Length == 0)
                    continue;

                bool isAdaptive = kind.ToString().IndexOf("Adaptive", StringComparison.OrdinalIgnoreCase) >= 0;
                Texture2D texture = isAdaptive ? adaptive : legacy;
                Debug.Log($"[MobileSetup] kind={kind} slots={slots.Length} layers={slots[0].minLayerCount}-{slots[0].maxLayerCount} texture={texture.name}");

                foreach (var slot in slots)
                {
                    int layers = Mathf.Max(1, slot.maxLayerCount);
                    var textures = new Texture2D[layers];
                    for (int i = 0; i < layers; i++)
                        textures[i] = texture;
                    slot.SetTextures(textures);
                }

                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, slots);
            }

            Debug.Log("[MobileSetup] quality levels: " + string.Join(", ",
                Enumerable.Range(0, QualitySettings.names.Length).Select(i => i + "=" + QualitySettings.names[i])));
            Debug.Log("[MobileSetup] current quality: " + QualitySettings.GetQualityLevel());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MobileSetup] orientation=" + PlayerSettings.defaultInterfaceOrientation
                + " portrait=" + PlayerSettings.allowedAutorotateToPortrait
                + " upsideDown=" + PlayerSettings.allowedAutorotateToPortraitUpsideDown
                + " landscapeLeft=" + PlayerSettings.allowedAutorotateToLandscapeLeft
                + " landscapeRight=" + PlayerSettings.allowedAutorotateToLandscapeRight);
            Debug.Log("[MobileSetup] appId=" + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)
                + " internet=" + PlayerSettings.Android.forceInternetPermission
                + " apis=" + string.Join("+", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Select(a => a.ToString()))
                + " arch=" + PlayerSettings.Android.targetArchitectures);
            Debug.Log("[MobileSetup] OK");
        }
        catch (Exception e)
        {
            Debug.LogError("[MobileSetup] FAILED: " + e);
            throw;
        }
    }
}
