using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KambaDiami.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProvinciasDaApiPublica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Neighborhoods",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "MunicipalityName",
                table: "Neighborhoods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MunicipalitySlug",
                table: "Neighborhoods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProvinceName",
                table: "Neighborhoods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProvinceSlug",
                table: "Neighborhoods",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Preenche os novos campos a partir das tabelas antigas, antes de as apagar. Os slugs seguem
            // a API angolaprovinciasapi.ao: minúsculas, sem acentos e com hífenes no lugar dos espaços.
            const string Slug = "trim(both '-' from regexp_replace(lower(translate({0}, " +
                "'ÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇáàâãäéèêëíìîïóòôõöúùûüç', " +
                "'AAAAAEEEEIIIIOOOOOUUUUCaaaaaeeeeiiiiooooouuuuc')), '[^a-z0-9]+', '-', 'g'))";

            migrationBuilder.Sql($"""
                UPDATE "Neighborhoods" AS n SET
                    "MunicipalityName" = m."Name",
                    "MunicipalitySlug" = {string.Format(Slug, "m.\"Name\"")},
                    "ProvinceName" = p."Name",
                    "ProvinceSlug" = {string.Format(Slug, "p.\"Name\"")}
                FROM "Municipalities" AS m
                JOIN "Provinces" AS p ON p."Id" = m."ProvinceId"
                WHERE m."Id" = n."MunicipalityId";
                """);

            // Desde a reorganização de 2024 já não existe um município "Luanda": a Maianga e a Ingombota
            // passaram a ser municípios. Os restantes bairros desse antigo município ficam na Ingombota.
            migrationBuilder.Sql($"""
                UPDATE "Neighborhoods" SET
                    "MunicipalityName" = "Name",
                    "MunicipalitySlug" = {string.Format(Slug, "\"Name\"")}
                WHERE "ProvinceSlug" = 'luanda' AND "MunicipalitySlug" = 'luanda'
                  AND {string.Format(Slug, "\"Name\"")} IN ('maianga', 'ingombota');

                UPDATE "Neighborhoods" SET
                    "MunicipalityName" = 'Ingombota',
                    "MunicipalitySlug" = 'ingombota'
                WHERE "ProvinceSlug" = 'luanda' AND "MunicipalitySlug" = 'luanda';
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Neighborhoods_Municipalities_MunicipalityId",
                table: "Neighborhoods");

            migrationBuilder.DropTable(
                name: "Municipalities");

            migrationBuilder.DropTable(
                name: "Provinces");

            migrationBuilder.DropIndex(
                name: "IX_Neighborhoods_MunicipalityId",
                table: "Neighborhoods");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Neighborhoods");

            migrationBuilder.CreateIndex(
                name: "IX_Neighborhoods_MunicipalitySlug",
                table: "Neighborhoods",
                column: "MunicipalitySlug");

            migrationBuilder.CreateIndex(
                name: "IX_Neighborhoods_ProvinceSlug",
                table: "Neighborhoods",
                column: "ProvinceSlug");
        }

        /// <summary>
        /// Repõe a estrutura antiga, mas não os dados: as tabelas de províncias e municípios voltam vazias.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Neighborhoods_MunicipalitySlug",
                table: "Neighborhoods");

            migrationBuilder.DropIndex(
                name: "IX_Neighborhoods_ProvinceSlug",
                table: "Neighborhoods");

            migrationBuilder.DropColumn(
                name: "MunicipalityName",
                table: "Neighborhoods");

            migrationBuilder.DropColumn(
                name: "MunicipalitySlug",
                table: "Neighborhoods");

            migrationBuilder.DropColumn(
                name: "ProvinceName",
                table: "Neighborhoods");

            migrationBuilder.DropColumn(
                name: "ProvinceSlug",
                table: "Neighborhoods");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Neighborhoods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<int>(
                name: "MunicipalityId",
                table: "Neighborhoods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Provinces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Municipalities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProvinceId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipalities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Municipalities_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Neighborhoods_MunicipalityId",
                table: "Neighborhoods",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_ProvinceId",
                table: "Municipalities",
                column: "ProvinceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Neighborhoods_Municipalities_MunicipalityId",
                table: "Neighborhoods",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
