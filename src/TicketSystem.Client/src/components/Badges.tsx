import type { Priority, TicketStatus } from '../api/types'
import { statusLabels } from './labels'

export function StatusBadge({ status }: { status: TicketStatus }) {
  return <span className={`badge status-${status.toLowerCase()}`}>{statusLabels[status]}</span>
}

/** The priority as a coloured dot and its name. */
export function PriorityBadge({ priority }: { priority: Priority }) {
  return (
    <span className={`priority-tag priority-${priority.level}`}>
      <span className="dot" aria-hidden="true" />
      {priority.name}
    </span>
  )
}
