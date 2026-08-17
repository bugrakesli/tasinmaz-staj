import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.html'
})
export class ResetPassword implements OnInit {
  errorMessage = signal('');
  successMessage = signal('');
  token = '';
  email = '';

  form;

  constructor(
    private formBuilder: FormBuilder,
    private route: ActivatedRoute,
    private authService: AuthService
  ) {
    this.form = this.formBuilder.group({
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(12)
      ]],
      confirmPassword: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.email || !this.token) {
      this.errorMessage.set('Geçersiz şifre sıfırlama bağlantısı.');
    }
  }

  onSubmit(): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const password = this.form.value.password ?? '';
    const confirmPassword = this.form.value.confirmPassword ?? '';

    if (password !== confirmPassword) {
      this.errorMessage.set('Şifreler eşleşmiyor.');
      return;
    }

    if (!/[A-Za-zÇĞİÖŞÜçğıöşü]/.test(password) ||
        !/[0-9]/.test(password) ||
        !/[^A-Za-z0-9ÇĞİÖŞÜçğıöşü]/.test(password)) {
      this.errorMessage.set(
        'Şifre 8-12 karakter olmalı ve en az bir harf, rakam ve özel karakter içermelidir.'
      );
      return;
    }

    this.authService.resetPassword(this.email, this.token, password).subscribe({
      next: (response) => {
        this.successMessage.set(response.message);
        this.form.reset();
      },
      error: (error) => this.errorMessage.set(
        error?.error?.message ?? 'Şifre sıfırlanamadı.'
      )
    });
  }
}
