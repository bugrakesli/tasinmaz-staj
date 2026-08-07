import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

import { PropertyService } from '../../services/property.service';
import { Property } from '../../models/property.model';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-property-list',
  standalone: true,
  imports: [CommonModule],
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

  constructor(
    private propertyService: PropertyService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.isAdmin.set(this.authService.isAdmin());
    this.loadProperties();
  }

  loadProperties(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.propertyService.getAll().subscribe({
      next: (result) => {
        this.properties.set(result.data);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Taşınmazlar yüklenirken bir hata oluştu.');
        this.loading.set(false);
      }
    });
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
}
