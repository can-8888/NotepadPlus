using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotepadPlusApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDriveTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Folders_FolderId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_UserId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Folders_Folders_ParentId",
                table: "Folders");

            migrationBuilder.DropForeignKey(
                name: "FK_Folders_Users_UserId",
                table: "Folders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Files",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Files");

            migrationBuilder.RenameTable(
                name: "Files",
                newName: "DriveFiles");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Folders",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "Folders",
                newName: "ParentFolderId");

            migrationBuilder.RenameIndex(
                name: "IX_Folders_UserId",
                table: "Folders",
                newName: "IX_Folders_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Folders_ParentId",
                table: "Folders",
                newName: "IX_Folders_ParentFolderId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "DriveFiles",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "DriveFiles",
                newName: "Path");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "DriveFiles",
                newName: "ContentType");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "DriveFiles",
                newName: "UploadedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Files_UserId",
                table: "DriveFiles",
                newName: "IX_DriveFiles_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Files_FolderId",
                table: "DriveFiles",
                newName: "IX_DriveFiles_FolderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DriveFiles",
                table: "DriveFiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DriveFiles_Folders_FolderId",
                table: "DriveFiles",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DriveFiles_Users_OwnerId",
                table: "DriveFiles",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Folders_Folders_ParentFolderId",
                table: "Folders",
                column: "ParentFolderId",
                principalTable: "Folders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Folders_Users_OwnerId",
                table: "Folders",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriveFiles_Folders_FolderId",
                table: "DriveFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_DriveFiles_Users_OwnerId",
                table: "DriveFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Folders_Folders_ParentFolderId",
                table: "Folders");

            migrationBuilder.DropForeignKey(
                name: "FK_Folders_Users_OwnerId",
                table: "Folders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DriveFiles",
                table: "DriveFiles");

            migrationBuilder.RenameTable(
                name: "DriveFiles",
                newName: "Files");

            migrationBuilder.RenameColumn(
                name: "ParentFolderId",
                table: "Folders",
                newName: "ParentId");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Folders",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Folders_ParentFolderId",
                table: "Folders",
                newName: "IX_Folders_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_Folders_OwnerId",
                table: "Folders",
                newName: "IX_Folders_UserId");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "Files",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Files",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Files",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ContentType",
                table: "Files",
                newName: "Type");

            migrationBuilder.RenameIndex(
                name: "IX_DriveFiles_OwnerId",
                table: "Files",
                newName: "IX_Files_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_DriveFiles_FolderId",
                table: "Files",
                newName: "IX_Files_FolderId");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Folders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Files",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Files",
                table: "Files",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Folders_FolderId",
                table: "Files",
                column: "FolderId",
                principalTable: "Folders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Users_UserId",
                table: "Files",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Folders_Folders_ParentId",
                table: "Folders",
                column: "ParentId",
                principalTable: "Folders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Folders_Users_UserId",
                table: "Folders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
