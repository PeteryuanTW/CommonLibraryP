using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonLibraryP.MachinePKG.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MachineStatusLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MachineID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LogTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineStatusLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModbusSlaveConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Station = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModbusSlaveConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagCategory",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConnectionType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagCategory", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TagWarningBoolConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ComparisonCode = table.Column<int>(type: "int", nullable: false),
                    WarningMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetBoolValue = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagWarningBoolConditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagWarningUshortConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ComparisonCode = table.Column<int>(type: "int", nullable: false),
                    WarningMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetUshortValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagWarningUshortConditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Machine",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    ConnectionType = table.Column<int>(type: "int", nullable: false),
                    MaxRetryCount = table.Column<int>(type: "int", nullable: false),
                    TagCategoryID = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdateDelay = table.Column<int>(type: "int", nullable: false),
                    RecordStatusChanged = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machine", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Machine_TagCategory_TagCategoryID",
                        column: x => x.TagCategoryID,
                        principalTable: "TagCategory",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ModbusTCPTags",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    UpdateByTime = table.Column<bool>(type: "bit", nullable: false),
                    Station = table.Column<byte>(type: "tinyint", nullable: false),
                    InputOrOutput = table.Column<bool>(type: "bit", nullable: false),
                    StartIndex = table.Column<int>(type: "int", nullable: false),
                    Offset = table.Column<int>(type: "int", nullable: false),
                    StringReverse = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModbusTCPTags", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ModbusTCPTags_TagCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TagCategory",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Machine_Name",
                table: "Machine",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Machine_TagCategoryID",
                table: "Machine",
                column: "TagCategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_ModbusTCPTags_CategoryId",
                table: "ModbusTCPTags",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ModbusTCPTags_Name",
                table: "ModbusTCPTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagCategory_Name",
                table: "TagCategory",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagWarningBoolConditions_Name",
                table: "TagWarningBoolConditions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagWarningBoolConditions_TagId",
                table: "TagWarningBoolConditions",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagWarningUshortConditions_Name",
                table: "TagWarningUshortConditions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TagWarningUshortConditions_TagId",
                table: "TagWarningUshortConditions",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Machine");

            migrationBuilder.DropTable(
                name: "MachineStatusLogs");

            migrationBuilder.DropTable(
                name: "ModbusSlaveConfigs");

            migrationBuilder.DropTable(
                name: "ModbusTCPTags");

            migrationBuilder.DropTable(
                name: "TagWarningBoolConditions");

            migrationBuilder.DropTable(
                name: "TagWarningUshortConditions");

            migrationBuilder.DropTable(
                name: "TagCategory");
        }
    }
}
