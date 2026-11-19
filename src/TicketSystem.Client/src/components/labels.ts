import type { TicketSortField, TicketStatus } from '../api/types'

/** Estonian names of the ticket states. */
export const statusLabels: Record<TicketStatus, string> = {
  New: 'Uus',
  InProgress: 'Töös',
  Resolved: 'Lahendatud',
}

export const sortLabels: Record<TicketSortField, string> = {
  CreatedAt: 'Loomise aeg',
  Deadline: 'Tähtaeg',
  Priority: 'Prioriteet',
}

const dateTime = new Intl.DateTimeFormat('et-EE', { dateStyle: 'short', timeStyle: 'short' })

/** Formats a UTC time from the API in the time zone of the browser. */
export function formatDateTime(value: string): string {
  return dateTime.format(new Date(value))
}
