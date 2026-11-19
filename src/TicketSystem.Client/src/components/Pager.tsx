interface Props {
  page: number
  totalPages: number
  totalCount: number
  onChange: (page: number) => void
}

export function Pager({ page, totalPages, totalCount, onChange }: Props) {
  return (
    <div className="pager">
      <span className="muted">Kokku {totalCount}</span>
      <div className="pager-buttons">
        <button type="button" className="button secondary small" disabled={page <= 1} onClick={() => onChange(page - 1)}>
          Eelmine
        </button>
        <span>
          Leht {page} / {Math.max(totalPages, 1)}
        </span>
        <button
          type="button"
          className="button secondary small"
          disabled={page >= totalPages}
          onClick={() => onChange(page + 1)}
        >
          Järgmine
        </button>
      </div>
    </div>
  )
}
