using UnityEngine;
using UnityEngine.UI;

namespace MythMayhem.Gameplay
{
    public class CreatureHealthBar : MonoBehaviour
    {
        private const float BaseWorldScale = 0.01f;

        private static Font cachedFont;

        private MonoBehaviour targetBehaviour;
        private IHealthTarget target;
        private RectTransform fillRect;
        private Text valueLabel;
        private bool built;
        private float heightMultiplier = 1.3f;
        private float scaleMultiplier = 1f;
        private int lastCurrent = int.MinValue;
        private int lastMax = int.MinValue;

        public static CreatureHealthBar Create(IHealthTarget instance, float scale, Vector2 canvasSize, float heightOffset)
        {
            MonoBehaviour behaviour = instance as MonoBehaviour;
            if (instance == null || behaviour == null)
                return null;

            if (instance.HealthBar != null)
                return instance.HealthBar;

            CreatureHealthBar existing = behaviour.GetComponentInChildren<CreatureHealthBar>(true);
            if (existing == null)
            {
                GameObject root = new GameObject("HealthBar_" + behaviour.name);
                existing = root.AddComponent<CreatureHealthBar>();
            }

            existing.Configure(instance, behaviour, scale, canvasSize, heightOffset);
            instance.HealthBar = existing;
            return existing;
        }

        private void Configure(IHealthTarget instance, MonoBehaviour behaviour, float scale, Vector2 canvasSize, float heightOffset)
        {
            target = instance;
            targetBehaviour = behaviour;
            scaleMultiplier = scale > 0f ? scale : 1f;
            heightMultiplier = heightOffset;

            Build(canvasSize);
            SnapToTarget();
            Refresh(true);
        }

        private void Build(Vector2 canvasSize)
        {
            if (built)
                return;

            built = true;

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = canvasSize;
            rect.localScale = Vector3.one * (BaseWorldScale * scaleMultiplier);

            Image background = CreateImage("Background", rect, new Color(0.05f, 0.05f, 0.05f, 0.75f));
            Stretch(background.rectTransform);

            Image fill = CreateImage("Fill", rect, new Color(0.20f, 0.85f, 0.30f, 1f));
            fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            valueLabel = CreateLabel("Value", rect);
        }

        private void LateUpdate()
        {
            if (targetBehaviour == null)
            {
                Destroy(gameObject);
                return;
            }

            SnapToTarget();
            Refresh(false);
        }

        private void SnapToTarget()
        {
            if (targetBehaviour == null)
                return;

            transform.position = targetBehaviour.transform.position + Vector3.up * heightMultiplier;

            Camera camera = Camera.main;
            if (camera != null)
                transform.rotation = camera.transform.rotation;
        }

        private void Refresh(bool force)
        {
            if (targetBehaviour == null || target == null || fillRect == null || valueLabel == null)
                return;

            int max = target.MaxHealth;
            int current = target.CurrentHealth;

            if (!force && current == lastCurrent && max == lastMax)
                return;

            lastCurrent = current;
            lastMax = max;

            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            fillRect.anchorMax = new Vector2(ratio, 1f);
            valueLabel.text = current + " / " + max;
        }

        private static Image CreateImage(string name, RectTransform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateLabel(string name, RectTransform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = 15;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Stretch(text.rectTransform);
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font GetFont()
        {
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return cachedFont;
        }
    }
}
