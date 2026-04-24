using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tms.Orders.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameColumnsAndWeightValueObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PickupTo",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupWindowTo");

            migrationBuilder.RenameColumn(
                name: "PickupSubDistrict",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupAddress_SubDistrict");

            migrationBuilder.RenameColumn(
                name: "PickupStreet",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupAddress_Street");

            migrationBuilder.RenameColumn(
                name: "PickupProvince",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupAddress_Province");

            migrationBuilder.RenameColumn(
                name: "PickupPostalCode",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupAddress_PostalCode");

            migrationBuilder.RenameColumn(
                name: "PickupName",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupAddress_Name");

            migrationBuilder.RenameColumn(
                name: "PickupLng",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupAddress_Longitude");

            migrationBuilder.RenameColumn(
                name: "PickupLat",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupAddress_Latitude");

            migrationBuilder.RenameColumn(
                name: "PickupFrom",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupWindowFrom");

            migrationBuilder.RenameColumn(
                name: "PickupDistrict",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupAddress_District");

            migrationBuilder.RenameColumn(
                name: "DropoffTo",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffWindowTo");

            migrationBuilder.RenameColumn(
                name: "DropoffSubDistrict",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffAddress_SubDistrict");

            migrationBuilder.RenameColumn(
                name: "DropoffStreet",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffAddress_Street");

            migrationBuilder.RenameColumn(
                name: "DropoffProvince",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffAddress_Province");

            migrationBuilder.RenameColumn(
                name: "DropoffPostalCode",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffAddress_PostalCode");

            migrationBuilder.RenameColumn(
                name: "DropoffName",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffAddress_Name");

            migrationBuilder.RenameColumn(
                name: "DropoffLng",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffAddress_Longitude");

            migrationBuilder.RenameColumn(
                name: "DropoffLat",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffAddress_Latitude");

            migrationBuilder.RenameColumn(
                name: "DropoffFrom",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffWindowFrom");

            migrationBuilder.RenameColumn(
                name: "DropoffDistrict",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffAddress_District");

            migrationBuilder.RenameColumn(
                name: "Weight",
                schema: "ord",
                table: "OrderItems",
                newName: "WeightKg");

            migrationBuilder.RenameColumn(
                name: "Volume",
                schema: "ord",
                table: "OrderItems",
                newName: "VolumeCBM");

            migrationBuilder.AddColumn<string>(
                name: "WeightUnit",
                schema: "ord",
                table: "OrderItems",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeightUnit",
                schema: "ord",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "PickupWindowTo",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupTo");

            migrationBuilder.RenameColumn(
                name: "PickupWindowFrom",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupFrom");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_SubDistrict",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupSubDistrict");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_Street",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupStreet");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_Province",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupProvince");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_PostalCode",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupPostalCode");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_Name",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupName");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_Longitude",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupLng");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_Latitude",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupLat");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_District",
                schema: "ord",
                table: "TransportOrders",
                newName: "PickupDistrict");

            migrationBuilder.RenameColumn(
                name: "DropoffWindowTo",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffTo");

            migrationBuilder.RenameColumn(
                name: "DropoffWindowFrom",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffFrom");

            migrationBuilder.RenameColumn(
                name: "DropoffAddress_SubDistrict",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffSubDistrict");

            migrationBuilder.RenameColumn(
                name: "DropoffAddress_Street",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffStreet");

            migrationBuilder.RenameColumn(
                name: "DropoffAddress_Province",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffProvince");

            migrationBuilder.RenameColumn(
                name: "DropoffAddress_PostalCode",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffPostalCode");

            migrationBuilder.RenameColumn(
                name: "DropoffAddress_Name",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffName");

            migrationBuilder.RenameColumn(
                name: "DropoffAddress_Longitude",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffLng");

            migrationBuilder.RenameColumn(
                name: "DropoffAddress_Latitude",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffLat");

            migrationBuilder.RenameColumn(
                name: "DropoffAddress_District",
                schema: "ord",
                table: "TransportOrders",
                newName: "DropoffDistrict");

            migrationBuilder.RenameColumn(
                name: "WeightKg",
                schema: "ord",
                table: "OrderItems",
                newName: "Weight");

            migrationBuilder.RenameColumn(
                name: "VolumeCBM",
                schema: "ord",
                table: "OrderItems",
                newName: "Volume");
        }
    }
}
