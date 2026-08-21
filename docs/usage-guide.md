# Usage Guide & Core Features

## 1. Feature Walkthrough
- **Authentication & Authorization:** Users must log in to access the system. Upon login, a JWT is issued. The system supports secure logout by blacklisting the JWT (`jti` claim) in an in-memory cache.
- **Property Management:** Users can view a list of properties, add new properties with detailed metadata, update existing ones, and delete records. 
- **Map Integration (Geometries):** Users can define property boundaries or point locations using the integrated OpenLayers map interface. The API applies rate-limiting to geometry endpoints to prevent abuse (10 requests per minute).
- **Image Uploads:** Users can attach multiple images to a property record.
- **Export/Import:** The system supports exporting property data and system logs to external formats (e.g., Excel/PDF using ClosedXML/QuestPDF).

## 2. Code Examples

**Logging into the application via CLI (cURL):**
```bash
curl -X POST https://localhost:5001/api/Auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@example.com", "password":"password123"}'
```
*Expected Output:*
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI...",
  "expiration": "2026-08-21T18:00:00Z"
}
```
