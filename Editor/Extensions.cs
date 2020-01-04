using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace VisualTemplates
{
    public static class VisualTreeHelpers
    {

        public static IEnumerable<VisualElement> Parents(this VisualElement element)
        {
            var parent = element;
            while (parent.parent != null)
            {
                parent = parent.parent;
                yield return parent;
            }
        }

        public static string GetBindingPath(this BindableElement element) =>
            element.Parents().OfType<BindableElement>().Where(be => !string.IsNullOrEmpty(be.bindingPath)).First().bindingPath;
    }
}