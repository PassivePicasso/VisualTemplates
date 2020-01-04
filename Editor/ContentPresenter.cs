using System;
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
        static readonly Type[] methodSignature = new[] { typeof(VisualElement) };

        static readonly object[] methodDataArray = new object[1];

        SerializedObject boundObject;
        SerializedProperty boundProperty;

        private string configMethodName;
        public Func<SerializedProperty, bool> makeItem;
        MethodInfo configMethod;
        AutoTemplate templateRoot;
        VisualTreeAsset visualTreeAsset;

        public string DataType { get; private set; }

        public ContentPresenter()
        {
            name = $"content-presenter";
            RegisterCallback<AttachToPanelEvent>(OnAttached);
        }

        private void OnAttached(AttachToPanelEvent evt)
        {
            if (templateRoot == null)
                templateRoot = this.Parents().OfType<AutoTemplate>().FirstOrDefault();

            if (templateRoot == null)
            {
                Debug.LogError("Could not find ancestor AutoTemplate in VisualTree");
            }
            else
            {
                Reset();
            }
        }


        private void Reset()
        {
            if (boundObject == null || templateRoot == null) return;
            Clear();

            boundProperty = GetProperty(boundObject);
            if (boundProperty == null) return;

            var property = boundProperty.Copy();

            if (!string.IsNullOrEmpty(configMethodName))
            {
                if (templateRoot?.editor != null)
                    if (configMethod == null)
                        configMethod = templateRoot.editor.GetType().GetMethod(configMethodName, methodSignature);
            }

            if (templateRoot == null) return;

            var dataType = property.type;

            if (dataType?.StartsWith("managedReference") ?? false)
            {
                dataType = property.managedReferenceFullTypename;
                dataType = dataType.Substring(dataType.LastIndexOf('.') + 1);
            }

            if (dataType == null)
                return;

            if (DataType != dataType)
                visualTreeAsset = templateRoot.AssetLoader.Invoke(dataType);

            var propertyPath = property.propertyPath;
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

            configMethod?.Invoke(templateRoot.editor, methodDataArray);

            this.Bind(boundObject);
        }

        private SerializedProperty GetProperty(SerializedObject boundObject)
        {
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

        public new class UxmlFactory : UxmlFactory<ContentPresenter, UxmlTraits>
        {
            UxmlStringAttributeDescription m_configMethod = new UxmlStringAttributeDescription { name = "config-method" };

            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var itemsControl = (ContentPresenter)base.Create(bag, cc);

                itemsControl.configMethodName = m_configMethod.GetValueFromBag(bag, cc);

                return itemsControl;
            }
        }

        public new class UxmlTraits : BindableElement.UxmlTraits { }
    }
}