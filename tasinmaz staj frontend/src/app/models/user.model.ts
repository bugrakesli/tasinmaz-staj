export interface User {
  id: number;
  email: string;
  role: string;
}

export interface UserListResponse {
  totalCount: number;
  data: User[];
}

export interface UserCreateRequest {
  email: string;
  password: string;
  role: string;
}

export interface UserUpdateRequest {
  email: string;
  password?: string;
  role: string;
}
