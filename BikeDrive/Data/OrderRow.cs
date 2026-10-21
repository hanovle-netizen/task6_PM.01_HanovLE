namespace BikeDrive.Data;

// Строка списка заказов
public class OrderRow
{
    public OrderRow(Order order)
    {
        OrderId = order.OrderId;
        Articles = FormatArticles(order.Items);
        StatusName = order.Status.StatusName;
        Address = order.PickupPoint.Address;
        OrderDateText = order.OrderDate.ToString("dd.MM.yyyy");
        DeliveryDateText = order.DeliveryDate.ToString("dd.MM.yyyy");
    }

    public int OrderId { get; }
    public string Articles { get; }
    public string StatusName { get; }
    public string Address { get; }
    public string OrderDateText { get; }
    public string DeliveryDateText { get; }

    // Артикул заказа в формате исходных данных: «А112Т4, 2, G843H5, 2»
    public static string FormatArticles(IEnumerable<OrderItem> items)
    {
        return string.Join(", ", items.OrderBy(item => item.Article).Select(item => $"{item.Article}, {item.Quantity}"));
    }
}
