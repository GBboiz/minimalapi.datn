using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinimalAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftsAndDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Thêm cột quà tặng vào bảng products
            migrationBuilder.AddColumn<Guid>(
                name: "gift_product_id",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gift_product_name",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // 2. Thêm cột is_gift vào bảng order_items
            migrationBuilder.AddColumn<bool>(
                name: "is_gift",
                table: "order_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // 3. Thêm các cột giảm giá & tạm tính vào bảng orders
            migrationBuilder.AddColumn<decimal>(
                name: "sub_total_amount",
                table: "orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "sub_total_currency",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "VND");

            migrationBuilder.AddColumn<decimal>(
                name: "discount_percent",
                table: "orders",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount",
                table: "orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "discount_currency",
                table: "orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "VND");

            // Backfill dữ liệu cho các đơn hàng cũ (nếu có)
            migrationBuilder.Sql("UPDATE orders SET sub_total_amount = total_amount, sub_total_currency = currency, discount_currency = currency WHERE sub_total_amount = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gift_product_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "gift_product_name",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_gift",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "sub_total_amount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "sub_total_currency",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "discount_percent",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "discount_amount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "discount_currency",
                table: "orders");
        }
    }
}
