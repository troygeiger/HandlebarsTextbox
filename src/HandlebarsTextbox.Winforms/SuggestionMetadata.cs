using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandlebarsTextbox.Winforms
{
    public enum SuggestionType
    {
        Data,
        Partial,
        Helper,
        BlockHelper // Added BlockHelper
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
    }
}
