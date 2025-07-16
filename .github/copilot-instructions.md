# HandlebarsTextbox Project - Copilot Instructions

## Overview
This repository contains class libraries for providing auto-complete functionality for Handlebars markup in textboxes. It supports both WinForms and Avalonia UI frameworks.

## Projects
- **HandlebarsTextbox.Winforms**: .NET 8, Windows Forms control (`HandlebarsTextbox`) with auto-complete for Handlebars markup.
- **HandlebarsTextbox.Avalonia**: .NET 9, Avalonia control (`HandlebarsTextbox`) with similar auto-complete features.

## Key Concepts
- **SuggestionMetadata**: Represents a suggestion item (helper, block helper, partial, or data field) and supports nested children for hierarchical completion.
- **SuggestionType**: Enum for categorizing suggestions (Data, Partial, Helper, BlockHelper).
- **Auto-complete Logic**: Parses the current token under the caret to show relevant suggestions. Handles nested fields, block helpers, and partials.
- **Bracket Handling**: Supports auto-closing `{{` with `}}` and tab-to-exit bracket navigation.

## WinForms Implementation
- Uses `ToolStripDropDown` and `ListBox` for suggestion UI.
- Suggestions are managed via a root `SuggestionMetadata` object.
- Key events trigger, navigate, and select suggestions.

## Avalonia Implementation
- Uses Avalonia properties and a `Flyout` for suggestion UI.
- Suggestions are set via a property and shown in a `ListBox` inside a popup.
- Key and text input events handle suggestion logic and insertion.

## Extending/Modifying
- To add new suggestion types, update `SuggestionType` and relevant logic in both platforms.
- To change UI behavior, modify the popup/listbox logic in each control.
- To support new Handlebars features, update token parsing and suggestion filtering logic.

## Testing
- Test projects exist for both WinForms and Avalonia implementations.
- Ensure auto-complete, bracket handling, and navigation work as expected.

## Best Practices
- Keep suggestion logic in sync between platforms for consistent behavior.
- Use hierarchical `SuggestionMetadata` for nested data fields.
- Validate changes with unit tests and manual UI testing.
- Make sure that every public class and public members are documented with XML comments for clarity.

---
This file is intended to help Copilot and future contributors quickly understand the structure and purpose of the project, and where to look for key logic and extension points.
