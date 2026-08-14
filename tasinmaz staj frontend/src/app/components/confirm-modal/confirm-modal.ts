import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  imports: [],
  templateUrl: './confirm-modal.html',
  styleUrl: './confirm-modal.scss'
})
export class ConfirmModal {
  @Input() title = 'İşlemi Onayla';
  @Input() message = 'Bu işlemi gerçekleştirmek istediğinize emin misiniz?';
  @Input() confirmText = 'Onayla';
  @Input() cancelText = 'Vazgeç';
  @Input() busy = false;

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  confirm(): void {
    if (this.busy) {
      return;
    }

    this.confirmed.emit();
  }

  cancel(): void {
    if (this.busy) {
      return;
    }

    this.cancelled.emit();
  }
}