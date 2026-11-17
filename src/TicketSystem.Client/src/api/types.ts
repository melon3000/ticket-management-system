// Shapes of the JSON that the Web API sends and receives (see TicketSystem.Api/Dtos).

export type UserRole = 'User' | 'Support' | 'Admin' | 'Service'

export interface User {
  id: number
  email: string
  displayName: string
  role: UserRole
}

export interface AuthResponse {
  token: string
  expiresAt: string
  user: User
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  displayName: string
  password: string
}
