import { Component, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.html'
})
export class ForgotPassword {
  submitted = signal(false);
  errorMessage = signal('');

  form;

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService
  ) {
    this.form = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set('');

    this.authService.forgotPassword(this.form.value.email ?? '').subscribe({
      next: () => this.submitted.set(true),
      error: (error) => this.errorMessage.set(
        error?.error?.message ?? 'Şifre sıfırlama e-postası gönderilemedi.'
      )
    });
  }
}
