import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Navbar } from './navbar';
import { AuthService } from '../../services/auth.service';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';

describe('Navbar', () => {
  let component: Navbar;
  let fixture: ComponentFixture<Navbar>;
  let mockAuthService: any;

  beforeEach(async () => {
    // Create a mock AuthService
    mockAuthService = {
      isAuthenticated: () => true,
      getEmail: () => 'admin@test.com',
      isAdmin: () => true,
      logout: () => {}
    };

    await TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Navbar);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
  
  it('should display user email from auth service', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('admin@test.com');
  });
});
