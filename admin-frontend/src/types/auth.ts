export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  access_token?: string;
  refresh_token?: string;
  token_type?: string;
  expires_in?: number;
  user?: {
    id: string;
    username: string;
    email: string;
    full_name: string;
    roles: string[];
    tenant_id: number;
    is_active: boolean;
  };
  error?: string;
}

export interface User {
  id: string;
  username: string;
  email: string;
  full_name: string;
  roles: string[];
  tenant_id: number;
  is_active: boolean;
  role?: string; // Computed from roles array for compatibility
  firstName?: string;
  lastName?: string;
  createdAt?: string;
  lastLoginAt?: string;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  role: string;
}

export interface UpdateUserRequest {
  id: string;
  username?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  role?: string;
  isActive?: boolean;
}
