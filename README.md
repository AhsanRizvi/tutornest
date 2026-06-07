# TutorNest - Private Tutor LMS

TutorNest is a modern, mobile-first Learning Management System (LMS) designed for private tutors. It features robust role-based access control (Admin, Teacher, Student), strict multi-tenant data isolation per teacher, video streaming analytics, assignment grading, notices broadcasting, virtual class scheduling, and master course bundling with completion certificate issuing.

---

## 🛠️ System Requirements & Prerequisites

Make sure the following are installed on your system before proceeding:

1. **.NET 9 SDK**: For building and running the Backend Web API.
   - [Download .NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. **Node.js (v18+) & npm**: For building and running the Angular frontend.
   - [Download Node.js](https://nodejs.org/)
3. **PostgreSQL Database**: Relational database storage.
   - [Download PostgreSQL](https://www.postgresql.org/download/)

---

## 🔑 Default Seeded Credentials

Upon the initial application startup, the backend automatically provisions, migrates, and seeds the PostgreSQL database with default roles and a pre-configured administrator account.

| Role | Default Email | Default Password |
| :--- | :--- | :--- |
| **Administrator** | `admin@tutornest.com` | `Admin@Password123` |

> [!NOTE]
> - **Teachers** are registered exclusively by the platform **Admin**.
> - **Students** are enrolled and registered by their respective **Teacher** (either individually or in bulk via CSV).

---

## 🚀 How to Run the Application

### 1. Database Setup

Ensure PostgreSQL is running on your machine.
- The default connection string configured in `TutorNest.API/appsettings.json` points to:
  `Host=localhost;Database=tutornest;Username=ahsanrizvi;Password=;Port=5432`
- Update the username and password in `TutorNest.API/appsettings.json` to match your local PostgreSQL server configuration if needed.

---

### 2. Backend (ASP.NET Core Web API)

The backend code is located in the `TutorNest.API/` directory.

Run the following commands in your terminal:

```bash
# Navigate to the backend directory
cd TutorNest.API

# Restore dependencies
dotnet restore

# Run the backend API server
dotnet run
```

Once started, the backend API server will listen on the following local ports:
- **HTTP Endpoint**: `http://localhost:5299`
- **HTTPS Endpoint**: `https://localhost:7259`
- **Interactive Swagger UI Documentation**: `http://localhost:5299/swagger/index.html` or `https://localhost:7259/swagger/index.html`

---

### 3. Frontend (Angular 18 SPA)

The frontend code is located in the `TutorNest.Web/` directory.

Run the following commands in a new terminal window:

```bash
# Navigate to the frontend web directory
cd TutorNest.Web

# Install all npm dependencies
npm install

# Start the local development server
npm start
```

Once started, the Angular development server will listen on the default port:
- **Web App URL**: `http://localhost:4200`
- The dev server is configured to hot-reload dynamically whenever changes to components, styles, or services are detected.

---

## ⚙️ Cloud Storage Configuration (Optional)

TutorNest supports local file storage, but is fully ready to store lecture videos and homework files on **Cloudflare R2** (S3-compatible). To configure R2 for local testing:

1. Open `TutorNest.API/appsettings.json`
2. Fill in the keys under the `"R2"` section:
   ```json
   "R2": {
     "AccountId": "YOUR_CLOUDFLARE_ACCOUNT_ID",
     "AccessKeyId": "YOUR_R2_ACCESS_KEY_ID",
     "SecretAccessKey": "YOUR_R2_SECRET_ACCESS_KEY",
     "BucketName": "tutornest-uploads",
     "PublicUrl": "https://pub-xxxx.r2.dev"
   }
   ```
   *(Note: Bypassing payload signatures is pre-configured on chunked file streams for smooth R2 integration.)*
