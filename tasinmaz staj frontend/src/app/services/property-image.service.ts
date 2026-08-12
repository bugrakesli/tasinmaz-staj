import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PropertyImageService {
  private readonly apiUrl = `${environment.apiUrl}/properties`;

  constructor(private http: HttpClient) {}

  // Backend: POST /api/properties/{propertyId}/image (multipart/form-data)
  upload(propertyId: number, file: File): Observable<{ message: string; imagePath: string }> {
    const formData = new FormData();
    formData.append('Image', file);
    return this.http.post<{ message: string; imagePath: string }>(
      `${this.apiUrl}/${propertyId}/image`,
      formData
    );
  }

  // Backend: DELETE /api/properties/{propertyId}/image
  delete(propertyId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${propertyId}/image`);
  }
}
