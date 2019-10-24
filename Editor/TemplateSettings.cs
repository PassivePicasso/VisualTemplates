using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public static class VisualTemplateSettings
    {
        public static Func<string, VisualTreeAsset> TemplateLoader { get; set; }
    }
}