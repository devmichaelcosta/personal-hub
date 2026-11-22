using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalHub.Infrastructure.Migrations
{
    public partial class TagsDespesasTableRename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagDespesa_Despesas_DespesaId",
                table: "TagDespesa");

            migrationBuilder.DropForeignKey(
                name: "FK_TagDespesa_Tag_TagId",
                table: "TagDespesa");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TagDespesa",
                table: "TagDespesa");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tag",
                table: "Tag");

            migrationBuilder.RenameTable(
                name: "TagDespesa",
                newName: "TagsDespesas");

            migrationBuilder.RenameTable(
                name: "Tag",
                newName: "Tags");

            migrationBuilder.RenameIndex(
                name: "IX_TagDespesa_TagId",
                table: "TagsDespesas",
                newName: "IX_TagsDespesas_TagId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TagsDespesas",
                table: "TagsDespesas",
                columns: new[] { "DespesaId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tags",
                table: "Tags",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TagsDespesas_Despesas_DespesaId",
                table: "TagsDespesas",
                column: "DespesaId",
                principalTable: "Despesas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TagsDespesas_Tags_TagId",
                table: "TagsDespesas",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagsDespesas_Despesas_DespesaId",
                table: "TagsDespesas");

            migrationBuilder.DropForeignKey(
                name: "FK_TagsDespesas_Tags_TagId",
                table: "TagsDespesas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TagsDespesas",
                table: "TagsDespesas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tags",
                table: "Tags");

            migrationBuilder.RenameTable(
                name: "TagsDespesas",
                newName: "TagDespesa");

            migrationBuilder.RenameTable(
                name: "Tags",
                newName: "Tag");

            migrationBuilder.RenameIndex(
                name: "IX_TagsDespesas_TagId",
                table: "TagDespesa",
                newName: "IX_TagDespesa_TagId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TagDespesa",
                table: "TagDespesa",
                columns: new[] { "DespesaId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tag",
                table: "Tag",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TagDespesa_Despesas_DespesaId",
                table: "TagDespesa",
                column: "DespesaId",
                principalTable: "Despesas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TagDespesa_Tag_TagId",
                table: "TagDespesa",
                column: "TagId",
                principalTable: "Tag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
