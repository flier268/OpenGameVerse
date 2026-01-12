# IGDB Metadata Integration Setup

OpenGameVerse uses the IGDB (Internet Game Database) API to enrich game metadata and download cover art.

## Getting IGDB API Credentials

1. **Create a Twitch Account** (if you don't have one)
   - Go to https://www.twitch.tv
   - Sign up for a free account

2. **Register Your Application**
   - Go to https://dev.twitch.tv/console/apps
   - Click "Register Your Application"
   - Fill in the details:
     - **Name**: OpenGameVerse (or any name you prefer)
     - **OAuth Redirect URLs**: http://localhost
     - **Category**: Application Integration
   - Click "Create"

3. **Get Your Credentials**
   - After creating the app, click "Manage"
   - You'll see your **Client ID**
   - Click "New Secret" to generate a **Client Secret**
   - Save both values securely

## Configuring OpenGameVerse

Set the following environment variables before running OpenGameVerse:

### Linux/macOS
```bash
export IGDB_CLIENT_ID="your_client_id_here"
export IGDB_CLIENT_SECRET="your_client_secret_here"
```

To make these permanent, add them to your `~/.bashrc` or `~/.zshrc`:
```bash
echo 'export IGDB_CLIENT_ID="your_client_id_here"' >> ~/.bashrc
echo 'export IGDB_CLIENT_SECRET="your_client_secret_here"' >> ~/.bashrc
source ~/.bashrc
```

### Windows (PowerShell)
```powershell
$env:IGDB_CLIENT_ID="your_client_id_here"
$env:IGDB_CLIENT_SECRET="your_client_secret_here"
```

To make these permanent:
```powershell
[System.Environment]::SetEnvironmentVariable('IGDB_CLIENT_ID', 'your_client_id_here', 'User')
[System.Environment]::SetEnvironmentVariable('IGDB_CLIENT_SECRET', 'your_client_secret_here', 'User')
```

### Windows (Command Prompt)
```cmd
set IGDB_CLIENT_ID=your_client_id_here
set IGDB_CLIENT_SECRET=your_client_secret_here
```

## Features

When IGDB credentials are configured, OpenGameVerse will:

- **Automatically fetch metadata** for games when they're loaded
- **Download and cache cover art** in WebP format (optimized for size)
- **Store cover images** in `~/.config/OpenGameVerse/covers` (Linux) or `%APPDATA%\OpenGameVerse\covers` (Windows)
- **Update the database** with IGDB IDs and cover art paths

## Running Without IGDB

OpenGameVerse works perfectly fine without IGDB credentials. If credentials are not provided:
- Games will still be discovered and displayed
- Only game information from the platform (Steam, Epic, etc.) will be shown
- No cover art will be downloaded

## Troubleshooting

### "Failed to obtain access token"
- Verify your Client ID and Client Secret are correct
- Make sure you haven't exceeded the API rate limits (500 requests per second)
- Check that your Twitch app is in "Released" status

### "No cover art displayed"
- Check that environment variables are set correctly
- Verify the covers directory has write permissions
- Some games might not have cover art in IGDB

### API Rate Limits
IGDB has generous rate limits:
- **500 requests per second**
- **4 requests per second per IP**

If you have a large library, metadata fetching happens in the background and won't block the UI.

## Data Storage

- **Database**: `~/.config/OpenGameVerse/opengameverse.db` (Linux) or `%APPDATA%\OpenGameVerse\opengameverse.db` (Windows)
- **Cover Cache**: `~/.config/OpenGameVerse/covers` (Linux) or `%APPDATA%\OpenGameVerse\covers` (Windows)
- Cover images are stored as WebP format (264x352px) for optimal size

## Privacy

OpenGameVerse only sends game titles to IGDB for metadata lookup. No personal information is transmitted.
