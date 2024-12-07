import { HOST } from '@/api/host'
import type { ChatMeta } from '@/types/chatMeta.ts'
import type { StreamingChatResponse } from '@/types/chatResponse'
import type { ChatMessageContent } from '@/types/content'
import type { UploadImagesResult } from '@/types/uploadImagesResult'

export async function create(): Promise<ChatMeta> {
  const path = 'chat'
  const url = new URL(path, HOST)

  try {
    const response = await fetch(url, {
      method: 'POST',
    })

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }

    return (await response.json()) as ChatMeta
  } catch (error) {
    console.error('Error:', error)
    throw error
  }
}

export async function addMessages(id: string, contents: ChatMessageContent[]): Promise<void> {
  const path = `chat/${id}`
  const url = new URL(path, HOST)

  try {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(contents),
    })

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
  } catch (error) {
    console.error('Error:', error)
    throw error
  }
}

export async function run(id: string, callback: (text: string) => void): Promise<void> {
  const path = `chat/${id}/run`
  const url = new URL(path, HOST)

  try {
    const response = await fetch(url, {
      method: 'POST',
    })

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }

    const reader = response.body!.getReader()
    const decoder = new TextDecoder()

    while (true) {
      const { done, value } = await reader.read()
      if (done) {
        break
      }

      const text = decoder.decode(value, { stream: true })
      if (text) {
        callback(parseStreamingChatResponses(text))
      }
    }
  } catch (error) {
    console.error('Error:', error)
    throw error
  }
}

export async function uploadImages(
  id: string,
  files: FileList | File[],
): Promise<UploadImagesResult> {
  const path = `chat/${id}/images`
  const url = new URL(path, HOST)

  try {
    const formData = new FormData()
    for (const file of files) {
      formData.append('images', file)
    }

    const response = await fetch(url, {
      method: 'POST',
      body: formData,
    })

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }

    return (await response.json()) as UploadImagesResult
  } catch (error) {
    console.error('Error:', error)
    throw error
  }
}

function parseStreamingChatResponses(text: string): string {
  let result = ''
  switch (text[0]) {
    case '[':
      result = text
      break
    case '{':
      result = '[' + text
      break
    case ',':
      result = '[' + text.slice(1)
      break
    case ']':
      return ''
    default:
      throw new Error('Unrecognized text start')
  }

  switch (result.slice(-1)) {
    case ']':
      break
    case ',':
      result = result.slice(0, -1) + ']'
      break
    case '}':
      result += ']'
      break
    default:
      throw new Error('Unrecognized text end')
  }

  const responses: StreamingChatResponse[] = JSON.parse(result)
  return responses.map((r) => r.content).join('')
}
