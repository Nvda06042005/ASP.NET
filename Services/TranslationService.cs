using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VtvNewsApp.Models;

namespace VtvNewsApp.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TranslationService> _logger;

        // Sử dụng Google Translate API (miễn phí thông qua API không chính thức)
        private readonly string _primaryApiUrl = "https://translate.googleapis.com/translate_a/single";
        // Backup API: MyMemory Translate API
        private readonly string _backupApiUrl = "https://api.mymemory.translated.net/get";
        // LibreTranslate API (tùy chọn) - có thể tự host hoặc sử dụng dịch vụ công cộng
        private readonly string _libreTranlateUrl = "https://libretranslate.com/translate";

        public TranslationService(HttpClient httpClient, ILogger<TranslationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(string Title, string Description)> TranslateArticleAsync(string? title, string? description)
        {
            // Đảm bảo title và description không null
            if (title == null) title = string.Empty;
            if (description == null) description = string.Empty;
            
            _logger.LogInformation($"BEGIN TRANSLATION DEBUG =========");
            _logger.LogInformation($"Original Title: {title}");
            _logger.LogInformation($"Original Description: {description}");
            
            try
            {
                // Dịch tiêu đề và mô tả
                string translatedTitle = !string.IsNullOrWhiteSpace(title) 
                    ? await TranslateTextAsync(title) 
                    : string.Empty;
                
                string translatedDescription = !string.IsNullOrWhiteSpace(description) 
                    ? await TranslateTextAsync(description) 
                    : string.Empty;
                
                _logger.LogInformation($"Translated Title: {translatedTitle}");
                _logger.LogInformation($"Translated Description: {translatedDescription}");
                _logger.LogInformation($"END TRANSLATION DEBUG ===========");
                
                // Nếu dịch không thành công (trả về chuỗi rỗng), sử dụng văn bản gốc
                if (string.IsNullOrWhiteSpace(translatedTitle)) translatedTitle = title;
                if (string.IsNullOrWhiteSpace(translatedDescription)) translatedDescription = description;
                
                return (translatedTitle, translatedDescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during translation. Using original text.");
                return (title, description);
            }
        }
        
        private async Task<string> TranslateTextAsync(string text)
        {
            try
            {
                // Thử dịch với Google Translate API trước
                var result = await TryGoogleTranslateAsync(text);
                
                // Nếu Google Translate thất bại, thử MyMemory API
                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogWarning("Google Translate failed, trying MyMemory API");
                    result = await TryMyMemoryTranslateAsync(text);
                
                    // Nếu cả hai API đều thất bại, dùng phương thức dự phòng
                    if (string.IsNullOrWhiteSpace(result))
                    {
                        _logger.LogWarning("MyMemory API failed, using fallback translation");
                        result = FallbackTranslate(text);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"All translation methods failed for text: {text}");
                return FallbackTranslate(text);
            }
        }
        
        private async Task<string> TryGoogleTranslateAsync(string text)
        {
            try
            {
                // Giới hạn độ dài văn bản để tránh vượt quá giới hạn API
                if (text.Length > 1000)
                {
                    text = text.Substring(0, 1000);
                }
                
                // Thêm User-Agent để tránh bị chặn
                var request = new HttpRequestMessage(HttpMethod.Get, 
                    $"{_primaryApiUrl}?client=gtx&sl=auto&tl=vi&dt=t&q={Uri.EscapeDataString(text)}");
                    
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                
                _logger.LogInformation($"Sending translation request to Google Translate API");
                
                // Gửi yêu cầu đến API
                var response = await _httpClient.SendAsync(request);
                
                // Kiểm tra phản hồi
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Google Translate API returned status code: {response.StatusCode}");
                    return string.Empty;
                }
                
                // Đọc phản hồi
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Parse phản hồi JSON - Google Translate trả về mảng phức tạp
                using var doc = JsonDocument.Parse(responseContent);
                
                // Xây dựng kết quả dịch từ mảng các đoạn
                var translationArray = doc.RootElement[0];
                var stringBuilder = new StringBuilder();
                
                for (int i = 0; i < translationArray.GetArrayLength(); i++)
                {
                    var translatedPart = translationArray[i][0];
                    if (translatedPart.ValueKind != JsonValueKind.Null)
                    {
                        stringBuilder.Append(translatedPart.GetString());
                    }
                }
                
                var result = stringBuilder.ToString();
                _logger.LogInformation($"Google Translate successful: {text} -> {result}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error with Google Translate API: {text}");
                return string.Empty;
            }
        }
        
        private async Task<string> TryMyMemoryTranslateAsync(string text)
        {
            try
            {
                // Giới hạn độ dài văn bản để tránh vượt quá giới hạn API
                if (text.Length > 500)
                {
                    text = text.Substring(0, 500);
                }
                
                // Xây dựng URL với langpair=auto|vi để tự động nhận diện ngôn ngữ nguồn
                string url = $"{_backupApiUrl}?q={Uri.EscapeDataString(text)}&langpair=auto|vi";
                
                _logger.LogInformation($"Sending translation request to MyMemory API");
                
                // Gửi yêu cầu GET đến API
                var response = await _httpClient.GetAsync(url);
                
                // Kiểm tra phản hồi
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"MyMemory API returned status code: {response.StatusCode}");
                    return string.Empty;
                }
                
                // Đọc phản hồi
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Parse phản hồi JSON
                using var doc = JsonDocument.Parse(responseContent);
                var responseData = doc.RootElement.GetProperty("responseData");
                var translatedText = responseData.GetProperty("translatedText").GetString();
                
                _logger.LogInformation($"MyMemory translation successful: {text} -> {translatedText}");
                
                return translatedText ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error with MyMemory API: {text}");
                return string.Empty;
            }
        }
        
        // Tùy chọn: Thêm phương thức dùng LibreTranslate nếu cần
        private async Task<string> TryLibreTranslateAsync(string text)
        {
            try
            {
                // Nếu văn bản quá dài, cắt bớt
                if (text.Length > 1000)
                {
                    text = text.Substring(0, 1000);
                }
                
                // Tạo JSON payload
                var payload = JsonSerializer.Serialize(new 
                {
                    q = text,
                    source = "auto",
                    target = "vi",
                    format = "text"
                });
                
                // Tạo request với Content-Type là application/json
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                
                _logger.LogInformation($"Sending translation request to LibreTranslate API");
                
                // Gửi POST request
                var response = await _httpClient.PostAsync(_libreTranlateUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"LibreTranslate API returned status code: {response.StatusCode}");
                    return string.Empty;
                }
                
                // Đọc phản hồi
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Parse phản hồi JSON
                using var doc = JsonDocument.Parse(responseContent);
                var translatedText = doc.RootElement.GetProperty("translatedText").GetString();
                
                _logger.LogInformation($"LibreTranslate successful: {text} -> {translatedText}");
                
                return translatedText ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error with LibreTranslate API: {text}");
                return string.Empty;
            }
        }
        
        // Phương thức dự phòng với từ điển mở rộng
        private string FallbackTranslate(string text)
        {
            try
            {
                // Dictionary từ nhiều ngôn ngữ sang Tiếng Việt
                var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // Tiếng Anh sang tiếng Việt
                    {"Vietnam", "Việt Nam"},
                    {"Vietnamese", "Việt Nam"},
                    {"Hanoi", "Hà Nội"},
                    {"Ho Chi Minh City", "Thành phố Hồ Chí Minh"},
                    {"Saigon", "Sài Gòn"},
                    {"ENEOS", "ENEOS"},
                    {"PVEP", "PVEP"},
                    {"bloc", "lô"},
                    {"contract", "hợp đồng"},
                    {"production", "sản xuất"},
                    {"offshore", "ngoài khơi"},
                    {"signs", "ký kết"},
                    {"signed", "đã ký"},
                    {"economy", "kinh tế"},
                    {"economic", "kinh tế"},
                    {"politics", "chính trị"},
                    {"political", "chính trị"},
                    {"government", "chính phủ"},
                    {"energy", "năng lượng"},
                    {"oil", "dầu"},
                    {"gas", "khí đốt"},
                    {"partage", "chia sẻ"},
                    {"share", "chia sẻ"},
                    {"company", "công ty"},
                    {"japanese", "Nhật Bản"},
                    {"french", "Pháp"},
                    {"american", "Mỹ"},
                    {"china", "Trung Quốc"},
                    {"chinese", "Trung Quốc"},
                    {"news", "tin tức"},
                    {"latest", "mới nhất"},
                    {"update", "cập nhật"},
                    {"president", "chủ tịch"},
                    {"minister", "bộ trưởng"},
                    {"ministry", "bộ"},
                    {"agreement", "thỏa thuận"},
                    {"sign", "ký"},
                    {"cooperation", "hợp tác"},
                    {"development", "phát triển"},
                    
                    // Bổ sung từ các ngôn ngữ khác - những từ phổ biến nhất
                    // Tiếng Pháp
                    {"bonjour", "xin chào"},
                    {"merci", "cảm ơn"},
                    {"France", "Pháp"},
                    {"Paris", "Paris"},
                    
                    // Tiếng Trung
                    {"你好", "xin chào"},
                    {"谢谢", "cảm ơn"},
                    {"中国", "Trung Quốc"},
                    {"北京", "Bắc Kinh"},
                    
                    // Tiếng Nhật
                    {"こんにちは", "xin chào"},
                    {"ありがとう", "cảm ơn"},
                    {"日本", "Nhật Bản"},
                    {"東京", "Tokyo"},
                    
                    // Tiếng Đức
                    {"Hallo", "xin chào"},
                    {"Danke", "cảm ơn"},
                    {"Deutschland", "Đức"},
                    {"Berlin", "Berlin"},
                    
                    // Tiếng Nga
                    {"Привет", "xin chào"},
                    {"Спасибо", "cảm ơn"},
                    {"Россия", "Nga"},
                    {"Москва", "Mátxcơva"},
                    
                    // Tiếng Tây Ban Nha
                    {"Hola", "xin chào"},
                    {"Gracias", "cảm ơn"},
                    {"España", "Tây Ban Nha"},
                    {"Madrid", "Madrid"},
                    
                    // Tiếng Ý
                    {"Ciao", "xin chào"},
                    {"Grazie", "cảm ơn"},
                    {"Italia", "Ý"},
                    {"Roma", "Roma"}
                };
                
                // Thay thế trong văn bản
                foreach (var entry in translations)
                {
                    text = text.Replace(entry.Key, entry.Value, StringComparison.OrdinalIgnoreCase);
                }
                
                return text;
            }
            catch
            {
                return text; // Trả về văn bản gốc nếu có lỗi
            }
        }

        public string ConvertUtcToVnTime(string? utcTimeStr)
        {
            if (string.IsNullOrEmpty(utcTimeStr))
                return string.Empty;

            try
            {
                if (DateTime.TryParse(utcTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime utcTime))
                {
                    // Chuyển đổi UTC sang giờ Việt Nam (UTC+7)
                    var vnTime = utcTime.AddHours(7);
                    return vnTime.ToString("yyyy-MM-dd HH:mm:ss") + " (Giờ VN)";
                }
                return utcTimeStr;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error converting time: {utcTimeStr}");
                return utcTimeStr;
            }
        }
    }
}