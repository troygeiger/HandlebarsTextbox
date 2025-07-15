using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace HandlebarTextboxTest
{
    internal class HandlebarTextbox : TextBox
    {
        private ToolStripDropDown suggestionDropDown;
        private ListBox suggestionListBox;

        SuggestionMetadata rootSuggestions = new() { Name = "root" };

        

        [DllImport("user32.dll")]
        static extern bool GetCaretPos(out Point lpPoint);

        public HandlebarTextbox()
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

        public List<SuggestionMetadata> Suggestions => rootSuggestions.Children;

        private void HandleLostFocus(object? sender, EventArgs e)
        {
            // Only hide if focus is not moving to the suggestionListBox
            if (!suggestionListBox.Focused && !suggestionDropDown.Focused)
            {
                HideSuggestion();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (this.SelectionStart < 3 || this.SelectionLength > 0)
            {
                HideSuggestion();
                return;
            }
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
                // Split token by '.' for nested suggestions
                var pathParts = token.Split('.');
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
                var meta = current
                    .Where(kv => kv.Name.StartsWith(lastPart, StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Name)
                    .ToList();
                // If there is a single exact match, do not show suggestions
                if (meta.Count == 1 && string.Equals(meta[0], lastPart, StringComparison.OrdinalIgnoreCase))
                {
                    HideSuggestion();
                }
                else if (meta.Any())
                {
                    ShowSuggestion(meta, lastPart);
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
                    return;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    // Select the suggestion if popup is visible
                    InsertSelectedSuggestion();
                    e.Handled = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }

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
            }
            else if (e.KeyCode == Keys.Escape)
            {
                HideSuggestion();
                e.Handled = true;
            }
        }

        private void InsertSelectedSuggestion()
        {
            if (suggestionListBox.SelectedItem is string item && TryGetToken(out var token))
            {
                // Split token by '.' for nested suggestions
                var start = this.SelectionStart - token.Length;
                var pathParts = token.Split('.');
                // Replace only the last part
                pathParts[pathParts.Length - 1] = item;
                var newToken = string.Join(".", pathParts);
                this.Text = this.Text.Remove(start, token.Length).Insert(start, newToken);
                this.SelectionStart = start + newToken.Length;
                HideSuggestion();
            }
        }

        private bool TryGetToken([NotNullWhen(true)] out string? token)
        {
            try
            {
                for (int i = this.SelectionStart - 1; i >= 0; i--)
                {
                    if (this.Text[i] == '{')
                    {
                        int j = i + 1;
                        while (j < this.Text.Length && this.Text[j] != '}')
                        {
                            j++;
                        }
                        // Only extract token if caret is before or at the closing bracket
                        if ((j < this.Text.Length && this.Text[j] == '}') && this.SelectionStart <= j)
                        {
                            i++;
                            token = this.Text.Substring(i, j - i);
                            return true;
                        }
                        // Or if no closing bracket, allow token extraction
                        else if (j == this.Text.Length)
                        {
                            i++;
                            token = this.Text.Substring(i, j - i);
                            return true;
                        }
                    }
                }
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
