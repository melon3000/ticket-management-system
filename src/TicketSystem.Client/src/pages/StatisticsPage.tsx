import type { ResolutionTime } from '../api/types'
import { useStatistics } from '../api/useStatistics'
import { FormError } from '../components/FormError'
import { useRefreshOnFocus } from '../components/useRefreshOnFocus'

/** Writes hours as days, hours and minutes, for example "1 p 3 h 20 min". */
function formatDuration(hours: number | null): string {
  if (hours === null) return '—'
  const totalMinutes = Math.round(hours * 60)
  const days = Math.floor(totalMinutes / 1440)
  const h = Math.floor((totalMinutes % 1440) / 60)
  const minutes = totalMinutes % 60
  const parts = [days > 0 ? `${days} p` : '', h > 0 ? `${h} h` : '', minutes > 0 || totalMinutes === 0 ? `${minutes} min` : '']
  return parts.filter(Boolean).join(' ')
}

function TimeTable({ title, nameLabel, rows }: { title: string; nameLabel: string; rows: ResolutionTime[] }) {
  const longest = Math.max(0, ...rows.map((row) => row.averageHours ?? 0))
  return (
    <div className="card table-card">
      <h2 className="table-title">{title}</h2>
      <div className="table-scroll">
        <table>
          <thead>
            <tr>
              <th>{nameLabel}</th>
              <th>Lahendatud tikette</th>
              <th>Keskmine lahendamisaeg</th>
              <th className="bar-col" aria-hidden="true" />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id}>
                <td className="title-cell">{row.name}</td>
                <td>{row.resolvedCount}</td>
                <td title={row.averageHours === null ? undefined : `${row.averageHours} h`}>
                  {formatDuration(row.averageHours)}
                </td>
                <td className="bar-col">
                  {row.averageHours !== null && longest > 0 && (
                    <div className="bar" style={{ width: `${(row.averageHours / longest) * 100}%` }} />
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

export function StatisticsPage() {
  const { data, error, loading, reload } = useStatistics()
  useRefreshOnFocus(reload)

  return (
    <section className="stats">
      <div className="page-head">
        <h1>Statistika</h1>
        <button type="button" className="button secondary" onClick={reload} disabled={loading}>
          Värskenda
        </button>
      </div>
      <p className="muted">
        Aeg loomisest lahendamiseni. Arvesse lähevad ainult tikettid, mis on praegu lahendatud.
      </p>

      <FormError error={error} />

      {data === null ? (
        <p className="muted">{loading ? 'Laen…' : ''}</p>
      ) : (
        <>
          <div className="summary">
            <div className="card">
              <span className="muted">Lahendatud tikette</span>
              <strong>{data.resolvedCount}</strong>
            </div>
            <div className="card">
              <span className="muted">Keskmine lahendamisaeg</span>
              <strong>{formatDuration(data.averageHours)}</strong>
            </div>
          </div>
          <TimeTable title="Kategooriate kaupa" nameLabel="Kategooria" rows={data.byCategory} />
          <TimeTable title="Tugiisikute kaupa" nameLabel="Tugiisik" rows={data.bySupport} />
        </>
      )}
    </section>
  )
}
