import type { AuthorRole } from '@/types/authorRole.ts'

export enum ChatMessageType {
  Text,
  Image,
  Html,
}

export type ChatMessage = {
  author: AuthorRole
  content: string
  type: ChatMessageType
}
