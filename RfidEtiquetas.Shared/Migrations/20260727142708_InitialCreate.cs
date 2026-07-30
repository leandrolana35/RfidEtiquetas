using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RfidEtiquetas.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstacoesImpressoras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NomeEstacao = table.Column<string>(type: "text", nullable: false),
                    ModeloImpressora = table.Column<int>(type: "integer", nullable: false),
                    TipoConexao = table.Column<int>(type: "integer", nullable: false),
                    PortaCom = table.Column<string>(type: "text", nullable: false),
                    BaudRate = table.Column<int>(type: "integer", nullable: false),
                    ImpressoraIp = table.Column<string>(type: "text", nullable: false),
                    ImpressoraPorta = table.Column<int>(type: "integer", nullable: false),
                    DpiOverride = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstacoesImpressoras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Etiquetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Modelo = table.Column<string>(type: "text", nullable: false),
                    EtiquetaX = table.Column<int>(type: "integer", nullable: false),
                    EtiquetaY = table.Column<int>(type: "integer", nullable: false),
                    LarguraMm = table.Column<int>(type: "integer", nullable: false),
                    AlturaMm = table.Column<int>(type: "integer", nullable: false),
                    DefinirTamanho = table.Column<bool>(type: "boolean", nullable: false),
                    Texto1 = table.Column<string>(type: "text", nullable: false),
                    Texto1X = table.Column<int>(type: "integer", nullable: false),
                    Texto1Y = table.Column<int>(type: "integer", nullable: false),
                    Texto1Tam = table.Column<int>(type: "integer", nullable: false),
                    Texto2 = table.Column<string>(type: "text", nullable: false),
                    Texto2X = table.Column<int>(type: "integer", nullable: false),
                    Texto2Y = table.Column<int>(type: "integer", nullable: false),
                    Texto2Tam = table.Column<int>(type: "integer", nullable: false),
                    Texto3 = table.Column<string>(type: "text", nullable: false),
                    Texto3X = table.Column<int>(type: "integer", nullable: false),
                    Texto3Y = table.Column<int>(type: "integer", nullable: false),
                    Texto3Tam = table.Column<int>(type: "integer", nullable: false),
                    Texto4 = table.Column<string>(type: "text", nullable: false),
                    Texto4X = table.Column<int>(type: "integer", nullable: false),
                    Texto4Y = table.Column<int>(type: "integer", nullable: false),
                    Texto4Tam = table.Column<int>(type: "integer", nullable: false),
                    BarTipo = table.Column<int>(type: "integer", nullable: false),
                    BarCodX = table.Column<int>(type: "integer", nullable: false),
                    BarCodY = table.Column<int>(type: "integer", nullable: false),
                    BarAltura = table.Column<int>(type: "integer", nullable: false),
                    BarEspessura = table.Column<int>(type: "integer", nullable: false),
                    BarImprimir = table.Column<bool>(type: "boolean", nullable: false),
                    BarImprimirCodigo = table.Column<bool>(type: "boolean", nullable: false),
                    BarFonteTam = table.Column<int>(type: "integer", nullable: false),
                    BarPrefixo = table.Column<string>(type: "text", nullable: false),
                    BarSufixo = table.Column<string>(type: "text", nullable: false),
                    BarZerosEsquerda = table.Column<int>(type: "integer", nullable: false),
                    RfidAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    RfidBanco = table.Column<int>(type: "integer", nullable: false),
                    RfidTamanhoBits = table.Column<int>(type: "integer", nullable: false),
                    RfidEncodingTipo = table.Column<int>(type: "integer", nullable: false),
                    RfidAlinhamento = table.Column<int>(type: "integer", nullable: false),
                    RfidPrefixo = table.Column<string>(type: "text", nullable: false),
                    RfidImprimirValor = table.Column<bool>(type: "boolean", nullable: false),
                    RfidValorX = table.Column<int>(type: "integer", nullable: false),
                    RfidValorY = table.Column<int>(type: "integer", nullable: false),
                    RfidValorTam = table.Column<int>(type: "integer", nullable: false),
                    LogoImprimir = table.Column<bool>(type: "boolean", nullable: false),
                    LogoArquivo = table.Column<string>(type: "text", nullable: false),
                    LogoX = table.Column<int>(type: "integer", nullable: false),
                    LogoY = table.Column<int>(type: "integer", nullable: false),
                    LogoLargura = table.Column<int>(type: "integer", nullable: false),
                    LogoAltura = table.Column<int>(type: "integer", nullable: false),
                    Intervalo = table.Column<int>(type: "integer", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    Velocidade = table.Column<int>(type: "integer", nullable: false),
                    Densidade = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Etiquetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Perfil = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Projetos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    EtiquetaTemplateId = table.Column<int>(type: "integer", nullable: false),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    SequenciaInicial = table.Column<long>(type: "bigint", nullable: false),
                    SequenciaFinal = table.Column<long>(type: "bigint", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoPorUsuarioId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projetos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projetos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projetos_Etiquetas_EtiquetaTemplateId",
                        column: x => x.EtiquetaTemplateId,
                        principalTable: "Etiquetas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagRegistros",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjetoId = table.Column<int>(type: "integer", nullable: false),
                    NumeroSequencia = table.Column<long>(type: "bigint", nullable: false),
                    CodigoBarras = table.Column<string>(type: "text", nullable: true),
                    DadoRfid = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataImpressao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MensagemErro = table.Column<string>(type: "text", nullable: true),
                    EstacaoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagRegistros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagRegistros_EstacoesImpressoras_EstacaoId",
                        column: x => x.EstacaoId,
                        principalTable: "EstacoesImpressoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TagRegistros_Projetos_ProjetoId",
                        column: x => x.ProjetoId,
                        principalTable: "Projetos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmpresaId",
                table: "AspNetUsers",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projetos_EmpresaId",
                table: "Projetos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Projetos_EtiquetaTemplateId",
                table: "Projetos",
                column: "EtiquetaTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TagRegistros_EstacaoId",
                table: "TagRegistros",
                column: "EstacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_TagRegistros_ProjetoId_NumeroSequencia",
                table: "TagRegistros",
                columns: new[] { "ProjetoId", "NumeroSequencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagRegistros_ProjetoId_Status",
                table: "TagRegistros",
                columns: new[] { "ProjetoId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "TagRegistros");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "EstacoesImpressoras");

            migrationBuilder.DropTable(
                name: "Projetos");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "Etiquetas");
        }
    }
}
