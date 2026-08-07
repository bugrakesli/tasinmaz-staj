import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { PagedResult, Property } from '../models/property.model';
import { CreatePropertyDto } from '../models/create-property.model';

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

  delete(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}`);
  }
}