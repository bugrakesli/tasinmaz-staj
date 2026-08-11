import { Routes } from '@angular/router';

import { Login } from './components/login/login';
import { PropertyList } from './components/property-list/property-list';
import { PropertyForm } from './components/property-form/property-form';
import { GeometryAnalysis } from './components/geometry-analysis/geometry-analysis';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';

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
    path: 'properties/new',
    component: PropertyForm,
    canActivate: [authGuard]
  },
  {
    path: 'properties/:id/edit',
    component: PropertyForm,
    canActivate: [authGuard]
  },
  {
    path: 'analysis',
    component: GeometryAnalysis,
    canActivate: [authGuard]
  },
  {
    path: 'users',
    loadComponent: () =>
      import('./components/user-management/user-management').then(m => m.UserManagement),
    canActivate: [adminGuard]
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  }
];