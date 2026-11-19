import { http } from './http'
import type { Category, PagedResult, Priority, Ticket, TicketQuery, UserSummary } from './types'

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
}

export const lookupApi = {
  categories: () => http.get<Category[]>('/api/categories'),
  priorities: () => http.get<Priority[]>('/api/priorities'),
  supportStaff: () => http.get<UserSummary[]>('/api/users/support'),
}
