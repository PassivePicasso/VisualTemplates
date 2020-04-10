#if UNITY_2020
using System;
using UnityEditor;
using UnityEngine;

using UnityEditor.UIElements;
using UnityEngine.UIElements;


namespace VisualTemplates
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class AutoEditor : Editor
    {
        /// <summary>
        /// This is how we setup how we want to access uxml files, I decided to use Resources, but you could probably replace this with AssetReferences or use the AssetDatabase.
        /// The premise has been on using types to define templates, and idea taken from WPF's DataTemplates.

        /// The only standard Unity call, here we are just going to create an AutoTemplate, which is the base for the entire system.
        /// It should be assumed that all controls in VisualTemplates require that AutoTemplate is an ancestor in the VisualTree
        /// </summary>
        public sealed override VisualElement CreateInspectorGUI()
        {
            if (LoadAsset(target.GetType().Name) == null) return null;
            else
                return CreateContentPresenter();
        }

        public virtual VisualTreeAsset LoadAsset(string typeName) => Resources.Load<VisualTreeAsset>($@"Templates/{typeName}");

        public virtual ContentPresenter CreateContentPresenter() => new ContentPresenter() { userData = this, LoadAsset = LoadAsset };
    }

}
#endif
