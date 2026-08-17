import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        // Token expired or invalid — force logout
        authService.logout();
      } else if (error.status === 403) {
        // Forbidden — user doesn't have permission
        console.warn('Access denied:', req.url);
      }
      // Let the component handle the error further
      return throwError(() => error);
    })
  );
};
