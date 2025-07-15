namespace HandlebarTextboxTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            handlebarTextbox1.Suggestions.AddRange([
                new SuggestionMetadata
                {
                    Name = "Order",
                    Type = SuggestionType.Data,
                    Children = [
                        new SuggestionMetadata { Name = "OrderNum", Type = SuggestionType.Data }
                    ]
                },
                new SuggestionMetadata { Name = "Onion", Type = SuggestionType.Data },
                new SuggestionMetadata
                {
                    Name = "Now",
                    Type = SuggestionType.Data,
                    Children = [
                        new SuggestionMetadata { Name = "Year", Type = SuggestionType.Data }
                    ]
                },
                new SuggestionMetadata { Name = "MyPartial", Type = SuggestionType.Partial },
                new SuggestionMetadata { Name = "SomeHelper", Type = SuggestionType.Helper }
            ]);
        }
    }
}
