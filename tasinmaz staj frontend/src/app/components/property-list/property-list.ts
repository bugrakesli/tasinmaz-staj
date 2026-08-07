import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

import { PropertyService } from '../../services/property.service';
import { Property } from '../../models/property.model';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-property-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './property-list.html',
  styleUrl: './property-list.scss'
})
export class PropertyList implements OnInit {
  properties: Property[] = [];
  loading = true;
  errorMessage = '';

  constructor(
    private propertyService: PropertyService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.propertyService.getAll().subscribe({
      next: (result) => {
        this.properties = result.data;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Taşınmazlar yüklenirken bir hata oluştu.';
        this.loading = false;
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }
}