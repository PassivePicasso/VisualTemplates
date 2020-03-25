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
                if (IncludeDescendants)
                    return descendantIncludedLookup.Where(kvp => Types.Contains(kvp.Key)).SelectMany(kvp => kvp.Value);
                
                else if (rootOnlyLookup.ContainsKey(Types)) 
                    return rootOnlyLookup.Where(kvp => Types.Contains(kvp.Key)).SelectMany(kvp => kvp.Value);

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
            var typeNames = Types.Split(',');

            if (IncludeDescendants)
            {
                foreach (var typeName in typeNames)
                {
                    if (descendantIncludedLookup.ContainsKey(typeName)) continue;

                    var type = AllTypes.FirstOrDefault(t => t.IsPublic && t.FullName == typeName);
                    if (type == null) continue;

                    var assignables = AllTypes.Where(t => type.IsAssignableFrom(t) && !t.IsAbstract);
                    var suggestOptions = assignables.Select(at => new SuggestOption { DisplayName = at.Name, Data = at });

                    if (!type.IsAbstract)
                        suggestOptions.Append(new SuggestOption { DisplayName = type.Name, Data = type });

                    descendantIncludedLookup[typeName] = suggestOptions.ToArray();
                }
            }
            else
            {
                foreach (var typeName in typeNames)
                {
                    if (rootOnlyLookup.ContainsKey(typeName)) continue;
                    var type = AllTypes.FirstOrDefault(t => t.FullName == typeName && t.IsPublic && !t.IsAbstract);
                    rootOnlyLookup[typeName] = new[] { new SuggestOption { DisplayName = type.Name, Data = type } };
                }
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