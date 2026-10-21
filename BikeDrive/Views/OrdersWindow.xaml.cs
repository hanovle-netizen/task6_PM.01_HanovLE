using System.Windows;
using System.Windows.Controls;
using BikeDrive.Data;
using Microsoft.EntityFrameworkCore;

namespace BikeDrive.Views;

public partial class OrdersWindow : Window
{
    private readonly User currentUser;
    private readonly Window productsWindow;
    private readonly bool isAdmin;
    private OrderEditWindow? editWindow;

    public OrdersWindow(User user, Window products)
    {
        InitializeComponent();
        currentUser = user;
        productsWindow = products;

        // Просматривают заказы менеджер и администратор, изменяет только администратор
        isAdmin = user.Role.RoleName == "Администратор";
        UserNameTextBlock.Text = user.FullName;
        UserRoleTextBlock.Text = user.Role.RoleName;

        if (isAdmin)
        {
            AddOrderButton.Visibility = Visibility.Visible;
            EditHintTextBlock.Visibility = Visibility.Visible;
            OrdersListBox.Cursor = System.Windows.Input.Cursors.Hand;
        }

        // Список товаров снова показывается при любом закрытии окна, в том числе крестиком
        Closed += (_, _) => productsWindow.Show();

        LoadOrders();
    }

    private void LoadOrders()
    {
        try
        {
            using BikeDriveContext context = new BikeDriveContext();
            OrdersListBox.ItemsSource = context.Orders
                .Include(order => order.Status)
                .Include(order => order.PickupPoint)
                .Include(order => order.Items)
                .OrderBy(order => order.OrderId)
                .AsEnumerable()
                .Select(order => new OrderRow(order))
                .ToList();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "Не удалось загрузить список заказов из базы данных. Проверьте подключение к серверу "
                + "и повторите попытку.\n\n" + exception.Message,
                "Ошибка загрузки данных",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void AddOrderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenEditWindow(null);
    }

    private void OrderItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (isAdmin && sender is ListBoxItem { DataContext: OrderRow order })
        {
            OpenEditWindow(order.OrderId);
        }
    }

    // Как и для товаров, допускается только одно окно редактирования
    private void OpenEditWindow(int? orderId)
    {
        if (editWindow != null)
        {
            MessageBox.Show(
                "Окно редактирования заказа уже открыто. Сохраните изменения или нажмите «Назад» в нём, "
                + "затем откройте другой заказ.",
                "Редактирование уже открыто",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            editWindow.Activate();
            return;
        }

        editWindow = new OrderEditWindow(orderId, currentUser) { Owner = this };
        editWindow.Closed += EditWindow_Closed;
        editWindow.Show();
    }

    private void EditWindow_Closed(object? sender, EventArgs e)
    {
        bool isChanged = editWindow?.IsChanged == true;
        editWindow = null;
        if (isChanged)
        {
            LoadOrders();
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
