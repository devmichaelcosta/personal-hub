using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalHub.Infrastructure.Migrations
{
    public partial class rendimentos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcela_Despesas_DespesaId",
                table: "Parcela");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Despesas",
                table: "Despesas");

            migrationBuilder.RenameTable(
                name: "Despesas",
                newName: "Valores");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Valores",
                table: "Valores",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Rendimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descritivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rendimento", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Parcela_Valores_DespesaId",
                table: "Parcela",
                column: "DespesaId",
                principalTable: "Valores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcela_Valores_DespesaId",
                table: "Parcela");

            migrationBuilder.DropTable(
                name: "Rendimento");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Valores",
                table: "Valores");

            migrationBuilder.RenameTable(
                name: "Valores",
                newName: "Despesas");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Despesas",
                table: "Despesas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parcela_Despesas_DespesaId",
                table: "Parcela",
                column: "DespesaId",
                principalTable: "Despesas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
