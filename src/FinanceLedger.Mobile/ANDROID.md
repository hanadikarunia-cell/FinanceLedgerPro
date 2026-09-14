# Android notes

## Emulator networking

The Android emulator cannot reach `localhost` on the host machine directly.
Use the special alias `10.0.2.2` to reach the host loopback (this is the default
fallback in `src/config/env.ts`). For a physical device on the same LAN, set
`EXPO_PUBLIC_API_BASE_URL` to the host's LAN IP (e.g. `http://192.168.1.10:5000`).

## HTTP (cleartext) during development

If the dev API is plain HTTP, Android blocks cleartext traffic by default in
release builds. For dev builds you may need a network security config allowing
cleartext for your dev host. Production should be HTTPS only.

## Permissions

Declared in `app.json`:

- `INTERNET` — REST calls
- `ACCESS_NETWORK_STATE` — NetInfo connectivity detection

## Background fetch

`expo-background-fetch` maps to Android `JobScheduler`/`WorkManager`. The
`minimumInterval` (15 min) is a floor, not a guarantee; the OS batches jobs for
battery. Background fetch is limited/unavailable in **Expo Go** — build a
development or production client (`eas build -p android`) to exercise it. The
foreground NetInfo sync covers the common "reopen the app after being offline"
case regardless.

## Building

```bash
# Development client (recommended for SecureStore + background fetch):
eas build --profile development --platform android

# Production APK/AAB:
eas build --profile production --platform android
```

`expo-secure-store` requires a dev/prod build (works in Expo Go on Android but
with reduced guarantees). Adjust `eas.json` profiles as needed.
