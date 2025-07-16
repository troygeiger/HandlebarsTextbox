using System.Collections.ObjectModel;
using HandlebarsTextbox.Avalonia;

namespace HandlebarsTextbox.Avalonia.Test.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";

    public ObservableCollection<SuggestionMetadata> Suggestions { get; } = new([
        new SuggestionMetadata
        {
            Name = "Order",
            Type = SuggestionType.Data,
            Children =
            {
                new SuggestionMetadata { Name = "OrderNum", Type = SuggestionType.Data }
            }
        },
        new SuggestionMetadata { Name = "Onion", Type = SuggestionType.Data },
        new SuggestionMetadata
        {
            Name = "Now",
            Type = SuggestionType.Data,
            Children =
            {
                new SuggestionMetadata { Name = "Year", Type = SuggestionType.Data }
            }
        },
        new SuggestionMetadata { Name = "MyPartial", Type = SuggestionType.Partial },
        new SuggestionMetadata { Name = "SomeHelper", Type = SuggestionType.Helper },
        new SuggestionMetadata { Name = "MyBlockHelper", Type = SuggestionType.BlockHelper },
    ]);
}
