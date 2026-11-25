import { http } from './http'
import type {
  Category,
  PagedResult,
  Priority,
  ResolutionStatistics,
  Ticket,
  TicketComment,
  TicketInput,
  TicketLogEntry,
  TicketQuery,
  TicketStatus,
  User,
  UserRole,
  UserSummary,
} from './types'

function toQueryString(query: TicketQuery): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === '') continue
    params.set(key, String(value))
  }
  return params.toString()
}

export const ticketsApi = {
  list: (query: TicketQuery) => http.get<PagedResult<Ticket>>(`/api/tickets?${toQueryString(query)}`),
  get: (id: number) => http.get<Ticket>(`/api/tickets/${id}`),
  log: (id: number) => http.get<TicketLogEntry[]>(`/api/tickets/${id}/log`),
  create: (input: TicketInput) => http.post<Ticket>('/api/tickets', input),
  update: (id: number, input: TicketInput) => http.put<Ticket>(`/api/tickets/${id}`, input),
  assign: (id: number) => http.post<Ticket>(`/api/tickets/${id}/assign`),
  changeStatus: (id: number, status: TicketStatus) => http.patch<Ticket>(`/api/tickets/${id}/status`, { status }),
  remove: (id: number) => http.delete(`/api/tickets/${id}`),
}

export const commentsApi = {
  list: (ticketId: number) => http.get<TicketComment[]>(`/api/tickets/${ticketId}/comments`),
  add: (ticketId: number, text: string) => http.post<TicketComment>(`/api/tickets/${ticketId}/comments`, { text }),
}

export const lookupApi = {
  categories: () => http.get<Category[]>('/api/categories'),
  priorities: () => http.get<Priority[]>('/api/priorities'),
  supportStaff: () => http.get<UserSummary[]>('/api/users/support'),
}

export const statisticsApi = {
  resolutionTimes: () => http.get<ResolutionStatistics>('/api/statistics/resolution-time'),
}

export const usersApi = {
  list: () => http.get<User[]>('/api/users'),
  changeRole: (id: number, role: UserRole) => http.patch<User>(`/api/users/${id}/role`, { role }),
}
