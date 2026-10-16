# Stream API

A RESTful backend built with .NET for a multi-provider music streaming application. The service aggregates music from multiple providers (YouTube and Jamendo) into a unified catalog and exposes endpoints for authentication, search, audio streaming, playlists, favorites, and listening history.

## Architecture and Technologies

The project is structured according to Clean Architecture principles:

- Stream.Domain: Core entities (User, Track, Artist, ProviderTrack, Playlist, Favorite, ListeningHistory).
- Stream.Application: Interfaces, DTOs, and business logic services (SearchService, MusicProviderManager, TrackResolver, AuthService, etc.).
- Stream.Infrastructure: Data access (EF Core with PostgreSQL), external music providers (YouTubeExplode, Jamendo HTTP Client), security, and JWT handling.
- Stream.Api: REST controllers, global exception handling middleware, and Swagger / CORS configuration.

## How the Data Flow Works

1. Search: When a user searches for a track, the API queries all configured providers in parallel. Candidates are automatically resolved and persisted to the database via TrackResolver, ensuring every search result returns a valid `trackId` from the start.
2. Streaming: For stable playback on web browsers and mobile devices, the API provides audio proxy streaming endpoints (`/play`). These support HTTP Range requests (206 Partial Content), allowing audio players to seek forward and backward smoothly without CORS or IP-restriction issues from external providers.
3. Authentication: Authentication uses JWT tokens. Upon register/login, clients receive an `accessToken` (used in the `Authorization: Bearer <token>` header) and a `refreshToken` to renew sessions. In addition to the response body, the `refreshToken` is also set as an `HttpOnly`, `SameSite=Lax` cookie, providing seamless XSS protection for web clients while maintaining compatibility with native mobile apps (e.g., React Native).

---

## API Endpoints

### Authentication (/api/auth)

User account management and session handling. Supports both web browsers (using HttpOnly cookies) and mobile clients (using token in body).

- POST /api/auth/register
  - Description: Registers a new user account.
  - Body: `{ "username": "...", "email": "...", "password": "..." }`
  - Response: User details, accessToken, refreshToken.
  - Cookies: Sets `refreshToken` in an HttpOnly cookie.

- POST /api/auth/login
  - Description: Authenticates an existing user by email or username.
  - Body: `{ "emailOrUsername": "...", "password": "..." }`
  - Response: User details, accessToken, refreshToken.
  - Cookies: Sets `refreshToken` in an HttpOnly cookie.

- POST /api/auth/refresh
  - Description: Issues a new pair of tokens when the access token expires.
  - Body: Optional `{ "refreshToken": "..." }` (web clients omit this and rely on the automatic HttpOnly cookie; mobile clients can send it in the request body).
  - Response: New access and refresh tokens.
  - Cookies: Updates the `refreshToken` HttpOnly cookie.

- GET /api/auth/me [Requires Auth]
  - Description: Returns the profile data of the currently authenticated user.

- POST /api/auth/logout [Requires Auth]
  - Description: Revokes the refresh token of the authenticated user in the database and clears the `refreshToken` HttpOnly cookie.

---

### Search (/api/search)

- GET /api/search?q={term}
  - Description: Searches tracks across YouTube and Jamendo.
  - Behavior: Automatically resolves new tracks to the local database and returns a standardized list.
  - Each item includes: `trackId`, `title`, `artist`, `duration`, `artworkUrl`, `provider`, `externalId`.

---

### Tracks (/api/tracks)

- GET /api/tracks/{id}
  - Description: Returns full details for a track from the database by its unique identifier (Guid).

- POST /api/tracks/resolve
  - Description: Resolves and persists an external candidate track into the local catalog.

---

### Audio Streaming (/api/stream)

Endpoints used by the audio player to stream playback.

- GET /api/stream/play/{externalId}?provider={provider}
  - Description: Streams binary audio as a proxy directly from the external source.
  - Recommended for: Playing fresh tracks directly from search results. Supports HTTP Range requests.

- GET /api/stream/track/{trackId}/play
  - Description: Streams binary audio for a database track with automatic provider fallback (tries the next available provider if one fails).
  - Recommended for: Playing tracks from playlists, favorites, or history.

- GET /api/stream/{externalId}?provider={provider}
  - Description: Returns the direct playback source URL (useful when direct client playback is preferred, e.g. Jamendo).

- GET /api/stream/track/{trackId}
  - Description: Returns the direct playback source URL for a stored track.

---

### Playlists (/api/playlists) [Requires Auth]

User playlist management.

- GET /api/playlists
  - Description: Returns all playlists created by the authenticated user.

- GET /api/playlists/{id}
  - Description: Returns the details of a playlist including its tracks ordered by position.

- POST /api/playlists
  - Description: Creates a new playlist.
  - Body: `{ "name": "Playlist Name", "description": "Optional description" }`

- DELETE /api/playlists/{id}
  - Description: Deletes a playlist owned by the user.

- POST /api/playlists/{id}/tracks/{trackId}
  - Description: Adds a track to a playlist.

- DELETE /api/playlists/{id}/tracks/{trackId}
  - Description: Removes a track from a playlist.

---

### Favorites (/api/favorites) [Requires Auth]

- GET /api/favorites
  - Description: Returns all tracks saved as favorites by the current user.

- POST /api/favorites/{trackId}
  - Description: Adds a track to favorites.

- DELETE /api/favorites/{trackId}
  - Description: Removes a track from favorites.

- GET /api/favorites/{trackId}/check
  - Description: Checks if a track is in the user's favorites (returns `{ isFavorite: true/false }`).

---

### Listening History (/api/history) [Requires Auth]

- GET /api/history?limit={number}
  - Description: Returns recently played tracks (default is 20 items).

- POST /api/history
  - Description: Records a track playback event.
  - Body: `{ "trackId": "guid", "duration": "00:03:30", "completed": true }`

- DELETE /api/history
  - Description: Clears the authenticated user's listening history.

---

## Local Setup and Running

1. Prerequisites:
   - .NET 10 SDK
   - Running PostgreSQL server

2. Database and migrations:
   Apply database migrations by running the following command from the solution root:
   ```powershell
   dotnet ef database update --project Stream.Infrastructure --startup-project Stream.Api
   ```

3. Running the application:
   ```powershell
   dotnet run --project Stream.Api
   ```
   The API will start and expose the Swagger UI at `/swagger` (in Development mode).
