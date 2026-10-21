-- Скрипт БД «ВелосипедДрайв» (MS SQL Server). Модули 1–4, ДЭ 09.02.07-2-2026, вариант 2.
-- Схема в 3НФ, ссылочная целостность обеспечена внешними ключами.
-- Скрипт можно выполнять повторно: база пересоздаётся.
USE master;
GO
IF DB_ID(N'BikeDrive') IS NOT NULL
BEGIN
    ALTER DATABASE BikeDrive SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BikeDrive;
END
GO
CREATE DATABASE BikeDrive COLLATE Cyrillic_General_CI_AS;
GO
USE BikeDrive;
GO

CREATE TABLE Role (
    RoleId INT IDENTITY(1,1) CONSTRAINT PK_Role PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL CONSTRAINT UQ_Role_RoleName UNIQUE
);

CREATE TABLE [User] (
    UserId INT IDENTITY(1,1) CONSTRAINT PK_User PRIMARY KEY,
    RoleId INT NOT NULL CONSTRAINT FK_User_Role REFERENCES Role(RoleId),
    FullName NVARCHAR(150) NOT NULL,
    Login NVARCHAR(100) NOT NULL CONSTRAINT UQ_User_Login UNIQUE,
    Password NVARCHAR(50) NOT NULL
);

CREATE TABLE Unit (
    UnitId INT IDENTITY(1,1) CONSTRAINT PK_Unit PRIMARY KEY,
    UnitName NVARCHAR(20) NOT NULL CONSTRAINT UQ_Unit_UnitName UNIQUE
);

CREATE TABLE Supplier (
    SupplierId INT IDENTITY(1,1) CONSTRAINT PK_Supplier PRIMARY KEY,
    SupplierName NVARCHAR(100) NOT NULL CONSTRAINT UQ_Supplier_SupplierName UNIQUE
);

CREATE TABLE Manufacturer (
    ManufacturerId INT IDENTITY(1,1) CONSTRAINT PK_Manufacturer PRIMARY KEY,
    ManufacturerName NVARCHAR(100) NOT NULL CONSTRAINT UQ_Manufacturer_ManufacturerName UNIQUE
);

CREATE TABLE Category (
    CategoryId INT IDENTITY(1,1) CONSTRAINT PK_Category PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL CONSTRAINT UQ_Category_CategoryName UNIQUE
);

CREATE TABLE Product (
    Article NVARCHAR(20) CONSTRAINT PK_Product PRIMARY KEY,
    ProductName NVARCHAR(250) NOT NULL,
    UnitId INT NOT NULL CONSTRAINT FK_Product_Unit REFERENCES Unit(UnitId),
    Price DECIMAL(10,2) NOT NULL CONSTRAINT CK_Product_Price CHECK (Price >= 0),
    SupplierId INT NOT NULL CONSTRAINT FK_Product_Supplier REFERENCES Supplier(SupplierId),
    ManufacturerId INT NOT NULL CONSTRAINT FK_Product_Manufacturer REFERENCES Manufacturer(ManufacturerId),
    CategoryId INT NOT NULL CONSTRAINT FK_Product_Category REFERENCES Category(CategoryId),
    Discount TINYINT NOT NULL CONSTRAINT DF_Product_Discount DEFAULT 0
        CONSTRAINT CK_Product_Discount CHECK (Discount BETWEEN 0 AND 100),
    Stock INT NOT NULL CONSTRAINT DF_Product_Stock DEFAULT 0
        CONSTRAINT CK_Product_Stock CHECK (Stock >= 0),
    Description NVARCHAR(MAX) NULL,
    Photo NVARCHAR(260) NULL,
    -- Порядковый ID товара (модуль 3): новое значение приложение вычисляет как MAX + 1
    ProductId INT NULL
);

CREATE TABLE PickupPoint (
    PickupPointId INT IDENTITY(1,1) CONSTRAINT PK_PickupPoint PRIMARY KEY,
    Address NVARCHAR(250) NOT NULL
);

CREATE TABLE OrderStatus (
    StatusId INT IDENTITY(1,1) CONSTRAINT PK_OrderStatus PRIMARY KEY,
    StatusName NVARCHAR(50) NOT NULL CONSTRAINT UQ_OrderStatus_StatusName UNIQUE
);

CREATE TABLE [Order] (
    OrderId INT CONSTRAINT PK_Order PRIMARY KEY,
    OrderDate DATE NOT NULL,
    DeliveryDate DATE NOT NULL,
    PickupPointId INT NOT NULL CONSTRAINT FK_Order_PickupPoint REFERENCES PickupPoint(PickupPointId),
    UserId INT NOT NULL CONSTRAINT FK_Order_User REFERENCES [User](UserId),
    ReceiveCode INT NOT NULL,
    StatusId INT NOT NULL CONSTRAINT FK_Order_OrderStatus REFERENCES OrderStatus(StatusId),
    CONSTRAINT CK_Order_Dates CHECK (DeliveryDate >= OrderDate)
);

CREATE TABLE OrderItem (
    OrderId INT NOT NULL CONSTRAINT FK_OrderItem_Order REFERENCES [Order](OrderId) ON DELETE CASCADE,
    Article NVARCHAR(20) NOT NULL CONSTRAINT FK_OrderItem_Product REFERENCES Product(Article),
    Quantity INT NOT NULL CONSTRAINT CK_OrderItem_Quantity CHECK (Quantity > 0),
    CONSTRAINT PK_OrderItem PRIMARY KEY (OrderId, Article)
);
GO

INSERT INTO Role (RoleName) VALUES
    (N'Администратор'),
    (N'Менеджер'),
    (N'Авторизированный клиент');
GO

INSERT INTO Unit (UnitName) VALUES
    (N'шт.');
GO

INSERT INTO Supplier (SupplierName) VALUES
    (N'ВелоСтрана'),
    (N'ЯндексМаркет'),
    (N'Скилс'),
    (N'ПерспективаГрупп'),
    (N'kari');
GO

INSERT INTO Manufacturer (ManufacturerName) VALUES
    (N'Slash'),
    (N'Shimano'),
    (N'Skill bike'),
    (N'NEXT'),
    (N'Aero'),
    (N'Fizard'),
    (N'kari');
GO

INSERT INTO Category (CategoryName) VALUES
    (N'Велосипед взрослый горный'),
    (N'Велосипед городской подростковый'),
    (N'Велосипед городской взрослый'),
    (N'Велосипед детский горный'),
    (N'Велосипед детский городской');
GO

INSERT INTO OrderStatus (StatusName) VALUES
    (N'Новый'),
    (N'Завершен');
GO

INSERT INTO PickupPoint (Address) VALUES
    (N'125061, г. Лесной, ул. Подгорная, 8'),
    (N'630370, г. Лесной, ул. Шоссейная, 24'),
    (N'400562, г. Лесной, ул. Зеленая, 32'),
    (N'614510, г. Лесной, ул. Маяковского, 47'),
    (N'410542, г. Лесной, ул. Светлая, 46'),
    (N'620839, г. Лесной, ул. Цветочная, 8'),
    (N'443890, г. Лесной, ул. Коммунистическая, 1'),
    (N'603379, г. Лесной, ул. Спортивная, 46'),
    (N'603721, г. Лесной, ул. Гоголя, 41'),
    (N'410172, г. Лесной, ул. Северная, 13'),
    (N'614611, г. Лесной, ул. Молодежная, 50'),
    (N'454311, г. Лесной, ул. Новая, 19'),
    (N'660007, г. Лесной, ул. Октябрьская, 19'),
    (N'603036, г. Лесной, ул. Садовая, 4'),
    (N'394060, г. Лесной, ул. Фрунзе, 43'),
    (N'410661, г. Лесной, ул. Школьная, 50'),
    (N'625590, г. Лесной, ул. Коммунистическая, 20'),
    (N'625683, г. Лесной, ул. 8 Марта'),
    (N'450983, г. Лесной, ул. Комсомольская, 26'),
    (N'394782, г. Лесной, ул. Чехова, 3'),
    (N'603002, г. Лесной, ул. Дзержинского, 28'),
    (N'450558, г. Лесной, ул. Набережная, 30'),
    (N'344288, г. Лесной, ул. Чехова, 1'),
    (N'614164, г. Лесной, ул. Степная, 30'),
    (N'394242, г. Лесной, ул. Коммунистическая, 43'),
    (N'660540, г. Лесной, ул. Солнечная, 25'),
    (N'125837, г. Лесной, ул. Шоссейная, 40'),
    (N'125703, г. Лесной, ул. Партизанская, 49'),
    (N'625283, г. Лесной, ул. Победы, 46'),
    (N'614753, г. Лесной, ул. Полевая, 35'),
    (N'426030, г. Лесной, ул. Маяковского, 44'),
    (N'450375, г. Лесной, ул. Клубная, 44'),
    (N'625560, г. Лесной, ул. Некрасова, 12'),
    (N'630201, г. Лесной, ул. Комсомольская, 17'),
    (N'190949, г. Лесной, ул. Мичурина, 26');
GO

INSERT INTO [User] (RoleId, FullName, Login, Password) VALUES
    (1, N'Ханов Леонид Евгеньевич', N'94d5ous@gmail.com', N'uzWC67'),
    (1, N'Сазонов Руслан Германович', N'uth4iz@mail.com', N'2L6KZG'),
    (1, N'Одинцов Серафим Артёмович', N'5d4zbu@tutanota.com', N'rwVDh9'),
    (2, N'Ситдикова Елена Анатольевна', N'ptec8ym@yahoo.com', N'LdNyos'),
    (2, N'Ворсин Петр Евгеньевич', N'1qz4kw@mail.com', N'gynQMT'),
    (2, N'Старикова Елена Павловна', N'4np6se@mail.com', N'AtnDjr'),
    (3, N'Никифорова Анна Семеновна', N'yzls62@outlook.com', N'JlFRCZ'),
    (3, N'Стелина Евгения Петровна', N'1diph5e@tutanota.com', N'8ntwUp'),
    (3, N'Михайлюк Анна Вячеславовна', N'tjde7c@yahoo.com', N'YOyhfR'),
    (3, N'Степанов Михаил Артёмович', N'wpmrc3do@tutanota.com', N'RSbvHv');
GO

INSERT INTO Product (Article, ProductName, UnitId, Price, SupplierId, ManufacturerId, CategoryId, Discount, Stock, Description, Photo) VALUES
    (N'А112Т4', N'Велосипед взрослый горный Slash Stream 27.5 колеса (2025) 17" Черный (162-172 см)', 1, 19775, 1, 1, 1, 30, 15, N'Горный велосипед Slash Stream 27.5 (2025) – легкий и надежный компаньон для поездок по пересеченной местности. Мощные шатуны интенсивно передают усилия мышц на вал каретки.', N'1.jpg'),
    (N'G843H5', N'Велосипед взрослый горный Slash Stream 27.5 колеса (2025) 19" Синий (172-182 см)', 1, 19791, 1, 1, 1, 30, 9, N'В комплектацию включены дисковые механические тормоза RPT DSC-310. Особый упор разработчики рамы данной модели сделали на увеличение прочности мест наибольшей нагрузки.', N'2.jpg'),
    (N'D325D4', N'Велосипед городской подростковый серый', 1, 9919, 2, 2, 2, 5, 12, N'Городской подростковый велосипед - идеальный выбор для активного образа жизни! Откройте для себя комфорт и свободу передвижения с нашим стильным городским велосипедом.', N'3.jpg'),
    (N'S432T5', N'Велосипед Skill Bike 3051, городской, 21 скорость, сталь, 29" колеса, черно-красный', 1, 16442, 3, 3, 3, 15, 15, N'SKILL BIKE модель 3051 - горный велосипед на спицах, обеспечивающий уверенную и комфортную езду как по городским улицам, так и по горной местности.', N'4.jpg'),
    (N'F325D4', N'Велосипед Skillbike 3052, горный, складной, рама 17 дюймов, колеса 26 дюймов, 21 скорость', 1, 17985, 3, 3, 1, 18, 50, N'SKILL BIKE модель 3052 - велосипед складной, предназначен для тех, кто ценит комфорт, стиль и максимальную мобильность. Горный велосипед легко помещается в багажник и идеально подходит для активных поездок в городской суете.', N'5.jpg'),
    (N'G432G6', N'Велосипед Skill Bike 3053, горный, двухподвесный, рама 17 дюймов, колеса 26 дюймов, 21 скорость', 1, 17621, 3, 3, 1, 20, 0, N'SKILL BIKE модель 3053 - горный велосипед на литых дисках, имеет амортизаторы как на переднем, так и на заднем колесе.', N'6.jpg'),
    (N'H542F5', N'Велосипед MILANO M300, горный, для взрослых, 26", 7 скоростей', 1, 13509, 4, 4, 1, 4, 5, N'Горный велосипед MILANO M300 с диаметром колес 26 дюйма подойдет для подростков и взрослых, без усилий позволит преодолевать любые непроходимые каменистые поверхности и зоны бездорожья.', N'7.jpg'),
    (N'C346F5', N'Горный велосипед скоростной, колёса 24", рама - 14", черно-красный', 1, 15212, 4, 5, 4, 5, 4, N'Горный велосипед – это надежный и стильный выбор для любителей активного отдыха. Удобное седло из искусственной кожи и наличие подножки добавляют удобства во время поездок.', N'8.jpg'),
    (N'F256G6', N'26" Велосипед Fizard, 15" алюминий, дисковые тормоза, 21 скорость, серый', 1, 15126, 4, 6, 4, 25, 3, N'Горный велосипед Fizard — надёжный универсальный маунтинбайк для города и бездорожья.', N'9.jpg'),
    (N'J532V5', N'Велосипед двухколесный детский 14 дюймов, со светящимися колесами, черный', 1, 6417, 5, 7, 5, 8, 6, N'Велосипед двухколесный детский 14 дюймов от Kari - это надежный и безопасный транспорт для вашего ребенка.', NULL);
GO

-- Нумеруем импортированные товары в порядке загрузки и делаем ID обязательным и уникальным
UPDATE Product SET ProductId = Numbers.Id
FROM Product
JOIN (VALUES
    (N'А112Т4', 1),
    (N'G843H5', 2),
    (N'D325D4', 3),
    (N'S432T5', 4),
    (N'F325D4', 5),
    (N'G432G6', 6),
    (N'H542F5', 7),
    (N'C346F5', 8),
    (N'F256G6', 9),
    (N'J532V5', 10)
) AS Numbers(Article, Id) ON Numbers.Article = Product.Article;
ALTER TABLE Product ALTER COLUMN ProductId INT NOT NULL;
ALTER TABLE Product ADD CONSTRAINT UQ_Product_ProductId UNIQUE (ProductId);
GO

INSERT INTO [Order] (OrderId, OrderDate, DeliveryDate, PickupPointId, UserId, ReceiveCode, StatusId) VALUES
    (1, '2023-02-27', '2023-04-20', 1, 10, 901, 1),
    (2, '2022-09-28', '2023-04-21', 11, 1, 902, 1),
    (3, '2023-03-21', '2023-04-22', 2, 2, 903, 1),
    (4, '2023-02-20', '2023-04-23', 11, 3, 904, 2),
    (5, '2023-03-17', '2023-04-24', 2, 10, 905, 2),
    (6, '2023-03-01', '2023-04-25', 15, 1, 906, 2),
    (7, '2023-02-28', '2023-04-26', 3, 2, 907, 2),
    (8, '2023-03-31', '2023-04-27', 19, 3, 908, 1),
    (9, '2023-04-02', '2023-04-28', 5, 10, 909, 1),
    (10, '2023-04-03', '2023-04-29', 19, 10, 910, 1);
GO

INSERT INTO OrderItem (OrderId, Article, Quantity) VALUES
    (1, N'А112Т4', 2),
    (1, N'G843H5', 2),
    (2, N'G843H5', 1),
    (2, N'А112Т4', 1),
    (3, N'D325D4', 10),
    (3, N'S432T5', 10),
    (4, N'F325D4', 5),
    (4, N'D325D4', 4),
    (5, N'G432G6', 20),
    (5, N'H542F5', 20),
    (6, N'А112Т4', 2),
    (6, N'G843H5', 2),
    (7, N'C346F5', 3),
    (7, N'F256G6', 3),
    (8, N'F325D4', 1),
    (8, N'G432G6', 1),
    (9, N'J532V5', 5),
    (9, N'F256G6', 1),
    (10, N'F256G6', 5),
    (10, N'J532V5', 5);
GO
