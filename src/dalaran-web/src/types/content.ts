export enum ContentType {
  Text = 'TextContent',
  Image = 'ImageContent',
}

export type TextContent = {
  $type: ContentType.Text
  text: string
}

export type ImageContent = {
  $type: ContentType.Image
  uri: string
}

export type ChatMessageContent = {
  role: { label: 'system' | 'assistant' | 'user' | 'tool' }
  items: (TextContent | ImageContent)[]
}
