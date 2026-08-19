import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
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
import { getCenter } from 'ol/extent';
import { ScaleLine, defaults as defaultControls } from 'ol/control';
import { Style, Fill, Stroke, Circle as CircleStyle } from 'ol/style';

import { Property } from '../../models/property.model';

// Backend WKT EPSG:4326 (derece); harita gösterimi EPSG:3857 kullanır.
const MAP_PROJECTION = 'EPSG:3857';
const DATA_PROJECTION = 'EPSG:4326';

export type BasemapType = 'osm' | 'google';

// Basemap seçimi sayfa yenilendiğinde sıfırlanmasın diye localStorage'da tutulur.
const BASEMAP_STORAGE_KEY = 'tys-basemap';

function getStoredBasemap(): BasemapType {
  const stored = localStorage.getItem(BASEMAP_STORAGE_KEY);
  return stored === 'google' ? 'google' : 'osm';
}

const HIGHLIGHT_STYLE = new Style({
  fill: new Fill({ color: 'rgba(11, 94, 215, 0.4)' }),
  stroke: new Stroke({ color: '#0a58ca', width: 3 }),
  image: new CircleStyle({
    radius: 7,
    fill: new Fill({ color: '#0a58ca' }),
    stroke: new Stroke({ color: '#ffffff', width: 2 })
  })
});

const SELECTED_STYLE = new Style({
  fill: new Fill({ color: 'rgba(11, 94, 215, 0.6)' }),
  stroke: new Stroke({ color: '#0a58ca', width: 4 }),
  image: new CircleStyle({
    radius: 8,
    fill: new Fill({ color: '#0a58ca' }),
    stroke: new Stroke({ color: '#ffffff', width: 2 })
  })
});

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
  @Input() hoveredPropertyId: number | null = null;
  @Input() selectedPropertyId: number | null = null;

  @Output() propertyHovered = new EventEmitter<number | null>();
  @Output() propertySelected = new EventEmitter<number | null>();

  @ViewChild('mapContainer', { static: true }) mapContainerRef!: ElementRef<HTMLDivElement>;
  @ViewChild('popup', { static: true }) popupRef!: ElementRef<HTMLDivElement>;

  basemap = signal<BasemapType>(getStoredBasemap());
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

  private readonly baseLayer = new TileLayer({
    source: this.basemap() === 'google' ? this.googleSource : this.osmSource,
    opacity: 1
  });

  ngAfterViewInit(): void {
    this.initMap();
    this.viewReady = true;
    this.renderProperties();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.viewReady) {
      if (changes['properties']) {
        this.renderProperties();
      }
      if (changes['properties'] || changes['hoveredPropertyId'] || changes['selectedPropertyId']) {
        this.updateFeatureStyles();
      }
      if (changes['selectedPropertyId'] || changes['properties']) {
        this.updatePopup();
      }
    }
  }

  private updateFeatureStyles(): void {
    this.vectorSource.getFeatures().forEach(feature => {
      const property = feature.get('property') as Property;
      if (this.selectedPropertyId === property.id) {
        feature.setStyle(SELECTED_STYLE);
      } else if (this.hoveredPropertyId === property.id) {
        feature.setStyle(HIGHLIGHT_STYLE);
      } else {
        feature.setStyle(PROPERTY_STYLE);
      }
    });
  }

  private updatePopup(): void {
    if (!this.selectedPropertyId) {
      this.popupOverlay?.setPosition(undefined);
      return;
    }
    const feature = this.vectorSource.getFeatures().find(f => (f.get('property') as Property).id === this.selectedPropertyId);
    if (feature) {
      const property = feature.get('property') as Property;
      const extent = feature.getGeometry()?.getExtent();
      if (extent) {
        const center = getCenter(extent);
        this.popupRef.nativeElement.innerHTML = `
          <strong>${property.adres ?? ''}</strong><br>
          ${property.city ?? ''} / ${property.district ?? ''} / ${property.neighborhood ?? ''}<br>
          Ada: ${property.adaNo ?? ''} &nbsp; Parsel: ${property.parselNo ?? ''}
        `;
        this.popupOverlay!.setPosition(center);
        this.panToFeature(extent);
      }
    }
  }

  // Listeden bir taşınmaz seçildiğinde harita da o taşınmaza pan etsin
  // (kullanıcı listeyle haritayı elle senkronize etmek zorunda kalmasın).
  private panToFeature(extent: number[]): void {
    if (!this.map || !extent.every((n) => Number.isFinite(n))) return;

    const view = this.map.getView();
    const center = getCenter(extent);
    const currentZoom = view.getZoom() ?? 6;
    // Seçilen taşınmaz çok küçükse (nokta gibi) aşırı yakınlaşmayı önle,
    // ama zaten uzaktaysak (şehir seviyesi) makul bir seviyeye yaklaştır.
    const targetZoom = currentZoom < 15 ? 16 : currentZoom;

    view.animate({
      center,
      zoom: targetZoom,
      duration: 400
    });
  }

  ngOnDestroy(): void {
    this.map?.setTarget(undefined);
  }

  setBasemap(type: BasemapType): void {
    this.basemap.set(type);
    this.baseLayer.setSource(type === 'google' ? this.googleSource : this.osmSource);
    localStorage.setItem(BASEMAP_STORAGE_KEY, type);
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

    // Harita etkileşimi: hover (pointermove) ve click
    this.map.on('click', (event) => {
      const feature = this.map!.forEachFeatureAtPixel(event.pixel, (f) => f);
      if (feature) {
        const property = feature.get('property') as Property;
        if (this.selectedPropertyId === property.id) {
          this.propertySelected.emit(null);
        } else {
          this.propertySelected.emit(property.id);
        }
      } else {
        this.propertySelected.emit(null);
      }
    });

    this.map.on('pointermove', (event) => {
      if (event.dragging) return;
      const feature = this.map!.forEachFeatureAtPixel(event.pixel, (f) => f);
      if (feature) {
        const property = feature.get('property') as Property;
        if (property && property.id !== this.hoveredPropertyId) {
          this.propertyHovered.emit(property.id);
        }
      } else {
        if (this.hoveredPropertyId !== null) {
          this.propertyHovered.emit(null);
        }
      }
      this.map!.getTargetElement().style.cursor = feature ? 'pointer' : '';
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
