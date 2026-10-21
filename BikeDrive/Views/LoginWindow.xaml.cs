using System.Windows;
using BikeDrive.Data;
using Microsoft.EntityFrameworkCore;

namespace BikeDrive.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string login = LoginTextBox.Text.Trim();
        string password = PasswordBox.Password;

        if (login.Length == 0 || password.Length == 0)
        {
            MessageBox.Show(
                "Введите логин и пароль. Оба поля обязательны для заполнения.",
                "Не заполнены поля",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        User? user;
        try
        {
            using BikeDriveContext context = new BikeDriveContext();
            user = context.Users
                .Include(found => found.Role)
                .FirstOrDefault(found => found.Login == login && found.Password == password);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "Не удалось подключиться к базе данных. Проверьте, что сервер SQL Server запущен, "
                + "а строка подключения в файле appsettings.json указана верно.\n\n" + exception.Message,
                "Ошибка подключения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        if (user == null)
        {
            MessageBox.Show(
                "Неверный логин или пароль. Проверьте раскладку клавиатуры и повторите ввод.",
                "Ошибка авторизации",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            PasswordBox.Clear();
            return;
        }

        OpenProducts(user);
    }

    private void GuestButton_Click(object sender, RoutedEventArgs e)
    {
        OpenProducts(null);
    }

    private void OpenProducts(User? user)
    {
        ProductsWindow productsWindow = new ProductsWindow(user);
        productsWindow.Show();
        Close();
    }
}
