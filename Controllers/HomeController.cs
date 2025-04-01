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
            // Sử dụng từ khóa "Vietnam" để luôn lọc tin tức liên quan đến Việt Nam
            return await GetNewsForCategory("Vietnam", "home", "Trang Chủ");
        }

        [HttpPost]
        public async Task<IActionResult> Index(NewsViewModel model)
        {
            // Thêm "Vietnam" vào từ khóa tìm kiếm nếu chưa có
            model.Query = AddVietnamKeywordIfNeeded(model.Query);
            return await ProcessSearch(model);
        }

        [HttpGet]
        public async Task<IActionResult> ThoiSu()
        {
            return await GetNewsForCategory("Vietnam chính trị", "thoisu", "Thời Sự");
        }

        [HttpPost]
        public async Task<IActionResult> ThoiSu(NewsViewModel model)
        {
            model.Query = AddVietnamKeywordIfNeeded(model.Query);
            return await ProcessSearch(model);
        }

        [HttpGet]
        public async Task<IActionResult> KinhTe()
        {
            return await GetNewsForCategory("Vietnam kinh tế", "kinhte", "Kinh Tế");
        }

        [HttpPost]
        public async Task<IActionResult> KinhTe(NewsViewModel model)
        {
            model.Query = AddVietnamKeywordIfNeeded(model.Query);
            return await ProcessSearch(model);
        }

        [HttpGet]
        public async Task<IActionResult> TheGioi()
        {
            // Đối với thế giới, chúng ta vẫn cần thêm Vietnam để ưu tiên tin tức liên quan đến Việt Nam
            return await GetNewsForCategory("Vietnam thế giới", "thegioi", "Thế Giới");
        }

        [HttpPost]
        public async Task<IActionResult> TheGioi(NewsViewModel model)
        {
            model.Query = AddVietnamKeywordIfNeeded(model.Query);
            return await ProcessSearch(model);
        }

        [HttpGet]
        public async Task<IActionResult> TheThao()
        {
            return await GetNewsForCategory("Vietnam thể thao", "thethao", "Thể Thao");
        }

        [HttpPost]
        public async Task<IActionResult> TheThao(NewsViewModel model)
        {
            model.Query = AddVietnamKeywordIfNeeded(model.Query);
            return await ProcessSearch(model);
        }

        [HttpGet]
        public async Task<IActionResult> GiaiTri()
        {
            return await GetNewsForCategory("Vietnam giải trí", "giaitri", "Giải Trí");
        }

        [HttpPost]
        public async Task<IActionResult> GiaiTri(NewsViewModel model)
        {
            model.Query = AddVietnamKeywordIfNeeded(model.Query);
            return await ProcessSearch(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Phương thức hỗ trợ
        private async Task<IActionResult> GetNewsForCategory(string query, string activeTab, string categoryName)
        {
            var viewModel = new NewsViewModel
            {
                ActiveTab = activeTab,
                CategoryName = categoryName,
                Query = query // Lưu từ khóa tìm kiếm vào model để hiển thị trong form
            };

            try
            {
                var articles = await _newsService.GetArticlesAsync(query, null, "popularity", 100);
                var filteredArticles = _newsService.FilterArticles(articles, query);
                viewModel.Articles = await TranslateArticlesAsync(filteredArticles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching {categoryName} news");
                viewModel.ErrorMessage = ex.Message;
            }

            return View("Index", viewModel);
        }

        private async Task<IActionResult> ProcessSearch(NewsViewModel model)
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
                    model.SortBy ?? "popularity",
                    100);

                var filteredArticles = _newsService.FilterArticles(articles, model.Query);
                viewModel.Articles = await TranslateArticlesAsync(filteredArticles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing search");
                viewModel.ErrorMessage = ex.Message;
            }

            return View("Index", viewModel);
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
        
        // Thêm từ khóa Vietnam vào query nếu chưa có
        private string AddVietnamKeywordIfNeeded(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return "Vietnam";
                
            // Kiểm tra xem query đã có từ khóa liên quan đến Việt Nam chưa
            var vietnamKeywords = new[] 
            { 
                "vietnam", "việt nam", "viet nam", "vietnamese", "việt", "viet", 
                "hanoi", "hà nội", "ha noi", "ho chi minh", "hồ chí minh", 
                "saigon", "sài gòn", "sai gon", "đà nẵng", "da nang", "hue", "huế"
            };
            
            var lowerQuery = query.ToLower();
            bool hasVietnamKeyword = vietnamKeywords.Any(keyword => lowerQuery.Contains(keyword));
            
            if (hasVietnamKeyword)
                return query;
                
            // Nếu chưa có, thêm "Vietnam" vào
            return $"Vietnam {query}";
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}