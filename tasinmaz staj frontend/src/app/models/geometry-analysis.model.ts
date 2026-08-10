// PropertyGeometryService.AnalyzeIntersectionAsync / AnalyzeUnionAsync ile
// eşleşen istek/cevap modelleri. Backend System.Text.Json varsayılanıyla
// camelCase serileştirir, bu yüzden alan adları burada da camelCase.

export interface CoordinateInput {
  longitude: number;
  latitude: number;
}

// IntersectionAnalysisDto
export interface IntersectionAnalysisRequest {
  propertyId: number;
  coordinates: CoordinateInput[][];
}

// IntersectionResultDto
export interface IntersectionResult {
  propertyId: number;
  intersects: boolean;
  propertyAreaSquareMeters: number;
  intersectionAreaSquareMeters: number;
  intersectionPercentage: number;
  intersectionGeometry: string | null;
}

// UnionAnalysisDto
export interface UnionAnalysisRequest {
  propertyAId: number;
  propertyBId: number;
  propertyCId?: number | null;
}

// UnionResultDto ("D" = A∪B, "E" = A∪B∪C)
export interface UnionResult {
  resultLabel: string;
  areaSquareMeters: number;
  geometry: string;
}
