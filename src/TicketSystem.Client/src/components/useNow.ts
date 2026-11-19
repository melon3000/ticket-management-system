import { useEffect, useState } from 'react'

/** The current time in milliseconds. It moves on once a minute, so deadlines turn red when they pass. */
export function useNow(): number {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 60_000)
    return () => clearInterval(timer)
  }, [])
  return now
}
