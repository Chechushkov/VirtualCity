using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Excursion_GPT.Infrastructure.Migrations
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
                    position = table.Column<List<double>>(type: "jsonb", nullable: false),
                    rotation = table.Column<List<double>>(type: "jsonb", nullable: false)
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
                    x = table.Column<double>(type: "double precision", nullable: false),
                    z = table.Column<double>(type: "double precision", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    height = table.Column<double>(type: "double precision", nullable: true),
                    modelid = table.Column<Guid>(type: "uuid", nullable: true),
                    rotation = table.Column<List<double>>(type: "jsonb", nullable: true),
                    nodes_json = table.Column<string>(type: "text", nullable: true)
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
                    position = table.Column<List<double>>(type: "jsonb", nullable: false),
                    rotation = table.Column<List<double>>(type: "jsonb", nullable: false),
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
