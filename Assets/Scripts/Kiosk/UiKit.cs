using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QuestKiosk
{
    /// <summary>
    /// Small helpers to build plain uGUI objects from code. Positions use the same top-left,
    /// pixel based coordinates as the Figma frames (402 x 874).
    /// The look is intentionally neutral (flat grey boxes, black text): visual styling is a separate task.
    /// All colors live here so they can be changed in one place.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Background = Color.white;
        public static readonly Color Surface = new Color(0.88f, 0.88f, 0.88f, 1f);
        public static readonly Color Card = new Color(0.80f, 0.80f, 0.80f, 1f);
        public static readonly Color ButtonFill = new Color(0.70f, 0.70f, 0.70f, 1f);
        public static readonly Color TextColor = Color.black;
        public static readonly Color ErrorFill = new Color(1f, 0.85f, 0.85f, 1f);

        // Status colors: part of the interaction (see docs/design.md, decision 1), always shown with a word.
        public static readonly Color StatusAvailable = new Color(0.13f, 0.78f, 0f, 1f);
        public static readonly Color StatusReserved = new Color(1f, 0.70f, 0f, 1f);
        public static readonly Color StatusLoaned = new Color(1f, 0.36f, 0.36f, 1f);
        public static readonly Color StatusCharging = new Color(0.31f, 0.76f, 1f, 1f);

        static Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _font = null;
        }

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                }
                return _font;
            }
        }

        // ------------------------------------------------------------------ layout

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
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

        public static Text Label(Transform parent, string text, float x, float y, float w, float h,
            int size, FontStyle style, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var rt = Node("Label", parent);
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
        public static Button MakeTappable(Image image, UnityAction onClick)
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

        /// <summary>A plain button: grey box with a centered label.</summary>
        public static Button MakeButton(Transform parent, string label, float x, float y, float w, float h,
            UnityAction onClick, int fontSize = 22)
        {
            var img = Panel(parent, "Button_" + label, x, y, w, h, ButtonFill);
            var btn = MakeTappable(img, onClick);
            Label(img.transform, label, 0, 0, w, h, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
            return btn;
        }

        /// <summary>Creates a vertical scroll area and returns its content rect (set to contentHeight).</summary>
        public static RectTransform ScrollArea(Transform parent, float x, float y, float w, float h, float contentHeight)
        {
            var rt = Node("ScrollArea", parent);
            TopLeft(rt, x, y, w, h);

            var catcher = rt.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f); // invisible, but receives drags
            rt.gameObject.AddComponent<RectMask2D>();

            var content = Node("Content", rt);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, contentHeight);

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
    }
}
