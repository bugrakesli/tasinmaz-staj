import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { PropertyService } from '../../services/property.service';
import { CreatePropertyDto } from '../../models/create-property.model';
import { Property } from '../../models/property.model';
import { MapDraw } from '../map-draw/map-draw';
import WKT from 'ol/format/WKT';
import Polygon from 'ol/geom/Polygon';

@Component({
  selector: 'app-property-form',
  standalone: true,
  imports: [ReactiveFormsModule, MapDraw],
  templateUrl: './property-form.html',
  styleUrl: './property-form.scss'
})
export class PropertyForm implements OnInit {
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

  propertyForm;

  constructor(
    private formBuilder: FormBuilder,
    private propertyService: PropertyService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.propertyForm = this.formBuilder.group({
      city: ['', Validators.required],
      district: ['', Validators.required],
      neighborhood: ['', Validators.required],
      lotNumber: ['', Validators.required],
      parcelNumber: ['', Validators.required],
      address: ['', Validators.required],
      propertyType: ['', Validators.required],
      coordinate: ['', Validators.required]
    });
  }

  ngOnInit(): void {
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
