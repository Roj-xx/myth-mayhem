using System.Text;
using UnityEditor;
using UnityEngine;

namespace MythMayhem.EditorTools
{
    // Temporary diagnostic: tries different FBX animation import settings (compression/resample,
    // Generic vs Humanoid) and reports how many curves actually carry motion.
    public static class CreatureAnimationDiagnostics
    {
        private const string ModelPath = "Assets/Art/Characters/Aswang/Meshy_AI_Aswang_Rigged_biped_Meshy_Merged_Animations.fbx";
        private const string AriseClip = "target_character|target_character|Arise";

        public static void Diagnose()
        {
            var sb = new StringBuilder();
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (importer == null)
            {
                Debug.Log("[Diag] importer not found");
                return;
            }

            ModelImporterAnimationCompression originalCompression = importer.animationCompression;
            bool originalResample = importer.resampleCurves;

            sb.AppendLine("[Diag] baseline compression=" + originalCompression + " resample=" + originalResample);
            sb.AppendLine("[Diag]   varying=" + Measure(ModelPath));

            Try(importer, "compression=Off", ModelImporterAnimationCompression.Off, originalResample, sb);
            Try(importer, "resampleCurves=false", originalCompression, false, sb);
            Try(importer, "compression=Off resample=false", ModelImporterAnimationCompression.Off, false, sb);

            importer.animationCompression = originalCompression;
            importer.resampleCurves = originalResample;
            importer.SaveAndReimport();
            sb.AppendLine("[Diag] restored");

            Debug.Log(sb.ToString());
        }

        public static void DiagnoseHumanoid()
        {
            var sb = new StringBuilder();
            ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (imp == null)
            {
                Debug.Log("[Diag] importer not found");
                return;
            }

            sb.AppendLine("[Diag] before animationType=" + imp.animationType + " avatarSetup=" + imp.avatarSetup + " autoMap=" + imp.autoGenerateAvatarMappingIfUnspecified);
            sb.AppendLine("[Diag] generic totalVarying=" + MeasureAll());

            ModelImporterAnimationType origType = imp.animationType;
            ModelImporterAvatarSetup origSetup = imp.avatarSetup;

            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.autoGenerateAvatarMappingIfUnspecified = true;
            imp.SaveAndReimport();

            Avatar avatar = null;
            foreach (Object a in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
                if (a is Avatar av) { avatar = av; break; }

            sb.AppendLine("[Diag] humanoid avatar=" + (avatar != null ? avatar.name : "NULL") +
                          " isValid=" + (avatar != null && avatar.isValid) +
                          " isHuman=" + (avatar != null && avatar.isHuman));

            sb.AppendLine("[Diag] humanoid totalVarying=" + MeasureAll());
            sb.AppendLine("[Diag] clips after humanoid import:");
            foreach (Object a in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                    sb.AppendLine("[Diag]   '" + c.name + "' varying=" + Varying(c) + " bindings=" + AnimationUtility.GetCurveBindings(c).Length + " length=" + c.length.ToString("F3"));
            }

            imp.animationType = origType;
            imp.avatarSetup = origSetup;
            imp.SaveAndReimport();
            sb.AppendLine("[Diag] restored animationType=" + origType);

            Debug.Log(sb.ToString());
        }

        private static void Try(ModelImporter importer, string label, ModelImporterAnimationCompression compression, bool resample, StringBuilder sb)
        {
            importer.animationCompression = compression;
            importer.resampleCurves = resample;
            importer.SaveAndReimport();
            sb.AppendLine("[Diag] " + label + " -> varying=" + Measure(ModelPath));
        }

        private static int MeasureAll()
        {
            int total = 0;
            foreach (Object a in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                    total += Varying(c);
            return total;
        }

        private static int Varying(AnimationClip clip)
        {
            int varying = 0;
            foreach (EditorCurveBinding b in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationCurve c = AnimationUtility.GetEditorCurve(clip, b);
                if (c == null || c.keys.Length < 2)
                    continue;

                float min = c.keys[0].value, max = c.keys[0].value;
                foreach (Keyframe k in c.keys)
                {
                    if (k.value < min) min = k.value;
                    if (k.value > max) max = k.value;
                }

                if (max - min > 1e-4f)
                    varying++;
            }

            return varying;
        }

        private static int Measure(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null)
                return -1;

            foreach (Object a in assets)
            {
                AnimationClip clip = a as AnimationClip;
                if (clip == null || clip.name != AriseClip)
                    continue;

                return Varying(clip);
            }

            return -2;
        }
    }
}
