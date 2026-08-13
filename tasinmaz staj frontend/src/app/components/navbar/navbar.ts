import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss'
})
export class Navbar {
  private authService = inject(AuthService);
  private router = inject(Router);

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  get email(): string | null {
    return this.authService.getEmail();
  }

  logout(): void {
    this.authService.logout();
  }
}
