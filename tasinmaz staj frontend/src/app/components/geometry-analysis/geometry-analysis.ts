import { Component, OnInit, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import WKTFormat from 'ol/format/WKT';
import Polygon from 'ol/geom/Polygon';

import { MapDraw } from '../map-draw/map-draw';
import { PropertyService } from '../../services/property.service';
import { Property } from '../../models/property.model';
import {
  CoordinateInput,
  IntersectionResult,
  UnionResult
} from '../../models/geometry-analysis.model';

// SRS 3.2.7: Kesişim (A ∩ B) ve birleşim (A ∪ B [∪ C]) analiz ekranı.
// MapDraw bileşeni yeniden kullanılarak sorgu poligonu (A) çizilir; B/C ise
// mevcut kayıtlı taşınmazlar arasından seçilir.
@Component({
  selector: 'app-geometry-analysis',
  standalone: true,
  imports: [CommonModule, FormsModule, MapDraw],
  templateUrl: './geometry-analysis.html',
  styleUrl: './geometry-analysis.scss'
})
export class GeometryAnalysis implements OnInit {
  @ViewChild(MapDraw) private mapDraw!: MapDraw;

  private readonly wktFormat = new WKTFormat();

  // Zoneless Angular: HTTP callback'lerinden sonra ekranı güncelleyen tüm
  // durum signal olarak tutulur (bkz. MapDraw ile aynı prensip).
  properties = signal<Property[]>([]);
  loadingProperties = signal(true);
  loadError = signal('');

  // Sorgu poligonu (A) — haritada çizilir.
  queryWkt = signal('');

  // Kesişim (A ∩ B)
  intersectionPropertyId = signal<number | null>(null);
  intersectionLoading = signal(false);
  intersectionResult = signal<IntersectionResult | null>(null);
  intersectionError = signal('');

  // Birleşim (A ∪ B [∪ C]) — burada A/B/C mevcut taşınmazlardır.
  unionAId = signal<number | null>(null);
  unionBId = signal<number | null>(null);
  unionCId = signal<number | null>(null);
  unionLoading = signal(false);
  unionResult = signal<UnionResult | null>(null);
  unionError = signal('');

  constructor(
    private propertyService: PropertyService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.propertyService.getAllForSelection().subscribe({
      next: (result) => {
        this.properties.set(result.data);
        this.loadingProperties.set(false);
      },
      error: () => {
        this.loadError.set('Taşınmaz listesi yüklenirken bir hata oluştu.');
        this.loadingProperties.set(false);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/properties']);
  }

  // MapDraw poligon çizildiğinde/temizlendiğinde WKT'yi bildirir.
  onQueryPolygonChange(wkt: string): void {
    this.queryWkt.set(wkt);
    this.intersectionResult.set(null);
    this.intersectionError.set('');
  }

  runIntersection(): void {
    const propertyId = this.intersectionPropertyId();
    const wkt = this.queryWkt();

    this.intersectionError.set('');
    this.intersectionResult.set(null);

    if (!propertyId) {
      this.intersectionError.set('Lütfen kesişim için bir taşınmaz seçin.');
      return;
    }

    if (!wkt) {
      this.intersectionError.set('Lütfen haritada bir sorgu poligonu çizin.');
      return;
    }

    let coordinates: CoordinateInput[][];
    try {
      coordinates = this.wktToCoordinateRings(wkt);
    } catch {
      this.intersectionError.set('Çizilen poligon okunamadı.');
      return;
    }

    this.intersectionLoading.set(true);

    this.propertyService
      .analyzeIntersection({ propertyId, coordinates })
      .subscribe({
        next: (result) => {
          this.intersectionResult.set(result);
          this.intersectionLoading.set(false);
          this.mapDraw.showIntersection(result.intersectionGeometry);
        },
        error: (err) => {
          this.intersectionError.set(
            err?.error?.message || 'Kesişim hesaplanırken bir hata oluştu.'
          );
          this.intersectionLoading.set(false);
        }
      });
  }

  runUnion(): void {
    const a = this.unionAId();
    const b = this.unionBId();
    const c = this.unionCId();

    this.unionError.set('');
    this.unionResult.set(null);

    if (!a || !b) {
      this.unionError.set('Lütfen A ve B taşınmazlarını seçin.');
      return;
    }

    const ids = c ? [a, b, c] : [a, b];
    if (new Set(ids).size !== ids.length) {
      this.unionError.set('A, B ve C birbirinden farklı taşınmazlar olmalıdır.');
      return;
    }

    this.unionLoading.set(true);

    this.propertyService
      .analyzeUnion({ propertyAId: a, propertyBId: b, propertyCId: c ?? undefined })
      .subscribe({
        next: (result) => {
          this.unionResult.set(result);
          this.unionLoading.set(false);
          // D/E sonucunu haritada aynı vurgulama katmanında gösteriyoruz.
          this.mapDraw.showIntersection(result.geometry);
        },
        error: (err) => {
          this.unionError.set(err?.error?.message || 'Birleşim hesaplanırken bir hata oluştu.');
          this.unionLoading.set(false);
        }
      });
  }

  clearResults(): void {
    this.intersectionResult.set(null);
    this.intersectionError.set('');
    this.unionResult.set(null);
    this.unionError.set('');
    this.mapDraw.clearIntersection();
  }

  // MapDraw'ın ürettiği EPSG:4326 WKT poligonunu, backend'in
  // IntersectionAnalysisDto.Coordinates alanının beklediği
  // List<List<{Longitude, Latitude}>> biçimine çevirir.
  private wktToCoordinateRings(wkt: string): CoordinateInput[][] {
    const geometry = this.wktFormat.readGeometry(wkt) as Polygon;
    const rings = geometry.getCoordinates();

    return rings.map((ring) =>
      ring.map(([longitude, latitude]) => ({ longitude, latitude }))
    );
  }
}
