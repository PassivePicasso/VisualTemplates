using UnityEngine;
using UnityEditor;
using System;

#if UNITY_2020
using UnityEditor.UIElements;
#elif UNITY_2018
using UnityEditor.Experimental.UIElements;
#endif
#if UNITY_2020
using UnityEngine.UIElements;
#elif UNITY_2018
using UnityEngine.Experimental.UIElements;
#endif


namespace VisualTemplates
{
    public static class VisualTemplateSettings
    {
        public static Func<string, VisualTreeAsset> TemplateLoader { get; set; }
    }
}