import type { Priority, TicketStatus } from '../api/types'
import { statusLabels } from './labels'

export function StatusBadge({ status }: { status: TicketStatus }) {
  return <span className={`badge status-${status.toLowerCase()}`}>{statusLabels[status]}</span>
}

export function PriorityBadge({ priority }: { priority: Priority }) {
  return <span className={`badge priority-${priority.level}`}>{priority.name}</span>
}
