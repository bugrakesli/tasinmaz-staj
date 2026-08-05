import { Component } from '@angular/core';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import {
  Router
} from '@angular/router';

import {
  LoginService
} from './login.service';

import {
  LoginRequest
} from '../../models/login-request.model';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {

  loginError = '';

  loginForm;

  constructor(
    private formBuilder: FormBuilder,
    private loginService: LoginService,
    private router: Router
  ) {

    this.loginForm =
      this.formBuilder.group({

        email: [
          '',
          [
            Validators.required,
            Validators.email
          ]
        ],

        password: [
          '',
          [
            Validators.required
          ]
        ]

      });
  }

  onSubmit(): void {

    if (this.loginForm.invalid) {

      this.loginForm.markAllAsTouched();

      return;
    }

    this.loginError = '';

    const loginRequest: LoginRequest = {
      email:
        this.loginForm.value.email ?? '',

      password:
        this.loginForm.value.password ?? ''
    };

    this.loginService
      .login(loginRequest)
      .subscribe({

        next: (response) => {

          localStorage.setItem(
            'token',
            response.token
          );

          localStorage.setItem(
            'role',
            response.role
          );

          localStorage.setItem(
            'email',
            response.email
          );

          console.log(
            'Giriş başarılı:',
            response
          );

          this.router.navigate(
            ['/properties']
          );
        },

        error: () => {

          this.loginError =
            'E-posta veya şifre hatalı.';
        }

      });
  }
}