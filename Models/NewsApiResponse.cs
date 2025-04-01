using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VtvNewsApp.Models
{
    public class NewsApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("totalResults")]
        public int TotalResults { get; set; }
        
        [JsonPropertyName("articles")]
        public List<Article> Articles { get; set; } = new List<Article>();
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}