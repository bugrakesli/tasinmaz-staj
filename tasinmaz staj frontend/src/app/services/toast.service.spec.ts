import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should show success toast', () => {
    service.success('Success message');
    const toasts = service.toasts();
    expect(toasts.length).toBe(1);
    expect(toasts[0].message).toBe('Success message');
    expect(toasts[0].type).toBe('success');
  });
});
