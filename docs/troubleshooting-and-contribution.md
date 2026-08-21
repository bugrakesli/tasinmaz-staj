# Troubleshooting & Contribution

## 1. Common Issues (FAQ)

**Issue: JWT Token Validation Fails / Unauthorized (401)**
- **Cause:** The token has expired, the Issuer/Key doesn't match the `appsettings.json`, or the token was blacklisted on logout.
- **Resolution:** Verify your `appsettings.json` JWT settings match exactly between generation and validation. If you logged out previously, the token is permanently revoked; request a new one via `/api/Auth/login`.

**Issue: Database Migration Fails (PostGIS Extension Missing)**
- **Cause:** Entity Framework is trying to map `Geometry` types, but PostgreSQL lacks the spatial extension.
- **Resolution:** Connect to your PostgreSQL database as a superuser and run: `CREATE EXTENSION postgis;` before applying EF migrations.

**Issue: Angular Map not rendering features**
- **Cause:** CORS blocking the request or invalid GeoJSON format.
- **Resolution:** Check the browser console for CORS errors. Ensure backend `Startup.cs` has `AllowAngular` CORS policy properly configured for `http://localhost:4200`.

## 2. Contribution Guidelines
- **Branching Strategy:** We use standard Git Flow. All new features should be developed on `feature/short-description` branches created from `develop`. Hotfixes branch from `main`.
- **Testing:** All new backend services must be accompanied by unit tests located in the `tasinmaz staj.Tests` project. Do not merge code that reduces overall code coverage.
- **Pull Requests (PR):** Open a PR against the `develop` branch. PRs must pass all automated builds and require at least one code review approval from a senior developer before merging. Ensure PR descriptions clearly define the "Why" and "What" of the changes.
