// ============================================================
//  TechnoShopEntities.cs
//  WpfApp1337/ApplicationData/TechnoShopEntities.cs
// ============================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1337.ApplicationData
{
    public class Roles
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)] public string RoleName { get; set; } = string.Empty;
    }

    public class Users
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string UserName { get; set; } = string.Empty;
        [Required, MaxLength(50)]  public string Login    { get; set; } = string.Empty;
        [Required, MaxLength(256)] public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; } = 3;
        [MaxLength(20)]  public string? Phone { get; set; }
        [MaxLength(100)] public string? Email { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [ForeignKey(nameof(RoleId))] public Roles? Role { get; set; }
    }

    public class Categories
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(100)] public string CategoryName { get; set; } = string.Empty;
        [MaxLength(500)] public string? Description { get; set; }
    }

    public class Suppliers
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
        [MaxLength(20)]  public string? ContactPhone { get; set; }
        [MaxLength(100)] public string? ContactName  { get; set; }
        [MaxLength(100)] public string? Email   { get; set; }
        [MaxLength(255)] public string? Address  { get; set; }
        [MaxLength(12)]  public string? INN      { get; set; }
    }

    public class Products
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
        [MaxLength(100)] public string? Category    { get; set; }
        [MaxLength(100)] public string? Brand       { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
        public int Quantity { get; set; }
        [MaxLength(1000)] public string? Description { get; set; }
        // Путь к фото товара — храним относительный путь: "Images/product_1.jpg"



        [MaxLength(500)]  public string? ImagePath   { get; set; }
        public int? SupplierId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [ForeignKey(nameof(SupplierId))] public Suppliers? Supplier { get; set; }
    }

    public class Cart
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)]  public string UserLogin   { get; set; } = string.Empty;
        public int ProductId { get; set; }
        [Required, MaxLength(200)] public string ProductName { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
        public int Quantity { get; set; } = 1;
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(18,2)")] public decimal TotalPrice { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
        [ForeignKey(nameof(ProductId))] public Products? Product { get; set; }
        [ForeignKey(nameof(UserLogin))]  public Users?   User    { get; set; }
    }

    public class Orders
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)]  public string UserLogin   { get; set; } = string.Empty;
        [Required, MaxLength(200)] public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        [Column(TypeName = "decimal(18,2)")] public decimal? UnitPrice { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        [MaxLength(50)]  public string Status          { get; set; } = "Оформлен";
        [MaxLength(500)] public string? DeliveryAddress { get; set; }
        [MaxLength(500)] public string? Comment         { get; set; }
        [ForeignKey(nameof(UserLogin))] public Users? User { get; set; }
    }

    public class SupplierProducts
    {
        [Key] public int Id { get; set; }
        public int SupplierId { get; set; }
        public int ProductId  { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal SupplyPrice { get; set; }
        public DateTime? LastDelivery { get; set; }
        public int? DeliveryDays { get; set; }
        [ForeignKey(nameof(SupplierId))] public Suppliers? Supplier { get; set; }
        [ForeignKey(nameof(ProductId))]  public Products?  Product  { get; set; }
    }

    // ── НОВАЯ ТАБЛИЦА: Избранное ──────────────────────────────
    public class Favorites
    {
        [Key] public int Id { get; set; }
        [Required, MaxLength(50)] public string UserLogin { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
        [ForeignKey(nameof(UserLogin))]  public Users?    User    { get; set; }
        [ForeignKey(nameof(ProductId))]  public Products? Product { get; set; }
    }

    // ─────────────────────────────────────────────────────────
    //  DbContext
    // ─────────────────────────────────────────────────────────
    public class TechnoShopEntities : DbContext
    {
        public DbSet<Roles>            Roles            { get; set; }
        public DbSet<Users>            Users            { get; set; }
        public DbSet<Categories>       Categories       { get; set; }
        public DbSet<Suppliers>        Suppliers        { get; set; }
        public DbSet<Products>         Products         { get; set; }
        public DbSet<Cart>             Cart             { get; set; }
        public DbSet<Orders>           Orders           { get; set; }
        public DbSet<SupplierProducts> SupplierProducts { get; set; }
        public DbSet<Favorites>        Favorites        { get; set; }  // ← новое

        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseSqlServer(
                @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TechnoShopDB;Integrated Security=True");

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<Users>().HasIndex(u => u.Login).IsUnique();
            mb.Entity<Categories>().HasIndex(c => c.CategoryName).IsUnique();
            mb.Entity<SupplierProducts>().HasIndex(sp => new { sp.SupplierId, sp.ProductId }).IsUnique();

            mb.Entity<Cart>()
                .Property(c => c.TotalPrice)
                .HasComputedColumnSql("[UnitPrice] * [Quantity]", stored: true);

            // FK через Login (не Id)
            mb.Entity<Cart>()
                .HasOne(c => c.User).WithMany()
                .HasForeignKey(c => c.UserLogin).HasPrincipalKey(u => u.Login);

            mb.Entity<Orders>()
                .HasOne(o => o.User).WithMany()
                .HasForeignKey(o => o.UserLogin).HasPrincipalKey(u => u.Login);

            mb.Entity<Favorites>()
                .HasOne(f => f.User).WithMany()
                .HasForeignKey(f => f.UserLogin).HasPrincipalKey(u => u.Login);

            // Один пользователь — один раз добавить товар в избранное
            mb.Entity<Favorites>()
                .HasIndex(f => new { f.UserLogin, f.ProductId }).IsUnique();
        }
    }
}
