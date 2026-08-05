import { Routes } from '@angular/router';

import { Login } from './components/login/login';
import { PropertyList } from './components/property-list/property-list';

export const routes: Routes = [
  {
    path: 'login',
    component: Login
  },
  {
    path: 'properties',
    component: PropertyList
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  }
];