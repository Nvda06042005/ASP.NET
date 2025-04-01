using System.Collections.Generic;

namespace VtvNewsApp.Models
{
    public class NewsViewModel
    {
        public List<Article> Articles { get; set; } = new List<Article>();
        public string ErrorMessage { get; set; } = string.Empty;
        public string ActiveTab { get; set; } = "home";
        public string CategoryName { get; set; } = "Trang Chủ";
        public string Query { get; set; } = string.Empty;
        public string FromDate { get; set; } = string.Empty;
        public string SortBy { get; set; } = "popularity";
    }
}