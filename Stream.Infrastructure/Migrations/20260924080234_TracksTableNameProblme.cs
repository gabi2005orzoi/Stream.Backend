using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stream.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TracksTableNameProblme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_favorites_tables_TrackId",
                table: "favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_listening_history_tables_TrackId",
                table: "listening_history");

            migrationBuilder.DropForeignKey(
                name: "FK_playlist_tracks_tables_TrackId",
                table: "playlist_tracks");

            migrationBuilder.DropForeignKey(
                name: "FK_provider_tracks_tables_TrackId",
                table: "provider_tracks");

            migrationBuilder.DropForeignKey(
                name: "FK_tables_albums_AlbumId",
                table: "tables");

            migrationBuilder.DropForeignKey(
                name: "FK_tables_artists_ArtistId",
                table: "tables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tables",
                table: "tables");

            migrationBuilder.RenameTable(
                name: "tables",
                newName: "tracks");

            migrationBuilder.RenameIndex(
                name: "IX_tables_Title",
                table: "tracks",
                newName: "IX_tracks_Title");

            migrationBuilder.RenameIndex(
                name: "IX_tables_ArtistId",
                table: "tracks",
                newName: "IX_tracks_ArtistId");

            migrationBuilder.RenameIndex(
                name: "IX_tables_AlbumId",
                table: "tracks",
                newName: "IX_tracks_AlbumId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tracks",
                table: "tracks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_favorites_tracks_TrackId",
                table: "favorites",
                column: "TrackId",
                principalTable: "tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_listening_history_tracks_TrackId",
                table: "listening_history",
                column: "TrackId",
                principalTable: "tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_playlist_tracks_tracks_TrackId",
                table: "playlist_tracks",
                column: "TrackId",
                principalTable: "tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_provider_tracks_tracks_TrackId",
                table: "provider_tracks",
                column: "TrackId",
                principalTable: "tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tracks_albums_AlbumId",
                table: "tracks",
                column: "AlbumId",
                principalTable: "albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_tracks_artists_ArtistId",
                table: "tracks",
                column: "ArtistId",
                principalTable: "artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_favorites_tracks_TrackId",
                table: "favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_listening_history_tracks_TrackId",
                table: "listening_history");

            migrationBuilder.DropForeignKey(
                name: "FK_playlist_tracks_tracks_TrackId",
                table: "playlist_tracks");

            migrationBuilder.DropForeignKey(
                name: "FK_provider_tracks_tracks_TrackId",
                table: "provider_tracks");

            migrationBuilder.DropForeignKey(
                name: "FK_tracks_albums_AlbumId",
                table: "tracks");

            migrationBuilder.DropForeignKey(
                name: "FK_tracks_artists_ArtistId",
                table: "tracks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tracks",
                table: "tracks");

            migrationBuilder.RenameTable(
                name: "tracks",
                newName: "tables");

            migrationBuilder.RenameIndex(
                name: "IX_tracks_Title",
                table: "tables",
                newName: "IX_tables_Title");

            migrationBuilder.RenameIndex(
                name: "IX_tracks_ArtistId",
                table: "tables",
                newName: "IX_tables_ArtistId");

            migrationBuilder.RenameIndex(
                name: "IX_tracks_AlbumId",
                table: "tables",
                newName: "IX_tables_AlbumId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tables",
                table: "tables",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_favorites_tables_TrackId",
                table: "favorites",
                column: "TrackId",
                principalTable: "tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_listening_history_tables_TrackId",
                table: "listening_history",
                column: "TrackId",
                principalTable: "tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_playlist_tracks_tables_TrackId",
                table: "playlist_tracks",
                column: "TrackId",
                principalTable: "tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_provider_tracks_tables_TrackId",
                table: "provider_tracks",
                column: "TrackId",
                principalTable: "tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tables_albums_AlbumId",
                table: "tables",
                column: "AlbumId",
                principalTable: "albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_tables_artists_ArtistId",
                table: "tables",
                column: "ArtistId",
                principalTable: "artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
