using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public class AutoTemplate : VisualElement
    {
        public AutoTemplate(Editor editorHost, Type type, Func<string, VisualTreeAsset> assetLoader)
        {
            string typeName = type.Name;

            name = $"{typeName}-auto-template";

            VisualTreeAsset visualTreeAsset = assetLoader.Invoke(typeName);

            if (visualTreeAsset != null)
                visualTreeAsset.CloneTree(this);
            else
                Add(new Label($"Template file: {typeName}.uxml not found in Resources"));

            userData = editorHost;
        }
    }
}