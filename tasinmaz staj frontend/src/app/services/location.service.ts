import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { Il, Ilce, Mahalle } from '../models/location.model';

// Nominatim (OpenStreetMap) sonuç kaydı; yalnızca ihtiyacımız olan alanlar.
export interface GeocodeResult {
  lat: string;
  lon: string;
}

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private readonly apiUrl = `${environment.apiUrl}/Location`;

  // Not: Ücretsiz/anahtar gerektirmeyen genel Nominatim uç noktasıdır
  // (bkz. property-map'teki Google tile notu ile aynı yaklaşım). Üretim
  // ortamında kullanım politikası (1 istek/sn, aşırı kullanım yasağı)
  // gözetilmeli; yoğun kullanımda kendi coğrafi kodlama servisimiz ya da
  // il/ilçe/mahalle için önceden hesaplanmış merkez koordinatları tercih
  // edilmelidir.
  private readonly geocodeUrl = 'https://nominatim.openstreetmap.org/search';

  constructor(private http: HttpClient) {}

  // Seçilen İl/İlçe/Mahalle adına göre yaklaşık merkez koordinatını
  // bulur; haritayı bu konuma pan/zoom yapmak için kullanılır.
  geocode(query: string): Observable<GeocodeResult[]> {
    const params = new HttpParams()
      .set('format', 'json')
      .set('limit', '1')
      .set('countrycodes', 'tr')
      .set('q', query);

    return this.http.get<GeocodeResult[]>(this.geocodeUrl, { params });
  }

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
    return this.http.get<Mahalle[]>(`${this.apiUrl}/ilceler/${ilceId}/mahalleler`);
  }
}
