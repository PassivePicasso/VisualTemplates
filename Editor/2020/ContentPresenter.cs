#if UNITY_2020
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public partial class ContentPresenter
    {
        public new class UxmlFactory : UxmlFactory<ContentPresenter, UxmlTraits> { }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private UxmlStringAttributeDescription m_configMethod = new UxmlStringAttributeDescription { name = "config-method" };

            public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(visualElement, bag, cc);

                var contentPresenter = (ContentPresenter)visualElement;

                contentPresenter.ConfigMethod = m_configMethod.GetValueFromBag(bag, cc);
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }
    }
}
#endif
