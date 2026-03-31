# Demo

This project is a Blazor WebAssembly demo for the local PocketBase SDK workspace.

## Run it

1. Start PocketBase from the repo root:

   ```powershell
   just pb
   ```

2. Start the demo app from the repo root:

   ```powershell
   dotnet run --project PocketBaseSharp.Demo/Demo.csproj
   ```

3. Open the app and visit the Auth page if you need admin-only pages.

## Bundled admin credentials

- Email: `admin@admin.com`
- Password: `demo123456`

## Pages

- `/health`: `HealthService`
- `/auth`: user auth methods, user login, legacy admin bootstrap, `AuthStore`
- `/components`: kitchen-sink showcase for all local demo components and icons
- `/records`: `RecordService` over the public `entry` collection
- `/users`: `UserService` admin CRUD wrappers
- `/collections`: `CollectionService`
- `/settings`: `SettingsService`
- `/backups`: `BackupService`
- `/realtime`: `RealTimeService`

## Notes

- The demo targets `http://127.0.0.1:8090/`.
- The local PocketBase bundle is `v0.22.x`, so admin bootstrap uses the legacy `/api/admins/auth-with-password` endpoint.
- Backup restore is intentionally not exposed in the UI because it is destructive.
