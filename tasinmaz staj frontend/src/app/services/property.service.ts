import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

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

  getAll(pageNumber = 1, pageSize = 10): Observable<PagedResult<Property>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

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

  // SRS 3.2.4: ekrandaki (o an uygulanan) filtreleri yansıtır; şu an filtre
  // formu bağlı olmadığından tüm eşleşen kayıtlar export edilir.
  exportToExcel(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/excel`, { responseType: 'blob' });
  }

  exportToPdf(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/pdf`, { responseType: 'blob' });
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
    return this.getAll(1, 1000);
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