using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalHub.Infrastructure.Migrations
{
    public partial class rendimentosusuario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "Rendimentos",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UsuariosDespesas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DespesaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosDespesas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosDespesas_Despesas_DespesaId",
                        column: x => x.DespesaId,
                        principalTable: "Despesas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosDespesas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rendimentos_UsuarioId",
                table: "Rendimentos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosDespesas_DespesaId",
                table: "UsuariosDespesas",
                column: "DespesaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosDespesas_UsuarioId",
                table: "UsuariosDespesas",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rendimentos_Usuarios_UsuarioId",
                table: "Rendimentos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rendimentos_Usuarios_UsuarioId",
                table: "Rendimentos");

            migrationBuilder.DropTable(
                name: "UsuariosDespesas");

            migrationBuilder.DropIndex(
                name: "IX_Rendimentos_UsuarioId",
                table: "Rendimentos");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Rendimentos");
        }
    }
}
