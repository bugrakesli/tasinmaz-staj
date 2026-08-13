import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, ToastMessage } from '../../services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-container.html',
  styleUrls: ['./toast-container.scss']
})
export class ToastContainer {
  constructor(public toastService: ToastService) {}

  getToastClass(type: ToastMessage['type']): string {
    switch (type) {
      case 'success': return 'bg-success text-white';
      case 'error': return 'bg-danger text-white';
      case 'warning': return 'bg-warning text-dark';
      case 'info': return 'bg-info text-white';
      default: return 'bg-info text-white';
    }
  }

  getIconClass(type: ToastMessage['type']): string {
    switch (type) {
      case 'success': return 'bi-check-circle-fill';
      case 'error': return 'bi-x-circle-fill';
      case 'warning': return 'bi-exclamation-triangle-fill';
      case 'info': return 'bi-info-circle-fill';
      default: return 'bi-info-circle-fill';
    }
  }

  getTitle(type: ToastMessage['type']): string {
    switch (type) {
      case 'success': return 'Başarılı';
      case 'error': return 'Hata';
      case 'warning': return 'Uyarı';
      case 'info': return 'Bilgi';
      default: return 'Bilgi';
    }
  }
}
