# Code-Level Documentation & Maintainability

## 1. Directory Structure

```text
tasinmaz staj/
├── tasinmaz staj/                  # ASP.NET Core Backend
│   ├── Controllers/                # API Endpoints (Auth, Property, Location, etc.)
│   ├── Data/                       # EF Core DbContext and Configurations
│   ├── DTOs/                       # Data Transfer Objects for API requests/responses
│   ├── Entities/                   # Database Models (Property, User, Log)
│   ├── Interfaces/                 # Service contracts (e.g., IPropertyService)
│   ├── Middleware/                 # Custom middleware (GlobalExceptionHandler)
│   ├── Migrations/                 # EF Core database migrations
│   ├── Services/                   # Business logic implementation
│   └── Program.cs / Startup.cs     # Application bootstrapping & DI container setup
│
└── tasinmaz staj frontend/         # Angular Frontend
    ├── src/
    │   ├── app/                    # Angular components, services, and routing
    │   ├── assets/                 # Static assets (images, icons)
    │   └── environments/           # Environment configurations (dev, prod)
    ├── angular.json                # Angular CLI configuration
    └── package.json                # Frontend dependencies
```

## 2. Inline Commenting Strategy

For complex C# backend logic, utilize XML docstrings to ensure IntelliSense support and easy documentation generation (e.g., via Swashbuckle/Swagger).

**C# Backend Example:**
```csharp
/// <summary>
/// Calculates the total area of a given polygon geometry representing a property boundary.
/// </summary>
/// <param name="propertyId">The unique identifier of the property.</param>
/// <returns>The calculated area in square meters.</returns>
/// <exception cref="ArgumentException">Thrown when the property geometry is not a valid polygon.</exception>
public async Task<double> CalculatePropertyAreaAsync(int propertyId)
{
    // Fetch geometry from database ensuring we only load necessary spatial columns
    var geometry = await _geometryService.GetGeometryByIdAsync(propertyId);
    
    if (geometry.GeometryType != "Polygon")
    {
        throw new ArgumentException("Invalid geometry type for area calculation.");
    }
    
    return geometry.Area;
}
```

**TypeScript Frontend Example (JSDoc):**
```typescript
/**
 * Renders the property geometry on the OpenLayers map instance.
 * Centers the map view on the provided coordinates and applies default styling.
 * 
 * @param {GeoJSON} geoJsonData - The geometry data in GeoJSON format.
 * @param {boolean} zoomToExtent - Whether the map viewport should automatically zoom to fit the geometry.
 * @returns {void}
 */
renderPropertyOnMap(geoJsonData: any, zoomToExtent: boolean = true): void {
  // Logic to parse GeoJSON and add features to the vector layer
}
```
