# TutorNest - Private Tutor LMS

A mobile-first, private Learning Management System (LMS) for individual tutors. Features role-based access control, strict data isolation per teacher, and student video progress tracking.

## System Requirements
- .NET SDK (supports .NET 9+)
- Node.js (v18+)
- PostgreSQL Database

## Default Seeded Credentials

Upon application startup, the database is automatically created, migrated, and seeded with standard roles and a default administrator account.

| Role | Email | Password |
| :--- | :--- | :--- |
| **Admin** | `admin@tutornest.com` | `Admin@Password123` |

*Note: Teachers can only be created by the Admin. Students can only be created/registered by their respective Teacher.*

---

## How to Run

### 1. Backend (ASP.NET Core Web API)
The backend project is located in `TutorNest.API/`.

Ensure PostgreSQL is running and update the connection string in `TutorNest.API/appsettings.json` if necessary (defaults to macOS standard `Host=localhost;Database=tutornest;Username=ahsanrizvi;Password=`).

Run the following commands:
```bash
cd TutorNest.API
dotnet run
```
The API will start and be available at:
- Web API: `https://localhost:7198` (or similar configured port)
- Swagger UI: `http://localhost:5242/swagger` / `https://localhost:7198/swagger`

### 2. Frontend (Angular 18)
The frontend project is located in `TutorNest.Web/`.

Run the following commands to install packages and start the dev server:
```bash
cd TutorNest.Web
npm install
npm run start
```
The client app will be available at:
- Dev Server: `http://localhost:4200`
