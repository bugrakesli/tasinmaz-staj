import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import { UserService } from '../../services/user.service';
import {
  User,
  UserCreateRequest,
  UserUpdateRequest
} from '../../models/user.model';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './user-management.html',
  styleUrl: './user-management.scss'
})
export class UserManagement implements OnInit {
  users = signal<User[]>([]);
  loading = signal(true);
  saving = signal(false);
  deletingId = signal<number | null>(null);
  errorMessage = signal('');

  totalCount = signal(0);
  pageNumber = signal(1);
  pageSize = signal(10);

  editingId = signal<number | null>(null);

  readonly passwordPattern =
    /^(?=.*[a-zA-Z])(?=.*\d)(?=.*[\W_]).{8,12}$/;

  readonly userForm: FormGroup;

  get totalPages(): number {
    return Math.max(
      1,
      Math.ceil(this.totalCount() / this.pageSize())
    );
  }

  constructor(
    private userService: UserService,
    private fb: FormBuilder,
    private toastService: ToastService
  ) {
    this.userForm = this.fb.nonNullable.group({
      email: [
        '',
        [Validators.required, Validators.email]
      ],
      password: [
        '',
        [
          Validators.required,
          Validators.pattern(this.passwordPattern)
        ]
      ],
      role: [
        'User',
        Validators.required
      ]
    });
  }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.userService
      .getAll(this.pageNumber(), this.pageSize())
      .subscribe({
        next: (result) => {
          this.users.set(result.data);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err) => {
          this.errorMessage.set(
            err?.error?.message ??
              'Kullanıcılar yüklenirken bir hata oluştu.'
          );
          this.loading.set(false);
        }
      });
  }

  submit(): void {
    this.errorMessage.set('');

    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    const value = this.userForm.getRawValue();

    if (this.editingId() === null) {
      const request: UserCreateRequest = {
        email: value.email.trim(),
        password: value.password,
        role: value.role
      };

      this.userService.create(request).subscribe({
        next: (result) => this.finishSave(result.message),
        error: (err) => this.handleSaveError(err)
      });

      return;
    }

    const request: UserUpdateRequest = {
      email: value.email.trim(),
      role: value.role
    };

    if (value.password) {
      request.password = value.password;
    }

    this.userService
      .update(this.editingId()!, request)
      .subscribe({
        next: (result) => this.finishSave(result.message),
        error: (err) => this.handleSaveError(err)
      });
  }

  edit(user: User): void {
    this.editingId.set(user.id);
    this.errorMessage.set('');

    this.userForm.reset({
      email: user.email,
      password: '',
      role: user.role
    });

    // Güncellemede şifre zorunlu değil.
    this.userForm.controls['password'].clearValidators();

    this.userForm.controls['password'].addValidators(
      Validators.pattern(this.passwordPattern)
    );

    this.userForm.controls['password'].updateValueAndValidity();
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.resetForm();
  }

  remove(user: User): void {
    const confirmed = window.confirm(
      `"${user.email}" kullanıcısını silmek istediğinize emin misiniz? ` +
      'Bu kullanıcıya ait tüm taşınmazlar da silinecektir.'
    );

    if (!confirmed) {
      return;
    }

    this.deletingId.set(user.id);
    this.errorMessage.set('');

    this.userService.delete(user.id).subscribe({
      next: (result) => {
        this.deletingId.set(null);
        this.toastService.success(result.message);
        this.loadUsers();
      },
      error: (err) => {
        this.deletingId.set(null);
        this.toastService.error(
          err?.error?.message ??
            'Kullanıcı silinirken bir hata oluştu.'
        );
      }
    });
  }

  previousPage(): void {
    if (this.pageNumber() <= 1) {
      return;
    }

    this.pageNumber.update(
      page => page - 1
    );

    this.loadUsers();
  }

  nextPage(): void {
    if (this.pageNumber() >= this.totalPages) {
      return;
    }

    this.pageNumber.update(
      page => page + 1
    );

    this.loadUsers();
  }

  private finishSave(message: string): void {
    this.saving.set(false);
    this.toastService.success(message);
    this.editingId.set(null);

    this.resetForm();
    this.loadUsers();
  }

  private handleSaveError(err: any): void {
    this.saving.set(false);

    this.toastService.error(
      err?.error?.message ??
        'Kullanıcı kaydedilirken bir hata oluştu.'
    );
  }

  private resetForm(): void {
    this.userForm.reset({
      email: '',
      password: '',
      role: 'User'
    });

    this.userForm.controls['password'].setValidators([
      Validators.required,
      Validators.pattern(this.passwordPattern)
    ]);

    this.userForm.controls['password'].updateValueAndValidity();
  }
}