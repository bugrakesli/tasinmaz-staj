# Getting Started (Installation & Setup)

## 1. Prerequisites
Ensure you have the following installed on your machine before setting up the project:
- **.NET 8.0 SDK** (for the backend API)
- **Node.js** (v18.x or higher) and **npm** (v11.x+)
- **Angular CLI** (`npm install -g @angular/cli`)
- **PostgreSQL** (v14+) with the **PostGIS** extension enabled (for spatial data support)

## 2. Local Development Setup

### Backend Setup (.NET Core)
1. **Navigate to the backend directory:**
   ```bash
   cd "tasinmaz staj/tasinmaz staj"
   ```
2. **Configure Environment Variables:**
   Create an `appsettings.Development.json` file if it doesn't exist, and configure your database connection and JWT secret.
   *Example `.env` / `appsettings.json` structure:*
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=tasinmazdb;Username=postgres;Password=yourpassword"
     },
     "Jwt": {
       "Key": "YourSuperSecretJWTKeyForLocalDevelopment123!",
       "Issuer": "TasinmazLocal"
     }
   }
   ```
3. **Apply Database Migrations:**
   ```bash
   dotnet ef database update
   ```
4. **Run the API:**
   ```bash
   dotnet run
   ```
   *The API will start at `https://localhost:5001` or `http://localhost:5000` and Swagger UI will be available at `/swagger`.*

### Frontend Setup (Angular)
1. **Navigate to the frontend directory:**
   ```bash
   cd "tasinmaz staj/tasinmaz staj frontend"
   ```
2. **Install dependencies:**
   ```bash
   npm install
   ```
3. **Configure API Endpoint:**
   Ensure your `src/environments/environment.ts` points to your local backend API URL (e.g., `http://localhost:5000/api`).
4. **Run the Angular application:**
   ```bash
   npm start
   ```
   *The application will be accessible at `http://localhost:4200`.*

## 3. Build & Deployment
- **Backend:** Publish the application using `dotnet publish -c Release -o ./publish`. The resulting artifact can be hosted on IIS, Docker, or any Linux/Windows server supporting .NET 8.
- **Frontend:** Build the production bundle using `npm run build`. This generates static files in the `dist/` folder, which can be served via Nginx, Apache, or any static hosting service.
