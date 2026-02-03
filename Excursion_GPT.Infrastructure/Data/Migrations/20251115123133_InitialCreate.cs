using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Excursion_GPT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    login = table.Column<string>(type: "text", nullable: false),
                    passwordhash = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    schoolname = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    creatorid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracks", x => x.id);
                    table.ForeignKey(
                        name: "FK_tracks_users_creatorid",
                        column: x => x.creatorid,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trackid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<double[]>(type: "jsonb", nullable: false),
                    rotation = table.Column<double[]>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_points_tracks_trackid",
                        column: x => x.trackid,
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "buildings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    modelid = table.Column<Guid>(type: "uuid", nullable: true),
                    rotation = table.Column<double[]>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    buildingid = table.Column<Guid>(type: "uuid", nullable: false),
                    trackid = table.Column<Guid>(type: "uuid", nullable: false),
                    minioobjectname = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<double[]>(type: "jsonb", nullable: false),
                    rotation = table.Column<double[]>(type: "jsonb", nullable: false),
                    scale = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_models", x => x.id);
                    table.ForeignKey(
                        name: "FK_models_buildings_buildingid",
                        column: x => x.buildingid,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_models_tracks_trackid",
                        column: x => x.trackid,
                        principalTable: "tracks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "buildings",
                columns: new[] { "id", "latitude", "longitude", "modelid", "rotation" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 55.751244, 37.618423, null, null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 55.755825999999999, 37.6173, null, null }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "login", "name", "passwordhash", "phone", "role", "schoolname" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "admin", "Admin User", "$2a$11$fIWZeN/DLZtPs9OltDnyc.352duNTb7f3SW5J9ylJHNv8IcI8/U9W", "+1234567890", "Admin", "Admin Academy" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "creator", "Creator User", "$2a$11$6E0uIsWE1Qp8E9mxyocveuRhUbZ6Vmip9J2UGPe40qKwb1Oe9h/wC", "+0987654321", "Creator", "Art School" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_buildings_modelid",
                table: "buildings",
                column: "modelid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_models_buildingid",
                table: "models",
                column: "buildingid");

            migrationBuilder.CreateIndex(
                name: "IX_models_trackid",
                table: "models",
                column: "trackid");

            migrationBuilder.CreateIndex(
                name: "IX_points_trackid",
                table: "points",
                column: "trackid");

            migrationBuilder.CreateIndex(
                name: "IX_tracks_creatorid",
                table: "tracks",
                column: "creatorid");

            migrationBuilder.CreateIndex(
                name: "IX_users_login",
                table: "users",
                column: "login",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_buildings_models_modelid",
                table: "buildings",
                column: "modelid",
                principalTable: "models",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_buildings_models_modelid",
                table: "buildings");

            migrationBuilder.DropTable(
                name: "points");

            migrationBuilder.DropTable(
                name: "models");

            migrationBuilder.DropTable(
                name: "buildings");

            migrationBuilder.DropTable(
                name: "tracks");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
