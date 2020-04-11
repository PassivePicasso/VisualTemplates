using UnityEngine;
using UnityEditor;
using System;

#if UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#elif UNITY_2018
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace VisualTemplates
{
    public static class VisualTemplateSettings
    {
        public static Func<string, VisualTreeAsset> TemplateLoader { get; set; }
    }
}