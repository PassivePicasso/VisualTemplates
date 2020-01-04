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
    public class ItemsControl : BindableElement, INotifyValueChanged<string>
    {
        static readonly Type[] methodSignature = new[] { typeof(VisualElement) };

        static readonly object[] methodDataArray = new object[1];

        SerializedObject boundObject;
        SerializedProperty boundArray;
        private int lastArraySize;

        [SerializeField]
        private string _configMethodName;
        public string configMethodName
        {
            get { return ((INotifyValueChanged<string>)this).value; }
            set
            {
                ((INotifyValueChanged<string>)this).value = value;
            }
        }

        public Func<SerializedProperty, bool> makeItem;
        MethodInfo configMethod;
        AutoTemplate templateRoot;

        string INotifyValueChanged<string>.value
        {
            get
            {
                return _configMethodName ?? String.Empty;
            }

            set
            {
                if (_configMethodName != value)
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(this._configMethodName, value))
                        {
                            evt.target = this;
                            ((INotifyValueChanged<string>)this).SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        ((INotifyValueChanged<string>)this).SetValueWithoutNotify(value);
                    }
                }
            }
        }

        public void SetValueWithoutNotify(string newValue)
        {
            _configMethodName = newValue;
        }

        public ItemsControl()
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

            boundArray = GetArray(boundObject);
            if (boundArray == null) return;

            var property = boundArray.Copy();
            property.NextVisible(true);
            var arraySize = property.Copy();
            
            if (!string.IsNullOrEmpty(_configMethodName)
             && configMethod == null
             && templateRoot?.editor != null)
                configMethod = templateRoot.editor.GetType().GetMethod(_configMethodName, methodSignature);

            while (property.NextVisible(false))
            {
                VisualElement child = MakeItem(boundObject, property);
                Add(child);
                child.Bind(boundObject);

                if (childCount >= arraySize.intValue)
                    break;
            }
        }

        private VisualElement MakeItem(SerializedObject boundObject, SerializedProperty property)
        {
            var itemContainer = new VisualElement();
            itemContainer.name = $"items-control-item-{childCount}";
            itemContainer.AddToClassList("items-control-item");

            ContentPresenter contentPresenter = new ContentPresenter { bindingPath = property.propertyPath };

            void DeleteItem()
            {
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

            methodDataArray[0] = contentPresenter;
            configMethod?.Invoke(templateRoot.editor, methodDataArray);

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
            int arraySize = boundArray.arraySize;
            boundArray.InsertArrayElementAtIndex(arraySize);
            var cdsp = boundArray.GetArrayElementAtIndex(arraySize);

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
                Reset();
            }
        }

        public new class UxmlFactory : UxmlFactory<ItemsControl, UxmlTraits> { }
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_configMethod = new UxmlStringAttributeDescription { name = "config-method" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var itemsControl = (ItemsControl)ve;

                itemsControl.configMethodName = m_configMethod.GetValueFromBag(bag, cc);
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }
}