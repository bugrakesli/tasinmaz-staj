import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  ViewChild,
  signal
} from '@angular/core';

import Map from 'ol/Map';
import View from 'ol/View';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import VectorSource from 'ol/source/Vector';
import OSM from 'ol/source/OSM';
import XYZ from 'ol/source/XYZ';
import Draw from 'ol/interaction/Draw';
import WKT from 'ol/format/WKT';
import { fromLonLat } from 'ol/proj';
import Feature from 'ol/Feature';
import Polygon from 'ol/geom/Polygon';
import { ScaleLine, defaults as defaultControls } from 'ol/control';
import { Style, Fill, Stroke } from 'ol/style';

// Backend WKT'yi EPSG:4326 (derece) olarak bekliyor/üretiyor
// (bkz. PropertyGeometryService, NtsGeometryServices srid: 4326).
// OpenLayers haritası ekranda EPSG:3857 kullanır; okuma/yazma sırasında
// bu iki projeksiyon arasında dönüşüm yapıyoruz.
const MAP_PROJECTION = 'EPSG:3857';
const DATA_PROJECTION = 'EPSG:4326';

export type BasemapType = 'osm' | 'google';

// SRS 3.2.7/4.3: kesişim (intersection) analizinin sonucunu haritada
// ayırt edici biçimde vurgulamak için kullanılan stil.
const INTERSECTION_STYLE = new Style({
  fill: new Fill({ color: 'rgba(220, 53, 69, 0.35)' }),
  stroke: new Stroke({ color: '#dc3545', width: 2, lineDash: [6, 4] })
});

@Component({
  selector: 'app-map-draw',
  standalone: true,
  templateUrl: './map-draw.html',
  styleUrl: './map-draw.scss'
})
export class MapDraw implements OnChanges, OnDestroy {
  // Düzenleme modunda mevcut WKT'yi haritada göstermek için kullanılır.
  @Input() initialWkt: string | null = null;

  // Kullanıcı poligon çizdiğinde/temizlediğinde parent bileşene EPSG:4326
  // WKT string'i olarak bildirir.
  @Output() wktChange = new EventEmitter<string>();

  @ViewChild('mapContainer', { static: true }) mapContainerRef!: ElementRef<HTMLDivElement>;

  // Map, OpenLayers'ın kendi (zone dışı) event sisteminde çalıştığından
  // ekranda gösterilecek her durum signal olarak tutuluyor; aksi halde
  // zoneless Angular otomatik olarak yeniden render etmiyor.
  hasGeometry = signal(false);
  hasIntersection = signal(false);
  drawHint = signal('Poligon çizmek için haritaya tıklayın, bitirmek için çift tıklayın.');

  // 4.3: Altlık harita seçimi (OSM / Google Maps) ve katman şeffaflığı (opacity).
  basemap = signal<BasemapType>('osm');
  opacityPercent = signal(100);

  private map: Map | null = null;
  private vectorSource = new VectorSource();
  private intersectionSource = new VectorSource();
  private draw: Draw | null = null;
  private readonly wktFormat = new WKT();
  private initialized = false;

  // OSM ve Google Maps altlıklarının kaynaklarını önceden oluşturup tek bir
  // TileLayer'ın source'unu değiştiriyoruz; böylece harita katman sayısı
  // sabit kalıyor ve geçiş anlık oluyor.
  private readonly osmSource = new OSM();
  private readonly googleSource = new XYZ({
    // Not: Bu, API anahtarı gerektirmeyen genel Google karo (tile) uç
    // noktasıdır; resmi/lisanslı bir entegrasyon değildir. Üretim
    // ortamında Google Maps Platform şartlarına uygun, API anahtarlı
    // resmi bir entegrasyon (örn. @googlemaps/js-api-loader) tercih
    // edilmelidir.
    url: 'https://mt1.google.com/vt/lyrs=m&x={x}&y={y}&z={z}',
    attributions: '© Google Maps',
    crossOrigin: 'anonymous'
  });

  private readonly baseLayer = new TileLayer({ source: this.osmSource, opacity: 1 });

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.initialized) {
      this.initMap();
      this.initialized = true;
    }

    if (changes['initialWkt'] && !changes['initialWkt'].firstChange) {
      this.renderInitialGeometry();
    }
  }

  ngOnDestroy(): void {
    this.draw && this.map?.removeInteraction(this.draw);
    this.map?.setTarget(undefined);
  }

  private initMap(): void {
    const vectorLayer = new VectorLayer({ source: this.vectorSource });
    const intersectionLayer = new VectorLayer({
      source: this.intersectionSource,
      style: INTERSECTION_STYLE
    });

    // 4.3: sağ altta metrik ölçek çubuğu (scale bar).
    const scaleLine = new ScaleLine({ units: 'metric' });

    this.map = new Map({
      target: this.mapContainerRef.nativeElement,
      controls: defaultControls().extend([scaleLine]),
      layers: [
        this.baseLayer,
        vectorLayer,
        intersectionLayer
      ],
      view: new View({
        // Türkiye merkezi civarı, makul bir varsayılan zoom.
        center: fromLonLat([35.0, 39.0]),
        zoom: 6
      })
    });

    this.renderInitialGeometry();
    this.addDrawInteraction();
  }

  // 4.3: OSM / Google Maps altlığı arasında geçiş.
  setBasemap(type: BasemapType): void {
    this.basemap.set(type);
    this.baseLayer.setSource(type === 'google' ? this.googleSource : this.osmSource);
  }

  // 4.3: taşınmaz/çizim katmanının şeffaflık (opacity) ayarı.
  setOpacity(percent: number): void {
    const clamped = Math.min(100, Math.max(10, percent));
    this.opacityPercent.set(clamped);
    this.baseLayer.setOpacity(clamped / 100);
  }

  // 3.2.7: bir kesişim (intersection) analizi sonucunu haritada vurgular.
  // Backend'in IntersectionResultDto.IntersectionGeometry alanından dönen
  // WKT ile beslenmesi amaçlanır (bkz. PropertyGeometryService.AnalyzeIntersectionAsync).
  showIntersection(wkt: string | null | undefined): void {
    this.intersectionSource.clear();

    if (!wkt) {
      this.hasIntersection.set(false);
      return;
    }

    try {
      const feature = this.wktFormat.readFeature(wkt, {
        dataProjection: DATA_PROJECTION,
        featureProjection: MAP_PROJECTION
      });

      this.intersectionSource.addFeature(feature as Feature);
      this.hasIntersection.set(true);
    } catch {
      // Geçersiz/boş kesişim WKT'si sessizce yok sayılır.
      this.hasIntersection.set(false);
    }
  }

  clearIntersection(): void {
    this.intersectionSource.clear();
    this.hasIntersection.set(false);
  }

  private renderInitialGeometry(): void {
    this.vectorSource.clear();

    if (!this.initialWkt) {
      this.hasGeometry.set(false);
      return;
    }

    try {
      const feature = this.wktFormat.readFeature(this.initialWkt, {
        dataProjection: DATA_PROJECTION,
        featureProjection: MAP_PROJECTION
      });

      this.vectorSource.addFeature(feature as Feature);
      this.hasGeometry.set(true);

      const extent = this.vectorSource.getExtent();
      if (extent && extent.every((n) => Number.isFinite(n))) {
        this.map?.getView().fit(extent, { padding: [40, 40, 40, 40], maxZoom: 17 });
      }
    } catch {
      // Geçersiz/boş WKT ise sessizce yok sayıyoruz; kullanıcı manuel
      // düzenleme yapmış olabilir, harita boş kalır.
      this.hasGeometry.set(false);
    }
  }

  private addDrawInteraction(): void {
    if (!this.map) return;

    this.draw = new Draw({
      source: this.vectorSource,
      type: 'Polygon'
    });

    this.draw.on('drawstart', () => {
      // Yeni çizime başlarken önceki poligonu kaldır (tek poligon desteği).
      this.vectorSource.clear();
    });

    this.draw.on('drawend', (event) => {
      const geometry = event.feature.getGeometry() as Polygon;
      const wkt = this.wktFormat.writeGeometry(geometry, {
        dataProjection: DATA_PROJECTION,
        featureProjection: MAP_PROJECTION
      });

      this.hasGeometry.set(true);
      this.wktChange.emit(wkt);
    });

    this.map.addInteraction(this.draw);
  }

  clear(): void {
    this.vectorSource.clear();
    this.hasGeometry.set(false);
    this.wktChange.emit('');
  }

  // property-form'da kullanıcı textarea'ya manuel WKT yapıştırdığında
  // haritada göstermek için çağrılır.
  showWkt(wkt: string): void {
    this.initialWkt = wkt;
    this.renderInitialGeometry();
  }
}
