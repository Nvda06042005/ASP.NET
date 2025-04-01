# VTV News Clone - ASP.NET Core MVC

Đây là phiên bản ASP.NET Core của ứng dụng VTV News Clone, được chuyển đổi từ ứng dụng Flask.

## Cách chạy trong Visual Studio Code

### Yêu cầu

- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) hoặc cao hơn
- [Visual Studio Code](https://code.visualstudio.com/)
- [C# Extension for VS Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

### Các bước chạy

1. Mở thư mục trong Visual Studio Code:
```
code .
```

2. Mở Terminal trong VS Code (Ctrl+` hoặc View > Terminal)

3. Khôi phục các gói NuGet:
```
dotnet restore
```

4. Build dự án:
```
dotnet build
```

5. Chạy ứng dụng:
```
dotnet run
```

6. Mở trình duyệt và truy cập vào `https://localhost:5001` hoặc `http://localhost:5000`

## Cấu trúc dự án

- **Program.cs**: Điểm khởi đầu ứng dụng, cấu hình dịch vụ và middleware
- **Controllers/**: Chứa các controller xử lý các request
- **Models/**: Chứa các model dữ liệu
- **Services/**: Chứa các dịch vụ như NewsService, TranslationService
- **Views/**: Chứa các Razor view để hiển thị giao diện người dùng
- **wwwroot/**: Chứa các tài nguyên tĩnh như CSS

## Các tính năng

- Hiển thị tin tức từ News API
- Phân chia theo chuyên mục
- Tìm kiếm bài báo theo từ khóa
- Lọc bài báo theo ngày và thứ tự sắp xếp
- Dịch tiêu đề và mô tả bài báo sang tiếng Việt
- Chuyển đổi thời gian từ UTC sang múi giờ Việt Nam
- Giao diện đáp ứng (responsive)

## Cấu hình

API key cho News API được cấu hình trong file `appsettings.json`. Bạn có thể thay đổi key này nếu cần:

```json
{
  "NewsApiKey": "your-api-key"
}
```

## Lưu ý

- Phiên bản demo sử dụng API key mặc định, có thể bị giới hạn số lượng lượt gọi API.
- Chức năng dịch được mô phỏng và không sử dụng API dịch thực tế, bạn cần tích hợp API dịch thực tế như Google Translate API nếu cần.

## Debug trong VS Code

1. Chuyển đến tab "Run and Debug" (Ctrl+Shift+D)
2. Nhấp "create a launch.json file" và chọn ".NET Core"
3. VS Code sẽ tạo file cấu hình launch.json phù hợp
4. Bạn có thể đặt breakpoint và sau đó nhấn F5 để bắt đầu debug
