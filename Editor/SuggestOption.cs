﻿using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public class SuggestOption : VisualElement
    {
        public string DisplayName { get; set; }

        public object Data { get; set; }
        public new class UxmlFactory : UxmlFactory<SuggestOption, UxmlTraits> { }
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_displayName = new UxmlStringAttributeDescription { name = "display-name" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var suggestOption = (SuggestOption)ve;

                suggestOption.DisplayName = m_displayName.GetValueFromBag(bag, cc);
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }
}