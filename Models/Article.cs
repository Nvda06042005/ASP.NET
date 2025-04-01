using System;
using System.Text.Json.Serialization;

namespace VtvNewsApp.Models
{
    public class Article
    {
        [JsonPropertyName("source")]
        public Source? Source { get; set; }
        
        [JsonPropertyName("author")]
        public string? Author { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        
        [JsonPropertyName("urlToImage")]
        public string? UrlToImage { get; set; }
        
        [JsonPropertyName("publishedAt")]
        public string? PublishedAt { get; set; }
        
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        
        // Thuộc tính bổ sung cho phiên bản đã dịch
        public string? TranslatedTitle { get; set; }
        public string? TranslatedDescription { get; set; }
        public string? VnPublishedAt { get; set; }
    }
    
    public class Source
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}