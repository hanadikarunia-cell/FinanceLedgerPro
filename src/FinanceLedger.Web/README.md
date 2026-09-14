# Finance Ledger Pro — Web

Production-quality frontend for **Finance Ledger Pro**, built with React 18, TypeScript, Vite,
Material UI v5, React Router v6, TanStack React Query v5, Axios, Recharts, and
react-hook-form + zod.

## Features

- **JWT auth** with access + refresh tokens, automatic 401 refresh via an Axios interceptor
  (queued concurrent requests during refresh).
- **Role-based access control** (`Manager` | `User`):
  - Managers see Users, Branches and Audit Logs nav plus approve/reject/edit/delete actions.
  - Users cannot edit approved transactions, cannot delete, and cannot access management pages.
- **Dashboard** — stat cards, income vs expense composed bar+line chart, expense bar chart,
  top-categories pie chart, and a recent-transactions grid.
- **Transactions** — server-paginated data grid with filters (type, status, branch, category,
  date range), create/edit dialog validated with zod, file attachment upload, and
  role-gated approve/reject/delete actions.
- **Reports** — daily/monthly/yearly tabs, filters (date range, category, branch, user),
  summary cards, chart, and Excel/PDF/CSV blob export.
- **Users / Branches / Audit Logs** — Manager-only management pages.
- **Settings** — theme toggle, profile edit, password reset form.
- **Light / dark mode** with persisted preference and system default detection.
- Fully responsive: collapsible drawer on mobile, permanent drawer on desktop.

## Getting started

```bash
npm install
cp .env.example .env   # then set VITE_API_BASE_URL
npm run dev
```

## Scripts

| Script            | Description                        |
| ----------------- | ---------------------------------- |
| `npm run dev`     | Start Vite dev server (port 5173). |
| `npm run build`   | Type-check and build for prod.     |
| `npm run preview` | Preview the production build.       |
| `npm run lint`    | Run ESLint.                         |

## Configuration

| Variable            | Description                                             |
| ------------------- | ------------------------------------------------------- |
| `VITE_API_BASE_URL` | API host, e.g. `https://localhost:5001`. The client appends `/api/v1`. |

## Project structure

```
src/
  api/          Axios client + typed API modules (auth, transactions, ...)
  components/   Shared UI (Layout, DataTable, StatCard, guards, ...)
  context/      AuthContext + ThemeContext
  hooks/        React Query hooks
  pages/        Route pages
  routes/       Route definitions with role guards
  theme/        MUI theme factory
  types/        Shared TypeScript interfaces (matches the API contract)
  utils/        Formatting helpers
```

## API contract

Base URL: `${VITE_API_BASE_URL}/api/v1`. See `src/types/index.ts` for the full set of
DTOs the client expects.
