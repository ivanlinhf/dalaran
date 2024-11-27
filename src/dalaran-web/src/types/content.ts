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
  Uri: string
}
