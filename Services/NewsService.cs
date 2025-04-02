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
        private bool _useMockData = false;

        public NewsService(HttpClient httpClient, ILogger<NewsService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiKey = _configuration["NewsApiKey"] ?? "4dee47ab8112c1b949ecda490be79a17"; // Sử dụng key mặc định nếu không có cấu hình
            
            // Thêm timeout cho HttpClient
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<List<Article>> GetArticlesAsync(string query, DateTime? fromDate, string sortBy, int pageSize = 100)
        {
            // Nếu đã gặp lỗi trước đó và quyết định sử dụng dữ liệu mẫu
            if (_useMockData)
            {
                _logger.LogWarning("Đang sử dụng dữ liệu mẫu do gặp lỗi với API trước đó");
                return GetMockData();
            }
            
            try
            {
                // Thử tìm kiếm tin tức bằng NewsAPI thay vì GNews
                string newsApiKey = "bb42bbe28f674f9c971b294deea569be"; // API key miễn phí cho NewsAPI
                var url = "https://newsapi.org/v2/everything";
                
                // Chuẩn bị từ khóa tìm kiếm
                string searchQuery = query;
                if (!searchQuery.Contains("vietnam", StringComparison.OrdinalIgnoreCase) && 
                    !searchQuery.Contains("việt nam", StringComparison.OrdinalIgnoreCase))
                {
                    // Thêm tiếng Việt vào ngôn ngữ tìm kiếm thay vì giới hạn bằng từ khóa
                    searchQuery = searchQuery;
                }
                
                var queryParams = new Dictionary<string, string>
                {
                    { "q", searchQuery },
                    { "apiKey", newsApiKey },
                    { "pageSize", pageSize.ToString() },
                    { "language", "vi" }, // Tập trung tìm kiếm nội dung tiếng Việt
                    { "sortBy", sortBy ?? "publishedAt" }
                };

                // Thêm tham số from nếu có
                if (fromDate.HasValue)
                {
                    queryParams.Add("from", fromDate.Value.ToString("yyyy-MM-dd"));
                }

                var requestUrl = url + "?" + string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
                
                _logger.LogInformation($"Calling NewsAPI with URL: {requestUrl}");
                
                try
                {
                    var response = await _httpClient.GetAsync(requestUrl);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"HTTP Error from NewsAPI: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                        
                        // Thử với GNews API nếu NewsAPI không thành công
                        return await GetArticlesFromGNewsAsync(query, fromDate, sortBy, pageSize);
                    }
                    
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug($"NewsAPI response: {content}");
                    
                    try
                    {
                        var newsApiResponse = JsonSerializer.Deserialize<NewsApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (newsApiResponse == null || newsApiResponse.Articles == null)
                        {
                            _logger.LogError("Failed to deserialize NewsAPI response");
                            return await GetArticlesFromGNewsAsync(query, fromDate, sortBy, pageSize);
                        }
                        
                        // Chuyển đổi kết quả sang model của ứng dụng (đã phù hợp)
                        return newsApiResponse.Articles;
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, $"JSON Deserialization error with NewsAPI: {jsonEx.Message}");
                        return await GetArticlesFromGNewsAsync(query, fromDate, sortBy, pageSize);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error with NewsAPI: {ex.Message}");
                    return await GetArticlesFromGNewsAsync(query, fromDate, sortBy, pageSize);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"General error in GetArticlesAsync: {ex.Message}");
                _useMockData = true;
                return GetMockData();
            }
        }

        private async Task<List<Article>> GetArticlesFromGNewsAsync(string query, DateTime? fromDate, string sortBy, int pageSize)
        {
            try
            {
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
                    _logger.LogError($"HTTP Error from GNews: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    _useMockData = true;
                    return GetMockData();
                }
                
                var content = await response.Content.ReadAsStringAsync();
                
                var gnewsResponse = JsonSerializer.Deserialize<GNewsResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (gnewsResponse == null)
                {
                    _logger.LogError("Failed to deserialize GNews API response");
                    _useMockData = true;
                    return GetMockData();
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
                
                return articles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error with GNews API: {ex.Message}");
                _useMockData = true;
                return GetMockData();
            }
        }

        public List<Article> FilterArticles(List<Article> articles, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) 
                return articles;
                
            // Tách các từ khóa tìm kiếm và lọc riêng lẻ thay vì toàn bộ chuỗi
            var searchTerms = term.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2) // Chỉ sử dụng từ có ý nghĩa (loại bỏ từ ngắn)
                .ToList();
            
            if (!searchTerms.Any())
                return articles;
            
            // Một bài viết chỉ cần khớp với một trong các từ khóa là được
            return articles.Where(article => 
            {
                string title = (article.Title ?? "").ToLower();
                string description = (article.Description ?? "").ToLower();
                string content = (article.Content ?? "").ToLower();
                
                // Kiểm tra xem bài viết có chứa ít nhất một trong các từ khóa không
                return searchTerms.Any(term => 
                    title.Contains(term) || 
                    description.Contains(term) || 
                    content.Contains(term)
                );
            }).ToList();
        }
        
        // Tạo dữ liệu mẫu khi API không hoạt động
        private List<Article> GetMockData()
        {
            var currentTime = DateTime.UtcNow.ToString("o");
            return new List<Article>
            {
                new Article 
                {
                    Title = "Việt Nam tiếp tục duy trì tăng trưởng kinh tế ổn định",
                    Description = "Theo báo cáo mới nhất, kinh tế Việt Nam duy trì đà tăng trưởng ổn định bất chấp nhiều thách thức từ thị trường toàn cầu.",
                    UrlToImage = "https://via.placeholder.com/400x250?text=Kinh+Te+Viet+Nam",
                    PublishedAt = currentTime,
                    Url = "#",
                    Source = new Source { Name = "Báo VTV" }
                },
                new Article 
                {
                    Title = "Hà Nội triển khai nhiều dự án hạ tầng mới",
                    Description = "Thành phố Hà Nội đang đẩy mạnh triển khai các dự án hạ tầng giao thông trọng điểm nhằm giảm ùn tắc và phát triển đô thị bền vững.",
                    UrlToImage = "https://via.placeholder.com/400x250?text=Ha+Noi+Projects",
                    PublishedAt = currentTime,
                    Url = "#",
                    Source = new Source { Name = "Báo Chính Phủ" }
                },
                new Article 
                {
                    Title = "Đội tuyển Việt Nam chuẩn bị cho vòng loại World Cup",
                    Description = "HLV Philippe Troussier đang tích cực chuẩn bị cho đội tuyển Việt Nam trước các trận đấu vòng loại World Cup sắp tới.",
                    UrlToImage = "https://via.placeholder.com/400x250?text=Vietnam+Football",
                    PublishedAt = currentTime,
                    Url = "#",
                    Source = new Source { Name = "VTV Sports" }
                }
            };
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