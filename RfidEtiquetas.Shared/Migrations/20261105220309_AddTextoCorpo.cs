using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RfidEtiquetas.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddTextoCorpo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TextoCorpo",
                table: "Etiquetas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TextoCorpoLargura",
                table: "Etiquetas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TextoCorpoTam",
                table: "Etiquetas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TextoCorpoX",
                table: "Etiquetas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TextoCorpoY",
                table: "Etiquetas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextoCorpo",
                table: "Etiquetas");

            migrationBuilder.DropColumn(
                name: "TextoCorpoLargura",
                table: "Etiquetas");

            migrationBuilder.DropColumn(
                name: "TextoCorpoTam",
                table: "Etiquetas");

            migrationBuilder.DropColumn(
                name: "TextoCorpoX",
                table: "Etiquetas");

            migrationBuilder.DropColumn(
                name: "TextoCorpoY",
                table: "Etiquetas");
        }
    }
}
