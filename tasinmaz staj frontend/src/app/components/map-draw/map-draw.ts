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
import Draw from 'ol/interaction/Draw';
import WKT from 'ol/format/WKT';
import { fromLonLat } from 'ol/proj';
import Feature from 'ol/Feature';
import Polygon from 'ol/geom/Polygon';

// Backend WKT'yi EPSG:4326 (derece) olarak bekliyor/üretiyor
// (bkz. PropertyGeometryService, NtsGeometryServices srid: 4326).
// OpenLayers haritası ekranda EPSG:3857 kullanır; okuma/yazma sırasında
// bu iki projeksiyon arasında dönüşüm yapıyoruz.
const MAP_PROJECTION = 'EPSG:3857';
const DATA_PROJECTION = 'EPSG:4326';

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
  drawHint = signal('Poligon çizmek için haritaya tıklayın, bitirmek için çift tıklayın.');

  private map: Map | null = null;
  private vectorSource = new VectorSource();
  private draw: Draw | null = null;
  private readonly wktFormat = new WKT();
  private initialized = false;

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

    this.map = new Map({
      target: this.mapContainerRef.nativeElement,
      layers: [
        new TileLayer({ source: new OSM() }),
        vectorLayer
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
