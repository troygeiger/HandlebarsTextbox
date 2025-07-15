using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandlebarTextboxTest
{
    internal class SuggestionMetadata
    {
        private List<SuggestionMetadata>? children;

        public required string Name { get; set; }

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
