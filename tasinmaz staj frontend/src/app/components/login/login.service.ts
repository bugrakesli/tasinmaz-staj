import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { LoginRequest } from '../../models/login-request.model';
import { LoginResponse } from '../../models/login-response.model';

@Injectable({
  providedIn: 'root'
})
export class LoginService {

  private readonly apiUrl =
    'https://localhost:5001/api/Auth/login';

  constructor(
    private http: HttpClient
  ) {
  }

  login(
    loginRequest: LoginRequest
  ): Observable<LoginResponse> {

    return this.http.post<LoginResponse>(
      this.apiUrl,
      loginRequest
    );
  }
}