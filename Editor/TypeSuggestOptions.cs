using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public class TypeSuggestOptions : SuggestOptions
    {
        private static Type[] AllTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes()).ToArray();

        private static Dictionary<string, IEnumerable<SuggestOption>> rootOnlyLookup = new Dictionary<string, IEnumerable<SuggestOption>>();
        private static Dictionary<string, IEnumerable<SuggestOption>> descendantIncludedLookup = new Dictionary<string, IEnumerable<SuggestOption>>();

        public override IEnumerable<SuggestOption> Options { get; protected set; }
        public new class UxmlFactory : UxmlFactory<TypeSuggestOptions, UxmlTraits> { }

        public new class UxmlTraits : SuggestOptions.UxmlTraits
        {
            UxmlStringAttributeDescription m_types = new UxmlStringAttributeDescription { name = "types", use = UxmlAttributeDescription.Use.Required };
            UxmlBoolAttributeDescription m_descendants = new UxmlBoolAttributeDescription { name = "include-descendants" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                Profiler.BeginSample("CreateTypeSuggestOption");
                base.Init(ve, bag, cc);

                string typeString = m_types.GetValueFromBag(bag, cc);
                bool includeDescendants = m_descendants.GetValueFromBag(bag, cc);
                var typeStrings = typeString.Split(',');

                if (includeDescendants)
                {
                    if (!descendantIncludedLookup.ContainsKey(typeString))
                        descendantIncludedLookup[typeString] = typeStrings
                            .Select(ts => AllTypes.FirstOrDefault(t => t.FullName == ts))
                            .Where(t => t != null && t.IsPublic)
                            .SelectMany(t => AllTypes.Where(at => t.IsAssignableFrom(at)))
                            .Distinct()
                            .Select(at => new SuggestOption { Name = at.Name, data = at })
                            .ToArray();
                }
                else
                {
                    if (!rootOnlyLookup.ContainsKey(typeString))
                        rootOnlyLookup[typeString] = typeStrings
                            .Select(ts => AllTypes.FirstOrDefault(t => t.FullName == ts))
                            .Where(t => t != null && t.IsPublic)
                            .Select(at => new SuggestOption { Name = at.Name, data = at })
                            .ToArray();
                }

                ((TypeSuggestOptions)ve).Options = includeDescendants ? descendantIncludedLookup[typeString] : rootOnlyLookup[typeString];

                Profiler.EndSample();
            }


            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

        }
    }
}