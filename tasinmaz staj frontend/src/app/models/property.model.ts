export interface Property {
  id: number;
  city: string;
  district: string;
  neighborhood: string;
  parselNo: string;
  adaNo: string;
  adres: string;
  propertyType: string;
  coordinate: string;
  imagePath: string | null;
}

export interface PagedResult<T> {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  data: T[];
}