import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { Log } from '../../models/log.model';
import { LogFilter } from '../../models/log.model';
import { LogService } from '../../services/log.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-log-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './log-list.html',
  styleUrl: './log-list.scss'
})
export class LogList implements OnInit {
  // Zoneless Angular'da subscribe() callback'i icinde yapilan duz alan
  // atamalari (this.x = ...) change detection'i tetiklemiyor; bu yuzden
  // "sayfalama calismiyor / yavas yukleniyor" gibi gorunen sorunlarin
  // asil sebebi buydu. Diger bilesenlerdeki gibi state signal'e tasindi.
  logs = signal<Log[]>([]);
  totalCount = signal(0);
  pageNumber = signal(1);
  readonly pageSize = 10;
  loading = signal(false);
  exporting = signal(false);
  errorMessage = signal('');
  readonly filterForm: any;

  constructor(
    private formBuilder: FormBuilder,
    private logService: LogService,
    private router: Router,
    private toastService: ToastService
  ) {
    this.filterForm = this.formBuilder.group({
      userId: '',
      status: '',
      operationType: '',
      description: '',
      userIp: '',
      startDate: '',
      endDate: ''
    });
  }

  ngOnInit(): void {
    this.loadLogs();
  }

  applyFilters(): void {
    this.pageNumber.set(1);
    this.loadLogs();
  }

  clearFilters(): void {
    this.filterForm.reset({
      userId: '',
      status: '',
      operationType: '',
      description: '',
      userIp: '',
      startDate: '',
      endDate: ''
    });
    this.pageNumber.set(1);
    this.loadLogs();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.pageNumber()) return;
    this.pageNumber.set(page);
    this.loadLogs();
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount() / this.pageSize);
  }

  get pageNumbers(): number[] {
    const total = this.totalPages;
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const start = Math.max(1, Math.min(this.pageNumber() - 2, total - 4));
    return Array.from({ length: 5 }, (_, i) => start + i);
  }

  exportToExcel(): void {
    this.exportLogs('excel');
  }

  exportToPdf(): void {
    this.exportLogs('pdf');
  }

  trackByLogId(_: number, log: Log): number {
    return log.id;
  }

  private loadLogs(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.logService.getLogs(this.buildFilter()).subscribe({
      next: result => {
        this.logs.set(result.data);
        this.totalCount.set(result.totalCount);
        this.pageNumber.set(result.pageNumber);
        this.loading.set(false);
      },
      error: () => {
        this.logs.set([]);
        this.totalCount.set(0);
        this.loading.set(false);
        this.errorMessage.set('Log kayıtları yüklenemedi.');
      }
    });
  }

  private exportLogs(type: 'excel' | 'pdf'): void {
    if (this.exporting()) return;

    this.exporting.set(true);
    this.errorMessage.set('');

    const request$ = type === 'excel'
      ? this.logService.exportToExcel(this.buildFilter())
      : this.logService.exportToPdf(this.buildFilter());

    request$.subscribe({
      next: blob => {
        const extension = type === 'excel' ? 'xlsx' : 'pdf';
        this.downloadBlob(blob, `logs_${this.getFileTimestamp()}.${extension}`);
        this.exporting.set(false);
      },
      error: () => {
        this.exporting.set(false);
        this.toastService.error('Log dışa aktarma işlemi başarısız.');
      }
    });
  }

  private buildFilter(): LogFilter {
    const raw = this.filterForm.getRawValue();
    const filter: LogFilter = {
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize
    };

    const userId = Number(raw.userId);
    if (raw.userId?.trim() && Number.isInteger(userId) && userId > 0) {
      filter.userId = userId;
    }
    if (raw.status?.trim()) filter.status = raw.status.trim();
    if (raw.operationType?.trim()) filter.operationType = raw.operationType.trim();
    if (raw.description?.trim()) filter.description = raw.description.trim();
    if (raw.userIp?.trim()) filter.userIp = raw.userIp.trim();
    if (raw.startDate) filter.startDate = raw.startDate;
    if (raw.endDate) filter.endDate = raw.endDate;

    return filter;
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  }

  private getFileTimestamp(): string {
    return new Date().toISOString()
      .replace(/[-:]/g, '')
      .replace(/\.\d{3}Z$/, '');
  }
}
