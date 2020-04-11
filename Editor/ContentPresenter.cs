using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#elif UNITY_2018
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace VisualTemplates
{
    public partial class ContentPresenter : BindableElement
    {
        public new class UxmlFactory : UxmlFactory<ContentPresenter, UxmlTraits> { }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_configMethod = new UxmlStringAttributeDescription { name = "config-method" };
            private UxmlBoolAttributeDescription m_enableDebug = new UxmlBoolAttributeDescription { name = "enable-debug", defaultValue = false };

            public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(visualElement, bag, cc);

                var contentPresenter = (ContentPresenter)visualElement;

                contentPresenter.ConfigMethod = m_configMethod.GetValueFromBag(bag, cc);
                contentPresenter.EnableDebug = m_enableDebug.GetValueFromBag(bag, cc);
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        public static Func<string, VisualTreeAsset> DefaultLoadAsset = typeName => Resources.Load<VisualTreeAsset>($@"Templates/{typeName}");

        private SerializedProperty boundProperty;
        private VisualTreeAsset visualTreeAsset;

        public bool EnableDebug { get; set; }
        public string ConfigMethod { get; set; }

        public Func<string, VisualTreeAsset> LoadAsset;

        public Action<VisualElement> Configure;

        public ContentPresenter()
        {
            LoadAsset = DefaultLoadAsset;
            name = $"content-presenter";
        }

        private void Reset(SerializedObject boundObject)
        {
            if (boundObject == null) return;
            Clear();

            string dataType = boundObject.targetObject.GetType().Name;
            boundProperty = GetProperty(boundObject);

            if (boundProperty == null) visualTreeAsset = LoadAsset(dataType);
            else
            {
                var property = boundProperty.Copy();

                dataType = property.type.Substring(property.type.IndexOf(" ") + 1);
                if (dataType?.StartsWith("managedReference") ?? false)
                {
#if UNITY_2020
                    var mrft = property.managedReferenceFullTypename;
                    var assemblyName = mrft.Substring(0, mrft.IndexOf(" "));
                    dataType = mrft.Substring(mrft.IndexOf(" ") + 1);

                    var asm = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
                    var type = asm.GetType(dataType, false, true);
                    var parentType = type;

                    //Debug.Log($"type: {type?.Name}");
                    VisualTreeAsset treeAsset = null;
                    while (treeAsset == null)
                    {
                        treeAsset = LoadAsset(parentType.Name);
                        if (treeAsset == null)
                        {
                            if (parentType.BaseType == null) break;
                            if (parentType.BaseType == typeof(object)) break;

                            parentType = parentType.BaseType;
                        }
                    }
                    if (treeAsset != null)
                    {
                        dataType = dataType.Substring(dataType.LastIndexOf('.') + 1);
                        visualTreeAsset = LoadAsset(dataType);
                    }
#endif
                }
                else
                {
                    if (dataType == null)
                        return;

                    dataType = dataType.Substring(dataType.LastIndexOf('.') + 1);

                    visualTreeAsset = LoadAsset(dataType);
                }
            }

            if (visualTreeAsset == null)
            {
                //in order to maintain our index we must add a visual element even if we don't find a template for the item.
                Label error;

                if (EnableDebug)
                    error = new Label($"Template file not found: {dataType}.uxml");
                else
                    error = new Label(dataType);

                error.AddToClassList("template-error");

                Add(error);

                return;
            }

#if UNITY_2020
            visualTreeAsset.CloneTree(this);
#elif UNITY_2018
            visualTreeAsset.CloneTree(this, null);

            var path = AssetDatabase.GetAssetPath(visualTreeAsset).Replace(".uxml", ".uss");
            AddStyleSheetPath(path);
#endif

            if (Configure == null && !string.IsNullOrEmpty(ConfigMethod))
            {
                var ud = userData ?? FindAncestorUserData();
                if (ud != null)
                {
                    var methodInfo = ud.GetType()
                      .GetMethod(ConfigMethod, BindingFlags.Public | BindingFlags.Instance);
                    //?.Invoke(ud, null);
                    if (methodInfo != null)
                    {
                        var args = new[] { this };
                        Configure = (visualElement) => methodInfo.Invoke(ud, args);
                    }
                }
            }

            Configure?.Invoke(this);

            if (typeof(UnityEngine.Object).IsAssignableFrom(boundObject.targetObject.GetType()))
            {
                foreach (var child in Children())
                    child.Bind(boundObject);
            }
        }

        private SerializedProperty GetProperty(SerializedObject boundObject)
        {
            if (bindingPath == null) return boundObject.FindProperty("");
            var property = boundObject.FindProperty(bindingPath);
            if (property == null)
            {
#if UNITY_2020
                string propertyPath = this.GetBindingPath();
                property = boundObject.FindProperty($"{propertyPath}.{bindingPath}");
#elif UNITY_2018
#endif
            }

            return property;
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            switch (evt.GetType().Name)
            {
                case "SerializedPropertyBindEvent":
                    break;
                case "SerializedObjectBindEvent":
                    var bindObjectProperty = evt.GetType().GetProperty("bindObject");
                    Reset(bindObjectProperty.GetValue(evt) as SerializedObject);
                    break;

                default:
                    break;
            }
        }


    }
}