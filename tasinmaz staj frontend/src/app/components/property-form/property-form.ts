import { Component, OnInit, ViewChild, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { CommonModule } from '@angular/common';
import { PropertyService } from '../../services/property.service';
import { PropertyImageService } from '../../services/property-image.service';
import { LocationService } from '../../services/location.service';
import { CreatePropertyDto } from '../../models/create-property.model';
import { Property } from '../../models/property.model';
import { Il, Ilce, Mahalle } from '../../models/location.model';
import { MapDraw } from '../map-draw/map-draw';
import { environment } from '../../../environments/environment';
import WKT from 'ol/format/WKT';
import Polygon from 'ol/geom/Polygon';

@Component({
  selector: 'app-property-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MapDraw],
  templateUrl: './property-form.html',
  styleUrl: './property-form.scss'
})
export class PropertyForm implements OnInit {
  // Il/Ilce referans verisi (SRS: Sehir/Ilce alanlari serbest metin yerine
  // birbirine bagli (cascading) combobox olarak sunulur; Mahalle hala
  // serbest metin, cunku mahalle referans verisi kapsam disi birakildi).
  iller = signal<Il[]>([]);
  ilceler = signal<Ilce[]>([]);
  mahalleler = signal<Mahalle[]>([]);
  loadingIlceler = signal(false);
  loadingMahalleler = signal(false);
  private readonly wktFormat = new WKT();
  isEditMode = false;
  propertyId: number | null = null;

  // submit() sonucu async olarak (subscribe callback'i içinde) değiştiği için
  // zoneless change detection'ın bunu yakalaması adına signal kullanıyoruz.
  submitError = signal('');
  saving = signal(false);

  // Düzenleme modunda, listeden route state ile taşınan kayıt bulunamazsa
  // (örn. sayfa doğrudan URL ile açıldıysa) kullanıcıyı bilgilendiriyoruz.
  loadError = false;

  // Haritada gösterilecek/başlangıç WKT değeri. Harita OpenLayers'ın kendi
  // (zone dışı) event'leriyle çalıştığından, güncellemeler signal üzerinden
  // yapılıyor ki zoneless change detection değişikliği yakalasın.
  mapWkt = signal<string | null>(null);

  // İl/İlçe/Mahalle seçildiğinde haritayı ilgili konuma pan/zoom yapmak
  // için MapDraw bileşenine erişim.
  @ViewChild(MapDraw) mapDraw?: MapDraw;

  // Art arda hızlı seçim değişikliklerinde önceki geocode isteğinin
  // sonucunun haritayı geç güncellemesini (race condition) engellemek için.
  private geocodeRequestId = 0;

  // Görsel yükleme (SRS 4.3 / PropertyImageController): yalnızca düzenleme
  // modunda gösterilir çünkü upload/delete endpoint'leri var olan bir
  // taşınmaz kaydı gerektirir.
  currentImagePath = signal<string | null>(null);
  imageUploading = signal(false);
  imageError = signal('');
  // Statik dosyalar apiUrl'in ("/api" içeren) kökünden değil, sunucu
  // kökünden servis ediliyor (UseStaticFiles), o yüzden "/api" kısmını çıkarıyoruz.
  private readonly staticFilesBaseUrl = environment.apiUrl.replace(/\/api\/?$/, '');

  propertyForm;

  private readonly nonBlankValidator = (control: AbstractControl): ValidationErrors | null => {
    if (typeof control.value !== 'string') {
      return null;
    }

    return control.value.trim().length === 0 ? { required: true } : null;
  };

  constructor(
    private formBuilder: FormBuilder,
    private propertyService: PropertyService,
    private propertyImageService: PropertyImageService,
    private locationService: LocationService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.propertyForm = this.formBuilder.group({
      city: ['', [Validators.required, this.nonBlankValidator]],
      district: [{ value: '', disabled: true }, [Validators.required, this.nonBlankValidator]],
      neighborhood: [{ value: '', disabled: true }, [Validators.required, this.nonBlankValidator]],
      lotNumber: ['', [Validators.required, this.nonBlankValidator]],
      parcelNumber: ['', [Validators.required, this.nonBlankValidator]],
      address: ['', [Validators.required, this.nonBlankValidator]],
      propertyType: ['', [Validators.required, this.nonBlankValidator]],
      coordinate: ['', [Validators.required, this.nonBlankValidator]]
    });
  }

  ngOnInit(): void {
    this.locationService.getIller().subscribe({
      next: (iller) => {
        this.iller.set(iller);
        // Duzenleme modunda mevcut sehir zaten secilmis olabilir; iller
        // yuklendikten sonra o sehre ait ilceleri de getir.
        const currentCity = this.propertyForm.value.city;
        if (currentCity) {
          this.loadIlceler(currentCity);
        }
      },
      error: () => {
        // Il listesi yuklenemezse kullanici yine de serbest metinle devam
        // edebilir; formu bloklamiyoruz.
      }
    });

    const idParam = this.route.snapshot.paramMap.get('id');

    if (!idParam) {
      // Yeni kayıt ekleme modu
      return;
    }

    this.isEditMode = true;
    this.propertyId = Number(idParam);

    // Ayrı bir "GET by id" endpoint'i olmadığından, listeden düzenlemeye
    // geçerken kaydı router state üzerinden taşıyoruz.
    const state = history.state as { property?: Property };

    if (!state?.property || state.property.id !== this.propertyId) {
      this.loadError = true;
      return;
    }

    const property = state.property;

    this.propertyForm.patchValue({
      city: property.city,
      district: property.district,
      neighborhood: property.neighborhood,
      lotNumber: property.adaNo,
      parcelNumber: property.parselNo,
      address: property.adres,
      propertyType: property.propertyType,
      coordinate: property.coordinate
    });

    this.mapWkt.set(property.coordinate);
    this.currentImagePath.set(property.imagePath);

    if (property.city) {
      this.loadIlceler(property.city);
    }
    if (property.district) {
      this.loadMahalleler(property.district);
    }
  }

  // Sehir adina gore Il kaydini bulup o ile ait ilceleri getirir.
  // "city" alaninda Il adi (string) tutuldugu icin backend'deki
  // Mahalle/Ilce/Il esleme mantigiyla ayni sekilde isim uzerinden calisir.
  private loadIlceler(cityName: string): void {
    const il = this.iller().find(i => i.ad === cityName);
    if (!il) return;

    this.loadingIlceler.set(true);
    this.locationService.getIlceler(il.id).subscribe({
      next: (ilceler) => {
        this.ilceler.set(ilceler);
        this.loadingIlceler.set(false);
      },
      error: () => {
        this.loadingIlceler.set(false);
      }
    });
  }

  // Sehir combobox'i degistiginde ilce listesini yenile ve secili ilceyi sifirla.
  onCityChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const cityName = select.value;

    this.propertyForm.patchValue({ city: cityName, district: '', neighborhood: '' });
    this.ilceler.set([]);
    this.mahalleler.set([]);

    if (cityName) {
      this.loadIlceler(cityName);
      this.geocodeAndPan(`${cityName}, Türkiye`, 8);
    }
  }

  private loadMahalleler(districtName: string): void {
    const ilce = this.ilceler().find(i => i.ad === districtName);
    if (!ilce) return;

    this.loadingMahalleler.set(true);
    this.locationService.getMahalleler(ilce.id).subscribe({
      next: (mahalleler) => {
        this.mahalleler.set(mahalleler);
        this.loadingMahalleler.set(false);
      },
      error: () => {
        this.loadingMahalleler.set(false);
      }
    });
  }

  onDistrictChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const districtName = select.value;

    this.propertyForm.patchValue({ district: districtName, neighborhood: '' });
    this.mahalleler.set([]);

    if (districtName) {
      this.loadMahalleler(districtName);

      const cityName = this.propertyForm.value.city;
      this.geocodeAndPan(`${districtName}, ${cityName}, Türkiye`, 11);
    }
  }

  // Mahalle combobox'i değiştiğinde harita en yakın zoom seviyesiyle
  // seçilen mahalleye pan yapar.
  onNeighborhoodChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const neighborhoodName = select.value;

    this.propertyForm.patchValue({ neighborhood: neighborhoodName });

    if (!neighborhoodName) return;

    const cityName = this.propertyForm.value.city;
    const districtName = this.propertyForm.value.district;

    this.geocodeAndPan(
      `${neighborhoodName}, ${districtName}, ${cityName}, Türkiye`,
      15
    );
  }

  // Verilen adres metnini Nominatim üzerinden coğrafi kodlar ve sonucu
  // haritada gösterir. Sonuç bulunamazsa ya da istek başarısız olursa
  // (örn. rate limit, ağ hatası) haritayı olduğu yerde bırakır; kullanıcı
  // yine de polygon çizmeye devam edebilir.
  private geocodeAndPan(query: string, zoom: number): void {
    const requestId = ++this.geocodeRequestId;

    this.locationService.geocode(query).subscribe({
      next: (results) => {
        // Kullanıcı beklemeden başka bir seçim yaptıysa bu eski sonucu yok say.
        if (requestId !== this.geocodeRequestId) return;

        const result = results?.[0];
        if (!result) return;

        this.mapDraw?.panTo(parseFloat(result.lon), parseFloat(result.lat), zoom);
      },
      error: () => {
        // Coğrafi kodlama başarısız olsa da formu bloklamıyoruz.
      }
    });
  }

  // Görselin tam URL'sini oluşturur (relative path DB'de "/uploads/..." olarak tutuluyor).
  imageUrl(path: string): string {
    return `${this.staticFilesBaseUrl}${path}`;
  }

  onImageSelected(event: Event): void {
    if (!this.propertyId) return;

    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.imageError.set('');
    this.imageUploading.set(true);

    this.propertyImageService.upload(this.propertyId, file).subscribe({
      next: (result) => {
        this.currentImagePath.set(result.imagePath);
        this.imageUploading.set(false);
        input.value = '';
      },
      error: (err) => {
        this.imageUploading.set(false);
        this.imageError.set(
          err?.error?.message ?? 'Görsel yüklenirken bir hata oluştu.'
        );
        input.value = '';
      }
    });
  }

  deleteImage(): void {
    if (!this.propertyId) return;

    this.imageError.set('');
    this.imageUploading.set(true);

    this.propertyImageService.delete(this.propertyId).subscribe({
      next: () => {
        this.currentImagePath.set(null);
        this.imageUploading.set(false);
      },
      error: (err) => {
        this.imageUploading.set(false);
        this.imageError.set(
          err?.error?.message ?? 'Görsel silinirken bir hata oluştu.'
        );
      }
    });
  }

  // Harita üzerinde poligon çizildiğinde/temizlendiğinde tetiklenir.
  onGeometryDrawn(wkt: string): void {
    this.propertyForm.patchValue({ coordinate: wkt });
    // Signal set'i zoneless CD'yi tetikler, böylece textarea'daki değer de
    // güncellenmiş olarak render edilir.
    this.mapWkt.set(wkt);
  }

  private wktToCoordinates(wkt: string): { longitude: number; latitude: number }[][] {

    const geometry = this.wktFormat.readGeometry(wkt);

    if (geometry.getType() !== 'Polygon') {
      throw new Error('Geometri bir Polygon olmalıdır.');
    }

    const polygon = geometry as Polygon;
    const coordinates = polygon.getCoordinates();

    return coordinates.map(ring => ring.map(([longitude, latitude]) => ({longitude, latitude})));
  }

  // Kullanıcı textarea'ya elle WKT yapıştırdıysa haritada göstermek için.
  showOnMap(): void {
    this.mapWkt.set(this.propertyForm.value.coordinate ?? '');
  }

  onSubmit(): void {
    if (this.propertyForm.invalid) {
      this.propertyForm.markAllAsTouched();
      return;
    }

    this.submitError.set('');
    this.saving.set(true);

    const dto: CreatePropertyDto = {
      city: this.propertyForm.value.city ?? '',
      district: this.propertyForm.value.district ?? '',
      neighborhood: this.propertyForm.value.neighborhood ?? '',
      lotNumber: this.propertyForm.value.lotNumber ?? '',
      parcelNumber: this.propertyForm.value.parcelNumber ?? '',
      address: this.propertyForm.value.address ?? '',
      propertyType: this.propertyForm.value.propertyType ?? '',
      coordinate: this.propertyForm.value.coordinate ?? ''
    };

    const request$ = this.isEditMode && this.propertyId
      ? this.propertyService.update(this.propertyId, dto)
      : this.propertyService.create(dto);

    request$.subscribe({
      next: (result) => {
        const savedPropertyId = this.isEditMode && this.propertyId
          ? this.propertyId
          : result.data.id;

        try {
          const coordinates = this.wktToCoordinates(dto.coordinate);

          this.propertyService
            .updateGeometry(savedPropertyId, coordinates)
            .subscribe({
              next: () => {
                this.router.navigate(['/properties']);
              },
              error: (err) => {
                this.saving.set(false);
                this.submitError.set(
                  err?.error?.message ??
                  'Taşınmaz kaydedildi ancak geometri kaydedilemedi.'
                );
              }
            });

        } catch (error) {
          this.saving.set(false);
          this.submitError.set(
            'Geometri formatı geçersiz. Lütfen harita üzerinden tekrar polygon çizin.'
          );
        }
      },

      error: (err) => {
        this.saving.set(false);
        this.submitError.set(
          err?.error?.message ??
          'Kayıt sırasında bir hata oluştu. Lütfen bilgileri kontrol edin.'
        );
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/properties']);
  }
}
