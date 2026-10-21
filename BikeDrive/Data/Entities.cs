namespace BikeDrive.Data;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
}

public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string FullName { get; set; } = "";
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
    public Role Role { get; set; } = null!;
}

public class Unit
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = "";
}

public class Supplier
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
}

public class Manufacturer
{
    public int ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = "";
}

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
}

public class Product
{
    public int ProductId { get; set; }
    public string Article { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int UnitId { get; set; }
    public decimal Price { get; set; }
    public int SupplierId { get; set; }
    public int ManufacturerId { get; set; }
    public int CategoryId { get; set; }
    public byte Discount { get; set; }
    public int Stock { get; set; }
    public string? Description { get; set; }
    public string? Photo { get; set; }
    public Unit Unit { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public Manufacturer Manufacturer { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

public class PickupPoint
{
    public int PickupPointId { get; set; }
    public string Address { get; set; } = "";
}

public class OrderStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = "";
}

public class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public int PickupPointId { get; set; }
    public int UserId { get; set; }
    public int ReceiveCode { get; set; }
    public int StatusId { get; set; }
    public PickupPoint PickupPoint { get; set; } = null!;
    public User User { get; set; } = null!;
    public OrderStatus Status { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int OrderId { get; set; }
    public string Article { get; set; } = "";
    public int Quantity { get; set; }
    public Product Product { get; set; } = null!;
}
