import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors } from '@angular/forms';

import { Log } from '../../models/log.model';
import { LogFilter } from '../../models/log.model';
import { LogService } from '../../services/log.service';
import { ToastService } from '../../services/toast.service';
import { FileService } from '../../services/file.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-log-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './log-list.html',
  styleUrl: './log-list.scss'
})
export class LogList implements OnInit, import('@angular/core').OnDestroy {
  private filterSub!: Subscription;
  // Zoneless Angular'da subscribe() callback'i icinde yapilan duz alan
  // atamalari (this.x = ...) change detection'i tetiklemiyor; bu yuzden
  // "sayfalama calismiyor / yavas yukleniyor" gibi gorunen sorunlarin
  // asil sebebi buydu. Diger bilesenlerdeki gibi state signal'e tasindi.
  logs = signal<Log[]>([]);
  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(10);
  loading = signal(false);
  exporting = signal(false);
  errorMessage = signal('');
  readonly filterForm: any;

  constructor(
    private formBuilder: FormBuilder,
    private logService: LogService,
    private toastService: ToastService,
    private fileService: FileService
  ) {
    this.filterForm = this.formBuilder.group({
      id: '',
      userId: '',
      status: '',
      operationType: '',
      description: '',
      userIp: '',
      startDate: '',
      endDate: ''
    }, {
      validators: (group: AbstractControl): ValidationErrors | null => {
        const start = group.get('startDate')?.value;
        const end = group.get('endDate')?.value;

        if (!start || !end) {
          return null;
        }

        return new Date(start) <= new Date(end)
          ? null
          : { dateRange: true };
      }
    });
  }

  ngOnInit(): void {
    this.loadLogs();
  }

  applyFilters(): void {
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      return;
    }

    this.pageNumber.set(1);
    this.loadLogs();
  }

  clearFilters(): void {
    this.filterForm.reset({
      id: '',
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

  firstPage(): void {
    this.goToPage(1);
  }

  lastPage(): void {
    this.goToPage(this.totalPages);
  }

  changePageSize(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const size = Number(select.value);
    if (!size || size === this.pageSize()) return;
    this.pageSize.set(size);
    this.pageNumber.set(1);
    this.loadLogs();
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount() / this.pageSize());
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
        this.fileService.downloadBlob(blob, `logs_${this.fileService.getTimestampForFileName()}.${extension}`);
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
      pageSize: this.pageSize()
    };

    if (raw.id !== null && raw.id !== undefined && raw.id !== '') {
      const idStr = String(raw.id).trim();
      const id = Number(idStr);
      if (idStr && Number.isInteger(id) && id > 0) {
        filter.id = id;
      }
    }
    if (raw.userId !== null && raw.userId !== undefined && raw.userId !== '') {
      const userIdStr = String(raw.userId).trim();
      const userId = Number(userIdStr);
      if (userIdStr && Number.isInteger(userId) && userId > 0) {
        filter.userId = userId;
      }
    }
    
    if (typeof raw.status === 'string' && raw.status.trim()) filter.status = raw.status.trim();
    if (typeof raw.operationType === 'string' && raw.operationType.trim()) filter.operationType = raw.operationType.trim();
    if (typeof raw.description === 'string' && raw.description.trim()) filter.description = raw.description.trim();
    if (typeof raw.userIp === 'string' && raw.userIp.trim()) filter.userIp = raw.userIp.trim();
    if (raw.startDate) filter.startDate = raw.startDate;
    if (raw.endDate) filter.endDate = raw.endDate;

    return filter;
  }

  

  }
