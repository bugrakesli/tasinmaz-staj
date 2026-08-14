import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Il, Ilce, Mahalle } from '../models/location.model';

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private readonly apiUrl = `${environment.apiUrl}/Location`;
  private readonly referenceUrl = `${environment.apiUrl}/Reference`;

  constructor(private http: HttpClient) {}

  getIller(): Observable<Il[]> {
    return this.http.get<Il[]>(`${this.apiUrl}/iller`);
  }

  getIlceler(ilId: number | null): Observable<Ilce[]> {
    let params = new HttpParams();
    if (ilId) {
      params = params.set('ilId', ilId);
    }
    return this.http.get<Ilce[]>(`${this.apiUrl}/ilceler`, { params });
  }

  getMahalleler(ilceId: number): Observable<Mahalle[]> {
    return this.http.get<Mahalle[]>(`${this.referenceUrl}/ilceler/${ilceId}/mahalleler`);
  }
}
