using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public class TypeSuggestOptions : SuggestOptions
    {
        private static Type[] AllTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes()).Distinct().ToArray();

        private static Dictionary<string, IEnumerable<SuggestOption>> rootOnlyLookup = new Dictionary<string, IEnumerable<SuggestOption>>();
        private static Dictionary<string, IEnumerable<SuggestOption>> descendantIncludedLookup = new Dictionary<string, IEnumerable<SuggestOption>>();
        private string types;

        public override IEnumerable<SuggestOption> Options
        {
            get
            {
                if (IncludeDescendants && descendantIncludedLookup.ContainsKey(Types)) return descendantIncludedLookup[Types];
                else if (rootOnlyLookup.ContainsKey(Types)) return rootOnlyLookup[Types];
                else return Array.Empty<SuggestOption>();
            }
        }

        public new class UxmlFactory : UxmlFactory<TypeSuggestOptions, UxmlTraits> { }

        public string Types
        {
            get => types;
            set
            {
                types = value;
                UpdateCache();
            }
        }
        public bool IncludeDescendants { get; set; }

        private void UpdateCache()
        {
            var typeStrings = Types.Split(',');

            if (IncludeDescendants)
            {
                if (!descendantIncludedLookup.ContainsKey(Types))
                    descendantIncludedLookup[Types] = typeStrings
                        .Select(ts => AllTypes.FirstOrDefault(t => t.FullName == ts))
                        .Where(t => t != null && t.IsPublic /*&& !t.IsInterface*/)
                        .SelectMany(t => AllTypes.Where(at => t.IsAssignableFrom(at)))
                        .Where(t => !t.IsAbstract)
                        .Select(at => new SuggestOption { DisplayName = at.Name, Data = at })
                        .ToArray();
            }
            else
            {
                if (!rootOnlyLookup.ContainsKey(Types))
                    rootOnlyLookup[Types] = typeStrings
                        .Select(ts => AllTypes.FirstOrDefault(t => t.FullName == ts))
                        .Where(t => t != null && t.IsPublic && !t.IsAbstract /*&& !t.IsInterface*/)
                        .Select(at => new SuggestOption { DisplayName = at.Name, Data = at })
                        .ToArray();
            }
        }

        public new class UxmlTraits : SuggestOptions.UxmlTraits
        {
            UxmlStringAttributeDescription m_types = new UxmlStringAttributeDescription { name = "types", use = UxmlAttributeDescription.Use.Required };
            UxmlBoolAttributeDescription m_descendants = new UxmlBoolAttributeDescription { name = "include-descendants" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                Profiler.BeginSample("CreateTypeSuggestOption");
                base.Init(ve, bag, cc);

                var tso = (TypeSuggestOptions)ve;
                tso.IncludeDescendants = m_descendants.GetValueFromBag(bag, cc);
                tso.Types = m_types.GetValueFromBag(bag, cc);
                tso.UpdateCache();

                Profiler.EndSample();
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

        }
    }
}