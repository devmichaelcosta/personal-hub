using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalHub.Infrastructure.Migrations
{
    public partial class renomeandotables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcela_Valores_DespesaId",
                table: "Parcela");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Valores",
                table: "Valores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rendimento",
                table: "Rendimento");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Parcela",
                table: "Parcela");

            migrationBuilder.RenameTable(
                name: "Valores",
                newName: "Despesas");

            migrationBuilder.RenameTable(
                name: "Rendimento",
                newName: "Rendimentos");

            migrationBuilder.RenameTable(
                name: "Parcela",
                newName: "Parcelas");

            migrationBuilder.RenameIndex(
                name: "IX_Parcela_DespesaId",
                table: "Parcelas",
                newName: "IX_Parcelas_DespesaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Despesas",
                table: "Despesas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rendimentos",
                table: "Rendimentos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Parcelas",
                table: "Parcelas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parcelas_Despesas_DespesaId",
                table: "Parcelas",
                column: "DespesaId",
                principalTable: "Despesas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parcelas_Despesas_DespesaId",
                table: "Parcelas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rendimentos",
                table: "Rendimentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Parcelas",
                table: "Parcelas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Despesas",
                table: "Despesas");

            migrationBuilder.RenameTable(
                name: "Rendimentos",
                newName: "Rendimento");

            migrationBuilder.RenameTable(
                name: "Parcelas",
                newName: "Parcela");

            migrationBuilder.RenameTable(
                name: "Despesas",
                newName: "Valores");

            migrationBuilder.RenameIndex(
                name: "IX_Parcelas_DespesaId",
                table: "Parcela",
                newName: "IX_Parcela_DespesaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rendimento",
                table: "Rendimento",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Parcela",
                table: "Parcela",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Valores",
                table: "Valores",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Parcela_Valores_DespesaId",
                table: "Parcela",
                column: "DespesaId",
                principalTable: "Valores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
