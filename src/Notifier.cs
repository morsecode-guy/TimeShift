using System.Collections.Generic;
using UnityEngine;

namespace TimeShift
{
    // Top-center notification toasts that fade in and out
    public class Notifier
    {
        struct Toast
        {
            public string Text;
            public float TimeLeft;
            public float TotalTime;
        }

        readonly List<Toast> _toasts = new();

        // How long each toast stays on screen
        const float DefaultDuration = 3f;
        const float FadeTime = 0.4f;

        // Show a new notification
        public void Show(string text, float duration = DefaultDuration)
        {
            _toasts.Add(new Toast
            {
                Text = text,
                TimeLeft = duration,
                TotalTime = duration
            });
        }

        // Call from OnGUI to draw and tick all active toasts
        public void Draw()
        {
            if (_toasts.Count == 0) return;

            float screenW = Screen.width;
            float y = 12f;

            // Style for the notification text
            var style = new GUIStyle(GUI.skin.box);
            style.fontSize = 16;
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = false;
            var pad = style.padding;
            pad.left = 16;
            pad.right = 16;
            pad.top = 8;
            pad.bottom = 8;
            style.padding = pad;

            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                var toast = _toasts[i];
                toast.TimeLeft -= Time.unscaledDeltaTime;
                _toasts[i] = toast;

                if (toast.TimeLeft <= 0f)
                {
                    _toasts.RemoveAt(i);
                    continue;
                }

                // Calculate alpha for fade in and fade out
                float alpha = 1f;
                float age = toast.TotalTime - toast.TimeLeft;
                if (age < FadeTime)
                    alpha = age / FadeTime;
                else if (toast.TimeLeft < FadeTime)
                    alpha = toast.TimeLeft / FadeTime;

                // Measure text width
                var content = new GUIContent(toast.Text);
                var size = style.CalcSize(content);
                float w = size.x + 8;
                float h = size.y + 4;
                float x = (screenW - w) / 2f;

                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Box(new Rect(x, y, w, h), toast.Text, style);
                GUI.color = prev;

                y += h + 6f;
            }
        }
    }
}
