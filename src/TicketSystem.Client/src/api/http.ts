const API_URL: string = import.meta.env.VITE_API_URL ?? 'http://localhost:5119'

/** An error answer of the Web API (RFC 7807 problem details). */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

let currentToken: string | null = null
let unauthorizedHandler: (() => void) | null = null

/** Sets the token that is sent with every request. */
export function setAuthToken(token: string | null): void {
  currentToken = token
}

/** Registers a function that is called when the API answers 401 to a request that carried a token. */
export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return null
  }
}

async function toError(response: Response): Promise<ApiError> {
  const problem = await readProblem(response)
  const message = problem?.detail ?? problem?.title ?? `Päring ebaõnnestus (${response.status})`
  return new ApiError(response.status, message, problem?.errors ?? {})
}

export async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (currentToken) headers.Authorization = `Bearer ${currentToken}`

  let response: Response
  try {
    response = await fetch(`${API_URL}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(0, 'Serveriga ei saa ühendust. Kontrolli, et API töötab.')
  }

  if (!response.ok) {
    if (response.status === 401 && currentToken) unauthorizedHandler?.()
    throw await toError(response)
  }
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export const http = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, body),
  delete: <T = void>(path: string) => request<T>('DELETE', path),
}
