using UnityEngine;
using UnityEditor;

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

using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace VisualTemplates
{
    public abstract class SuggestOptions : VisualElement
    {
        public abstract IEnumerable<SuggestOption> Options { get; }
    }
}