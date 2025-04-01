using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VtvNewsApp.Models;

namespace VtvNewsApp.Services
{
    public interface INewsService
    {
        Task<List<Article>> GetArticlesAsync(string query, DateTime? fromDate, string sortBy, int pageSize = 100);
        List<Article> FilterArticles(List<Article> articles, string term);
    }
}