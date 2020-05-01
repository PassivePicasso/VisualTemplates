using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#elif UNITY_2018
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace VisualTemplates
{
    public class SearchSuggest : VisualElement
    {
        internal enum ShowMode
        {
            // Show as a normal window with max, min & close buttons.
            NormalWindow = 0,
            // Used for a popup menu. On mac this means light shadow and no titlebar.
            PopupMenu = 1,
            // Utility window - floats above the app. Disappears when app loses focus.
            Utility = 2,
            // Window has no shadow or decorations. Used internally for dragging stuff around.
            NoShadow = 3,
            // The Unity main window. On mac, this is the same as NormalWindow, except window doesn't have a close button.
            MainWindow = 4,
            // Aux windows. The ones that close the moment you move the mouse out of them.
            AuxWindow = 5,
            // Like PopupMenu, but without keyboard focus
            Tooltip = 6,
            // Modal Utility window
            ModalUtility = 7
        }

        public new class UxmlFactory : UxmlFactory<SearchSuggest, UxmlTraits>
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get
                {
                    yield return new UxmlChildElementDescription(typeof(SuggestOption));
                    yield return new UxmlChildElementDescription(typeof(SuggestOptions));
                }
            }

            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var searchSuggest = (SearchSuggest)base.Create(bag, cc);

                return searchSuggest;
            }
        }

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public delegate void SuggestionSelected(SuggestOption pickedSuggestion);

        public event SuggestionSelected OnSuggestedSelected;

        public List<SuggestOption> MatchedSuggestOption { get; set; }
        private Func<SuggestOption, bool> matchingSuggestOptions;
        public SuggestOption[] SuggestOption { get; set; }

        private EditorWindow popupWindow;
        private ListView optionList;
        private ToolbarSearchField textEntry;

        private PropertyInfo ownerObjectProperty;
        private PropertyInfo screenPositionProperty;
        private MethodInfo showPopupNonFocus;
        private object[] showValueArray;
        private object ownerObject;
        private object showValue;

        private bool hasFocus = false;
        private bool popupVisible = false;
        private Rect popupPosition;

        public SearchSuggest()
        {
            AddToClassList("search-suggest");

            textEntry = new ToolbarSearchField { name = "search-suggest-input" };
            MatchedSuggestOption = new List<SuggestOption>();

            ConfigureOptionList();


            textEntry.style.flexGrow = 1;

            matchingSuggestOptions = suggestOption => suggestOption.DisplayName.ToLower().Contains(textEntry.value.ToLower());

            RegisterCallback<AttachToPanelEvent>(OnAttached);
            RegisterCallback<DetachFromPanelEvent>(OnDetached);

            Add(textEntry);
        }

        private void CreateNewWindow()
        {
            if (popupWindow == null)
            {
                popupWindow = ScriptableObject.CreateInstance<EditorWindow>();
#if UNITY_2020
                popupWindow.rootVisualElement.hierarchy
#elif UNITY_2018
                popupWindow.GetRootVisualContainer()
#endif
                    .Add(optionList);
            }
        }

        private void ConfigureOptionList()
        {
            if (optionList == null)
            {
                optionList = new ListView { name = "search-suggest-list", itemHeight = 20 };

                optionList.makeItem = () =>
                {
                    var label = new Label();
                    label.AddToClassList("suggestion");
                    label.RegisterCallback<MouseDownEvent>(OnLabelMouseDown);

                    return label;
                };
                optionList.bindItem = (v, i) =>
                {
                    Label label = v as Label;
                    var suggestOption = (SuggestOption)optionList.itemsSource[i];

                    label.text = suggestOption.DisplayName;
                    label.userData = suggestOption;
                };
                optionList.selectionType = SelectionType.Single;
                OppahOptionStyle(optionList);
            }
        }

        private void OppahOptionStyle(VisualElement element)
        {
#if UNITY_2020
            element.style.left = 0;
            element.style.right = 0;
#elif UNITY_2018
            element.style.positionLeft = 0;
            element.style.positionRight = 0;
#endif
            element.style.height = 100;
            element.style.backgroundColor = Color.Lerp(Color.gray, Color.white, 0.5f);

            element.style.borderTopWidth =
            element.style.borderLeftWidth =
            element.style.borderRightWidth =
            element.style.borderBottomWidth = 1;

#if UNITY_2020
            element.style.borderTopColor =
            element.style.borderLeftColor =
            element.style.borderRightColor =
            element.style.borderBottomColor
#elif UNITY_2018

            element.style.borderColor
#endif
                = Color.Lerp(Color.gray, Color.black, 0.3f);
        }

        private void OnDetached(DetachFromPanelEvent evt)
        {

#if UNITY_2020
            textEntry.UnregisterValueChangedCallback(OnTextChanged);
#elif UNITY_2018
            textEntry.RemoveOnValueChanged(OnTextChanged);
#endif
            textEntry.UnregisterCallback<FocusOutEvent>(OnLostFocus);
            textEntry.UnregisterCallback<FocusInEvent>(OnGainedFocus);
            textEntry.UnregisterCallback<KeyDownEvent>(OnKeyDown);

            Cleanup();
        }

        private void OnAttached(AttachToPanelEvent evt)
        {
            ownerObjectProperty = evt.destinationPanel.GetType().GetProperty("ownerObject");
            ownerObject = ownerObjectProperty.GetValue(evt.destinationPanel);

            screenPositionProperty = ownerObject.GetType().GetProperty("screenPosition");

            var showMode = typeof(EditorWindow).Assembly.GetType("UnityEditor.ShowMode");

            showPopupNonFocus = typeof(EditorWindow).GetMethod("ShowPopupWithMode", BindingFlags.Instance | BindingFlags.NonPublic);

            showValue = Enum.GetValues(showMode).GetValue((int)ShowMode.Tooltip);
            showValueArray = new[] { showValue, false };

            var suggestOptions = Children().OfType<SuggestOptions>().SelectMany(sos => sos.Options)
                          .Union(Children().OfType<SuggestOption>()).ToArray();

            SuggestOption = suggestOptions;


#if UNITY_2020
            textEntry.RegisterValueChangedCallback(OnTextChanged);
#elif UNITY_2018
            textEntry.OnValueChanged(OnTextChanged);
#endif
            textEntry.RegisterCallback<FocusOutEvent>(OnLostFocus);
            textEntry.RegisterCallback<FocusInEvent>(OnGainedFocus);
            textEntry.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
            textEntry.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {

            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    var suggestOption = MatchedSuggestOption[optionList.selectedIndex];
                    OnSuggestedSelected?.Invoke(suggestOption);
                    textEntry.SetValueWithoutNotify("");
                    hasFocus = false;
                    UpdateVisibility();
                    return;

                case KeyCode.UpArrow:
                    evt.PreventDefault();
                    if (optionList.selectedIndex > 0)
                        optionList.selectedIndex--;
                    break;
                case KeyCode.DownArrow:
                    evt.PreventDefault();
                    if (optionList.selectedIndex < MatchedSuggestOption.Count - 1)
                        optionList.selectedIndex++;
                    break;
                default:
                    optionList.selectedIndex = -1;
                    break;
            }

            optionList.ScrollToItem(optionList.selectedIndex);
        }

        private void OnGeometryChange(GeometryChangedEvent evt)
        {
            UpdatePosition();
        }

        private void OnGainedFocus(FocusInEvent evt)
        {
            hasFocus = true;
            UpdateOptionList();
            UpdateVisibility();
            UpdatePosition();
        }

        private void OnLostFocus(FocusOutEvent evt)
        {
            if (evt.relatedTarget == null) return;
            hasFocus = false;
            UpdateVisibility();
        }

        private void OnTextChanged(ChangeEvent<string> evt)
        {
            UpdateOptionList();
            UpdateVisibility();
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (popupWindow == null) return;
            var worldSpaceTextLayout = textEntry.LocalToWorld(textEntry.layout);

            var windowPosition = (Rect)screenPositionProperty.GetValue(ownerObject);

            var topLeft = windowPosition.position + worldSpaceTextLayout.position;
            topLeft = new Vector2(topLeft.x - 3, topLeft.y + worldSpaceTextLayout.height);
            popupPosition = new Rect(topLeft, new Vector2(worldSpaceTextLayout.width, 100));


#if UNITY_2020
            popupWindow.rootVisualElement.style.height = 100;
            popupWindow.rootVisualElement.style.width = worldSpaceTextLayout.width;
#elif UNITY_2018
            popupWindow.GetRootVisualContainer().style.height = 100;
            popupWindow.GetRootVisualContainer().style.width = worldSpaceTextLayout.width;
#endif

            popupWindow.position = popupPosition;
        }

        private void UpdateOptionList()
        {
            MatchedSuggestOption.Clear();
            optionList.itemsSource = MatchedSuggestOption;
            optionList.selectedIndex = -1;

            if (string.IsNullOrEmpty(textEntry.value))
            {
                optionList.Refresh();
                return;
            }

            MatchedSuggestOption.AddRange(SuggestOption.Where(matchingSuggestOptions));

            optionList.Refresh();
        }

        private void UpdateVisibility()
        {
            if (hasFocus && optionList.itemsSource.Count > 0)
            {
                if (popupVisible) return;
                CreateNewWindow();
                showPopupNonFocus.Invoke(popupWindow, showValueArray);
                popupVisible = true;
            }
            else Cleanup();
        }

        private void Cleanup()
        {
            optionList.RemoveFromHierarchy();
            if (popupWindow != null)
            {
                popupWindow.Close();
                ScriptableObject.DestroyImmediate(popupWindow);
            }
            popupVisible = false;
        }

        private void OnLabelMouseDown(MouseDownEvent evt)
        {
            var pickedLabel = evt.target as VisualElement;
            var suggestOption = (SuggestOption)pickedLabel.userData;
            OnSuggestedSelected?.Invoke(suggestOption);
            textEntry.SetValueWithoutNotify("");
            hasFocus = false;
            UpdateVisibility();
        }
    }
}