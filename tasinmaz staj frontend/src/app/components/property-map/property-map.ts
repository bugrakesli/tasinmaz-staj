import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
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
import WKT from 'ol/format/WKT';
import Feature from 'ol/Feature';
import Overlay from 'ol/Overlay';
import { fromLonLat } from 'ol/proj';
import { ScaleLine, defaults as defaultControls } from 'ol/control';
import { Style, Fill, Stroke, Circle as CircleStyle } from 'ol/style';

import { Property } from '../../models/property.model';

// Backend WKT EPSG:4326 (derece); harita gösterimi EPSG:3857 kullanır.
const MAP_PROJECTION = 'EPSG:3857';
const DATA_PROJECTION = 'EPSG:4326';

export type BasemapType = 'osm' | 'google';

const PROPERTY_STYLE = new Style({
  fill: new Fill({ color: 'rgba(13, 110, 253, 0.25)' }),
  stroke: new Stroke({ color: '#0d6efd', width: 2 }),
  image: new CircleStyle({
    radius: 6,
    fill: new Fill({ color: '#0d6efd' }),
    stroke: new Stroke({ color: '#ffffff', width: 1.5 })
  })
});

// SRS 3.2.7: taşınmaz listesinin harita üzerinde (marker/poligon olarak)
// gösterimi. MapDraw bileşeni tek poligon çizim/düzenleme amaçlı olduğu
// için, salt-okunur çoklu taşınmaz gösterimi bu ayrı bileşende yapılır.
@Component({
  selector: 'app-property-map',
  standalone: true,
  templateUrl: './property-map.html',
  styleUrl: './property-map.scss'
})
export class PropertyMap implements AfterViewInit, OnChanges, OnDestroy {
  @Input() properties: Property[] = [];

  @ViewChild('mapContainer', { static: true }) mapContainerRef!: ElementRef<HTMLDivElement>;
  @ViewChild('popup', { static: true }) popupRef!: ElementRef<HTMLDivElement>;

  basemap = signal<BasemapType>('osm');
  opacityPercent = signal(100);
  visibleCount = signal(0);

  private map: Map | null = null;
  private vectorSource = new VectorSource();
  private popupOverlay: Overlay | null = null;
  private readonly wktFormat = new WKT();
  private viewReady = false;

  private readonly osmSource = new OSM();
  private readonly googleSource = new XYZ({
    // Not: API anahtarı gerektirmeyen genel Google karo uç noktası;
    // resmi/lisanslı bir entegrasyon değildir (bkz. MapDraw bileşeni).
    url: 'https://mt1.google.com/vt/lyrs=m&x={x}&y={y}&z={z}',
    attributions: '© Google Maps',
    crossOrigin: 'anonymous'
  });

  private readonly baseLayer = new TileLayer({ source: this.osmSource, opacity: 1 });

  ngAfterViewInit(): void {
    this.initMap();
    this.viewReady = true;
    this.renderProperties();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.viewReady && changes['properties']) {
      this.renderProperties();
    }
  }

  ngOnDestroy(): void {
    this.map?.setTarget(undefined);
  }

  setBasemap(type: BasemapType): void {
    this.basemap.set(type);
    this.baseLayer.setSource(type === 'google' ? this.googleSource : this.osmSource);
  }

  setOpacity(percent: number): void {
    const clamped = Math.min(100, Math.max(10, percent));
    this.opacityPercent.set(clamped);
    this.baseLayer.setOpacity(clamped / 100);
  }

  private initMap(): void {
    const vectorLayer = new VectorLayer({ source: this.vectorSource, style: PROPERTY_STYLE });
    const scaleLine = new ScaleLine({ units: 'metric' });

    this.popupOverlay = new Overlay({
      element: this.popupRef.nativeElement,
      positioning: 'bottom-center',
      offset: [0, -10],
      stopEvent: false
    });

    this.map = new Map({
      target: this.mapContainerRef.nativeElement,
      controls: defaultControls().extend([scaleLine]),
      overlays: [this.popupOverlay],
      layers: [this.baseLayer, vectorLayer],
      view: new View({
        // Türkiye merkezi civarı, taşınmaz verisi gelene kadarki varsayılan görünüm.
        center: fromLonLat([35.0, 39.0]),
        zoom: 6
      })
    });

    // Tıklanan taşınmazın bilgisini popup'ta göster.
    this.map.on('click', (event) => {
      const feature = this.map!.forEachFeatureAtPixel(event.pixel, (f) => f);
      if (feature) {
        const property = feature.get('property') as Property;
        this.popupRef.nativeElement.innerHTML = `
          <strong>${property.adres ?? ''}</strong><br>
          ${property.city ?? ''} / ${property.district ?? ''} / ${property.neighborhood ?? ''}<br>
          Ada: ${property.adaNo ?? ''} &nbsp; Parsel: ${property.parselNo ?? ''}
        `;
        this.popupOverlay!.setPosition(event.coordinate);
      } else {
        this.popupOverlay!.setPosition(undefined);
        this.popupRef.nativeElement.innerHTML = '';
      }
    });
  }

  private renderProperties(): void {
    this.vectorSource.clear();
    this.popupOverlay?.setPosition(undefined);

    let renderedCount = 0;

    for (const property of this.properties ?? []) {
      if (!property.coordinate) continue;

      try {
        const feature = this.wktFormat.readFeature(property.coordinate, {
          dataProjection: DATA_PROJECTION,
          featureProjection: MAP_PROJECTION
        }) as Feature;

        feature.set('property', property);
        this.vectorSource.addFeature(feature);
        renderedCount++;
      } catch {
        // Geçersiz/boş WKT'ye sahip kayıtlar haritada gösterilmeden atlanır.
      }
    }

    this.visibleCount.set(renderedCount);

    const extent = this.vectorSource.getExtent();
    if (renderedCount > 0 && extent && extent.every((n) => Number.isFinite(n))) {
      this.map?.getView().fit(extent, { padding: [40, 40, 40, 40], maxZoom: 17 });
    } else {
      this.map?.getView().setCenter(fromLonLat([35.0, 39.0]));
      this.map?.getView().setZoom(6);
    }
  }
}
