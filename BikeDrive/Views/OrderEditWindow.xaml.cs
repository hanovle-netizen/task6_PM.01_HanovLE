using System.Windows;
using BikeDrive.Data;
using Microsoft.EntityFrameworkCore;

namespace BikeDrive.Views;

public partial class OrderEditWindow : Window
{
    private readonly int? editedOrderId;
    private readonly User currentUser;

    // orderId == null: добавление нового заказа, иначе редактирование существующего
    public OrderEditWindow(int? orderId, User user)
    {
        InitializeComponent();
        editedOrderId = orderId;
        currentUser = user;

        bool isNew = orderId == null;
        Title = isNew ? "ВелосипедДрайв — Добавление заказа" : "ВелосипедДрайв — Редактирование заказа";
        HeaderTextBlock.Text = isNew ? "Добавление заказа" : "Редактирование заказа";
        IdPanel.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;
        DeleteButton.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;

        Loaded += (_, _) => LoadData();
    }

    public bool IsChanged { get; private set; }

    private void LoadData()
    {
        try
        {
            using BikeDriveContext context = new BikeDriveContext();
            List<OrderStatus> statuses = context.OrderStatuses.OrderBy(item => item.StatusId).ToList();
            List<PickupPoint> points = context.PickupPoints.OrderBy(item => item.Address).ToList();
            StatusComboBox.ItemsSource = statuses;
            PickupPointComboBox.ItemsSource = points;

            if (editedOrderId == null)
            {
                StatusComboBox.SelectedIndex = 0;
                OrderDatePicker.SelectedDate = DateTime.Today;
                DeliveryDatePicker.SelectedDate = DateTime.Today.AddDays(3);
                return;
            }

            Order? order = context.Orders.Include(item => item.Items).FirstOrDefault(item => item.OrderId == editedOrderId);
            if (order == null)
            {
                ShowError("Заказ не найден: возможно, его уже удалили. Список заказов будет обновлён.", "Заказ не найден");
                IsChanged = true;
                Close();
                return;
            }

            IdTextBox.Text = order.OrderId.ToString();
            ArticlesTextBox.Text = OrderRow.FormatArticles(order.Items);
            StatusComboBox.SelectedItem = statuses.FirstOrDefault(item => item.StatusId == order.StatusId);
            PickupPointComboBox.SelectedItem = points.FirstOrDefault(item => item.PickupPointId == order.PickupPointId);
            OrderDatePicker.SelectedDate = order.OrderDate;
            DeliveryDatePicker.SelectedDate = order.DeliveryDate;
        }
        catch (Exception exception)
        {
            ShowError("Не удалось загрузить данные заказа из базы данных. Проверьте подключение к серверу.\n\n"
                + exception.Message, "Ошибка загрузки данных");
            Close();
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using BikeDriveContext context = new BikeDriveContext();
            List<string> errors = new List<string>();
            Dictionary<string, int> items = ParseArticles(context, errors);

            if (StatusComboBox.SelectedItem == null)
            {
                errors.Add("• Выберите статус заказа из списка.");
            }

            if (PickupPointComboBox.SelectedItem == null)
            {
                errors.Add("• Выберите адрес пункта выдачи из списка.");
            }

            DateTime? orderDate = OrderDatePicker.SelectedDate;
            DateTime? deliveryDate = DeliveryDatePicker.SelectedDate;
            if (orderDate == null || deliveryDate == null)
            {
                errors.Add("• Укажите дату заказа и дату выдачи в формате ДД.ММ.ГГГГ.");
            }
            else if (deliveryDate < orderDate)
            {
                errors.Add("• Дата выдачи не может быть раньше даты заказа.");
            }

            if (errors.Count > 0)
            {
                MessageBox.Show("Заказ не сохранён. Исправьте ошибки:\n\n" + string.Join("\n", errors),
                    "Ошибка ввода данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Order? order;
            if (editedOrderId == null)
            {
                // Номер и код получения нового заказа: на единицу больше максимальных в БД
                int maxId = context.Orders.Max(item => (int?)item.OrderId) ?? 0;
                int maxCode = context.Orders.Max(item => (int?)item.ReceiveCode) ?? 900;
                order = new Order { OrderId = maxId + 1, ReceiveCode = maxCode + 1, UserId = currentUser.UserId };
                context.Orders.Add(order);
            }
            else
            {
                order = context.Orders.Include(item => item.Items).FirstOrDefault(item => item.OrderId == editedOrderId);
                if (order == null)
                {
                    ShowError("Заказ не найден: возможно, его уже удалили. Закройте окно и обновите список.",
                        "Заказ не найден");
                    return;
                }
            }

            order.StatusId = ((OrderStatus)StatusComboBox.SelectedItem!).StatusId;
            order.PickupPointId = ((PickupPoint)PickupPointComboBox.SelectedItem!).PickupPointId;
            order.OrderDate = orderDate!.Value.Date;
            order.DeliveryDate = deliveryDate!.Value.Date;
            UpdateItems(order, items);

            context.SaveChanges();
        }
        catch (Exception exception)
        {
            ShowError("Не удалось сохранить заказ в базе данных. Проверьте подключение к серверу и повторите попытку.\n\n"
                + exception.Message, "Ошибка сохранения");
            return;
        }

        IsChanged = true;
        MessageBox.Show("Заказ сохранён.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    // Разбирает строку «артикул, количество, ...» и сверяет артикулы с товарами в БД
    private Dictionary<string, int> ParseArticles(BikeDriveContext context, List<string> errors)
    {
        Dictionary<string, int> items = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);
        string[] parts = ArticlesTextBox.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            errors.Add("• Укажите артикул: хотя бы один товар и его количество.");
            return items;
        }

        if (parts.Length % 2 != 0)
        {
            errors.Add("• В поле «Артикул» после каждого артикула товара должно идти количество через запятую.");
            return items;
        }

        List<string> knownArticles = context.Products.Select(product => product.Article).ToList();
        for (int index = 0; index < parts.Length; index += 2)
        {
            string article = parts[index];
            string? known = knownArticles.FirstOrDefault(item => string.Equals(item, article, StringComparison.CurrentCultureIgnoreCase));
            if (known == null)
            {
                errors.Add($"• Товара с артикулом «{article}» нет в базе данных.");
                continue;
            }

            if (!int.TryParse(parts[index + 1], out int quantity) || quantity <= 0)
            {
                errors.Add($"• Количество товара {known} должно быть целым числом больше 0.");
                continue;
            }

            if (!items.TryAdd(known, quantity))
            {
                errors.Add($"• Артикул {known} указан дважды. Укажите его один раз с общим количеством.");
            }
        }

        return items;
    }

    // Состав меняем по разнице: удаляем лишние позиции, обновляем количество, добавляем новые
    private static void UpdateItems(Order order, Dictionary<string, int> items)
    {
        order.Items.RemoveAll(item => !items.ContainsKey(item.Article));
        foreach ((string article, int quantity) in items)
        {
            OrderItem? existing = order.Items.FirstOrDefault(item => item.Article == article);
            if (existing != null)
            {
                existing.Quantity = quantity;
            }
            else
            {
                order.Items.Add(new OrderItem { Article = article, Quantity = quantity });
            }
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult answer = MessageBox.Show(
            $"Удалить заказ № {editedOrderId} вместе с его составом? Это действие нельзя отменить.",
            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            using BikeDriveContext context = new BikeDriveContext();
            Order? order = context.Orders.FirstOrDefault(item => item.OrderId == editedOrderId);
            if (order != null)
            {
                // Позиции заказа удаляются каскадно (ON DELETE CASCADE в БД)
                context.Orders.Remove(order);
                context.SaveChanges();
            }
        }
        catch (Exception exception)
        {
            ShowError("Не удалось удалить заказ. Проверьте подключение к серверу и повторите попытку.\n\n"
                + exception.Message, "Ошибка удаления");
            return;
        }

        IsChanged = true;
        MessageBox.Show("Заказ удалён.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
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
