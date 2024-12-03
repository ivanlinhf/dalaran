import type { AuthorRole } from '@/types/authorRole.ts'

export type ChatMessage = {
  author: AuthorRole
  text: string
}
