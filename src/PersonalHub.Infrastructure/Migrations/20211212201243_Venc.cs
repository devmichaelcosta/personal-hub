using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalHub.Infrastructure.Migrations
{
    public partial class Venc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataFim",
                table: "Despesas");

            migrationBuilder.RenameColumn(
                name: "DataInicio",
                table: "Despesas",
                newName: "Vencimento");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Vencimento",
                table: "Despesas",
                newName: "DataInicio");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFim",
                table: "Despesas",
                type: "datetime2",
                nullable: true);
        }
    }
}
