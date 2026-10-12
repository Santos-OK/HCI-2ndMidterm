using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QuestKiosk
{
    /// <summary>
    /// Small helpers to create plain uGUI objects and to find them again by name.
    /// Positions use the same top-left, pixel based coordinates as the Figma frames (402 x 874).
    /// The default look is neutral (grey boxes, black text). The real look is edited in the scene.
    /// </summary>
    public static class UiKit
    {
        // Default colors used when the scene UI is generated.
        public static readonly Color Background = Color.white;
        public static readonly Color Surface = new Color(0.88f, 0.88f, 0.88f, 1f);
        public static readonly Color Card = new Color(0.80f, 0.80f, 0.80f, 1f);
        public static readonly Color ButtonFill = new Color(0.70f, 0.70f, 0.70f, 1f);
        public static readonly Color TextColor = Color.black;
        public static readonly Color ErrorFill = new Color(1f, 0.85f, 0.85f, 1f);

        // Status colors: applied by code to the StatusDot of every kit card (see docs/design.md, decision 1).
        // They are always shown together with a word, never alone.
        public static readonly Color StatusAvailable = new Color(0.13f, 0.78f, 0f, 1f);
        public static readonly Color StatusReserved = new Color(1f, 0.70f, 0f, 1f);
        public static readonly Color StatusLoaned = new Color(1f, 0.36f, 0.36f, 1f);
        public static readonly Color StatusCharging = new Color(0.31f, 0.76f, 1f, 1f);

        static Font _font;
        static readonly HashSet<string> reportedMissing = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _font = null;
            reportedMissing.Clear();
        }

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        // ------------------------------------------------------------------ layout

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI layer
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Places a rect using top-left pixel coordinates (like Figma).</summary>
        public static void TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------ widgets

        public static Image Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var rt = Node(name, parent);
            TopLeft(rt, x, y, w, h);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, float x, float y, float w, float h,
            int size, FontStyle style, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var rt = Node(name, parent);
            TopLeft(rt, x, y, w, h);

            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = TextColor;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Makes any Image react to taps.</summary>
        public static Button MakeTappable(Image image, UnityAction onClick = null)
        {
            image.raycastTarget = true;

            var btn = image.gameObject.AddComponent<Button>();
            btn.targetGraphic = image;

            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        /// <summary>A button: a box (the Image) with a centered child text called Txt_Label.</summary>
        public static Button MakeButton(Transform parent, string name, string label, float x, float y, float w, float h,
            int fontSize = 22)
        {
            var img = Panel(parent, name, x, y, w, h, ButtonFill);
            var btn = MakeTappable(img);
            Label(img.transform, "Txt_Label", label, 0, 0, w, h, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            return btn;
        }

        /// <summary>
        /// A vertical scroll list. Its child "ListContent" stacks its children from top to bottom
        /// (VerticalLayoutGroup), so spacing and margins are edited on ListContent in the Inspector.
        /// </summary>
        public static RectTransform ScrollList(Transform parent, string name, float x, float y, float w, float h,
            int padLeft, int padTop, float spacing)
        {
            var rt = Node(name, parent);
            TopLeft(rt, x, y, w, h);

            var catcher = rt.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f); // invisible, but receives drags
            rt.gameObject.AddComponent<RectMask2D>();

            var content = Node("ListContent", rt);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, h);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padLeft, 0, padTop, 10);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = rt.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = rt;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            scroll.inertia = true;

            return content;
        }

        // ------------------------------------------------------------------ finding objects by name

        /// <summary>Finds a child (active or not) by name, closest to the root first. Null if missing.</summary>
        public static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;

            var queue = new Queue<Transform>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                if (t != root && t.name == name) return t;
                for (int i = 0; i < t.childCount; i++) queue.Enqueue(t.GetChild(i));
            }
            return null;
        }

        /// <summary>Like FindDeep, but writes a warning once when the object is missing (renamed or deleted).</summary>
        public static Transform Find(Transform root, string name)
        {
            if (root == null) return null;

            var t = FindDeep(root, name);
            if (t == null)
            {
                string key = root.name + "/" + name;
                if (reportedMissing.Add(key))
                {
                    Debug.LogWarning("[Quest Kiosk] '" + name + "' was not found inside '" + root.name +
                        "'. It was probably renamed or deleted in the scene. Keep the original name.");
                }
            }
            return t;
        }

        public static void SetText(Transform root, string name, string text)
        {
            var t = Find(root, name);
            if (t == null) return;

            var label = t.GetComponent<Text>();
            if (label != null) label.text = text;
        }

        public static void SetShown(Transform root, string name, bool shown)
        {
            var t = Find(root, name);
            if (t != null && t.gameObject.activeSelf != shown) t.gameObject.SetActive(shown);
        }

        public static void OnClick(Transform root, string name, UnityAction action)
        {
            var t = Find(root, name);
            if (t == null) return;

            var btn = t.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(action);
        }
    }
}
