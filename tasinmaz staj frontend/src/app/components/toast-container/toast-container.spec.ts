import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastContainer } from './toast-container';
import { ToastService } from '../../services/toast.service';

describe('ToastContainer', () => {
  let component: ToastContainer;
  let fixture: ComponentFixture<ToastContainer>;
  let mockToastService: Partial<ToastService>;

  beforeEach(async () => {
    // We can use the actual ToastService for this test since it's just signals
    await TestBed.configureTestingModule({
      imports: [ToastContainer],
      providers: [ToastService]
    }).compileComponents();

    fixture = TestBed.createComponent(ToastContainer);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
