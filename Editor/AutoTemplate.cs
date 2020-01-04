using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public class AutoTemplate : VisualElement
    {
        public Func<string, VisualTreeAsset> AssetLoader { get; private set; }
        public Editor editor { get; private set; }

        public AutoTemplate(Editor editorHost, Type type, Func<string, VisualTreeAsset> assetLoader)
        {
            string typeName = type.Name;

            name = $"{typeName}-auto-template";
            AssetLoader = assetLoader;

            VisualTreeAsset visualTreeAsset = AssetLoader.Invoke(typeName);

            if (visualTreeAsset != null)
                visualTreeAsset.CloneTree(this);
            else
                Add(new Label($"Template file: {typeName}.uxml not found in Resources"));

            this.editor = editorHost;
        }
    }


}