import { ref } from 'vue'
import { create } from '@/api/chat'

export function useThread() {
  const threadId = ref<string>('')

  async function newThread() {
    const meta = await create()
    threadId.value = meta.threadId
  }

  return {
    threadId,
    newThread,
  }
}
