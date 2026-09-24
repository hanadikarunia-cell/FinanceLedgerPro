# Finance Ledger Pro — API Reference

> **Base URL:** `https://<host>/api/v1`
> **Auth:** Supabase Auth (email/password) → **Bearer JWT**
> **Content type:** `application/json` (unless noted)
> **Live docs:** interactive Swagger / OpenAPI UI is served at **`/swagger`**
> (OpenAPI JSON at `/swagger/v1/swagger.json`).

---

## 1. Conventions

### 1.1 Authentication

All endpoints except `POST /auth/login`, `POST /auth/refresh`, and
`POST /auth/reset-password` require an **`Authorization: Bearer <access_token>`** header.
Access tokens are short-lived (~15 min); use `POST /auth/refresh` to obtain a new one.

The **role** claim (`Manager` / `User`) and `branchId` claim drive authorization. Endpoints
below list the required role. `User`-scoped endpoints implicitly restrict data to the
caller's own branch / own resources.

### 1.2 Enumerations

**`TransactionType`**

| Value | Meaning |
|-------|---------|
| `Income` | Money in |
| `Expense` | Money out |

**`ApprovalStatus`**

| Value | Meaning |
|-------|---------|
| `Draft` | Created, not yet submitted |
| `Submitted` | Awaiting manager decision |
| `Approved` | Approved by a Manager |
| `Rejected` | Rejected by a Manager (with reason) |

**`AuditAction`**

| Value | Meaning |
|-------|---------|
| `Created` | Entity created |
| `Updated` | Entity updated |
| `Submitted` | Transaction submitted for approval |
| `Approved` | Transaction approved |
| `Rejected` | Transaction rejected |
| `Deleted` | Entity deleted |
| `LoggedIn` | User authenticated |
| `Exported` | Data exported |

**`UserRole`**

| Value | Meaning |
|-------|---------|
| `AppAdmin` | Application Admin: manages sites (`/sites`), What's New and all Feedback; belongs to no site, so site-level endpoints return no data |
| `Manager` | Site Admin: full access within their own site; can approve/reject; cross-branch |
| `User` | Limited; own branch / own transactions |

### 1.2a Sites and "View as"

Every request is scoped to the caller's **site** (tenant), taken from their account — there is no
site parameter and one site can never read another's data.

An admin can act as another user by adding request headers:

| Header | Meaning |
|--------|---------|
| `X-Act-As-User: <userId>` | Run this request as that user (their site, role and branches apply). Application Admin: any user except another Application Admin. Site Admin: regular users of their own site. Otherwise `403`. |
| `X-Act-As-Write: true` | Allow changes. Without it, everything except `GET`/`HEAD`/`OPTIONS`, exports and `/auth/*` is rejected with `403` (read-only). |

### 1.3 Paged Result Shape

List endpoints return a standard envelope:

```json
{
  "items": [ /* array of resources */ ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 137,
  "totalPages": 6,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "continuationToken": "W3sidG9rZW4iOiIrUklEOn..."
}
```

Common paging/sorting query params: `page` (default 1), `pageSize` (default 25, max 100),
`sortBy`, `sortDir` (`asc`|`desc`).

### 1.4 Error Response — RFC 7807 ProblemDetails

All errors use `application/problem+json`:

```json
{
  "type": "https://financeledgerpro.com/errors/validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The 'amount' field must be greater than 0.",
  "instance": "/api/v1/transactions",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": {
    "amount": [ "The 'amount' field must be greater than 0." ],
    "currency": [ "The 'currency' field is required." ]
  }
}
```

| Status | Meaning |
|--------|---------|
| 400 | Validation error |
| 401 | Missing/invalid/expired token |
| 403 | Authenticated but not authorized (role/ownership) |
| 404 | Resource not found |
| 409 | Concurrency conflict (ETag) / invalid state transition |
| 422 | Business rule violation |
| 429 | Rate limit exceeded |
| 500 | Unexpected server error |

### 1.5 Rate-Limit Headers

Every response includes rate-limit metadata; `429` adds `Retry-After`:

```
RateLimit-Limit: 100
RateLimit-Remaining: 87
RateLimit-Reset: 42
Retry-After: 42          (only on 429 responses)
```

### 1.6 Concurrency

Mutating endpoints support optimistic concurrency via the `ETag` returned on reads. Send it
back as `If-Match: "<etag>"`; a mismatch yields `409 Conflict`.

---

## 2. Auth

### 2.1 Login

`POST /api/v1/auth/login` · **Auth:** none

Request:

```json
{ "email": "aisha.rahman@example.com", "password": "•••••••••" }
```

Response `200 OK`:

```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "refreshToken": "def50200a1b2c3d4...",
  "user": {
    "id": "u_10293",
    "email": "aisha.rahman@example.com",
    "displayName": "Aisha Rahman",
    "role": "User",
    "branchId": "BR-KUL-01"
  }
}
```

### 2.2 Refresh

`POST /api/v1/auth/refresh` · **Auth:** none (refresh token)

Request:

```json
{ "refreshToken": "def50200a1b2c3d4..." }
```

Response `200 OK` (rotated refresh token returned):

```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiI...",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "refreshToken": "def50200f9e8d7c6..."
}
```

### 2.3 Logout

`POST /api/v1/auth/logout` · **Auth:** Bearer (any role)

Request:

```json
{ "refreshToken": "def50200f9e8d7c6..." }
```

Response `204 No Content` — refresh token revoked server-side.

### 2.4 Reset Password

`POST /api/v1/auth/reset-password` · **Auth:** none

Request:

```json
{ "email": "aisha.rahman@example.com" }
```

Response `202 Accepted` (always, to avoid account enumeration):

```json
{ "message": "If an account exists for this email, a reset link has been sent." }
```

---

### 2.5 Me

`GET /api/v1/auth/me` · **Auth:** any signed-in user

The effective identity for this request — the acted-as user while "View as" headers are sent.

```json
{
  "user": { "id": "…", "email": "…", "displayName": "…", "role": "User", "tenantId": "…", "tenantName": "Client 2", "assignedBranches": ["…"], "isActive": true },
  "actingAs": { "realUserId": "…", "realUserName": "Application Admin", "canWrite": false }
}
```

`actingAs` is omitted when not acting.

---

## 3. Transactions

### 3.1 List Transactions

`GET /api/v1/transactions` · **Auth:** Manager (all branches) / User (own branch)

Query params: `page`, `pageSize`, `sortBy`, `sortDir`, `branch`, `status`
(`ApprovalStatus`), `type` (`TransactionType`), `category`, `fromDate`, `toDate`,
`minAmount`, `maxAmount`, `createdBy`, `search`.

Response `200 OK`:

```json
{
  "items": [
    {
      "id": "3f2b6c9a-8e1d-4a77-9c2e-1f4d5b6a7c88",
      "branch": "BR-KUL-01",
      "type": "Expense",
      "amount": 1520.75,
      "currency": "MYR",
      "description": "Office lease — June 2026",
      "category": "Rent",
      "status": "Approved",
      "transactionDate": "2026-06-15T00:00:00Z",
      "createdBy": "u_10293",
      "createdByName": "Aisha Rahman",
      "approvedBy": "u_55011",
      "approvedAt": "2026-06-15T14:03:00Z",
      "attachmentCount": 2,
      "createdAt": "2026-06-15T09:10:00Z",
      "etag": "\"0000d1a2-0000-0700-0000-6675a1b80000\""
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 137,
  "totalPages": 6,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "continuationToken": null
}
```

### 3.2 Get Transaction

`GET /api/v1/transactions/{id}` · **Auth:** Manager (any) / User (own branch)

Response `200 OK`:

```json
{
  "id": "3f2b6c9a-8e1d-4a77-9c2e-1f4d5b6a7c88",
  "branch": "BR-KUL-01",
  "type": "Expense",
  "amount": 1520.75,
  "currency": "MYR",
  "description": "Office lease — June 2026",
  "category": "Rent",
  "status": "Approved",
  "transactionDate": "2026-06-15T00:00:00Z",
  "createdBy": "u_10293",
  "createdByName": "Aisha Rahman",
  "submittedAt": "2026-06-15T09:12:00Z",
  "approvedBy": "u_55011",
  "approvedByName": "Daniel Lim",
  "approvedAt": "2026-06-15T14:03:00Z",
  "rejectionReason": null,
  "attachments": [
    { "id": "att_7781", "fileName": "lease-invoice-june.pdf", "sizeBytes": 284517 }
  ],
  "tags": ["fixed-cost", "monthly"],
  "createdAt": "2026-06-15T09:10:00Z",
  "updatedAt": "2026-06-15T14:03:00Z",
  "etag": "\"0000d1a2-0000-0700-0000-6675a1b80000\""
}
```

### 3.3 Create Transaction

`POST /api/v1/transactions` · **Auth:** Manager / User

Request:

```json
{
  "branch": "BR-KUL-01",
  "type": "Expense",
  "amount": 1520.75,
  "currency": "MYR",
  "description": "Office lease — June 2026",
  "category": "Rent",
  "transactionDate": "2026-06-15T00:00:00Z",
  "tags": ["fixed-cost", "monthly"]
}
```

Response `201 Created` (Location: `/api/v1/transactions/{id}`):

```json
{
  "id": "9a1c2b3d-4e5f-6789-0abc-def123456789",
  "branch": "BR-KUL-01",
  "status": "Draft",
  "type": "Expense",
  "amount": 1520.75,
  "currency": "MYR",
  "createdBy": "u_10293",
  "createdAt": "2026-06-30T02:15:00Z",
  "etag": "\"0000e2b3-0000-0700-0000-667b2c400000\""
}
```

### 3.4 Update Transaction

`PUT /api/v1/transactions/{id}` · **Auth:** Manager (any) / User (own Draft only)

Only `Draft` transactions are editable by their creator. Send `If-Match` with the ETag.

Request:

```json
{
  "amount": 1600.00,
  "description": "Office lease — June 2026 (revised)",
  "category": "Rent",
  "transactionDate": "2026-06-15T00:00:00Z",
  "tags": ["fixed-cost"]
}
```

Response `200 OK`: updated transaction object (as §3.2). `409` on ETag mismatch or if not Draft.

### 3.5 Delete Transaction

`DELETE /api/v1/transactions/{id}` · **Auth:** Manager (any Draft) / User (own Draft)

Response `204 No Content`. Non-Draft deletion → `422 Unprocessable Entity`.

### 3.6 Submit Transaction

`POST /api/v1/transactions/{id}/submit` · **Auth:** Manager / User (own)

Transitions `Draft → Submitted` and notifies branch managers.

Response `200 OK`:

```json
{ "id": "9a1c2b3d-4e5f-6789-0abc-def123456789", "status": "Submitted", "submittedAt": "2026-06-30T02:20:00Z" }
```

### 3.7 Approve Transaction

`POST /api/v1/transactions/{id}/approve` · **Auth:** **Manager only**

Transitions `Submitted → Approved`; writes an audit log; emails the submitter.

Request (optional):

```json
{ "note": "Approved — within budget." }
```

Response `200 OK`:

```json
{
  "id": "9a1c2b3d-4e5f-6789-0abc-def123456789",
  "status": "Approved",
  "approvedBy": "u_55011",
  "approvedByName": "Daniel Lim",
  "approvedAt": "2026-06-30T03:00:00Z"
}
```

Errors: `403` (not Manager), `422` (not in `Submitted` state).

### 3.8 Reject Transaction

`POST /api/v1/transactions/{id}/reject` · **Auth:** **Manager only**

Transitions `Submitted → Rejected`; `reason` is required.

Request:

```json
{ "reason": "Missing receipt attachment." }
```

Response `200 OK`:

```json
{
  "id": "9a1c2b3d-4e5f-6789-0abc-def123456789",
  "status": "Rejected",
  "rejectionReason": "Missing receipt attachment.",
  "approvedBy": "u_55011",
  "approvedAt": "2026-06-30T03:05:00Z"
}
```

---

## 4. Dashboard

### 4.1 Get Dashboard

`GET /api/v1/dashboard` · **Auth:** Manager (all) / User (own branch)

Query params: `branch` (Manager only), `fromDate`, `toDate`.

Response `200 OK`:

```json
{
  "scope": { "branch": "BR-KUL-01", "fromDate": "2026-06-01", "toDate": "2026-06-30" },
  "totals": {
    "income": 84200.00,
    "expense": 51230.75,
    "net": 32969.25,
    "currency": "MYR"
  },
  "counts": {
    "draft": 4,
    "submitted": 7,
    "approved": 121,
    "rejected": 5
  },
  "pendingApprovals": 7,
  "recentTransactions": [
    { "id": "3f2b6c9a...", "type": "Expense", "amount": 1520.75, "status": "Approved", "transactionDate": "2026-06-15T00:00:00Z" }
  ],
  "monthlyTrend": [
    { "month": "2026-05", "income": 79000.00, "expense": 48000.00 },
    { "month": "2026-06", "income": 84200.00, "expense": 51230.75 }
  ]
}
```

---

## 5. Reports

All report endpoints: **Auth:** Manager (all branches) / User (own branch). Common query
params: `branch`, `type`, `category`.

### 5.1 Daily Report

`GET /api/v1/reports/daily` · Query: `date` (required, `YYYY-MM-DD`), plus common params.

```json
{
  "date": "2026-06-15",
  "branch": "BR-KUL-01",
  "currency": "MYR",
  "totals": { "income": 3200.00, "expense": 1520.75, "net": 1679.25 },
  "byCategory": [
    { "category": "Rent", "type": "Expense", "amount": 1520.75, "count": 1 },
    { "category": "Sales", "type": "Income", "amount": 3200.00, "count": 4 }
  ],
  "transactionCount": 5
}
```

### 5.2 Monthly Report

`GET /api/v1/reports/monthly` · Query: `year` (required), `month` (required, 1–12), plus common params.

```json
{
  "year": 2026,
  "month": 6,
  "branch": "BR-KUL-01",
  "currency": "MYR",
  "totals": { "income": 84200.00, "expense": 51230.75, "net": 32969.25 },
  "byDay": [
    { "date": "2026-06-15", "income": 3200.00, "expense": 1520.75 }
  ],
  "byCategory": [
    { "category": "Rent", "type": "Expense", "amount": 1520.75 }
  ],
  "transactionCount": 133
}
```

### 5.3 Yearly Report

`GET /api/v1/reports/yearly` · Query: `year` (required), plus common params.

```json
{
  "year": 2026,
  "branch": "BR-KUL-01",
  "currency": "MYR",
  "totals": { "income": 512000.00, "expense": 318400.00, "net": 193600.00 },
  "byMonth": [
    { "month": 1, "income": 41000.00, "expense": 26000.00 },
    { "month": 6, "income": 84200.00, "expense": 51230.75 }
  ],
  "transactionCount": 1580
}
```

---

## 6. Exports

Export endpoints stream a file (binary) with a `Content-Disposition: attachment` header, and
record an `Exported` audit entry. **Auth:** Manager (all) / User (own branch). They accept the
same filters as `GET /transactions` plus `fromDate`/`toDate`/`branch`.

| Endpoint | Method | Produces | Library |
|----------|--------|----------|---------|
| `/api/v1/export/excel` | `GET` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | ClosedXML |
| `/api/v1/export/pdf` | `GET` | `application/pdf` | QuestPDF |
| `/api/v1/export/csv` | `GET` | `text/csv` | built-in |

Example: `GET /api/v1/export/excel?branch=BR-KUL-01&fromDate=2026-06-01&toDate=2026-06-30`

Response `200 OK`:

```
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="transactions-BR-KUL-01-2026-06.xlsx"
<binary file stream>
```

For very large exports the API may return `202 Accepted` with a job handle:

```json
{ "jobId": "exp_88213", "status": "Processing", "statusUrl": "/api/v1/export/jobs/exp_88213" }
```

---

## 7. Branches

### 7.1 List Branches

`GET /api/v1/branches` · **Auth:** Manager / User (read). Query: `page`, `pageSize`, `region`, `isActive`.

```json
{
  "items": [
    { "id": "BR-KUL-01", "name": "Kuala Lumpur HQ", "code": "KUL01", "region": "Central", "currency": "MYR", "isActive": true }
  ],
  "page": 1, "pageSize": 25, "totalCount": 12, "totalPages": 1,
  "hasNextPage": false, "hasPreviousPage": false
}
```

### 7.2 Get Branch

`GET /api/v1/branches/{id}` · **Auth:** Manager / User

```json
{
  "id": "BR-KUL-01",
  "name": "Kuala Lumpur HQ",
  "code": "KUL01",
  "region": "Central",
  "country": "MY",
  "currency": "MYR",
  "managerUserId": "u_55011",
  "isActive": true,
  "address": { "line1": "Level 20, Menara ABC", "city": "Kuala Lumpur", "postalCode": "50450" },
  "createdAt": "2025-01-10T00:00:00Z"
}
```

### 7.3 Create Branch

`POST /api/v1/branches` · **Auth:** **Manager only**

```json
{ "name": "Penang Branch", "code": "PEN01", "region": "North", "country": "MY", "currency": "MYR" }
```

Response `201 Created`: the branch object.

### 7.4 Update Branch

`PUT /api/v1/branches/{id}` · **Auth:** **Manager only** → `200 OK` (updated branch).

### 7.5 Deactivate Branch

`DELETE /api/v1/branches/{id}` · **Auth:** **Manager only** — soft-delete (`isActive=false`).
Response `204 No Content`.

---

## 8. Users

### 8.1 List Users

`GET /api/v1/users` · **Auth:** **Manager only**. Query: `page`, `pageSize`, `role`, `branchId`, `isActive`, `search`.

```json
{
  "items": [
    { "id": "u_10293", "email": "aisha.rahman@example.com", "displayName": "Aisha Rahman", "role": "User", "branchId": "BR-KUL-01", "isActive": true }
  ],
  "page": 1, "pageSize": 25, "totalCount": 48, "totalPages": 2,
  "hasNextPage": true, "hasPreviousPage": false
}
```

### 8.2 Get User

`GET /api/v1/users/{id}` · **Auth:** Manager (any) / User (self only)

```json
{
  "id": "u_10293",
  "email": "aisha.rahman@example.com",
  "displayName": "Aisha Rahman",
  "role": "User",
  "branchId": "BR-KUL-01",
  "isActive": true,
  "lastLoginAt": "2026-06-29T08:41:00Z",
  "createdAt": "2025-11-02T03:00:00Z"
}
```

### 8.3 Create User

`POST /api/v1/users` · **Auth:** **Manager only**

```json
{
  "email": "new.user@example.com",
  "displayName": "New User",
  "role": "User",
  "branchId": "BR-KUL-01"
}
```

Response `201 Created`: the user object (provisioned in Supabase Auth via the Admin API).

### 8.4 Update User

`PUT /api/v1/users/{id}` · **Auth:** **Manager only** (role/branch changes) → `200 OK`.

```json
{ "displayName": "New User", "role": "Manager", "branchId": "BR-PEN-01", "isActive": true }
```

### 8.5 Deactivate User

`DELETE /api/v1/users/{id}` · **Auth:** **Manager only** — soft-delete. `204 No Content`.

---

## 9. Files (Attachments)

### 9.1 Upload Attachment

`POST /api/v1/files/upload` · **Auth:** Manager / User · `Content-Type: multipart/form-data`

Form fields:

| Field | Type | Description |
|-------|------|-------------|
| `file` | binary | The document (PDF/image). Max size enforced (e.g., 10 MB). |
| `transactionId` | string | Owning transaction id. |

Response `201 Created`:

```json
{
  "id": "att_7781",
  "transactionId": "3f2b6c9a-8e1d-4a77-9c2e-1f4d5b6a7c88",
  "fileName": "lease-invoice-june.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 284517,
  "blobUri": "3f2b6c9a8e1d4a779c2e1f4d5b6a7c88.pdf",
  "uploadedBy": "u_10293",
  "uploadedAt": "2026-06-30T02:30:00Z"
}
```

`blobUri` is the Supabase Storage object key within the private `attachments` bucket
(not a public URL) — download it through 9.2 below, not directly.

### 9.2 Download Attachment

`GET /api/v1/files/{id}` · **Auth:** Manager (any) / User (own branch)

Streams the file content directly (the API relays it from Supabase Storage using the
service_role key — the bucket is private, so there is no direct/redirected client URL).

### 9.3 List Attachments for a Transaction

`GET /api/v1/files?transactionId={id}` · **Auth:** Manager / User

```json
{
  "items": [
    { "id": "att_7781", "fileName": "lease-invoice-june.pdf", "contentType": "application/pdf", "sizeBytes": 284517, "uploadedAt": "2026-06-15T09:11:00Z" }
  ],
  "page": 1, "pageSize": 25, "totalCount": 2, "totalPages": 1,
  "hasNextPage": false, "hasPreviousPage": false
}
```

### 9.4 Delete Attachment

`DELETE /api/v1/files/{id}` · **Auth:** Manager (any) / User (own upload) → `204 No Content`
(deletes the Supabase Storage object + metadata row).

---

## 10. Audit Logs

### 10.1 List Audit Logs

`GET /api/v1/auditlogs` · **Auth:** Manager (all) / User (own actions only)

Query params: `page`, `pageSize`, `userId` (Manager only), `action` (`AuditAction`),
`entityType`, `entityId`, `fromDate`, `toDate`, `sortDir`.

```json
{
  "items": [
    {
      "id": "al_9f8e7d6c",
      "userId": "u_55011",
      "userName": "Daniel Lim",
      "action": "Approved",
      "entityType": "Transaction",
      "entityId": "3f2b6c9a-8e1d-4a77-9c2e-1f4d5b6a7c88",
      "branch": "BR-KUL-01",
      "details": { "previousStatus": "Submitted", "newStatus": "Approved", "amount": 1520.75 },
      "timestamp": "2026-06-15T14:03:00Z"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 4210,
  "totalPages": 169,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "continuationToken": "W3sidG9rZW4iOiIrUklEOn..."
}
```

### 10.2 Get Audit Log Entry

`GET /api/v1/auditlogs/{id}?userId={userId}` · **Auth:** Manager (any) / User (own)

Returns a single audit entry (shape as above). `userId` is a required filter.

---

## 11. Meta / Health (non-versioned utility)

| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/health` | GET | none | Liveness probe |
| `/health/ready` | GET | none | Readiness (Postgres + Supabase Storage) |
| `/api/v1/meta/version` | GET | none | Build/version + supported API versions |
| `/swagger` | GET | none (guard in prod) | Interactive OpenAPI UI |
| `/swagger/v1/swagger.json` | GET | none | OpenAPI 3 document |

Versioning headers on responses:

```
api-supported-versions: 1.0
api-deprecated-versions:
```

---

## 13. Sites

Application Admin only (`403` for everyone else).

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/api/v1/sites` | List sites with their user counts |
| `POST` | `/api/v1/sites` | Create a site **and its first Site Admin** (Supabase Auth user + local user + a "Main" branch) |
| `PUT` | `/api/v1/sites/{id}` | Rename / activate / deactivate. People in an inactive site cannot sign in |
| `GET` | `/api/v1/sites/{id}/users` | The people in a site (to choose someone to "View as") |

`POST /sites` body:

```json
{
  "name": "Client 2",
  "code": "CLIENT2",
  "adminEmail": "admin@client2.example",
  "adminDisplayName": "Client 2 Admin",
  "adminPassword": "at-least-8-chars"
}
```

`409` if the site code or the admin email already exists.

---

## 12. Admin

### 12.1 Trigger Google Sheets Sync

`POST /api/v1/admin/sync/google-sheets` · **Auth:** Application Admin only · **Disabled by default:** answers `404` unless `Sync__Enabled=true` (the target spreadsheet is a single global setting, so it must not run while several sites exist).

Runs one Google Sheets sync cycle synchronously and returns how many transactions
were pushed. Added for the free-tier deployment, where a scheduled GitHub Actions
workflow calls this on an interval in place of a dedicated always-on worker process
(see `docs/architecture.md` §11) — no request body.

```json
{
  "transactionsSynced": 3
}
```
