import { http } from './http'
import type { AuthResponse, LoginRequest, RegisterRequest } from './types'

export const authApi = {
  login: (data: LoginRequest) => http.post<AuthResponse>('/api/auth/login', data),
  register: (data: RegisterRequest) => http.post<AuthResponse>('/api/auth/register', data),
}
