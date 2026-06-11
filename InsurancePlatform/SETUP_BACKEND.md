# 🚀 Backend Setup Guide (.NET 8.0)

Welcome to the **Backend** setup guide! This document will walk you through restoring packages, setting up the local database, and running the ASP.NET Core Web API.

## 🛠️ Prerequisites
Ensure you have the following installed on your system before proceeding:
- **.NET 8.0 SDK**
- **SQL Server LocalDB** (Installed alongside Visual Studio or .NET developer tools)

---

## 📦 Package Installation

Navigate into the backend project directory (`c:\Sanjay\InsurancePlatform`) and run the following command to restore all NuGet packages across all projects (API, Application, Domain, Infrastructure):

```powershell
dotnet restore
```

### Key NuGet Dependencies Breakdown
If you were to set this up from scratch, here are the exact commands to add the necessary packages to your specific projects:

#### 1. Entity Framework Core & SQL Server (Infrastructure & Domain)
Used for Database management, ORM, and schema migrations.
```powershell
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
```

#### 2. Authentication & Identity (API & Infrastructure)
Powers user signups, logins, and JWT Token validation.
```powershell
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.Extensions.Identity.Core --version 8.0.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 8.0.0
```

#### 3. Real-Time Communication (Infrastructure)
SignalR is used to push real-time notifications and chat messages to the Angular frontend.
```powershell
dotnet add package Microsoft.AspNetCore.SignalR.Core --version 1.2.9
```

#### 4. External Integrations (Application & API)
- **Deepgram**: For real-time AI Voice transcription.
- **ImageKit**: For uploading and serving KYC documents and profile images.
```powershell
dotnet add package Deepgram --version 4.0.0
dotnet add package ImageKit --version 3.1.2
```

#### 5. Swagger Documentation (API)
Generates the API documentation UI.
```powershell
dotnet add package Swashbuckle.AspNetCore --version 6.6.2
```

---

## 🗄️ Database Setup

The application is configured to use `(localdb)\mssqllocaldb`. 
You do **NOT** need to run any manual SQL scripts. Entity Framework handles table creation automatically on startup via `context.Database.MigrateAsync()`.

> [!IMPORTANT]
> Just run the application! If the database does not exist, the code will automatically create it and seed it with the necessary default roles and data.

---

## 🚀 Running the Application

### 1. Build the Solution
Ensure there are no compilation errors:
```powershell
dotnet build
```

### 2. Run the API
Navigate to the `API` project folder or specify the project file, and launch using the HTTPS profile (this ensures it runs on `https://localhost:7140` which the frontend expects):

```powershell
dotnet run --project API\API.csproj --launch-profile https
```

### 3. Access Swagger API Docs
Once running, you can explore and test the backend endpoints visually by navigating to:
[https://localhost:7140/swagger](https://localhost:7140/swagger)
