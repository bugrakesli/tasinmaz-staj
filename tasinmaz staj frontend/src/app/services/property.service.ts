import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { PropertyFilter } from '../models/property-filter.model';

import { environment } from '../../environments/environment';
import { PagedResult, Property } from '../models/property.model';
import { CreatePropertyDto } from '../models/create-property.model';
import { PropertyImportResult } from '../models/property-import-result.model';
import {
  IntersectionAnalysisRequest,
  IntersectionResult,
  UnionAnalysisRequest,
  UnionResult
} from '../models/geometry-analysis.model';

@Injectable({
  providedIn: 'root'
})
export class PropertyService {
  private readonly apiUrl = `${environment.apiUrl}/Property`;

  constructor(private http: HttpClient) {}

  getAll(filter: PropertyFilter): Observable<PagedResult<Property>> {
    let params = new HttpParams()
      .set('pageNumber', filter.pageNumber)
      .set('pageSize', filter.pageSize);

    if (filter.city?.trim()) params = params.set('city', filter.city.trim());
    if (filter.district?.trim()) params = params.set('district', filter.district.trim());
    if (filter.neighborhood?.trim()) params = params.set('neighborhood', filter.neighborhood.trim());
    if (filter.parcelNumber?.trim()) params = params.set('parcelNumber', filter.parcelNumber.trim());
    if (filter.lotNumber?.trim()) params = params.set('lotNumber', filter.lotNumber.trim());
    if (filter.address?.trim()) params = params.set('address', filter.address.trim());
    if (filter.propertyType?.trim()) params = params.set('propertyType', filter.propertyType.trim());
    if (filter.ownerId !== undefined && filter.ownerId !== null) {
      params = params.set('ownerId', filter.ownerId);
    }

    return this.http.get<PagedResult<Property>>(this.apiUrl, { params });
  }

  create(dto: CreatePropertyDto): Observable<{ message: string; data: Property }> {
    return this.http.post<{ message: string; data: Property }>(this.apiUrl, dto);
  }

  update(id: number, dto: CreatePropertyDto): Observable<{ message: string; data: Property }> {
    return this.http.put<{ message: string; data: Property }>(`${this.apiUrl}/${id}`, dto);
  }

  updateGeometry(id: number, coordinates: { longitude: number; latitude: number }[][]): Observable<{ message: string }> {
  return this.http.put<{ message: string }>(`${this.apiUrl}/${id}/geometry`, { coordinates });
  }

  delete(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }

  private buildFilterParams(filter: PropertyFilter): HttpParams {
    let params = new HttpParams();

    const fields = [
      'city', 'district', 'neighborhood', 'parcelNumber',
      'lotNumber', 'address', 'propertyType'
    ] as const;

    for (const field of fields) {
      const value = filter[field];
      if (value?.trim()) params = params.set(field, value.trim());
    }

    if (filter.ownerId !== undefined && filter.ownerId !== null) {
      params = params.set('ownerId', filter.ownerId);
    }

    return params;
  }

  exportToExcel(filter: PropertyFilter): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/excel`, {
      params: this.buildFilterParams(filter),
      responseType: 'blob'
    });
  }

  exportToPdf(filter: PropertyFilter): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/pdf`, {
      params: this.buildFilterParams(filter),
      responseType: 'blob'
    });
  }

  // SRS 3.2.8: sadece normal kullanıcılar erişebilir (Admin backend'de Forbid alır).
  importFromExcel(file: File): Observable<PropertyImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<PropertyImportResult>(`${this.apiUrl}/import/excel`, formData);
  }

  // Geometri analiz ekranı için seçim listeleri gerekiyor; sayfalamayı
  // aşacak büyük bir sayfa boyutuyla tüm kayıtları tek seferde çekiyoruz.
  getAllForSelection(): Observable<PagedResult<Property>> {
    return this.getAll({ pageNumber: 1, pageSize: 1000 });
  }

  // 3.2.7: bir sorgu poligonu (A) ile seçili bir taşınmazın (B) kesişimini
  // (A ∩ B) hesaplar; PropertyGeometryService.AnalyzeIntersectionAsync.
  analyzeIntersection(dto: IntersectionAnalysisRequest): Observable<IntersectionResult> {
    return this.http.post<IntersectionResult>(`${this.apiUrl}/spatial/intersection`, dto);
  }

  // 3.2.7: iki (veya üç) taşınmazın birleşimini (A ∪ B [∪ C]) hesaplar ve
  // sonucu D/E etiketiyle veritabanına kaydeder; PropertyGeometryService.AnalyzeUnionAsync.
  analyzeUnion(dto: UnionAnalysisRequest): Observable<UnionResult> {
    return this.http.post<UnionResult>(`${this.apiUrl}/spatial/union`, dto);
  }
}