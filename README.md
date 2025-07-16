# HandlebarsTextbox

Auto-complete TextBox controls for Handlebars markup, supporting both WinForms (.NET 8) and Avalonia (.NET 8).

## Overview
This repository provides reusable class libraries for integrating Handlebars-aware auto-complete textboxes into your .NET applications. It supports:
- **WinForms**: `HandlebarsTextbox.Winforms` (Windows Forms)
- **Avalonia**: `HandlebarsTextbox.Avalonia` (cross-platform UI)

Both controls offer:
- Auto-complete for Handlebars helpers, block helpers, partials, and data fields
- Hierarchical/nested suggestions
- Auto-closing of `{{` with `}}`
- Tab-to-exit bracket navigation

## Projects
- `src/HandlebarsTextbox.Winforms` - WinForms control library
- `src/HandlebarsTextbox.Avalonia` - Avalonia control library
- `tests/HandlebarsTextbox.Winforms.Test` - WinForms demo/test app
- `tests/HandlebarsTextbox.Avalonia.Test` - Avalonia demo/test app

## Usage
### WinForms
1. Reference `HandlebarsTextbox.Winforms` in your project.
2. Add a `HandlebarsTextbox` to your form:
   ```csharp
   var textbox = new HandlebarsTextbox.Winforms.HandlebarsTextbox();
   textbox.Suggestions.Add(new SuggestionMetadata { Name = "user", Type = SuggestionType.Data });
   // Add more suggestions as needed
   this.Controls.Add(textbox);
   ```

### Avalonia
1. Reference `HandlebarsTextbox.Avalonia` in your project.
2. Add a `HandlebarsTextbox` to your XAML or code:
   ```xml
   <avalonia:HandlebarsTextbox.Suggestions>
     <avalonia:SuggestionMetadata Name="user" Type="Data" />
     <!-- Add more suggestions as needed -->
   </avalonia:HandlebarsTextbox.Suggestions>
   ```
   Or in code:
   ```csharp
   var textbox = new HandlebarsTextbox.Avalonia.HandlebarsTextbox();
   textbox.Suggestions.Add(new SuggestionMetadata { Name = "user", Type = SuggestionType.Data });
   // Add more suggestions as needed
   ```

## Features
- **Auto-complete**: Shows relevant suggestions as you type inside `{{ ... }}`
- **Bracket handling**: Auto-closes brackets and allows tabbing out
- **Customizable suggestions**: Add helpers, block helpers, partials, and nested data fields
- **Cross-platform**: Avalonia support for Windows, Linux, macOS
