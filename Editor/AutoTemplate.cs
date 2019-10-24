using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public class AutoTemplate : VisualElement
    {
        public Editor editorHost { get; private set; }
        public AutoTemplate(Editor editorHost, Type type)
        {
            string typeName = type.Name;

            name = $"{typeName}-auto-template";

            VisualTreeAsset visualTreeAsset = VisualTemplateSettings.TemplateLoader.Invoke(typeName);

            Add(visualTreeAsset == null
               ? new Label($"Template file: {typeName}.uxml not found in Resources")
               : (VisualElement)visualTreeAsset.Instantiate()
               );
            this.editorHost = editorHost;
        }
    }


}