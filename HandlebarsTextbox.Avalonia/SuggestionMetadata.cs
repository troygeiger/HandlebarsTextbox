using System.Collections.Generic;

namespace HandlebarsTextbox.Avalonia
{
    public enum SuggestionType
    {
        Data,
        Partial,
        Helper
    }

    public class SuggestionMetadata
    {
        private List<SuggestionMetadata>? children;

        public required string Name { get; set; }
        public SuggestionType Type { get; set; } = SuggestionType.Data;

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

        public override string ToString() => Name;
    }
}
