# API & Component Reference

## 1. Endpoints Overview
- `POST /api/Auth/login` - Authenticate a user and return a JWT.
- `GET /api/Property` - Retrieve a list of properties.
- `POST /api/Property` - Create a new property record.
- `GET /api/Location/geometry/{id}` - Fetch spatial data for a specific property.

## 2. Specifications & Examples

**Endpoint:** Create Property
- **URL:** `/api/Property`
- **Method:** `POST`
- **Headers:** `Authorization: Bearer <token>`, `Content-Type: application/json`

**Request Body (JSON Example):**
```json
{
  "name": "Central Park View Apartment",
  "type": "Residential",
  "price": 1250000.00,
  "locationId": 14,
  "geometry": {
    "type": "Point",
    "coordinates": [35.2433, 38.9637]
  }
}
```

## 3. Responses

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Property created successfully.",
  "data": {
    "id": 101,
    "name": "Central Park View Apartment"
  }
}
```

**Error Response (400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": [
      "The Name field is required."
    ]
  }
}
```
