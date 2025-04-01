using System.Threading.Tasks;
using VtvNewsApp.Models;

namespace VtvNewsApp.Services
{
    public interface ITranslationService
    {
        Task<(string Title, string Description)> TranslateArticleAsync(string? title, string? description);
        string ConvertUtcToVnTime(string? utcTimeStr);
    }
}