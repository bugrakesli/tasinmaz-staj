export interface PropertyFilter {
  city?: string;
  district?: string;
  neighborhood?: string;
  parcelNumber?: string;
  lotNumber?: string;
  address?: string;
  propertyType?: string;
  ownerId?: number;

  pageNumber: number;
  pageSize: number;
}