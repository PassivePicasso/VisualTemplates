using System;
using System.Collections.Generic;
using UnityEditor;

#if UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#elif UNITY_2018
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace VisualTemplates
{
    public class ItemsControl : BindableElement
    {
        private SerializedObject boundObject;

        public bool EnableDebug { get; set; }
        public string ConfigMethod { get; set; }
        public string Template { get; set; }

        public Func<SerializedProperty, bool> makeItem;

        public ItemsControl()
        {
            name = $"content-presenter";
        }

        private void Reset(SerializedObject boundObject)
        {
            if (boundObject == null) return;
            Clear();

            this.boundObject = boundObject;
            var boundArray = GetArray(boundObject);
            if (boundArray == null) return;

            var size = boundArray.arraySize;
            for (int i = 0; i < size; i++)
            {
                var p = boundArray.GetArrayElementAtIndex(i);
                VisualElement child = MakeItem(boundObject, p);
                Add(child);
                child.Bind(boundObject);
            }
        }

        private VisualElement MakeItem(SerializedObject boundObject, SerializedProperty property)
        {
            var itemContainer = new VisualElement();
            itemContainer.name = $"item-{childCount}";
            itemContainer.AddToClassList("items-control-item");

            ContentPresenter contentPresenter = new ContentPresenter { bindingPath = property.propertyPath, ConfigMethod = ConfigMethod, EnableDebug = EnableDebug, Template = Template };

            void DeleteItem()
            {
                var boundArray = GetArray(boundObject);
                if (boundArray == null) return;
                int index = IndexOf(itemContainer);
                boundArray.DeleteArrayElementAtIndex(index);
                itemContainer.RemoveFromHierarchy();
                boundObject.ApplyModifiedProperties();
            }

            itemContainer.Add(contentPresenter);

            itemContainer.Add(new Button
            {
                text = "X",
                clickable = new Clickable(DeleteItem),
                name = "items-control-item-delete"
            });

            itemContainer.userData = FindAncestorUserData();

            return itemContainer;
        }

        private SerializedProperty GetArray(SerializedObject boundObject)
        {
            var boundArray = boundObject.FindProperty(bindingPath);
            if (boundArray == null)
            {
                string propertyPath = this.GetBindingPath();
                boundArray = boundObject.FindProperty($"{propertyPath}.{bindingPath}");
            }

            return boundArray;
        }

        public void AddItem<T>(T data, Action<SerializedProperty, T> assignData = null)
        {
            var boundArray = GetArray(boundObject);
            if (boundArray == null) return;
            var cdsp = boundArray.GetArrayElementAtIndex(boundArray.arraySize++);

            if (assignData != null)
                assignData.Invoke(cdsp, data);

            boundArray.serializedObject.SetIsDifferentCacheDirty();

            boundArray.serializedObject.ApplyModifiedProperties();

            var item = MakeItem(boundArray.serializedObject, cdsp);

            Add(item);

            item.Bind(boundObject);
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

        public new class UxmlFactory : UxmlFactory<ItemsControl, UxmlTraits> { }
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_template = new UxmlStringAttributeDescription { name = "template" };
            private UxmlStringAttributeDescription m_configMethod = new UxmlStringAttributeDescription { name = "config-method" };
            private UxmlBoolAttributeDescription m_enableDebug = new UxmlBoolAttributeDescription { name = "enable-debug", defaultValue = false };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var itemsControl = (ItemsControl)ve;

                itemsControl.ConfigMethod = m_configMethod.GetValueFromBag(bag, cc);
                itemsControl.EnableDebug = m_enableDebug.GetValueFromBag(bag, cc);
                itemsControl.Template = m_template.GetValueFromBag(bag, cc);
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }
}