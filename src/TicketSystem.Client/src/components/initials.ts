/** Two letters for the round avatar, taken from the display name. */
export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  const letters = parts.length > 1 ? parts[0][0] + parts[parts.length - 1][0] : name.trim().slice(0, 2)
  return letters.toUpperCase()
}

/** A class name that gives every person the same avatar colour each time. */
export function avatarTone(userId: number): string {
  return `tone-${Math.abs(userId) % 6}`
}
