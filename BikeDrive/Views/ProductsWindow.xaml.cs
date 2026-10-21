using System.Windows;
using System.Windows.Controls;
using BikeDrive.Data;
using Microsoft.EntityFrameworkCore;

namespace BikeDrive.Views;

public partial class ProductsWindow : Window
{
    private const string AllRangesText = "Все диапазоны";
    private const string DefaultSortText = "Без сортировки";

    private readonly User? currentUser;
    private readonly bool canUseTools;
    private readonly bool isAdmin;
    private ProductEditWindow? editWindow;
    private List<ProductItem> allProducts = new();

    public ProductsWindow(User? user)
    {
        InitializeComponent();
        currentUser = user;

        // Поиск, сортировка и фильтрация доступны менеджеру и администратору
        string roleName = user?.Role.RoleName ?? "Гость";
        canUseTools = roleName == "Менеджер" || roleName == "Администратор";
        isAdmin = roleName == "Администратор";

        UserNameTextBlock.Text = user?.FullName ?? "Гость";
        UserRoleTextBlock.Text = user == null ? "" : roleName;

        // Гость не авторизован: вместо «Выйти» ведём его на экран авторизации
        if (user == null)
        {
            LogoutButton.Content = "Войти";
        }

        if (canUseTools)
        {
            ToolsPanel.Visibility = Visibility.Visible;
            FillTools();
            OrdersButton.Visibility = Visibility.Visible;
        }

        if (isAdmin)
        {
            AddProductButton.Visibility = Visibility.Visible;
            EditHintTextBlock.Visibility = Visibility.Visible;
            ProductsListBox.Cursor = System.Windows.Input.Cursors.Hand;
        }

        LoadProducts();
        ShowProducts();
    }

    private void FillTools()
    {
        SearchTextBox.Text = "";
        SortComboBox.ItemsSource = new[]
        {
            DefaultSortText,
            "Остаток: по возрастанию",
            "Остаток: по убыванию",
            "Цена: по возрастанию",
            "Цена: по убыванию",
            "Скидка: по возрастанию",
            "Скидка: по убыванию"
        };
        SortComboBox.SelectedIndex = 0;

        DiscountComboBox.ItemsSource = new[] { AllRangesText, "0–11,99%", "12–18,99%", "19% и более" };
        DiscountComboBox.SelectedIndex = 0;
    }

    private void LoadProducts()
    {
        try
        {
            using BikeDriveContext context = new BikeDriveContext();
            allProducts = context.Products
                .Include(product => product.Category)
                .Include(product => product.Manufacturer)
                .Include(product => product.Supplier)
                .Include(product => product.Unit)
                .OrderBy(product => product.Article)
                .AsEnumerable()
                .Select(product => new ProductItem(product))
                .ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "Не удалось загрузить список товаров из базы данных. Проверьте подключение к серверу "
                + "и повторите попытку.\n\n" + exception.Message,
                "Ошибка загрузки данных",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // Поиск, фильтр и сортировка применяются вместе при любом изменении параметров
    private void ShowProducts()
    {
        IEnumerable<ProductItem> products = allProducts;

        if (canUseTools)
        {
            products = Search(products);
            products = FilterByDiscount(products);
            products = Sort(products);
        }

        ProductsListBox.ItemsSource = products.ToList();
    }

    private IEnumerable<ProductItem> Search(IEnumerable<ProductItem> products)
    {
        string[] words = SearchTextBox.Text
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Каждое слово должно встретиться хотя бы в одном текстовом поле товара
        return products.Where(product => words.All(word => ContainsWord(product, word)));
    }

    private static bool ContainsWord(ProductItem product, string word)
    {
        string[] fields =
        {
            product.Article, product.ProductName, product.CategoryName, product.Description,
            product.ManufacturerName, product.SupplierName, product.UnitName
        };
        return fields.Any(field => field.Contains(word, StringComparison.CurrentCultureIgnoreCase));
    }

    private IEnumerable<ProductItem> FilterByDiscount(IEnumerable<ProductItem> products)
    {
        return DiscountComboBox.SelectedIndex switch
        {
            1 => products.Where(product => product.Discount < 12),
            2 => products.Where(product => product.Discount >= 12 && product.Discount < 19),
            3 => products.Where(product => product.Discount >= 19),
            _ => products
        };
    }

    private IEnumerable<ProductItem> Sort(IEnumerable<ProductItem> products)
    {
        return SortComboBox.SelectedIndex switch
        {
            1 => products.OrderBy(product => product.Stock),
            2 => products.OrderByDescending(product => product.Stock),
            3 => products.OrderBy(product => product.FinalPrice),
            4 => products.OrderByDescending(product => product.FinalPrice),
            5 => products.OrderBy(product => product.Discount),
            6 => products.OrderByDescending(product => product.Discount),
            _ => products
        };
    }

    private void Tools_Changed(object sender, RoutedEventArgs e)
    {
        // Событие приходит и во время построения окна, когда элементы ещё не созданы
        if (!IsLoaded)
        {
            return;
        }

        ShowProducts();
    }

    private void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        OpenEditWindow(null);
    }

    private void ProductItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (isAdmin && sender is ListBoxItem { DataContext: ProductItem product })
        {
            OpenEditWindow(product.Article);
        }
    }

    // Одновременно открыто не больше одного окна редактирования, чтобы не менять несколько товаров сразу
    private void OpenEditWindow(string? article)
    {
        if (editWindow != null)
        {
            MessageBox.Show(
                "Окно редактирования товара уже открыто. Сохраните изменения или нажмите «Назад» в нём, "
                + "затем откройте другой товар.",
                "Редактирование уже открыто",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            editWindow.Activate();
            return;
        }

        editWindow = new ProductEditWindow(article) { Owner = this };
        editWindow.Closed += EditWindow_Closed;
        editWindow.Show();
    }

    private void EditWindow_Closed(object? sender, EventArgs e)
    {
        bool isChanged = editWindow?.IsChanged == true;
        editWindow = null;

        // Перечитываем БД, текущие поиск, фильтр и сортировка сохраняются
        if (isChanged)
        {
            LoadProducts();
            ShowProducts();
        }
    }

    private void OrdersButton_Click(object sender, RoutedEventArgs e)
    {
        if (editWindow != null)
        {
            MessageBox.Show("Сначала закройте окно редактирования товара.", "Редактирование не завершено",
                MessageBoxButton.OK, MessageBoxImage.Information);
            editWindow.Activate();
            return;
        }

        OrdersWindow ordersWindow = new OrdersWindow(currentUser!, this);
        ordersWindow.Show();
        Hide();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        // Окно редактирования принадлежит списку и закроется вместе с ним
        LoginWindow loginWindow = new LoginWindow();
        loginWindow.Show();
        Close();
    }
}
