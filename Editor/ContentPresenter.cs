using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if UNITY_2019_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#elif UNITY_2018
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace VisualTemplates
{
    public partial class ContentPresenter : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ContentPresenter, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_template = new UxmlStringAttributeDescription { name = "template" };
            private UxmlStringAttributeDescription m_bindingPath = new UxmlStringAttributeDescription { name = "binding-path" };
            private UxmlStringAttributeDescription m_configMethod = new UxmlStringAttributeDescription { name = "config-method" };
            private UxmlBoolAttributeDescription m_enableDebug = new UxmlBoolAttributeDescription { name = "enable-debug", defaultValue = false };

            public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(visualElement, bag, cc);

                var contentPresenter = (ContentPresenter)visualElement;

                contentPresenter.ConfigMethod = m_configMethod.GetValueFromBag(bag, cc);
                contentPresenter.EnableDebug = m_enableDebug.GetValueFromBag(bag, cc);
                contentPresenter.Template = m_template.GetValueFromBag(bag, cc);
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        public static Func<string, VisualTreeAsset> DefaultLoadAsset = typeName => Resources.Load<VisualTreeAsset>($@"Templates/{typeName}");

        public string bindingPath;

        private SerializedProperty boundProperty;
        private VisualTreeAsset visualTreeAsset;

        public bool EnableDebug { get; set; }
        public string ConfigMethod { get; set; }
        public string Template { get; set; }

        public Func<string, VisualTreeAsset> LoadAsset;

        public Action<VisualElement> Configure;

        Regex reg_pptr = new Regex(".*PPtr<\\$(.*?)>", RegexOptions.Compiled);
        Regex reg_managedReference = new Regex(".*managedReference.*", RegexOptions.Compiled);
        public ContentPresenter()
        {
            LoadAsset = DefaultLoadAsset;
            name = $"content-presenter";
        }

        private void Reset(SerializedObject boundObject)
        {
            if (boundObject == null) return;
            Clear();

            if (!string.IsNullOrEmpty(Template))
                visualTreeAsset = LoadAsset(Template);

            if (visualTreeAsset == null)
            {
                string dataType = boundObject.targetObject.GetType().Name;
                boundProperty = GetProperty(boundObject);

                if (boundProperty != null)
                {
                    switch (boundProperty.propertyType)
                    {
                        case SerializedPropertyType.ObjectReference:
                            dataType = boundProperty.objectReferenceValue.GetType().Name;
                            break;
                        default:
                            switch (boundProperty.type)
                            {
#if UNITY_2020_1_OR_NEWER
                                case var type when reg_managedReference.IsMatch(type):
                                var mrft = boundProperty.managedReferenceFullTypename;
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
                            break;
#elif UNITY_2018
#endif
                                default:
                                    if (dataType == null) break;
                                    dataType = dataType.Substring(dataType.LastIndexOf('.') + 1);
                                    break;
                            }
                            break;
                    }

                }
                visualTreeAsset = LoadAsset(dataType);

                if (visualTreeAsset == null)
                {
                    //in order to maintain our index we must add a visual element even if we don't find a template for the item.
                    var error = new Label($"Template file not found: BoundType({boundObject.targetObject.GetType().FullName}) Template({dataType}.uxml)");
                    error.AddToClassList("template-error");
                    Add(error);

                    return;
                }
            }


#if UNITY_2019_1_OR_NEWER
            visualTreeAsset.CloneTree(this);
#elif UNITY_2018_1_OR_NEWER
            visualTreeAsset.CloneTree(this, null);

            var ussPath = AssetDatabase.GetAssetPath(visualTreeAsset).Replace(".uxml", ".uss");
            var ussDarkPath = ussPath.Replace(".uss", "_Dark.uss");
            var ussLghtPath = ussPath.Replace(".uss", "_Light.uss");
            var defaultSheet = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ussPath);
            var darkSheet = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ussDarkPath);
            var lghtSheet = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ussLghtPath);

            if (defaultSheet) AddStyleSheetPath(ussPath);
            
            if (EditorGUIUtility.isProSkin && darkSheet)
                AddStyleSheetPath(ussDarkPath);
            else if (lghtSheet)
                AddStyleSheetPath(ussLghtPath);
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
                        void Invoke(VisualElement visualElement) => methodInfo.Invoke(ud, args);
                        Configure = Invoke;
                    }
                }
            }
            try
            {
                Configure?.Invoke(this);
            }
            catch(Exception e) { Debug.LogError(e); }
        }

        private SerializedProperty GetProperty(SerializedObject boundObject)
        {
            if (bindingPath == null) return boundObject.FindProperty("");
            var property = boundObject.FindProperty(bindingPath);
            if (property == null)
            {
                string propertyPath = this.GetBindingPath();
                property = boundObject.FindProperty($"{propertyPath}.{bindingPath}");
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
                    var boundObject = bindObjectProperty.GetValue(evt) as SerializedObject;
                    Reset(boundObject);
                    if (typeof(UnityEngine.Object).IsAssignableFrom(boundObject.targetObject.GetType()))
                        BindRecursive(this, boundObject);
                    break;

                default:
                    break;
            }
        }

        void BindRecursive(VisualElement element, SerializedObject boundObject)
        {
            foreach (var child in element.Children())
            {
                child.Bind(boundObject);
                if (child is ContentPresenter) continue;
                if (child is ItemsControl) continue;
                BindRecursive(child, boundObject);
            }
        }

    }
}