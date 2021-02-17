using UnityEngine;
using UnityEditor;

#if UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#elif UNITY_2018
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace VisualTemplates
{
    public abstract class SuggestOptions : VisualElement
    {
        public virtual IEnumerable<SuggestOption> Options => Children().OfType<SuggestOption>();
        public new class UxmlFactory : UxmlFactory<SuggestOptions, UxmlTraits> { }
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get
                {
                    yield return new UxmlChildElementDescription(typeof(SuggestOption));
                    yield break;
                }
            }
        }
    }
}