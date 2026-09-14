# Finance Ledger Pro — Mobile (Android)

React Native (Expo, TypeScript) client for Finance Ledger Pro. Shares the same
REST API as the web/back-office. Android-focused; iOS is buildable via Expo but
untested.

## Stack

- **Expo SDK 51** + React Native 0.74 (TypeScript, strict)
- **@react-navigation** native-stack (auth) + bottom-tabs (main)
- **axios** REST client with a transparent JWT-refresh interceptor
- **@tanstack/react-query** for server state, with offline cache fallback
- **@react-native-async-storage/async-storage** — offline read cache + outbox
- **expo-secure-store** — JWT + user profile in the Android Keystore
- **@react-native-community/netinfo** — connectivity-driven sync
- **expo-background-fetch** + **expo-task-manager** — periodic background sync
- **react-native-paper** — Material 3 UI
- **react-native-chart-kit** — dashboard chart

## Getting started

```bash
npm install          # do NOT run in this scaffold task; run locally
cp .env.example .env # set EXPO_PUBLIC_API_BASE_URL
npm run android      # or: npm start, then press "a"
```

`API_BASE_URL` resolves (in order): `EXPO_PUBLIC_API_BASE_URL` env →
`app.json > expo.extra.apiBaseUrl` → `http://10.0.2.2:5000` (Android emulator
loopback to host). All endpoints are namespaced under `/api/v1`.

## Architecture

```
src/
  api/         axios client (refresh interceptor) + typed endpoint modules
  auth/        SecureStore token storage + AuthContext (login/logout/bootstrap)
  offline/     cache (AsyncStorage), outbox queue, sync service, background task
  hooks/       react-query hooks with write-through cache + offline fallback
  navigation/  RootNavigator -> AuthStack | MainTabs
  screens/     Login, Dashboard, IncomeEntry, ExpenseEntry, Reports
  components/   StatCard, FormInput, SyncStatusBar, TransactionForm
  theme/       Paper theme + brand palette
  config/      env resolution
  types/       shared domain types (matches the API contract)
```

### Auth & token refresh

- Tokens live in `expo-secure-store` (Android Keystore), never AsyncStorage.
- The axios response interceptor catches `401`, calls `POST /auth/refresh` once
  (single in-flight promise shared across concurrent 401s), retries the original
  request, and on refresh failure clears storage and fires a session-expired
  callback that `AuthContext` turns into a logout.

### Offline model

- **Reads**: every read hook writes-through to AsyncStorage on success and
  returns the cached copy on network failure.
- **Writes**: `useCreateTransaction` checks connectivity; if offline (or the
  POST fails) it durably enqueues the payload in the **outbox** and returns an
  optimistic record. `SyncStatusBar` shows the pending count live.
- **Sync**: `syncService` flushes the outbox (FIFO, stops on first failure to
  preserve ordering) then refreshes caches. Triggered by (1) NetInfo
  offline→online transitions in the foreground and (2) `expo-background-fetch`
  (~15 min floor) in the background.

## API contract

Base: `${API_BASE_URL}/api/v1`, JWT Bearer.

| Method | Path                | Used by                    |
| ------ | ------------------- | -------------------------- |
| POST   | `/auth/login`       | Login                      |
| POST   | `/auth/refresh`     | axios interceptor          |
| GET    | `/dashboard`        | Dashboard                  |
| GET    | `/transactions`     | (list; report fallback)    |
| POST   | `/transactions`     | Income/Expense entry, sync |
| GET    | `/reports/monthly`  | Reports                    |
| GET    | `/branches`         | Entry forms                |

Transaction: `{ type: Income|Expense, category, description, amount, date,
branch, approvalStatus: Draft|Submitted|Approved|Rejected }`.

## See also

`ANDROID.md` for build/emulator notes.
