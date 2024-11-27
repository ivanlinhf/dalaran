import { HOST } from '@/api/host'
import type { StreamingChatResponse } from '@/types/chatResponse'
import type { TextContent, ImageContent } from '@/types/content'

export async function chat(
  data: (TextContent | ImageContent)[],
  callback: (text: string) => void,
): Promise<void> {
  const path = 'llm/chat'
  const url = new URL(path, HOST)

  try {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
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
