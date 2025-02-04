using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddNoteOwnerRelationship : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // First ensure the OwnerId column exists and has correct values
        migrationBuilder.Sql(@"
            UPDATE Notes 
            SET OwnerId = (SELECT Id FROM Users WHERE Users.Id = Notes.OwnerId)
            WHERE EXISTS (SELECT 1 FROM Users WHERE Users.Id = Notes.OwnerId)
        ");

        // Then add the foreign key constraint
        migrationBuilder.AddForeignKey(
            name: "FK_Notes_Users_OwnerId",
            table: "Notes",
            column: "OwnerId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Notes_Users_OwnerId",
            table: "Notes");
    }
} 