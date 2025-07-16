using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace HandlebarsTextbox.Winforms
{
    /// <summary>
    /// A Windows Forms TextBox control with Handlebars auto-complete and bracket handling features.
    /// </summary>
    public class HandlebarsTextbox : TextBox
    {
        private ToolStripDropDown suggestionDropDown;
        private ListBox suggestionListBox;

        SuggestionMetadata rootSuggestions = new() { Name = "root" };

        /// <summary>
        /// Gets or sets whether pressing Tab inside brackets jumps to after closing brackets.
        /// </summary>
        public bool EnableTabToExitBrackets { get; set; } = true;

        /// <summary>
        /// Gets or sets whether typing '{{' auto-closes with '}}' and places the caret between.
        /// </summary>
        public bool EnableAutoCloseBrackets { get; set; } = true;

        [DllImport("user32.dll")]
        static extern bool GetCaretPos(out Point lpPoint);

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlebarsTextbox"/> class.
        /// </summary>
        public HandlebarsTextbox()
        {
            suggestionListBox = new ListBox
            {
                SelectionMode = SelectionMode.One,
                IntegralHeight = true,
                Height = 100,
                Width = 150
            };
            suggestionListBox.Click += SuggestionListBox_Click;
            suggestionListBox.KeyDown += SuggestionListBox_KeyDown;
            suggestionListBox.Leave += (s, e) => HideSuggestion();

            suggestionDropDown = new ToolStripDropDown
            {
                AutoClose = false
            };
            ToolStripControlHost host = new ToolStripControlHost(suggestionListBox)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };
            suggestionDropDown.Items.Add(host);

            this.LostFocus += HandleLostFocus;
            this.ParentChanged += (s, e) => HideSuggestion();
        }

        /// <summary>
        /// Gets the list of root suggestions for auto-complete.
        /// </summary>
        public List<SuggestionMetadata> Suggestions => rootSuggestions.Children;

        private void HandleLostFocus(object? sender, EventArgs e)
        {
            // Only hide if focus is not moving to the suggestionListBox
            if (!suggestionListBox.Focused && !suggestionDropDown.Focused)
            {
                HideSuggestion();
            }
        }

        /// <summary>
        /// Handles key up events to trigger or hide suggestions.
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            
            if (e.Modifiers != Keys.None || e.KeyCode == Keys.Enter)
            {
                HideSuggestion();
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                HideSuggestion();
                return;
            }

            if (TryGetToken(out var token))
            {
                int caret = this.SelectionStart;
                int openIdx = this.Text.LastIndexOf("{{", caret - 1, caret);
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
                        .Select(kv => kv.Name)
                        .ToList();
                    if (blockHelpers.Count == 1 && string.Equals(blockHelpers[0], blockHelperPrefix, StringComparison.OrdinalIgnoreCase))
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

                // Partial context: segment starts with '>' or caret is after '>' and whitespace
                if (isAfterPartial)
                {
                    string partialPrefix = segment == ">" ? string.Empty : segment;
                    var partialMeta = Suggestions
                        .Where(kv => kv.Type == SuggestionType.Partial && isAfterPartial && kv.Name.StartsWith(partialPrefix, StringComparison.OrdinalIgnoreCase))
                        .Select(kv => kv.Name)
                        .ToList();
                    if (!isAfterPartial && partialMeta.Count == 1 && string.Equals(partialMeta[0], partialPrefix, StringComparison.OrdinalIgnoreCase))
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
                // Helper context: first segment is a helper
                bool isHelper = false;
                if (spaceParts.Length > 0 && Suggestions.Any(s => s.Type == SuggestionType.Helper && s.Name.Equals(spaceParts[0], StringComparison.OrdinalIgnoreCase)))
                {
                    isHelper = true;
                }
                // Split segment by '.' for nested suggestions
                var pathParts = segment.Split('.');
                var current = Suggestions;
                SuggestionMetadata? node = null;
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    node = current.FirstOrDefault(s => s.Name.Equals(pathParts[i], StringComparison.OrdinalIgnoreCase));
                    if (node == null)
                    {
                        current = new List<SuggestionMetadata>();
                        break;
                    }
                    current = node.Children;
                }
                // Last part is what we're currently typing
                var lastPart = pathParts.Last();
                var dataMeta = current
                    .Where(kv => (kv.Type == SuggestionType.Helper || kv.Type == SuggestionType.Data) && kv.Name.StartsWith(lastPart, StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Name)
                    .ToList();
                // If there is a single exact match, do not show suggestions
                if (dataMeta.Count == 1 && string.Equals(dataMeta[0], lastPart, StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// Handles key down events for navigation and suggestion selection.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Tab: if inside {{...}}, jump to after closing }}
            if (EnableTabToExitBrackets && e.KeyCode == Keys.Tab)
            {
                int caret = this.SelectionStart;
                string text = this.Text;
                int openIdx = text.LastIndexOf("{{", caret - 1, caret);
                int closeIdx = text.IndexOf("}}", caret);
                if (openIdx != -1 && closeIdx != -1 && openIdx < caret && caret <= closeIdx)
                {
                    this.SelectionStart = closeIdx + 2;
                    this.SelectionLength = 0;
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }
            // Only handle navigation if TextBox has focus and suggestionDropDown is visible
            if (this.Focused && suggestionDropDown.Visible)
            {
                if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
                {
                    if (suggestionListBox.Items.Count > 0)
                    {
                        int idx = suggestionListBox.SelectedIndex;
                        if (e.KeyCode == Keys.Down)
                            suggestionListBox.SelectedIndex = Math.Min(suggestionListBox.Items.Count - 1, idx + 1);
                        else if (e.KeyCode == Keys.Up)
                            suggestionListBox.SelectedIndex = Math.Max(0, idx - 1);
                        // Move focus to the ListBox so it can handle further navigation
                        suggestionListBox.Focus();
                    }
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    // Select the suggestion if popup is visible
                    InsertSelectedSuggestion();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Handles key press events for bracket auto-close.
        /// </summary>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            // If user types '{{', auto-close with '}}' and place caret between
            if (EnableAutoCloseBrackets && e.KeyChar == '{')
            {
                int caret = this.SelectionStart;
                if (caret >= 2 && this.Text.Substring(caret - 2, 2) == "{{")
                {
                    this.Text = this.Text.Insert(caret, "}}");
                    this.SelectionStart = caret;
                    this.SelectionLength = 0;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Shows the suggestion drop-down with the provided suggestions.
        /// </summary>
        /// <param name="suggestions">The list of suggestions to display.</param>
        /// <param name="token">The current token under the caret.</param>
        private void ShowSuggestion(List<string> suggestions, string token)
        {
            suggestionListBox.BeginUpdate();
            suggestionListBox.Items.Clear();
            foreach (var item in suggestions)
                suggestionListBox.Items.Add(item);
            suggestionListBox.SelectedIndex = 0;
            suggestionListBox.EndUpdate();

            if (!suggestionDropDown.Visible)
            {
                Point caretPos;
                if (!GetCaretPos(out caretPos))
                {
                    caretPos = new Point(0, this.Height);
                }
                else
                {
                    caretPos = new Point(caretPos.X, this.Height);
                }
                var screenPoint = this.PointToScreen(caretPos);
                var clientPoint = this.PointToClient(screenPoint);
                suggestionDropDown.Show(this, clientPoint);
            }
        }

        /// <summary>
        /// Hides the suggestion drop-down.
        /// </summary>
        private void HideSuggestion()
        {
            if (suggestionDropDown.Visible)
                suggestionDropDown.Close();
        }

        private void SuggestionListBox_Click(object? sender, EventArgs e)
        {
            InsertSelectedSuggestion();
        }

        private void SuggestionListBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                InsertSelectedSuggestion();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                HideSuggestion();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Inserts the selected suggestion into the textbox at the caret position.
        /// </summary>
        private void InsertSelectedSuggestion()
        {
            if (suggestionListBox.SelectedItem is string item && TryGetToken(out var token))
            {
                int caret = this.SelectionStart;
                int openIdx = this.Text.LastIndexOf("{{", caret - 1, caret);
                int start = this.SelectionStart - token.Length;
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
                // Replace only the segment under the caret
                var segment = spaceParts[segIdx];
                if (segment.StartsWith(">"))
                {
                    // For partials, preserve the '>'
                    spaceParts[segIdx] = ">" + item;
                }
                else if (segment.StartsWith("#"))
                {
                    // For block helpers, preserve the '#'
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
                this.Text = this.Text.Remove(start, token.Length).Insert(start, newToken);
                this.SelectionStart = start + segStart + spaceParts[segIdx].Length;
                HideSuggestion();
            }
        }

        /// <summary>
        /// Attempts to extract the current token under the caret for suggestion logic.
        /// </summary>
        /// <param name="token">The extracted token, if successful.</param>
        /// <returns>True if a valid token is found; otherwise, false.</returns>
        private bool TryGetToken([NotNullWhen(true)] out string? token)
        {
            try
            {
                int caret = this.SelectionStart;
                string text = this.Text;
                // Find the nearest '{{' before the caret
                int openIdx = text.LastIndexOf("{{", caret - 1, caret);
                if (openIdx == -1)
                {
                    token = null;
                    return false;
                }
                // Find the nearest '}}' after the caret
                int closeIdx = text.IndexOf("}}", openIdx + 2);
                // Only allow if caret is strictly inside the brackets
                if (closeIdx != -1)
                {
                    /*if (!(openIdx + 2 <= caret && caret < closeIdx))
                    {
                        token = null;
                        return false;
                    }*/
                    // Ensure no stray braces between caret and closeIdx
                    for (int k = caret; k < closeIdx; k++)
                    {
                        if (text[k] == '{' || text[k] == '}')
                        {
                            token = null;
                            return false;
                        }
                    }
                    // Token is between openIdx+2 and closeIdx
                    token = text.Substring(openIdx + 2, closeIdx - (openIdx + 2));
                }
                else
                {
                    // No closing '}}', only allow if caret is after '{{' and before any stray braces or end of text
                    if (!(openIdx + 2 <= caret && caret <= text.Length))
                    {
                        token = null;
                        return false;
                    }
                    for (int k = caret; k < text.Length; k++)
                    {
                        if (text[k] == '{' || text[k] == '}')
                        {
                            token = null;
                            return false;
                        }
                    }
                    token = text.Substring(openIdx + 2, caret - (openIdx + 2));
                }
                // Check if caret is inside single or double quotes within the token
                int caretInToken = caret - (openIdx + 2);
                if (caretInToken < 0 || caretInToken > token.Length)
                {
                    token = null;
                    return false;
                }
                bool inSingleQuotes = false, inDoubleQuotes = false;
                for (int i = 0; i < caretInToken; i++)
                {
                    if (token[i] == '\'' && !inDoubleQuotes) inSingleQuotes = !inSingleQuotes;
                    if (token[i] == '"' && !inSingleQuotes) inDoubleQuotes = !inDoubleQuotes;
                }
                if (inSingleQuotes || inDoubleQuotes)
                {
                    token = null;
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                // Silently catch any exceptions that might occur during token extraction
            }
            token = null;
            return false;
        }
    }
}
