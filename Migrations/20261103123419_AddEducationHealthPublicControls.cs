using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvcSecurity.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationHealthPublicControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CongKhaiChucVu",
                table: "NhanSus",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CongKhaiDonVi",
                table: "NhanSus",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CongKhaiHoTen",
                table: "NhanSus",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CongKhaiNckh",
                table: "NhanSus",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CongKhaiTrinhDo",
                table: "NhanSus",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ChungChis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanSuId = table.Column<int>(type: "int", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DonViCap = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayCap = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoHieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TepMinhChungId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChungChis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChungChis_NhanSus_NhanSuId",
                        column: x => x.NhanSuId,
                        principalTable: "NhanSus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChungChis_TepMinhChungs_TepMinhChungId",
                        column: x => x.TepMinhChungId,
                        principalTable: "TepMinhChungs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuSucKhoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanSuId = table.Column<int>(type: "int", nullable: false),
                    NgayGhiNhan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TinhTrang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuSucKhoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuSucKhoes_NhanSus_NhanSuId",
                        column: x => x.NhanSuId,
                        principalTable: "NhanSus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChungChis_NhanSuId",
                table: "ChungChis",
                column: "NhanSuId");

            migrationBuilder.CreateIndex(
                name: "IX_ChungChis_TepMinhChungId",
                table: "ChungChis",
                column: "TepMinhChungId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuSucKhoes_NhanSuId",
                table: "LichSuSucKhoes",
                column: "NhanSuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChungChis");

            migrationBuilder.DropTable(
                name: "LichSuSucKhoes");

            migrationBuilder.DropColumn(
                name: "CongKhaiChucVu",
                table: "NhanSus");

            migrationBuilder.DropColumn(
                name: "CongKhaiDonVi",
                table: "NhanSus");

            migrationBuilder.DropColumn(
                name: "CongKhaiHoTen",
                table: "NhanSus");

            migrationBuilder.DropColumn(
                name: "CongKhaiNckh",
                table: "NhanSus");

            migrationBuilder.DropColumn(
                name: "CongKhaiTrinhDo",
                table: "NhanSus");
        }
    }
}
