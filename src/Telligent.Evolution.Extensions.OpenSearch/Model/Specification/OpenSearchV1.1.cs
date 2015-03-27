using System.Collections.Generic;

namespace Telligent.Evolution.Extensions.OpenSearch.Model.Specification
{
    public class OpenSearchV1_1 : IOpenSearchSpecification
    {
        public Dictionary<string, string> Get()
        {
            return new Dictionary<string, string>
            {
                {"searchTerms", ""},
                {"count", "0"},
                {"startIndex", "0"},
                {"startPage", "0"},
                {"language", "en-Us"},
                {"inputEncoding", "UTF-8"},
                {"outputEncoding", "UTF-8"}
            };
        }
    }
}
