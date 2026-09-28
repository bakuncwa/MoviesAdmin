using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MoviesAdmin.Migrations
{
    /// <inheritdoc />
    public partial class SeedDirectorsStudios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Directors",
                columns: new[] { "Id", "Bio", "Name" },
                values: new object[,]
                {
                    { 1, "Director of Pride & Prejudice (2005), produced by Working Title Films.", "Joe Wright" },
                    { 2, "Director of Little Women (2019), released by Columbia Pictures.", "Greta Gerwig" },
                    { 3, "Director of Inception (2010), produced by Syncopy.", "Christopher Nolan" },
                    { 4, "Director of Dune (2021), produced by Legendary Pictures.", "Denis Villeneuve" },
                    { 5, "Director of Parasite (2019), produced by Barunson E&A.", "Bong Joon-ho" }
                });

            migrationBuilder.InsertData(
                table: "Studios",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Working Title Films" },
                    { 2, "Columbia Pictures" },
                    { 3, "Syncopy" },
                    { 4, "Legendary Pictures" },
                    { 5, "Barunson E&A" }
                });

            migrationBuilder.InsertData(
                table: "Movies",
                columns: new[] { "Id", "DirectorId", "PosterImagePath", "PosterUrl", "ReleaseDate", "RuntimeMinutes", "StudioId", "Synopsis", "Title" },
                values: new object[] { 1, 1, null, "https://image.tmdb.org/t/p/w500/o8UhmEbWPHmTUxP0lMuCoqNkbB3.jpg", new DateTime(2005, 9, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), 129, 1, "Sparks fly when spirited Elizabeth Bennet meets single, rich, and proud Mr. Darcy. But Mr. Darcy reluctantly finds himself falling in love with a woman beneath his class. Can each overcome their own pride and prejudice?", "Pride & Prejudice" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Directors",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Directors",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Directors",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Directors",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Studios",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Studios",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Studios",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Studios",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Directors",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Studios",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
