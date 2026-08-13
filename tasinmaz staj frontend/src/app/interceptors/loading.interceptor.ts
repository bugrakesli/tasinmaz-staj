import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LoadingService } from '../services/loading.service';
import { finalize } from 'rxjs';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);
  
  // Skip loading indicator for background/silent requests if any, by checking headers
  if (req.headers.has('X-Skip-Loading')) {
    const newReq = req.clone({ headers: req.headers.delete('X-Skip-Loading') });
    return next(newReq);
  }

  loadingService.show();

  return next(req).pipe(
    finalize(() => {
      loadingService.hide();
    })
  );
};
