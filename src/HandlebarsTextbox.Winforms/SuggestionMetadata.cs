namespace HandlebarsTextbox.Winforms
{
    /// <summary>
    /// Represents the type of suggestion for Handlebars auto-complete.
    /// </summary>
    public enum SuggestionType
    {
        /// <summary>
        /// Represents a data field suggestion.
        /// </summary>
        Data,
        /// <summary>
        /// Represents a partial suggestion.
        /// </summary>
        Partial,
        /// <summary>
        /// Represents a helper suggestion.
        /// </summary>
        Helper,
        /// <summary>
        /// Represents a block helper suggestion.
        /// </summary>
        BlockHelper
    }

    /// <summary>
    /// Metadata for a Handlebars suggestion, including name, type, and child suggestions.
    /// </summary>
    public class SuggestionMetadata
    {
        private List<SuggestionMetadata>? children;

        /// <summary>
        /// Gets or sets the name of the suggestion.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the suggestion.
        /// </summary>
        public SuggestionType Type { get; set; } = SuggestionType.Data;

        /// <summary>
        /// Gets or sets the child suggestions for nested completion.
        /// </summary>
        public List<SuggestionMetadata> Children
        {
            get
            {
                if (children == null)
                {
                    children = new List<SuggestionMetadata>();
                }
                return children;
            }
            set
            {
                children = value;
            }
        }
    }
}
