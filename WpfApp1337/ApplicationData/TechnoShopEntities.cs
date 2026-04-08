// ============================================================
//  TechnoShopEntities.cs
//  Положить в папку: WpfApp1337/ApplicationData/
//
//  Требует NuGet пакеты (добавить в WpfApp1337.csproj):
//  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
//  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
//  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
// ============================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1337.ApplicationData
{
    // ──────────────────────────────────────────────────────
    //  МОДЕЛИ (таблицы)
    // ──────────────────────────────────────────────────────

    public class Roles
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;
    }

    public class Users
    {
        [Key]
        public int Id { get; set; }

        // wizard.UserName  в Autorization.xaml.cs
        [Required, MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        // AppConnect.CurrentUser.Login  везде в коде
        [Required, MaxLength(50)]
        public string Login { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string Password { get; set; } = string.Empty;

        public int RoleId { get; set; } = 3;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Навигационное свойство
        [ForeignKey(nameof(RoleId))]
        public Roles? Role { get; set; }
    }

    public class Categories
    {
        [Key]
        public int Id { get; set; }

        // CategoryComboBox.DisplayMemberPath = "CategoryName"
        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class Suppliers
    {
        [Key]
        public int Id { get; set; }

        // SuppliersDataGrid: Binding="Name"
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        // SuppliersDataGrid: Binding="ContactPhone"
        [MaxLength(20)]
        public string? ContactPhone { get; set; }

        [MaxLength(100)]
        public string? ContactName { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(12)]
        public string? INN { get; set; }
    }

    public class Products
    {
        [Key]
        public int Id { get; set; }

        // DataGrid: Binding="Name" | Поиск: x.Name.ToLower().Contains(...)
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // DataGrid: Binding="Category" | Фильтр: x.Category == selectedCategory
        // Хранится как строка — денормализация для простоты фильтрации
        [MaxLength(100)]
        public string? Category { get; set; }

        // DataGrid: Binding="Brand" | Поиск: x.Brand.ToLower().Contains(...)
        [MaxLength(100)]
        public string? Brand { get; set; }

        // DataGrid: Binding="Price"
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // DataGrid: Binding="Quantity" | AddRecip: QuantityTextBox
        public int Quantity { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ImagePath { get; set; }

        public int? SupplierId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(SupplierId))]
        public Suppliers? Supplier { get; set; }
    }

    public class Cart
    {
        [Key]
        public int Id { get; set; }

        // AppConnect.CurrentUser.Login
        [Required, MaxLength(50)]
        public string UserLogin { get; set; } = string.Empty;

        // Binding="ProductId"
        public int ProductId { get; set; }

        // Binding="ProductName"
        [Required, MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        // Binding="UnitPrice"
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Binding="Quantity"
        public int Quantity { get; set; } = 1;

        // Binding="TotalPrice" — вычисляемое (computed column в БД)
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ProductId))]
        public Products? Product { get; set; }

        [ForeignKey(nameof(UserLogin))]
        public Users? User { get; set; }
    }

    public class Orders
    {
        [Key]
        public int Id { get; set; }

        // DataGrid: Binding="UserLogin"
        [Required, MaxLength(50)]
        public string UserLogin { get; set; } = string.Empty;

        // DataGrid: Binding="ProductName"
        [Required, MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        // DataGrid: Binding="Quantity"
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        // DataGrid: Binding="OrderDate"
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // DataGrid: Binding="Status" | Checkout() -> Status = "Оформлен"
        [MaxLength(50)]
        public string Status { get; set; } = "Оформлен";

        [MaxLength(500)]
        public string? DeliveryAddress { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        [ForeignKey(nameof(UserLogin))]
        public Users? User { get; set; }
    }

    public class SupplierProducts
    {
        [Key]
        public int Id { get; set; }

        public int SupplierId { get; set; }
        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SupplyPrice { get; set; }

        public DateTime? LastDelivery { get; set; }

        public int? DeliveryDays { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Suppliers? Supplier { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Products? Product { get; set; }
    }

    // ──────────────────────────────────────────────────────
    //  DbContext
    // ──────────────────────────────────────────────────────

    public class TechnoShopEntities : DbContext
    {
        // Все таблицы — AppConnect.model01.XXX
        public DbSet<Roles>            Roles            { get; set; }
        public DbSet<Users>            Users            { get; set; }
        public DbSet<Categories>       Categories       { get; set; }
        public DbSet<Suppliers>        Suppliers        { get; set; }
        public DbSet<Products>         Products         { get; set; }
        public DbSet<Cart>             Cart             { get; set; }
        public DbSet<Orders>           Orders           { get; set; }
        public DbSet<SupplierProducts> SupplierProducts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // LocalDB — меняй на свою строку если используешь обычный SQL Server
            optionsBuilder.UseSqlServer(
                @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TechnoShopDB;Integrated Security=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Уникальный логин пользователя
            modelBuilder.Entity<Users>()
                .HasIndex(u => u.Login)
                .IsUnique();

            // Уникальное имя категории
            modelBuilder.Entity<Categories>()
                .HasIndex(c => c.CategoryName)
                .IsUnique();

            // Уникальная пара поставщик-товар
            modelBuilder.Entity<SupplierProducts>()
                .HasIndex(sp => new { sp.SupplierId, sp.ProductId })
                .IsUnique();

            // Вычисляемый столбец TotalPrice в корзине
            modelBuilder.Entity<Cart>()
                .Property(c => c.TotalPrice)
                .HasComputedColumnSql("[UnitPrice] * [Quantity]", stored: true);

            // FK: Cart.UserLogin -> Users.Login  (нестандартный FK не по Id)
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserLogin)
                .HasPrincipalKey(u => u.Login);

            // FK: Orders.UserLogin -> Users.Login
            modelBuilder.Entity<Orders>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserLogin)
                .HasPrincipalKey(u => u.Login);
        }
    }
}
