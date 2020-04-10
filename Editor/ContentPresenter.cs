using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

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
    public partial class ContentPresenter : BindableElement
    {
        private static Func<string, VisualTreeAsset> DefaultLoadAsset = typeName => Resources.Load<VisualTreeAsset>($@"Templates/{typeName}");
        private static readonly Type[] methodSignature = new[] { typeof(VisualElement) };
        private static readonly object[] methodDataArray = new object[1];

        private SerializedObject boundObject;
        private SerializedProperty boundProperty;
        private VisualTreeAsset visualTreeAsset;

        public string ConfigMethod { get; set; }
        public Func<string, VisualTreeAsset> LoadAsset;
        public Action<VisualElement> Configure;
        public ContentPresenter()
        {
            LoadAsset = DefaultLoadAsset;
            name = $"content-presenter";
            RegisterCallback<AttachToPanelEvent>(OnAttached);
        }
        private void OnAttached(AttachToPanelEvent evt) => Reset();

        private void Reset()
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
                Label error = new Label($"Template file not found: {dataType}.uxml");
                error.AddToClassList("template-error");

                Add(error);
                return;
            }

#if UNITY_2020
            visualTreeAsset.CloneTree(this);
#elif UNITY_2018
            visualTreeAsset.CloneTree(this, null);
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
                this.Bind(boundObject);
        }

        private SerializedProperty GetProperty(SerializedObject boundObject)
        {
            if (bindingPath == null) return boundObject.FindProperty("");
            var property = boundObject.FindProperty(bindingPath);
            if (property == null)
            {
#if UNITY_2020
                string propertyPath = this.GetBindingPath();
#elif UNITY_2018
                string propertyPath = this.bindingPath;
#endif
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
    }
}