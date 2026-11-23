import type { TicketLogEntry, TicketStatus } from '../api/types'
import { formatDateTime, statusLabels } from './labels'

/** The Estonian name of a state, or the text as it is when it is not a state. */
function statusName(value: string | null): string {
  return value !== null && value in statusLabels ? statusLabels[value as TicketStatus] : (value ?? '—')
}

/** Describes what one log entry changed. */
function describe(entry: TicketLogEntry): string {
  switch (entry.action) {
    case 'Created':
      return 'lõi tiketi'
    case 'Assigned':
      return 'võttis tiketi töösse'
    case 'StatusChanged':
      return `muutis olekut: ${statusName(entry.oldValue)} → ${statusName(entry.newValue)}`
    case 'Escalated':
      return `eskaleeris tiketi, prioriteet: ${entry.oldValue ?? '—'} → ${entry.newValue ?? '—'}`
    default:
      return entry.action
  }
}

export function ChangeLog({ entries }: { entries: TicketLogEntry[] }) {
  return (
    <div className="card">
      <h2>Muudatuste ajalugu</h2>
      {entries.length === 0 ? (
        <p className="muted">Muudatusi pole.</p>
      ) : (
        <ol className="timeline">
          {entries.map((entry) => (
            <li key={entry.id}>
              <span className="muted time">{formatDateTime(entry.createdAt)}</span>
              <span>
                <strong>{entry.user.displayName}</strong> {describe(entry)}
              </span>
            </li>
          ))}
        </ol>
      )}
    </div>
  )
}
