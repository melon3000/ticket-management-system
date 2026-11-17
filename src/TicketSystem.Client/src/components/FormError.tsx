import { ApiError } from '../api/http'

/** Shows what went wrong with a form: the main message and the message of every field. */
export function FormError({ error }: { error: unknown }) {
  if (!error) return null
  if (!(error instanceof ApiError)) {
    return (
      <div className="alert" role="alert">
        Midagi läks valesti. Proovi uuesti.
      </div>
    )
  }
  const fieldMessages = Object.values(error.fieldErrors).flat()
  return (
    <div className="alert" role="alert">
      {fieldMessages.length === 0 ? (
        error.message
      ) : (
        <ul>
          {fieldMessages.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
