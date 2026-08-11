import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

import { PropertyService } from '../../services/property.service';
import { Property } from '../../models/property.model';
import { PropertyFilter } from '../../models/property-filter.model';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-property-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './property-list.html',
  styleUrl: './property-list.scss'
})
export class PropertyList implements OnInit {
  // Zoneless Angular'da düz sınıf alanları (this.x = ...) HTTP callback'i
  // gibi zone dışı işlemlerden sonra ekranı otomatik güncellemiyor.
  // Bu yüzden state'i signal olarak tutuyoruz; signal.set() her zaman
  // change detection'ı tetikler.
  properties = signal<Property[]>([]);
  loading = signal(true);
  errorMessage = signal('');
  deletingId = signal<number | null>(null);

  // Admin taşınmaz ekleyemez/düzenleyemez/silemez (REQ-10) — şablon bu bayrağa göre
  // ilgili butonları gizler.
  isAdmin = signal(false);

  // SRS 3.2.4: export durumu (Excel/PDF ayrı ayrı, aynı anda ikisi de tetiklenebilir)
  exportingExcel = signal(false);
  exportingPdf = signal(false);

  // SRS 3.2.8: import durumu ve sonucu
  importing = signal(false);
  importMessage = signal('');
  importErrors = signal<string[]>([]);

  filterForm!: FormGroup;

  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(10);

  get totalPages(): number {
    return Math.ceil(this.totalCount() / this.pageSize());
  }

  private buildFilter(): PropertyFilter {
    const value = this.filterForm.getRawValue();
    const filter: PropertyFilter = {
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize()
    };

    if (value.city?.trim()) filter.city = value.city.trim();
    if (value.district?.trim()) filter.district = value.district.trim();
    if (value.neighborhood?.trim()) filter.neighborhood = value.neighborhood.trim();
    if (value.parcelNumber?.trim()) filter.parcelNumber = value.parcelNumber.trim();
    if (value.lotNumber?.trim()) filter.lotNumber = value.lotNumber.trim();
    if (value.address?.trim()) filter.address = value.address.trim();
    if (value.propertyType?.trim()) filter.propertyType = value.propertyType.trim();

    if (this.isAdmin() && value.ownerId?.trim()) {
      const ownerId = Number(value.ownerId);
      if (!Number.isNaN(ownerId)) filter.ownerId = ownerId;
    }

    return filter;
  }

  constructor(
  private propertyService: PropertyService,
  private authService: AuthService,
  private router: Router,
  private fb: FormBuilder
) {
  this.filterForm = this.fb.group({
    city: [''],
    district: [''],
    neighborhood: [''],
    parcelNumber: [''],
    lotNumber: [''],
    address: [''],
    propertyType: [''],
    ownerId: ['']
  });
}

  ngOnInit(): void {
    this.isAdmin.set(this.authService.isAdmin());
    this.loadProperties();
  }

  loadProperties(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.propertyService.getAll(this.buildFilter()).subscribe({
      next: (result) => {
        this.properties.set(result.data);
        this.totalCount.set(result.totalCount);
        this.pageNumber.set(result.pageNumber);
        this.pageSize.set(result.pageSize);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Taşınmazlar yüklenirken bir hata oluştu.');
        this.loading.set(false);
      }
    });
  }

  applyFilter(): void {
    this.pageNumber.set(1);
    this.loadProperties();
  }

  clearFilter(): void {
    this.filterForm.reset({
      city: '',
      district: '',
      neighborhood: '',
      parcelNumber: '',
      lotNumber: '',
      address: '',
      propertyType: '',
      ownerId: ''
    });
    this.pageNumber.set(1);
    this.loadProperties();
  }

  previousPage(): void {
    if (this.pageNumber() <= 1) return;
    this.pageNumber.update(page => page - 1);
    this.loadProperties();
  }

  nextPage(): void {
    if (this.pageNumber() >= this.totalPages) return;
    this.pageNumber.update(page => page + 1);
    this.loadProperties();
  }

  goToAnalysis(): void {
    this.router.navigate(['/analysis']);
  }

  addNew(): void {
    this.router.navigate(['/properties/new']);
  }

  edit(property: Property): void {
    this.router.navigate(['/properties', property.id, 'edit'], {
      state: { property }
    });
  }

  remove(property: Property): void {
    const confirmed = window.confirm(
      `"${property.adres}" adresli taşınmazı silmek istediğinize emin misiniz?`
    );

    if (!confirmed) {
      return;
    }

    this.deletingId.set(property.id);

    this.propertyService.delete(property.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.loadProperties();
      },
      error: () => {
        this.deletingId.set(null);
        this.errorMessage.set('Taşınmaz silinirken bir hata oluştu.');
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }

  // SRS 3.2.4 REQ-4/REQ-5/REQ-6
  exportExcel(): void {
    this.exportingExcel.set(true);
    this.propertyService.exportToExcel(this.buildFilter()).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `properties_${this.timestampForFileName()}.xlsx`);
        this.exportingExcel.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to export.');
        this.exportingExcel.set(false);
      }
    });
  }

  exportPdf(): void {
    this.exportingPdf.set(true);
    this.propertyService.exportToPdf(this.buildFilter()).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `properties_${this.timestampForFileName()}.pdf`);
        this.exportingPdf.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to export.');
        this.exportingPdf.set(false);
      }
    });
  }

  // SRS 3.2.8: dosya seçilince otomatik yükle, sonucu goster, listeyi yenile.
  onImportFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;

    if (!file) {
      return;
    }

    this.importing.set(true);
    this.importMessage.set('');
    this.importErrors.set([]);

    this.propertyService.importFromExcel(file).subscribe({
      next: (result) => {
        this.importing.set(false);
        this.importMessage.set(result.message ?? 'Properties imported successfully.');
        this.loadProperties();
        input.value = '';
      },
      error: (err) => {
        this.importing.set(false);
        const errors: string[] | undefined = err?.error?.errors;
        this.importErrors.set(
          errors && errors.length > 0
            ? errors
            : ['Import failed. Please check the file format and data.']
        );
        input.value = '';
      }
    });
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  private timestampForFileName(): string {
    const now = new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');
    return (
      `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}` +
      `${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`
    );
  }

  goToUsers(): void {
    this.router.navigate(['/users']);
  }
}

