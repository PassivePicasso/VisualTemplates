using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public class ItemsControl : BindableElement
    {
        private SerializedObject boundObject;

        public string ConfigMethod { get; set; }

        public Func<SerializedProperty, bool> makeItem;

        public ItemsControl()
        {
            name = $"content-presenter";
            RegisterCallback<AttachToPanelEvent>(OnAttached);
        }

        private void OnAttached(AttachToPanelEvent evt) => Reset();

        private void Reset()
        {
            if (boundObject == null) return;
            Clear();

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

            ContentPresenter contentPresenter = new ContentPresenter { bindingPath = property.propertyPath, ConfigMethod = ConfigMethod };

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

            boundArray.serializedObject.ApplyModifiedProperties();

            var item = MakeItem(boundArray.serializedObject, cdsp);

            Add(item);

            item.Bind(boundObject);
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            var bindObjectProperty = evt.GetType().GetProperty("bindObject");
            if (bindObjectProperty == null)
                return;

            if (boundObject == null)
            {
                boundObject = bindObjectProperty.GetValue(evt) as SerializedObject;
                //Reset();
            }
        }

        public new class UxmlFactory : UxmlFactory<ItemsControl, UxmlTraits> { }
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_configMethod = new UxmlStringAttributeDescription { name = "config-method" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var itemsControl = (ItemsControl)ve;

                itemsControl.ConfigMethod = m_configMethod.GetValueFromBag(bag, cc);
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }
}