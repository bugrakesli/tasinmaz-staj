import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

import { Log } from '../../models/log.model';
import { LogFilter } from '../../models/log.model';
import { LogService } from '../../services/log.service';

@Component({
  selector: 'app-log-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './log-list.html',
  styleUrl: './log-list.scss'
})
export class LogList implements OnInit {
  logs: Log[] = [];
  totalCount = 0;
  pageNumber = 1;
  readonly pageSize = 10;
  loading = false;
  exporting = false;
  errorMessage = '';
  readonly filterForm: any;

  constructor(
    private formBuilder: FormBuilder,
    private logService: LogService
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
    this.pageNumber = 1;
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
    this.pageNumber = 1;
    this.loadLogs();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.pageNumber) return;
    this.pageNumber = page;
    this.loadLogs();
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get pageNumbers(): number[] {
    const total = this.totalPages;
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const start = Math.max(1, Math.min(this.pageNumber - 2, total - 4));
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
    this.loading = true;
    this.errorMessage = '';

    this.logService.getLogs(this.buildFilter()).subscribe({
      next: result => {
        this.logs = result.data;
        this.totalCount = result.totalCount;
        this.pageNumber = result.pageNumber;
        this.loading = false;
      },
      error: () => {
        this.logs = [];
        this.totalCount = 0;
        this.loading = false;
        this.errorMessage = 'Log kayıtları yüklenemedi.';
      }
    });
  }

  private exportLogs(type: 'excel' | 'pdf'): void {
    if (this.exporting) return;

    this.exporting = true;
    this.errorMessage = '';

    const request$ = type === 'excel'
      ? this.logService.exportToExcel(this.buildFilter())
      : this.logService.exportToPdf(this.buildFilter());

    request$.subscribe({
      next: blob => {
        const extension = type === 'excel' ? 'xlsx' : 'pdf';
        this.downloadBlob(blob, `logs_${this.getFileTimestamp()}.${extension}`);
        this.exporting = false;
      },
      error: () => {
        this.exporting = false;
        this.errorMessage = 'Log dışa aktarma işlemi başarısız.';
      }
    });
  }

  private buildFilter(): LogFilter {
    const raw = this.filterForm.getRawValue();
    const filter: LogFilter = {
      pageNumber: this.pageNumber,
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
