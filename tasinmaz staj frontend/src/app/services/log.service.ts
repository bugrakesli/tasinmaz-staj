import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { LogFilter, LogPagedResult } from '../models/log.model';

@Injectable({
  providedIn: 'root'
})
export class LogService {
  private readonly apiUrl = `${environment.apiUrl}/Log`;

  constructor(private http: HttpClient) {}

  getLogs(filter: LogFilter): Observable<LogPagedResult> {
    return this.http.get<LogPagedResult>(this.apiUrl, {
      params: this.buildFilterParams(filter, true)
    });
  }

  exportToExcel(filter: LogFilter): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/excel`, {
      params: this.buildFilterParams(filter, false),
      responseType: 'blob'
    });
  }

  exportToPdf(filter: LogFilter): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export/pdf`, {
      params: this.buildFilterParams(filter, false),
      responseType: 'blob'
    });
  }

  private buildFilterParams(filter: LogFilter, includePagination: boolean): HttpParams {
    let params = new HttpParams();

    if (filter.id !== undefined && filter.id !== null) {
      params = params.set('id', filter.id);
    }
    if (filter.userId !== undefined && filter.userId !== null) {
      params = params.set('userId', filter.userId);
    }
    if (filter.status?.trim()) {
      params = params.set('status', filter.status.trim());
    }
    if (filter.operationType?.trim()) {
      params = params.set('operationType', filter.operationType.trim());
    }
    if (filter.description?.trim()) {
      params = params.set('description', filter.description.trim());
    }
    if (filter.userIp?.trim()) {
      params = params.set('userIp', filter.userIp.trim());
    }
    if (filter.startDate) {
      params = params.set('startDate', filter.startDate);
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate);
    }
    if (includePagination) {
      params = params
        .set('pageNumber', filter.pageNumber)
        .set('pageSize', filter.pageSize);
    }

    return params;
  }
}
