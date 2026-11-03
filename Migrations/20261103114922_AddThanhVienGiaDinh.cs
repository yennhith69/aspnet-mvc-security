using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvcSecurity.Migrations
{
    /// <inheritdoc />
    public partial class AddThanhVienGiaDinh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThanhVienGiaDinhs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanSuId = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QuanHe = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NamSinh = table.Column<int>(type: "int", nullable: true),
                    NgheNghiep = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhVienGiaDinhs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThanhVienGiaDinhs_NhanSus_NhanSuId",
                        column: x => x.NhanSuId,
                        principalTable: "NhanSus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienGiaDinhs_NhanSuId",
                table: "ThanhVienGiaDinhs",
                column: "NhanSuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThanhVienGiaDinhs");
        }
    }
}
