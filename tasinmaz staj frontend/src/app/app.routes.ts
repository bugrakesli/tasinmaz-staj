import { Routes } from '@angular/router';

import { Login } from './components/login/login';
import { PropertyList } from './components/property-list/property-list';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: Login
  },
  {
    path: 'properties',
    component: PropertyList,
    canActivate: [authGuard]
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  }
];