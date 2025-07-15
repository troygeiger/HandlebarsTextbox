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
                    Children = [
                        new SuggestionMetadata { Name = "OrderNum" }
                    ]
                },
                new SuggestionMetadata { Name = "Onion" },
                new SuggestionMetadata
                {
                    Name = "Now",
                    Children = [
                        new SuggestionMetadata { Name = "Year" }
                    ]
                }
                ]);
        }
    }
}
