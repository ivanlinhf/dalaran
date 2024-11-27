import { HOST } from '@/api/host'
import type { TextContent, ImageContent } from '@/types/content'

export async function chat(data: (TextContent | ImageContent)[]): Promise<void> {
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

    const result = await response.json()
    return result
  } catch (error) {
    console.error('Error:', error)
    throw error
  }
}
