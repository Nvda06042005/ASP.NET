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
            _apiKey = _configuration["NewsApiKey"] ?? "3aced82ed23d48b9af48973f9bde61b4";
        }

        public async Task<List<Article>> GetArticlesAsync(string query, DateTime? fromDate, string sortBy, int pageSize = 50)
        {
            // Mở rộng truy vấn để bao gồm các từ khóa Việt Nam nếu chưa có
            string expandedQuery = query;
            if (!query.Contains("Vietnam", StringComparison.OrdinalIgnoreCase) && 
                !query.Contains("Việt Nam", StringComparison.OrdinalIgnoreCase))
            {
                // Thêm từ khóa Việt Nam vào truy vấn nếu chưa có
                expandedQuery = $"{query} OR Vietnam OR \"Việt Nam\"";
            }
            
            var url = "https://newsapi.org/v2/everything";
            
            var queryParams = new Dictionary<string, string>
            {
                { "q", expandedQuery },
                { "apiKey", _apiKey },
                { "pageSize", pageSize.ToString() },
                { "language", "vi" } // Tìm kiếm tin tức tiếng Việt
            };

            // Thêm tham số from nếu có
            if (fromDate.HasValue)
            {
                queryParams.Add("from", fromDate.Value.ToString("yyyy-MM-dd"));
            }

            // Thêm tham số sortBy nếu có
            if (!string.IsNullOrEmpty(sortBy))
            {
                queryParams.Add("sortBy", sortBy);
            }

            var requestUrl = url + "?" + string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
            
            _logger.LogInformation($"Calling NewsAPI with URL: {requestUrl}");
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("User-Agent", "VtvNewsApp");
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"HTTP Error: {response.StatusCode}");
                throw new Exception($"HTTP Error: {response.StatusCode}");
            }
            
            var content = await response.Content.ReadAsStringAsync();
            
            var newsApiResponse = JsonSerializer.Deserialize<NewsApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (newsApiResponse == null || newsApiResponse.Status != "ok")
            {
                _logger.LogError($"API Error: {newsApiResponse?.Message ?? "Unknown error"}");
                throw new Exception($"API Error: {newsApiResponse?.Message ?? "Lỗi khi phân tích phản hồi từ API"}");
            }
            
            _logger.LogInformation($"API returned {newsApiResponse.Articles?.Count ?? 0} articles before relevance scoring");
            
            var articles = newsApiResponse.Articles ?? new List<Article>();
            
            // Thay vì lọc loại bỏ, hãy sắp xếp theo mức độ liên quan đến Việt Nam
            return RankArticlesByVietnameseRelevance(articles);
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
        
        // Phương thức mới: Sắp xếp bài viết theo mức độ liên quan đến Việt Nam thay vì lọc loại bỏ
        private List<Article> RankArticlesByVietnameseRelevance(List<Article> articles)
        {
            var vietnamKeywords = new[] 
            { 
                "vietnam", "việt nam", "viet nam", "vietnamese", "việt", "viet", 
                "hanoi", "hà nội", "ha noi", "ho chi minh", "hồ chí minh", 
                "saigon", "sài gòn", "sai gon", "đà nẵng", "da nang", "hue", "huế"
            };
            
            // Tính điểm liên quan cho mỗi bài viết
            var scoredArticles = articles.Select(article => 
            {
                var title = (article.Title ?? "").ToLower();
                var description = (article.Description ?? "").ToLower();
                var content = (article.Content ?? "").ToLower();
                
                // Tính điểm dựa trên số lượng từ khóa xuất hiện
                int relevanceScore = 0;
                foreach (var keyword in vietnamKeywords)
                {
                    // Từ khóa trong tiêu đề quan trọng hơn
                    if (title.Contains(keyword)) relevanceScore += 3;
                    if (description.Contains(keyword)) relevanceScore += 2;
                    if (content.Contains(keyword)) relevanceScore += 1;
                }
                
                return new { Article = article, Score = relevanceScore };
            })
            .OrderByDescending(item => item.Score) // Sắp xếp theo điểm giảm dần
            .Select(item => item.Article)
            .ToList();
            
            _logger.LogInformation($"Ranked {scoredArticles.Count} articles by Vietnamese relevance");
            
            return scoredArticles;
        }
    }

    public class NewsApiResponse
    {
        public string Status { get; set; } = "";
        public string? Message { get; set; }
        public int TotalResults { get; set; }
        public List<Article>? Articles { get; set; }
    }
}
