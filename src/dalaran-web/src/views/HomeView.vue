<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { addMessages, run } from '@/api/chat'
import { useThread } from '@/composables/thread'
import { AuthorRole } from '@/types/authorRole'
import { ChatMessageType } from '@/types/chatMessage'
import type { ChatMessage } from '@/types/chatMessage'
import { ContentType } from '@/types/content'
import type { ChatMessageContent, ImageContent } from '@/types/content'

import ChatInput from '@/components/ChatInput.vue'
import ChatViewer from '@/components/ChatViewer.vue'

const uploadedImageUrls = ref<string[]>([])
const inputText = ref<string | null>('')
const chatMessages = ref<ChatMessage[]>([])

const isLoading = ref(false)

let responseContent: ChatMessageContent | null = null

const { threadId, newThread } = useThread()

async function startNew() {
  isLoading.value = true

  uploadedImageUrls.value = []
  inputText.value = ''
  chatMessages.value = []

  await newThread()

  isLoading.value = false
}

async function sendMessage() {
  isLoading.value = true

  if (inputText.value) {
    const text = inputText.value!

    // Add last response and input.
    const imageContents: ImageContent[] = uploadedImageUrls.value.map((x) => ({
      $type: ContentType.Image,
      uri: x,
    }))
    const content: ChatMessageContent = {
      role: { label: 'user' },
      items: [...imageContents, { $type: ContentType.Text, text: text }],
    }
    const contents = responseContent ? [responseContent, content] : [content]
    await addMessages(threadId.value, contents)

    // Clear last response and input.
    responseContent = null
    uploadedImageUrls.value = []
    inputText.value = ''

    // Add input to chat component.
    chatMessages.value.push(
      ...imageContents.map((x) => {
        return { author: AuthorRole.User, content: x.uri, type: ChatMessageType.Image }
      }),
    )
    chatMessages.value.push({ author: AuthorRole.User, content: text, type: ChatMessageType.Text })
    chatMessages.value.push({
      author: AuthorRole.Assistant,
      content: '',
      type: ChatMessageType.Text,
    })

    // Get response.
    const responseMessage = chatMessages.value[chatMessages.value.length - 1]
    await run(threadId.value, (x) => {
      responseMessage.content += x
    })

    // Store response.
    responseContent = {
      role: { label: 'assistant' },
      items: [{ $type: ContentType.Text, text: responseMessage.content }],
    }
  }

  isLoading.value = false
}

onMounted(async () => {
  await startNew()
})
</script>

<template>
  <main>
    <div class="container">
      <chat-viewer class="chat-viewer-container" v-model="chatMessages" :isLoading="isLoading" />
      <chat-input
        class="chat-input-container q-pt-lg"
        v-model:text="inputText"
        v-model:uploadedImageUrls="uploadedImageUrls"
        :threadId="threadId"
        :isLoading="isLoading"
        @new="async () => await startNew()"
        @submit="async () => await sendMessage()"
      />
    </div>
  </main>
</template>

<style scoped lang="scss">
.container {
  height: 90vh;
  width: 90vw;
  margin: auto;
}

.chat-viewer-container {
  height: 80%;
  width: 65%;
  margin: 5% auto auto auto;
}

.chat-input-container {
  height: 15%;
  width: 50%;
}
</style>
