using System.Globalization;
using System.Windows;
using BikeDrive.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace BikeDrive.Views;

public partial class ProductEditWindow : Window
{
    private readonly string? editedArticle;
    private string? oldPhotoFile;
    private string? newPhotoSource;

    // article == null: добавление нового товара, иначе редактирование существующего
    public ProductEditWindow(string? article)
    {
        InitializeComponent();
        editedArticle = article;

        bool isNew = article == null;
        Title = isNew ? "ВелосипедДрайв — Добавление товара" : "ВелосипедДрайв — Редактирование товара";
        HeaderTextBlock.Text = isNew ? "Добавление товара" : "Редактирование товара";

        // ID при добавлении не показывается: он вычисляется при сохранении
        IdPanel.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;
        DeleteButton.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;
        ArticleTextBox.IsReadOnly = !isNew;

        Loaded += (_, _) => LoadData();
    }

    public bool IsChanged { get; private set; }

    private void LoadData()
    {
        try
        {
            using BikeDriveContext context = new BikeDriveContext();
            CategoryComboBox.ItemsSource = context.Categories.OrderBy(item => item.CategoryName).ToList();
            ManufacturerComboBox.ItemsSource = context.Manufacturers.OrderBy(item => item.ManufacturerName).ToList();
            SupplierComboBox.ItemsSource = context.Suppliers.OrderBy(item => item.SupplierName).ToList();
            UnitComboBox.ItemsSource = context.Units.OrderBy(item => item.UnitName).ToList();

            if (editedArticle == null)
            {
                PhotoImage.Source = PhotoStorage.Placeholder();
                StockTextBox.Text = "0";
                DiscountTextBox.Text = "0";
                return;
            }

            Product? product = context.Products.FirstOrDefault(item => item.Article == editedArticle);
            if (product == null)
            {
                ShowError("Товар не найден: возможно, его уже удалил другой пользователь. Список товаров будет обновлён.",
                    "Товар не найден");
                IsChanged = true;
                Close();
                return;
            }

            IdTextBox.Text = product.ProductId.ToString();
            ArticleTextBox.Text = product.Article;
            NameTextBox.Text = product.ProductName;
            DescriptionTextBox.Text = product.Description ?? "";
            PriceTextBox.Text = product.Price.ToString("0.00", CultureInfo.CurrentCulture);
            StockTextBox.Text = product.Stock.ToString();
            DiscountTextBox.Text = product.Discount.ToString();
            SelectById(CategoryComboBox, (Category item) => item.CategoryId == product.CategoryId);
            SelectById(ManufacturerComboBox, (Manufacturer item) => item.ManufacturerId == product.ManufacturerId);
            SelectById(SupplierComboBox, (Supplier item) => item.SupplierId == product.SupplierId);
            SelectById(UnitComboBox, (Unit item) => item.UnitId == product.UnitId);
            oldPhotoFile = product.Photo;
            PhotoImage.Source = PhotoStorage.Load(product.Photo);
        }
        catch (Exception exception)
        {
            ShowError("Не удалось загрузить данные товара из базы данных. Проверьте подключение к серверу.\n\n"
                + exception.Message, "Ошибка загрузки данных");
            Close();
        }
    }

    private static void SelectById<T>(System.Windows.Controls.ComboBox comboBox, Func<T, bool> match)
    {
        comboBox.SelectedItem = ((IEnumerable<T>)comboBox.ItemsSource).FirstOrDefault(match);
    }

    private void ChoosePhotoButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            Title = "Выбор фото товара",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            PhotoImage.Source = PhotoStorage.LoadFromFile(dialog.FileName);
            newPhotoSource = dialog.FileName;
        }
        catch (Exception)
        {
            ShowError("Выбранный файл не удалось открыть как изображение. Выберите файл JPG, PNG или BMP.",
                "Неверный файл");
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        string? errors = Validate(out decimal price, out int stock, out byte discount);
        if (errors != null)
        {
            MessageBox.Show("Товар не сохранён. Исправьте ошибки:\n\n" + errors,
                "Ошибка ввода данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string article = ArticleTextBox.Text.Trim();
        string? savedPhoto = null;
        try
        {
            using BikeDriveContext context = new BikeDriveContext();
            Product? product;
            if (editedArticle == null)
            {
                if (context.Products.Any(item => item.Article == article))
                {
                    MessageBox.Show($"Товар с артикулом «{article}» уже есть. Укажите другой артикул.",
                        "Ошибка ввода данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ID нового товара: на единицу больше максимального в БД
                int maxId = context.Products.Max(item => (int?)item.ProductId) ?? 0;
                product = new Product { ProductId = maxId + 1, Article = article };
                context.Products.Add(product);
            }
            else
            {
                product = context.Products.FirstOrDefault(item => item.Article == editedArticle);
                if (product == null)
                {
                    ShowError("Товар не найден: возможно, его уже удалили. Закройте окно и обновите список.",
                        "Товар не найден");
                    return;
                }
            }

            product.ProductName = NameTextBox.Text.Trim();
            string description = DescriptionTextBox.Text.Trim();
            product.Description = description.Length == 0 ? null : description;
            product.CategoryId = ((Category)CategoryComboBox.SelectedItem).CategoryId;
            product.ManufacturerId = ((Manufacturer)ManufacturerComboBox.SelectedItem).ManufacturerId;
            product.SupplierId = ((Supplier)SupplierComboBox.SelectedItem).SupplierId;
            product.UnitId = ((Unit)UnitComboBox.SelectedItem).UnitId;
            product.Price = price;
            product.Stock = stock;
            product.Discount = discount;

            if (newPhotoSource != null)
            {
                savedPhoto = PhotoStorage.Save(newPhotoSource, article);
                product.Photo = savedPhoto;
            }

            context.SaveChanges();
        }
        catch (Exception exception)
        {
            // Запись в БД не прошла: новое фото не должно остаться в папке без товара
            PhotoStorage.Delete(savedPhoto);
            ShowError("Не удалось сохранить товар в базе данных. Проверьте подключение к серверу и повторите попытку.\n\n"
                + exception.Message, "Ошибка сохранения");
            return;
        }

        if (savedPhoto != null)
        {
            DeleteOldPhoto();
        }

        IsChanged = true;
        MessageBox.Show("Товар сохранён.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private string? Validate(out decimal price, out int stock, out byte discount)
    {
        List<string> errors = new List<string>();

        if (ArticleTextBox.Text.Trim().Length == 0)
        {
            errors.Add("• Укажите артикул товара.");
        }

        if (NameTextBox.Text.Trim().Length == 0)
        {
            errors.Add("• Укажите наименование товара.");
        }

        if (CategoryComboBox.SelectedItem == null)
        {
            errors.Add("• Выберите категорию товара из списка.");
        }

        if (ManufacturerComboBox.SelectedItem == null)
        {
            errors.Add("• Выберите производителя из списка.");
        }

        if (SupplierComboBox.SelectedItem == null)
        {
            errors.Add("• Выберите поставщика из списка.");
        }

        if (UnitComboBox.SelectedItem == null)
        {
            errors.Add("• Выберите единицу измерения из списка.");
        }

        // Принимаем и запятую, и точку как разделитель копеек
        string priceText = PriceTextBox.Text.Trim().Replace(',', '.');
        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out price)
            || price < 0 || price > 99_999_999.99m || decimal.Round(price, 2) != price)
        {
            errors.Add("• Цена должна быть числом не меньше 0, не более двух знаков после запятой (например, 1500,50).");
        }

        if (!int.TryParse(StockTextBox.Text.Trim(), out stock) || stock < 0)
        {
            errors.Add("• Количество на складе должно быть целым числом не меньше 0.");
        }

        if (!byte.TryParse(DiscountTextBox.Text.Trim(), out discount) || discount > 100)
        {
            errors.Add("• Скидка должна быть целым числом от 0 до 100.");
        }

        return errors.Count == 0 ? null : string.Join("\n", errors);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using BikeDriveContext context = new BikeDriveContext();
            if (context.OrderItems.Any(item => item.Article == editedArticle))
            {
                MessageBox.Show("Этот товар есть в заказах, поэтому удалить его нельзя. "
                    + "Можно установить количество на складе равным 0.",
                    "Удаление запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult answer = MessageBox.Show(
                $"Удалить товар «{NameTextBox.Text}» (артикул {editedArticle})? Это действие нельзя отменить.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            Product? product = context.Products.FirstOrDefault(item => item.Article == editedArticle);
            if (product != null)
            {
                context.Products.Remove(product);
                context.SaveChanges();
            }
        }
        catch (Exception exception)
        {
            ShowError("Не удалось удалить товар. Проверьте подключение к серверу и повторите попытку.\n\n"
                + exception.Message, "Ошибка удаления");
            return;
        }

        DeleteOldPhoto();
        IsChanged = true;
        MessageBox.Show("Товар удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void DeleteOldPhoto()
    {
        try
        {
            PhotoStorage.Delete(oldPhotoFile);
        }
        catch (Exception)
        {
            // Файл мог быть занят другой программой: данные уже сохранены, лишний файл не критичен
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ShowError(string message, string caption)
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
