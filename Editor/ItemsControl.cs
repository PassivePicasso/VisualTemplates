using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public class ItemsControl : BindableElement
    {
        static readonly Type[] methodSignature = new[] { typeof(VisualElement) };

        static readonly object[] methodDataArray = new object[1];

        SerializedObject boundObject;
        SerializedProperty boundArray;
        private int lastArraySize;

        private string configMethodName;
        public Func<SerializedProperty, bool> makeItem;
        MethodInfo configMethod;
        Editor editor;

        public ItemsControl()
        {
        }

        private void Reset(SerializedObject boundObject)
        {
            Clear();

            boundArray = GetArray(boundObject);
            if (boundArray == null) return;

            var property = boundArray.Copy();
            property.NextVisible(true);
            var arraySize = property.Copy();

            if (!string.IsNullOrEmpty(configMethodName))
            {
                if (editor == null)
                    editor = this.Parents().OfType<AutoTemplate>().First().editorHost;
                if (configMethod == null)
                {
                    configMethod = editor.GetType().GetMethod(configMethodName, methodSignature);
                }
            }

            int i = 0;
            do
            {
                var itemResult = MakeItem(property);

                if (itemResult.terminate)
                    break;

                if (itemResult.madeItem && childCount >= arraySize.intValue)
                    break;

            } while (property.NextVisible(false));

            lastArraySize = arraySize.intValue;
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

            MakeItem(cdsp, true);
        }

        (bool madeItem, bool terminate) MakeItem(SerializedProperty property, bool bind = false)
        {
            var dataType = property.type;

            if (dataType.StartsWith("managedReference"))
            {
                dataType = property.managedReferenceFullTypename;
                dataType = dataType.Substring(dataType.LastIndexOf('.') + 1);
            }
            if (dataType == "ArraySize")
                if (property.intValue == 0)
                    return (false, true);
                else return (false, false);

            if (dataType == null)
                return (false, true);

            var visualTreeAsset = VisualTemplateSettings.TemplateLoader.Invoke(dataType);

            var itemContainer = new VisualElement();
            itemContainer.name = $"items-control-item-{childCount}";
            itemContainer.AddToClassList("items-control-item");

            void DeleteItem()
            {
                int index = IndexOf(itemContainer);
                boundArray.DeleteArrayElementAtIndex(index);
                itemContainer.RemoveFromHierarchy();
                boundObject.ApplyModifiedProperties();
            }

            var propertyPath = property.propertyPath;
            if (visualTreeAsset == null)
            {
                //in order to maintain our index we must add a visual element even if we don't find a template for the item.
                Label error = new Label($"Template file not found: {dataType}.uxml");
                error.AddToClassList("template-error");

                itemContainer.Add(error);
                itemContainer.Add(new PropertyField { bindingPath = $"{propertyPath}" });
            }
            else
            {
                var templateContainer = visualTreeAsset.Instantiate();

                templateContainer.bindingPath = $"{propertyPath}";
                methodDataArray[0] = templateContainer;
                configMethod?.Invoke(editor, methodDataArray);

                itemContainer.Add(templateContainer);
            }

            itemContainer.Add(new Button
            {
                text = "X",
                clickable = new Clickable(DeleteItem),
                name = "items-control-item-delete"
            });

            Add(itemContainer);

            if (bind)
                itemContainer.Bind(boundObject);

            return (true, false);

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
                Reset(boundObject);
            }
        }

        public new class UxmlFactory : UxmlFactory<ItemsControl, UxmlTraits>
        {
            UxmlStringAttributeDescription m_configMethod = new UxmlStringAttributeDescription { name = "config-method" };

            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var itemsControl = (ItemsControl)base.Create(bag, cc);

                itemsControl.configMethodName = m_configMethod.GetValueFromBag(bag, cc);

                return itemsControl;
            }
        }

        public new class UxmlTraits : BindableElement.UxmlTraits { }
    }
}