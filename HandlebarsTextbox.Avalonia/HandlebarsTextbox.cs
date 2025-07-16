using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HandlebarsTextbox.Avalonia
{
    public class HandlebarsTextbox : TextBox
    {
        public static readonly StyledProperty<IList<SuggestionMetadata>> SuggestionsProperty =
            AvaloniaProperty.Register<HandlebarsTextbox, IList<SuggestionMetadata>>(nameof(Suggestions), new List<SuggestionMetadata>());

        public static readonly StyledProperty<bool> EnableTabToExitBracketsProperty =
            AvaloniaProperty.Register<HandlebarsTextbox, bool>(nameof(EnableTabToExitBrackets), true);

        public static readonly StyledProperty<bool> EnableAutoCloseBracketsProperty =
            AvaloniaProperty.Register<HandlebarsTextbox, bool>(nameof(EnableAutoCloseBrackets), true);

        protected override Type StyleKeyOverride => typeof(TextBox);

        public IList<SuggestionMetadata> Suggestions
        {
            get => GetValue(SuggestionsProperty);
            set => SetValue(SuggestionsProperty, value);
        }

        public bool EnableTabToExitBrackets
        {
            get => GetValue(EnableTabToExitBracketsProperty);
            set => SetValue(EnableTabToExitBracketsProperty, value);
        }

        public bool EnableAutoCloseBrackets
        {
            get => GetValue(EnableAutoCloseBracketsProperty);
            set => SetValue(EnableAutoCloseBracketsProperty, value);
        }

        private Flyout? _suggestionPopup;
        private ListBox? _suggestionListBox;
        private double _popupOffsetX = 0;

        public HandlebarsTextbox()
        {
            this.AttachedToVisualTree += OnAttachedToVisualTree;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            var presenter = this.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
            if (presenter != null)
            {
                var boundsField = typeof(TextPresenter).GetField("_caretBounds", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                presenter.CaretBoundsChanged += (s, e) =>
                {
                    var bounds = boundsField?.GetValue(presenter) as Rect?;
                    if (bounds.HasValue)
                    {
                        _popupOffsetX = bounds.Value.X;
                    }
                };
            }
            base.OnLoaded(e);
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (_suggestionPopup != null) return;
            _suggestionListBox = new ListBox
            {
                SelectionMode = SelectionMode.Single,
                Height = 100,
                Width = 200,
                
            };
            _suggestionListBox.PointerReleased += SuggestionListBox_PointerReleased;
            _suggestionListBox.KeyDown += SuggestionListBox_KeyDown;

            var border = new Border
            {
                Child = _suggestionListBox,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(2),
                BorderThickness = new Thickness(1)
            };
            
            _suggestionPopup = new Flyout
            {
                Content = border,
                Placement = PlacementMode.BottomEdgeAlignedLeft,
                ShowMode = FlyoutShowMode.Transient
            };
            this.ContextFlyout = _suggestionPopup;
            this.AddHandler(LostFocusEvent, HandleLostFocus, RoutingStrategies.Tunnel);
        }

        private void HandleLostFocus(object? sender, RoutedEventArgs e)
        {
            HideSuggestion();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            
            if (e.KeyModifiers != KeyModifiers.None || e.Key == Key.Enter || this.SelectionEnd - this.SelectionStart != 0)
            {
                HideSuggestion();
                return;
            }
            if (e.Key == Key.Escape)
            {
                HideSuggestion();
                return;
            }

            if (TryGetToken(out var token))
            {
                int caret = SelectionStart;
                int openIdx = Text.LastIndexOf("{{", caret - 1, caret);
                int caretInToken = caret - (openIdx + 2);
                var spaceParts = token.Split(' ');
                int segStart = 0, segEnd = 0, segIdx = 0;
                for (int i = 0; i < spaceParts.Length; i++)
                {
                    segEnd = segStart + spaceParts[i].Length;
                    if (caretInToken >= segStart && caretInToken <= segEnd)
                    {
                        segIdx = i;
                        break;
                    }
                    segStart = segEnd + 1;
                }
                var segment = spaceParts[segIdx];
                bool isAfterPartial = token.TrimStart().StartsWith('>');
                string trimmedSegment = segment.TrimStart();

                // BlockHelper support
                if (segment.StartsWith("#"))
                {
                    string blockHelperPrefix = segment.Substring(1); // after #
                    var blockHelpers = Suggestions
                        .Where(kv => kv.Type == SuggestionType.BlockHelper && kv.Name.StartsWith(blockHelperPrefix, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (blockHelpers.Count == 1 && string.Equals(blockHelpers[0].Name, blockHelperPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HideSuggestion();
                    }
                    else if (blockHelpers.Any())
                    {
                        ShowSuggestion(blockHelpers, blockHelperPrefix);
                    }
                    else
                    {
                        HideSuggestion();
                    }
                    return;
                }

                if (isAfterPartial)
                {
                    string partialPrefix = segment == ">" ? string.Empty : segment;
                    var partialMeta = Suggestions
                        .Where(kv => kv.Type == SuggestionType.Partial && kv.Name.StartsWith(partialPrefix, System.StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (!isAfterPartial && partialMeta.Count == 1 && string.Equals(partialMeta[0].Name, partialPrefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        HideSuggestion();
                    }
                    else if (partialMeta.Any())
                    {
                        ShowSuggestion(partialMeta, partialPrefix);
                    }
                    else
                    {
                        HideSuggestion();
                    }
                    return;
                }
                bool isHelper = false;
                if (spaceParts.Length > 0 && Suggestions.Any(s => s.Type == SuggestionType.Helper && s.Name.Equals(spaceParts[0], System.StringComparison.OrdinalIgnoreCase)))
                {
                    isHelper = true;
                }
                var pathParts = segment.Split('.');
                var current = Suggestions;
                SuggestionMetadata? node = null;
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    node = current.FirstOrDefault(s => s.Name.Equals(pathParts[i], System.StringComparison.OrdinalIgnoreCase));
                    if (node == null)
                    {
                        current = new List<SuggestionMetadata>();
                        break;
                    }
                    current = node.Children;
                }
                var lastPart = pathParts.Last();
                var dataMeta = current
                    .Where(kv => (kv.Type == SuggestionType.Data || kv.Type == SuggestionType.Helper) && kv.Name.StartsWith(lastPart, System.StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (dataMeta.Count == 1 && string.Equals(dataMeta[0].Name, lastPart, System.StringComparison.OrdinalIgnoreCase))
                {
                    HideSuggestion();
                }
                else if (dataMeta.Any())
                {
                    ShowSuggestion(dataMeta, lastPart);
                }
                else
                {
                    HideSuggestion();
                }
            }
            else
            {
                HideSuggestion();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Tab: if inside {{...}}, jump to after closing }}
            if (EnableTabToExitBrackets && e.Key == Key.Tab)
            {
                int caret = SelectionStart;
                string text = Text;
                int openIdx = text.LastIndexOf("{{", caret - 1, caret);
                int closeIdx = text.IndexOf("}}", caret);
                if (openIdx != -1 && closeIdx != -1 && openIdx < caret && caret <= closeIdx)
                {
                    SelectionStart = closeIdx + 2;
                    SelectionEnd = SelectionStart;
                    e.Handled = true;
                    return;
                }
            }
            // Only handle navigation if TextBox has focus and suggestionPopup is visible
            if (IsFocused && _suggestionPopup?.IsOpen == true)
            {
                if (e.Key == Key.Down || e.Key == Key.Up)
                {
                    if (_suggestionListBox?.ItemCount > 0)
                    {
                        int idx = _suggestionListBox.SelectedIndex;
                        if (e.Key == Key.Down)
                            _suggestionListBox.SelectedIndex = System.Math.Min(_suggestionListBox.ItemCount - 1, idx + 1);
                        else if (e.Key == Key.Up)
                            _suggestionListBox.SelectedIndex = System.Math.Max(0, idx - 1);
                        // Only set focus if not already focused
                        if (!_suggestionListBox.IsFocused)
                            _suggestionListBox.Focus();
                    }
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Enter)
                {
                    InsertSelectedSuggestion();
                    e.Handled = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            // If user types '{{', auto-close with '}}' and place caret between
            if (EnableAutoCloseBrackets && e.Text == "{")
            {
                int caret = SelectionStart;
                // Check if previous char is also '{'
                if (caret >= 2 && Text.Substring(caret - 2, 2) == "{{")
                {
                    // Insert '}}' at caret
                    Text = Text.Insert(caret, "}}");
                    SelectionStart = caret; // Place caret between {{ and }}
                    SelectionEnd = caret;
                    e.Handled = true;
                }
            }
        }

        private void InsertSelectedSuggestion()
        {
            if (_suggestionListBox?.SelectedItem is SuggestionMetadata meta && TryGetToken(out var token))
            {
                string item = meta.Name;
                int caret = SelectionStart;
                int openIdx = Text.LastIndexOf("{{", caret - 1, caret);
                int start = SelectionStart - token.Length;
                int caretInToken = caret - (openIdx + 2);
                var spaceParts = token.Split(' ');
                int segStart = 0, segEnd = 0, segIdx = 0;
                for (int i = 0; i < spaceParts.Length; i++)
                {
                    segEnd = segStart + spaceParts[i].Length;
                    if (caretInToken >= segStart && caretInToken <= segEnd)
                    {
                        segIdx = i;
                        break;
                    }
                    segStart = segEnd + 1;
                }
                var segment = spaceParts[segIdx];
                if (segment.StartsWith(">"))
                {
                    spaceParts[segIdx] = ">" + item;
                }
                else if (segment.StartsWith("#"))
                {
                    // Only insert the name after the #
                    spaceParts[segIdx] = "#" + item;
                }
                else
                {
                    var pathParts = segment.Split('.');
                    pathParts[pathParts.Length - 1] = item;
                    var newSegment = string.Join(".", pathParts);
                    spaceParts[segIdx] = newSegment;
                }
                var newToken = string.Join(" ", spaceParts);
                Text = Text.Remove(start, token.Length).Insert(start, newToken);
                SelectionStart = start + segStart + spaceParts[segIdx].Length;
                SelectionEnd = SelectionStart; // Ensure no text is selected
                HideSuggestion();
            }
            else
            {
                HideSuggestion();
            }
        }

        private bool TryGetToken(out string? token)
        {
            token = null;
            try
            {
                int caret = SelectionStart;
                string text = Text;
                int openIdx = text.LastIndexOf("{{", caret - 1, caret);
                if (openIdx == -1)
                    return false;
                int closeIdx = text.IndexOf("}}", openIdx + 2);
                if (closeIdx != -1)
                {
                    for (int k = caret; k < closeIdx; k++)
                    {
                        if (text[k] == '{' || text[k] == '}')
                            return false;
                    }
                    token = text.Substring(openIdx + 2, closeIdx - (openIdx + 2));
                }
                else
                {
                    if (!(openIdx + 2 <= caret && caret <= text.Length))
                        return false;
                    for (int k = caret; k < text.Length; k++)
                    {
                        if (text[k] == '{' || text[k] == '}')
                            return false;
                    }
                    token = text.Substring(openIdx + 2, caret - (openIdx + 2));
                }
                int caretInToken = caret - (openIdx + 2);
                if (caretInToken < 0 || caretInToken > (token?.Length ?? 0))
                    return false;
                bool inSingleQuotes = false, inDoubleQuotes = false;
                for (int i = 0; i < caretInToken; i++)
                {
                    if (token[i] == '\'' && !inDoubleQuotes) inSingleQuotes = !inSingleQuotes;
                    if (token[i] == '"' && !inSingleQuotes) inDoubleQuotes = !inDoubleQuotes;
                }
                if (inSingleQuotes || inDoubleQuotes)
                    return false;
                return true;
            }
            catch
            {
                token = null;
                return false;
            }
        }

        private void ShowSuggestion(List<SuggestionMetadata> suggestions, string token)
        {
            if (_suggestionListBox == null || _suggestionPopup == null) return;
            // Only set SelectedIndex = 0 if ItemsSource is different or popup is not open
            bool itemsChanged = _suggestionListBox.ItemsSource != suggestions;
            _suggestionListBox.ItemsSource = suggestions;
            if ((itemsChanged || !_suggestionPopup.IsOpen) && _suggestionListBox.SelectedIndex == -1)
                _suggestionListBox.SelectedIndex = 0;
            _suggestionPopup.HorizontalOffset = _popupOffsetX;
            _suggestionPopup.ShowAt(this);
        }

        private void HideSuggestion()
        {
            if (_suggestionPopup != null)
            {
                _suggestionPopup.Hide();
            }
        }

        private void SuggestionListBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_suggestionListBox?.SelectedItem is SuggestionMetadata)
            {
                InsertSelectedSuggestion();
                e.Handled = true;
            }
        }

        private void SuggestionListBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                InsertSelectedSuggestion();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HideSuggestion();
                e.Handled = true;
            }
        }
    }
}
