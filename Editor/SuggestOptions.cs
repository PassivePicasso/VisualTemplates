using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
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