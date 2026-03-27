# MicroserviceHub — Frontend ↔ Backend Integration Guide

## What Was Integrated

### Backend changes
| File | Change |
|------|--------|
| `Program.cs` | Added **CORS policy** (`AllowFrontend`) that allows requests from the Vite dev server (`localhost:5173`) and preview server (`localhost:4173`). `UseCors` middleware is placed correctly before `UseAuthentication`. |

### Frontend changes
| File | Change |
|------|--------|
| `src/services/api.ts` | **New** — central typed API client. Attaches JWT from `localStorage` on every request. Covers all backend endpoints: `login`, `createUser`, `getApplications`, `createApplication`, `getApplicationDetails`, `updateApplicationSettings`, `regenerateSecret`, `revokeKey`, `getMicroservices`. |
| `src/context/AuthContext.tsx` | Replaced mock state with real JWT storage. Persists `token`, `role`, `email` in `localStorage`. Restores session on page reload. Exposes `isLoading` flag used by route guards. |
| `src/context/AppContext.tsx` | Simplified — app list is now fetched from the API, not stored in React context. |
| `src/routes/AppRoutes.tsx` | Added `ProtectedRoute` wrapper: redirects unauthenticated users to `/`, role-guards the SuperAdmin route. Catch-all `*` redirects to `/`. |
| `src/pages/auth/Login.tsx` | Calls `POST /v1.0.1/auth/login`. Navigates to `/superadmin-dashboard` for SuperAdmin, `/dashboard` for Admin/User. Shows API error messages inline. |
| `src/pages/dashboard/Dashboard.tsx` | Shows logged-in user email. Logout button clears JWT and redirects to `/`. |
| `src/pages/dashboard/SuperAdminDashboard.tsx` | Calls `POST /v1.0.1/auth/create-user` (requires SuperAdmin JWT). Shows success/error feedback. RoleId dropdown maps: Admin=2, User=3. |
| `src/pages/applications/CreateApplication.tsx` | Calls `POST /v1.0.1/Application`. After creation, displays the returned `appKey` and `appSecret` with copy buttons (secret shown once). |
| `src/pages/applications/ManageApplications.tsx` | Calls `GET /v1.0.1/Application` on mount. Refreshes after a new app is created. |
| `src/pages/applications/ApplicationDetails.tsx` | Calls `GET /v1.0.1/Application/{id}/details`. Toggle env calls `PUT /v1.0.1/Application/{id}/settings`. Regenerate calls `POST /v1.0.1/Application/{id}/keys/{keyId}/regenerate`. Revoke calls `PATCH /v1.0.1/Application/{id}/keys/{keyId}/revoke`. Update button saves microservice toggles via settings endpoint. |
| `vite.config.ts` | Added **Vite dev proxy**: requests to `/v1.0.1/*` are forwarded to `http://localhost:5266`, eliminating CORS issues during development. |

---

## Running the Project

### 1. Start the Backend

```bash
cd backend/MicroserviceHub.API
dotnet run --launch-profile http
# Runs on http://localhost:5266
# Swagger UI: http://localhost:5266/swagger
```

Make sure your SQL Server connection string in `appsettings.json` is correct:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=microservice_hub_db;TrustServerCertificate=True;Trusted_Connection=True;"
}
```

### 2. Start the Frontend

```bash
cd frontend
npm install
npm run dev
# Runs on http://localhost:5173
```

Open `http://localhost:5173` in your browser.

---

## Authentication Flow

```
Login Page
  → POST /v1.0.1/auth/login  { email, password }
  ← { token: "eyJ...", role: "Admin" | "SuperAdmin" | "User" }

Token stored in localStorage → attached as Bearer on all subsequent requests
Role-based routing:
  SuperAdmin  →  /superadmin-dashboard  (create users)
  Admin/User  →  /dashboard             (manage applications)
```

---

## API Endpoint Map

| Frontend Action | HTTP Method | Endpoint |
|----------------|------------|----------|
| Login | POST | `/v1.0.1/auth/login` |
| Create user (SuperAdmin) | POST | `/v1.0.1/auth/create-user` |
| List applications | GET | `/v1.0.1/Application` |
| Create application | POST | `/v1.0.1/Application` |
| Get app details | GET | `/v1.0.1/Application/{id}/details` |
| Update settings | PUT | `/v1.0.1/Application/{id}/settings` |
| Regenerate credentials | POST | `/v1.0.1/Application/{id}/keys/{keyId}/regenerate` |
| Revoke access | PATCH | `/v1.0.1/Application/{id}/keys/{keyId}/revoke` |
| List microservices | GET | `/v1.0.1/Application/microservices` |

---

## Production Deployment

For production, set the environment variable:

```env
VITE_API_BASE_URL=https://your-api-domain.com/v1.0.1
```

Or configure a reverse proxy (nginx/IIS) to serve both frontend and backend from the same domain — no CORS config needed in that case.
