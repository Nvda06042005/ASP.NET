using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VtvNewsApp.Models;

namespace VtvNewsApp.Services
{
    public class NewsService : INewsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NewsService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public NewsService(HttpClient httpClient, ILogger<NewsService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiKey = _configuration["NewsApiKey"] ?? "4dee47ab8112c1b949ecda490be79a17"; // Sử dụng key mặc định nếu không có cấu hình
        }

        public async Task<List<Article>> GetArticlesAsync(string query, DateTime? fromDate, string sortBy, int pageSize = 100)
        {
            // Chuyển sang sử dụng GNews API thay vì NewsAPI
            var url = "https://gnews.io/api/v4/search";
            
            var queryParams = new Dictionary<string, string>
            {
                { "q", query },
                { "token", _apiKey },
                { "max", pageSize.ToString() },
                { "lang", "vi" } // Tìm kiếm tin tức tiếng Việt
            };

            // Thêm tham số from nếu có
            if (fromDate.HasValue)
            {
                queryParams.Add("from", fromDate.Value.ToString("yyyy-MM-dd"));
            }

            var requestUrl = url + "?" + string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
            
            _logger.LogInformation($"Calling GNews API with URL: {requestUrl}");
            
            var response = await _httpClient.GetAsync(requestUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"HTTP Error: {response.StatusCode}");
                throw new Exception($"HTTP Error: {response.StatusCode}");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            
            // Chuyển đổi cấu trúc phản hồi từ GNews API sang phù hợp với model Article của ứng dụng
            var gnewsResponse = JsonSerializer.Deserialize<GNewsResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (gnewsResponse == null)
            {
                _logger.LogError("Failed to deserialize GNews API response");
                throw new Exception("Lỗi khi phân tích phản hồi từ API");
            }
            
            // Chuyển đổi cấu trúc GNews article sang cấu trúc Article của ứng dụng
            var articles = gnewsResponse.Articles.Select(gnewsArticle => new Article
            {
                Source = new Source
                {
                    Id = gnewsArticle.Source?.Id,
                    Name = gnewsArticle.Source?.Name
                },
                Author = gnewsArticle.Source?.Name, // GNews không luôn cung cấp tác giả
                Title = gnewsArticle.Title,
                Description = gnewsArticle.Description,
                Url = gnewsArticle.Url,
                UrlToImage = gnewsArticle.Image,
                PublishedAt = gnewsArticle.PublishedAt,
                Content = gnewsArticle.Content
            }).ToList();
            
            // Lọc các bài viết liên quan đến Vietnam ngay tại đây
            if (query.Contains("Vietnam", StringComparison.OrdinalIgnoreCase) ||
                query.Contains("Việt Nam", StringComparison.OrdinalIgnoreCase))
            {
                articles = FilterArticlesForVietnam(articles);
            }
            
            return articles;
        }

        public List<Article> FilterArticles(List<Article> articles, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) 
                return articles;
                
            var termLower = term.ToLower();
            return articles.Where(article => 
                (article.Title?.ToLower().Contains(termLower) ?? false) ||
                (article.Description?.ToLower().Contains(termLower) ?? false) ||
                (article.Content?.ToLower().Contains(termLower) ?? false)
            ).ToList();
        }
        
        // Phương thức mới để lọc các bài viết có liên quan đến Vietnam/Việt Nam
        private List<Article> FilterArticlesForVietnam(List<Article> articles)
        {
            // Danh sách các từ khóa liên quan đến Việt Nam
            var vietnamKeywords = new[] 
            { 
                "vietnam", "việt nam", "viet nam", "vietnamese", "việt", "viet", 
                "hanoi", "hà nội", "ha noi", "ho chi minh", "hồ chí minh", 
                "saigon", "sài gòn", "sai gon", "đà nẵng", "da nang", "hue", "huế"
            };
            
            return articles.Where(article => 
            {
                var title = (article.Title ?? "").ToLower();
                var description = (article.Description ?? "").ToLower();
                var content = (article.Content ?? "").ToLower();
                
                // Kiểm tra xem bài viết có chứa bất kỳ từ khóa nào trong danh sách không
                return vietnamKeywords.Any(keyword => 
                    title.Contains(keyword) || 
                    description.Contains(keyword) || 
                    content.Contains(keyword)
                );
            }).ToList();
        }
    }

    // Lớp phụ để deserialize phản hồi từ GNews API
    public class GNewsResponse
    {
        public int TotalArticles { get; set; }
        public List<GNewsArticle> Articles { get; set; } = new List<GNewsArticle>();
    }

    public class GNewsArticle
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Content { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public string? PublishedAt { get; set; }
        public GNewsSource? Source { get; set; }
    }

    public class GNewsSource
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Id { get; set; } // Thêm Id field để tương thích với Source của ứng dụng
    }
}