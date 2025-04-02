using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VtvNewsApp.Models;
using VtvNewsApp.Services;

namespace VtvNewsApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly INewsService _newsService;
        private readonly ITranslationService _translationService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            INewsService newsService, 
            ITranslationService translationService, 
            ILogger<HomeController> logger)
        {
            _newsService = newsService;
            _translationService = translationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Mở rộng từ khóa tìm kiếm
            var viewModel = await GetNewsViewModel("Vietnam OR Việt Nam tin tức", "home", "Trang Chủ");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(NewsViewModel model)
        {
            // Không bắt buộc phải có từ khóa Vietnam
            var resultModel = await ProcessSearch(model);
            return View(resultModel);
        }

        [HttpGet]
        public async Task<IActionResult> ThoiSu()
        {
            var viewModel = await GetNewsViewModel("tin tức chính trị", "thoisu", "Thời Sự");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ThoiSu(NewsViewModel model)
        {
            var resultModel = await ProcessSearch(model);
            return View(resultModel);
        }

        [HttpGet]
        public async Task<IActionResult> KinhTe()
        {
            var viewModel = await GetNewsViewModel("kinh tế tài chính thương mại", "kinhte", "Kinh Tế");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> KinhTe(NewsViewModel model)
        {
            var resultModel = await ProcessSearch(model);
            return View(resultModel);
        }

        [HttpGet]
        public async Task<IActionResult> TheGioi()
        {
            var viewModel = await GetNewsViewModel("thế giới quốc tế", "thegioi", "Thế Giới");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> TheGioi(NewsViewModel model)
        {
            var resultModel = await ProcessSearch(model);
            return View(resultModel);
        }

        [HttpGet]
        public async Task<IActionResult> TheThao()
        {
            var viewModel = await GetNewsViewModel("thể thao bóng đá world cup olympic", "thethao", "Thể Thao");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> TheThao(NewsViewModel model)
        {
            var resultModel = await ProcessSearch(model);
            return View(resultModel);
        }

        [HttpGet]
        public async Task<IActionResult> GiaiTri()
        {
            var viewModel = await GetNewsViewModel("giải trí nghệ sĩ điện ảnh âm nhạc", "giaitri", "Giải Trí");
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> GiaiTri(NewsViewModel model)
        {
            var resultModel = await ProcessSearch(model);
            return View(resultModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Phương thức hỗ trợ
        private async Task<NewsViewModel> GetNewsViewModel(string query, string activeTab, string categoryName)
        {
            var viewModel = new NewsViewModel
            {
                ActiveTab = activeTab,
                CategoryName = categoryName,
                Query = query // Lưu từ khóa tìm kiếm vào model để hiển thị trong form
            };

            try
            {
                // Tăng số lượng kết quả tìm kiếm
                var articles = await _newsService.GetArticlesAsync(query, null, "relevancy", 50);
                
                if (articles == null || !articles.Any())
                {
                    _logger.LogWarning($"Không tìm thấy bài viết nào cho danh mục {categoryName} với từ khóa {query}");
                    
                    // Thử lại với ít từ khóa hơn nếu không tìm thấy kết quả
                    string simpleQuery = GetSimplifiedQuery(query);
                    if (simpleQuery != query)
                    {
                        articles = await _newsService.GetArticlesAsync(simpleQuery, null, "relevancy", 50);
                    }
                    
                    if (articles == null || !articles.Any())
                    {
                        viewModel.ErrorMessage = "Không tìm thấy bài viết phù hợp. Vui lòng thử lại sau.";
                        return viewModel;
                    }
                }
                
                // Không lọc kết quả theo từ khóa để hiển thị nhiều bài viết hơn
                viewModel.Articles = await TranslateArticlesAsync(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải tin tức cho danh mục {categoryName}: {ex.Message}");
                viewModel.ErrorMessage = $"Đã xảy ra lỗi khi tải dữ liệu: {ex.Message}";
            }

            return viewModel;
        }

        private async Task<NewsViewModel> ProcessSearch(NewsViewModel model)
        {
            var viewModel = new NewsViewModel
            {
                ActiveTab = model.ActiveTab,
                CategoryName = model.CategoryName,
                Query = model.Query,
                FromDate = model.FromDate,
                SortBy = model.SortBy
            };

            try
            {
                DateTime? fromDate = null;
                if (!string.IsNullOrEmpty(model.FromDate))
                {
                    fromDate = DateTime.Parse(model.FromDate);
                }

                var articles = await _newsService.GetArticlesAsync(
                    model.Query,
                    fromDate,
                    model.SortBy ?? "relevancy",
                    50);

                if (articles == null || !articles.Any())
                {
                    _logger.LogWarning($"Không tìm thấy kết quả tìm kiếm cho {model.Query}");
                    
                    // Thử lại với ít từ khóa hơn nếu không tìm thấy kết quả
                    string simpleQuery = GetSimplifiedQuery(model.Query);
                    if (simpleQuery != model.Query)
                    {
                        articles = await _newsService.GetArticlesAsync(simpleQuery, fromDate, model.SortBy ?? "relevancy", 50);
                    }
                    
                    if (articles == null || !articles.Any())
                    {
                        viewModel.ErrorMessage = "Không tìm thấy kết quả phù hợp với từ khóa tìm kiếm.";
                        return viewModel;
                    }
                }

                // Không lọc kết quả theo từ khóa để hiển thị nhiều bài viết hơn
                viewModel.Articles = await TranslateArticlesAsync(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tìm kiếm: {ex.Message}");
                viewModel.ErrorMessage = $"Đã xảy ra lỗi khi tìm kiếm: {ex.Message}";
            }

            return viewModel;
        }

        private async Task<List<Article>> TranslateArticlesAsync(List<Article> articles)
        {
            var translatedArticles = new List<Article>();

            foreach (var article in articles)
            {
                var (translatedTitle, translatedDescription) = await _translationService.TranslateArticleAsync(
                    article.Title, 
                    article.Description);

                article.TranslatedTitle = translatedTitle;
                article.TranslatedDescription = translatedDescription;
                article.VnPublishedAt = _translationService.ConvertUtcToVnTime(article.PublishedAt);

                translatedArticles.Add(article);
            }

            return translatedArticles;
        }
        
        // Rút gọn từ khóa tìm kiếm khi không tìm thấy kết quả
        private string GetSimplifiedQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return query;
                
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 2)
                return query;
                
            // Chỉ lấy 1-2 từ khóa quan trọng nhất
            string[] importantKeywords = new[] { 
                "tin tức", "thời sự", "chính trị", "kinh tế", "tài chính", "thế giới", 
                "quốc tế", "thể thao", "bóng đá", "giải trí", "âm nhạc", "điện ảnh" 
            };
            
            var simplifiedWords = words.Where(w => importantKeywords.Any(k => w.Contains(k))).Take(2);
            if (!simplifiedWords.Any())
                return words.Take(2).Aggregate((a, b) => $"{a} {b}");
                
            return simplifiedWords.Aggregate((a, b) => $"{a} {b}");
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}