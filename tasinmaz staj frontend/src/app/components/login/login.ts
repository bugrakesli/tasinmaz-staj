import { Component, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { LoginRequest } from '../../models/login-request.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login implements OnInit {
  // Zoneless CD altında async subscribe callback'i içinde set edildiği için
  // signal kullanılıyor.
  loginError = signal('');

  loginForm;

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  ngOnInit(): void {
    // Eğer kullanıcı (tarayıcının geri tuşuyla vb.) giriş sayfasına geri dönerse,
    // güvenliği sağlamak ve navbar'ı gizlemek için otomatik çıkış yap.
    if (this.authService.isAuthenticated()) {
      this.authService.logout();
    }
  }


  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loginError.set('');

    const loginRequest: LoginRequest = {
      email: this.loginForm.value.email ?? '',
      password: this.loginForm.value.password ?? ''
    };

    this.authService.login(loginRequest).subscribe({
      next: () => this.router.navigate(['/properties']),
      error: () => this.loginError.set('E-posta veya şifre hatalı.')
    });
  }
}