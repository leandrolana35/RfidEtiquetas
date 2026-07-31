using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RfidEtiquetas.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeysAndPrintJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "EstacoesImpressoras",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    ChaveHash = table.Column<string>(type: "text", nullable: false),
                    ChavePrefixo = table.Column<string>(type: "text", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimoUsoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrintJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyId = table.Column<int>(type: "integer", nullable: false),
                    Epc = table.Column<string>(type: "text", nullable: false),
                    Sku = table.Column<string>(type: "text", nullable: true),
                    Lote = table.Column<string>(type: "text", nullable: true),
                    EstacaoId = table.Column<int>(type: "integer", nullable: false),
                    EtiquetaTemplateId = table.Column<int>(type: "integer", nullable: false),
                    ProjetoId = table.Column<int>(type: "integer", nullable: true),
                    TagRegistroId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MensagemErro = table.Column<string>(type: "text", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintJobs_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintJobs_EstacoesImpressoras_EstacaoId",
                        column: x => x.EstacaoId,
                        principalTable: "EstacoesImpressoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintJobs_Etiquetas_EtiquetaTemplateId",
                        column: x => x.EtiquetaTemplateId,
                        principalTable: "Etiquetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintJobs_Projetos_ProjetoId",
                        column: x => x.ProjetoId,
                        principalTable: "Projetos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintJobs_TagRegistros_TagRegistroId",
                        column: x => x.TagRegistroId,
                        principalTable: "TagRegistros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstacoesImpressoras_EmpresaId",
                table: "EstacoesImpressoras",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ChaveHash",
                table: "ApiKeys",
                column: "ChaveHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_EmpresaId",
                table: "ApiKeys",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_ApiKeyId",
                table: "PrintJobs",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_EstacaoId",
                table: "PrintJobs",
                column: "EstacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_EtiquetaTemplateId",
                table: "PrintJobs",
                column: "EtiquetaTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_ProjetoId",
                table: "PrintJobs",
                column: "ProjetoId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_Status_CriadoEm",
                table: "PrintJobs",
                columns: new[] { "Status", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_TagRegistroId",
                table: "PrintJobs",
                column: "TagRegistroId");

            migrationBuilder.AddForeignKey(
                name: "FK_EstacoesImpressoras_Empresas_EmpresaId",
                table: "EstacoesImpressoras",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EstacoesImpressoras_Empresas_EmpresaId",
                table: "EstacoesImpressoras");

            migrationBuilder.DropTable(
                name: "PrintJobs");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_EstacoesImpressoras_EmpresaId",
                table: "EstacoesImpressoras");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "EstacoesImpressoras");
        }
    }
}
