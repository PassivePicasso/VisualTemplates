using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public class ContentPresenter : BindableElement
    {
        private static readonly Type[] methodSignature = new[] { typeof(VisualElement) };
        private static readonly object[] methodDataArray = new object[1];
        private SerializedObject boundObject;
        private SerializedProperty boundProperty;

        public string ConfigMethod { get; set; }

        private MethodInfo configMethod;
        private VisualTreeAsset visualTreeAsset;
        private AutoEditor editor;

        public ContentPresenter()
        {
            name = $"content-presenter";
            RegisterCallback<AttachToPanelEvent>(OnAttached);
        }
        private void OnAttached(AttachToPanelEvent evt) => Reset();

        private void Reset()
        {
            if (boundObject == null) return;
            Clear();

            editor = (userData ?? FindAncestorUserData()) as AutoEditor;
            if (editor != null && !string.IsNullOrEmpty(ConfigMethod) && configMethod == null)
                configMethod = editor.GetType().GetMethod(ConfigMethod, methodSignature);

            if (editor == null) return;

            string dataType = boundObject.targetObject.GetType().Name;
            boundProperty = GetProperty(boundObject);

            if (boundProperty == null) visualTreeAsset = editor.LoadAsset(dataType);
            else
            {
                var property = boundProperty.Copy();

                dataType = property.type.Substring(property.type.IndexOf(" ") + 1);
                if (dataType?.StartsWith("managedReference") ?? false)
                {
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
                        treeAsset = editor.LoadAsset(parentType.Name);
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
                        visualTreeAsset = editor.LoadAsset(dataType);
                    }
                }
                else
                {
                    if (dataType == null)
                        return;

                    dataType = dataType.Substring(dataType.LastIndexOf('.') + 1);

                    visualTreeAsset = editor.LoadAsset(dataType);
                }
            }

            if (visualTreeAsset == null)
            {
                //in order to maintain our index we must add a visual element even if we don't find a template for the item.
                Label error = new Label($"Template file not found: {dataType}.uxml");
                error.AddToClassList("template-error");

                Add(error);
                return;
            }

            visualTreeAsset.CloneTree(this);

            methodDataArray[0] = this;

            configMethod?.Invoke(editor, methodDataArray);
            if (typeof(UnityEngine.Object).IsAssignableFrom(boundObject.targetObject.GetType()))
                this.Bind(boundObject);
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

            var bindObjectProperty = evt.GetType().GetProperty("bindObject");
            if (bindObjectProperty == null)
                return;

            if (boundObject == null)
                boundObject = bindObjectProperty.GetValue(evt) as SerializedObject;
        }

        public new class UxmlFactory : UxmlFactory<ContentPresenter, UxmlTraits> { }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_configMethod = new UxmlStringAttributeDescription { name = "config-method" };

            public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(visualElement, bag, cc);

                var contentPresenter = (ContentPresenter)visualElement;

                contentPresenter.ConfigMethod = m_configMethod.GetValueFromBag(bag, cc);
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }
}