using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BikeDrive.Data;

// Фото товаров лежат в папке Images рядом с приложением, в БД хранится имя файла
public static class PhotoStorage
{
    public const int MaxWidth = 300;
    public const int MaxHeight = 200;

    private static string Folder => Path.Combine(AppContext.BaseDirectory, "Images");

    public static ImageSource Load(string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            string path = Path.Combine(Folder, fileName);
            if (File.Exists(path))
            {
                return LoadFromFile(path);
            }
        }

        return Placeholder();
    }

    public static ImageSource Placeholder()
    {
        return new BitmapImage(new Uri("pack://application:,,,/Resources/picture.png"));
    }

    // OnLoad читает файл целиком и не держит его открытым, иначе старое фото нельзя удалить
    public static BitmapImage LoadFromFile(string path)
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }

    // Уменьшает картинку до 300×200 с сохранением пропорций и сохраняет в папку приложения
    public static string Save(string sourcePath, string article)
    {
        BitmapImage source = LoadFromFile(sourcePath);
        double scale = Math.Min(1.0, Math.Min((double)MaxWidth / source.PixelWidth, (double)MaxHeight / source.PixelHeight));
        BitmapSource resized = scale < 1.0
            ? new TransformedBitmap(source, new ScaleTransform(scale, scale))
            : source;

        Directory.CreateDirectory(Folder);
        string fileName = $"{article}_{DateTime.Now:yyyyMMddHHmmss}.jpg";

        JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = 90 };
        encoder.Frames.Add(BitmapFrame.Create(resized));
        using FileStream stream = File.Create(Path.Combine(Folder, fileName));
        encoder.Save(stream);
        return fileName;
    }

    public static void Delete(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        string path = Path.Combine(Folder, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
