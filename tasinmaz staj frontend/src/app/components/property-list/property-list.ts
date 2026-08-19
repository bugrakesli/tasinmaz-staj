import { Component, OnInit, signal, computed } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, forkJoin, map } from 'rxjs';

import { PropertyService } from '../../services/property.service';
import { LocationService } from '../../services/location.service';
import { ToastService } from '../../services/toast.service'; // Eklendi
import { Property } from '../../models/property.model';
import { Il, Ilce, Mahalle } from '../../models/location.model';
import { PropertyFilter } from '../../models/property-filter.model';
import { AuthService } from '../../services/auth.service';
import { PropertyMap } from '../property-map/property-map';
import { ConfirmModal } from '../confirm-modal/confirm-modal';

@Component({
  selector: 'app-property-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PropertyMap, ConfirmModal],
  templateUrl: './property-list.html',
  styleUrl: './property-list.scss'
})
export class PropertyList implements OnInit {
  properties = signal<Property[]>([]);
  loading = signal(true);
  errorMessage = signal('');

  isAdmin = signal(false);

  exportingExcel = signal(false);
  exportingPdf = signal(false);

  importing = signal(false);
  importMessage = signal('');
  importErrors = signal<string[]>([]);

  filterForm!: FormGroup;

  iller = signal<Il[]>([]);
  filterIlceler = signal<Ilce[]>([]);
  filterMahalleler = signal<Mahalle[]>([]);

  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(10);

  showMap = signal(true);

  hoveredId = signal<number | null>(null);
  selectedId = signal<number | null>(null);

  toggleSelection(id: number): void {
    this.selectedId.set(this.selectedId() === id ? null : id);
  }

  selectedForDelete = signal<Set<number>>(new Set<number>());
  isDeletingSelected = signal(false);
  showDeleteModal = signal(false);

  isAllSelected = computed(() => {
    const props = this.properties();
    return props.length > 0 && props.every(p => this.selectedForDelete().has(p.id));
  });

  toggleForDelete(property: Property, event: Event): void {
    event.stopPropagation();
    const current = new Set(this.selectedForDelete());
    if (current.has(property.id)) {
      current.delete(property.id);
    } else {
      current.add(property.id);
    }
    this.selectedForDelete.set(current);
  }

  toggleAllForDelete(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.checked) {
      const allIds = this.properties().map(p => p.id);
      this.selectedForDelete.set(new Set(allIds));
    } else {
      this.selectedForDelete.set(new Set<number>());
    }
  }

  deleteSelected(): void {
    const ids = Array.from(this.selectedForDelete());

    if (ids.length === 0) {
      return;
    }

    this.showDeleteModal.set(true);
  }

  confirmDeleteSelected(): void {
    const ids = Array.from(this.selectedForDelete());

    if (ids.length === 0) {
      this.showDeleteModal.set(false);
      return;
    }

    this.showDeleteModal.set(false);
    this.isDeletingSelected.set(true);

    const requests = ids.map(id =>
      this.propertyService.delete(id)
    );

    forkJoin(requests).subscribe({
      next: () => {
        this.selectedForDelete.set(new Set<number>());
        this.isDeletingSelected.set(false);
        this.loadProperties();
      },
      error: () => {
        this.errorMessage.set(
          'Bazı taşınmazlar silinirken hata oluştu.'
        );

        this.isDeletingSelected.set(false);
        this.loadProperties();
      }
    });
  }

  cancelDeleteSelected(): void {
    this.showDeleteModal.set(false);
  }

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

    if (this.isAdmin() && value.ownerId !== null && value.ownerId !== undefined && value.ownerId !== '') {
      const ownerId = Number(value.ownerId);
      if (!Number.isNaN(ownerId)) filter.ownerId = ownerId;
    }

    return filter;
  }

  constructor(
    private propertyService: PropertyService,
    private locationService: LocationService,
    private authService: AuthService,
    private toastService: ToastService, // Eklendi
    private router: Router,
    private fb: FormBuilder
  ) {
    this.filterForm = this.fb.group({
      city: [''],
      district: [{ value: '', disabled: true }],
      neighborhood: [{ value: '', disabled: true }],
      parcelNumber: [''],
      lotNumber: [''],
      address: [''],
      propertyType: [''],
      ownerId: ['']
    });
  }

  ngOnInit(): void {
    this.isAdmin.set(this.authService.isAdmin());
    this.locationService.getIller().subscribe({
      next: (iller) => this.iller.set(iller),
      error: () => {}
    });
    this.loadProperties();

    this.filterForm.valueChanges.pipe(
      map(value => JSON.stringify(value)),
      distinctUntilChanged(),
      debounceTime(300)
    ).subscribe(() => {
      this.pageNumber.set(1);
      this.loadProperties();
    });
  }

  onFilterCityChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const cityName = select.value;

    this.filterForm.patchValue({ district: '', neighborhood: '' });
    this.filterForm.get('neighborhood')?.disable();
    this.filterIlceler.set([]);
    this.filterMahalleler.set([]);

    if (!cityName) {
      this.filterForm.get('district')?.disable();
      return;
    }

    this.filterForm.get('district')?.enable();

    const il = this.iller().find(i => i.ad === cityName);
    if (!il) return;

    this.locationService.getIlceler(il.id).subscribe({
      next: (ilceler) => this.filterIlceler.set(ilceler),
      error: () => {}
    });
  }

  onFilterDistrictChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const districtName = select.value;

    this.filterForm.patchValue({ neighborhood: '' });
    this.filterMahalleler.set([]);

    if (!districtName) {
      this.filterForm.get('neighborhood')?.disable();
      return;
    }

    this.filterForm.get('neighborhood')?.enable();

    const ilce = this.filterIlceler().find(i => i.ad === districtName);
    if (!ilce) return;

    this.locationService.getMahalleler(ilce.id).subscribe({
      next: (mahalleler) => this.filterMahalleler.set(mahalleler),
      error: () => {}
    });
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
        this.selectedForDelete.set(new Set<number>());
      },
      error: () => {
        this.errorMessage.set('Taşınmazlar yüklenirken bir hata oluştu.');
        this.loading.set(false);
      }
    });
  }

  applyFilter(): void {
    this.pageNumber.set(1);
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
    this.filterForm.get('district')?.disable();
    this.filterForm.get('neighborhood')?.disable();
    this.filterIlceler.set([]);
    this.filterMahalleler.set([]);
    this.pageNumber.set(1);
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

  firstPage(): void {
    if (this.pageNumber() <= 1) return;
    this.pageNumber.set(1);
    this.loadProperties();
  }

  lastPage(): void {
    if (this.pageNumber() >= this.totalPages) return;
    this.pageNumber.set(this.totalPages);
    this.loadProperties();
  }

  changePageSize(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const size = Number(select.value);
    if (!size || size === this.pageSize()) return;
    this.pageSize.set(size);
    this.pageNumber.set(1);
    this.loadProperties();
  }

  toggleMap(): void {
    this.showMap.update(value => !value);
  }

  addNew(): void {
    this.router.navigate(['/properties/new']);
  }

  edit(property: Property, event?: Event): void {
    if (event) event.stopPropagation();
    this.router.navigate(['/properties', property.id, 'edit'], {
      state: { property }
    });
  }

  logout(): void {
    this.authService.logout();
  }

  exportExcel(): void {
    this.exportingExcel.set(true);
    this.propertyService.exportToExcel(this.buildFilter()).subscribe({
      next: (blob) => {
        this.downloadBlob(blob, `properties_${this.timestampForFileName()}.xlsx`);
        this.exportingExcel.set(false);
      },
      error: () => {
        this.toastService.error('Dışa aktarma başarısız oldu.');
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
        this.toastService.error('Dışa aktarma başarısız oldu.');
        this.exportingPdf.set(false);
      }
    });
  }

  onImportFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;

    if (!file) return;

    this.importing.set(true);
    this.importMessage.set('');
    this.importErrors.set([]);

    this.propertyService.importFromExcel(file).subscribe({
      next: (result) => {
        this.importing.set(false);
        this.toastService.success(result.message ?? 'Properties imported successfully.');
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
}