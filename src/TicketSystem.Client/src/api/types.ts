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

export type TicketStatus = 'New' | 'InProgress' | 'Resolved'

export interface Category {
  id: number
  name: string
}

export interface Priority {
  id: number
  name: string
  level: number
  deadlineHours: number
}

/** A person on a ticket. Has no email address. */
export interface UserSummary {
  id: number
  displayName: string
}

export interface Ticket {
  id: number
  title: string
  description: string
  status: TicketStatus
  category: Category
  priority: Priority
  createdBy: UserSummary
  assignee: UserSummary | null
  createdAt: string
  deadline: string
  resolvedAt: string | null
  isOverdue: boolean
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type TicketSortField = 'CreatedAt' | 'Deadline' | 'Priority'

/** Filters, sorting and paging of the ticket list. A missing filter means "all". */
export interface TicketQuery {
  status?: TicketStatus
  priority?: number
  assignee?: number
  categoryId?: number
  search?: string
  sortBy: TicketSortField
  descending: boolean
  page: number
  pageSize: number
}

export interface TicketComment {
  id: number
  text: string
  author: UserSummary
  createdAt: string
}

/** Data of a new ticket, also used when a ticket is edited. */
export interface TicketInput {
  title: string
  description: string
  categoryId: number
  priorityId: number
}
