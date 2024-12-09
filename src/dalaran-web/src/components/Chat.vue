<script setup lang="ts">
import type { Ref } from 'vue'
import { computed, onMounted, ref } from 'vue'
import { useFileDialog } from '@vueuse/core'
import { addMessages, create, run, uploadImages } from '@/api/chat'
import { AuthorRole } from '@/types/authorRole'
import { ChatMessageType } from '@/types/chatMessage'
import type { ChatMessage } from '@/types/chatMessage'
import { ContentType } from '@/types/content'
import type { ChatMessageContent, ImageContent } from '@/types/content'

import ChatViewer from '@/components/ChatViewer.vue'
import ImageViewer from '@/components/ImageViewer.vue'
import TextZoom from '@/components/TextZoom.vue'
import UrlInput from '@/components/UrlInput.vue'

const threadId: Ref<string> = ref('')

const uploadedImageUrls: Ref<string[]> = ref([])
const inputText: Ref<string | null> = ref('')
const chatMessages: Ref<ChatMessage[]> = ref<ChatMessage[]>([])

const isLoading = ref(false)
const isZoom = ref(false)
const isFromUrl = ref(false)
const showUploaded = ref(false)

const isInputTextValid = computed(() => inputText.value && inputText.value.trim())
const sendButtonEnabled = computed(() => !isLoading.value && isInputTextValid.value)

let responseContent: ChatMessageContent | null = null

const { open: openFileDialog, onChange: onImagesSelected } = useFileDialog({
  accept: 'image/*',
})

onImagesSelected(async (x) => {
  if (x) {
    const result = await uploadImages(threadId.value, x)
    uploadedImageUrls.value.push(...result.urls)
  }
})

async function uploadFromUrls(urlsText: string) {
  if (urlsText) {
    const urls = urlsText.split('\n').filter((x) => x)
    const files = await Promise.all(
      urls.map(async (x) => {
        const fileName = x.split('/').pop()
        const response = await fetch(x)
        const blob = await response.blob()
        return new File([blob], fileName!, { type: blob.type })
      }),
    )

    const result = await uploadImages(threadId.value, files)
    uploadedImageUrls.value.push(...result.urls)
  }
}

async function startNew() {
  isLoading.value = true

  uploadedImageUrls.value = []
  inputText.value = ''
  chatMessages.value = []

  const meta = await create()
  threadId.value = meta.threadId

  isLoading.value = false
}

async function sendMessage() {
  isZoom.value = false
  isLoading.value = true

  if (isInputTextValid.value) {
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
  <div class="container">
    <chat-viewer class="chat-viewer-container" v-model="chatMessages" :isLoading="isLoading" />
    <div class="input-container q-pt-lg">
      <q-input
        class="input"
        input-class="input-height"
        outlined
        clearable
        clear-icon="close"
        autogrow
        v-model="inputText"
        :disable="isLoading || !threadId"
        @keydown.shift.enter.prevent="sendMessage"
      >
        <template #before>
          <q-btn flat icon="add" @click="startNew">
            <q-tooltip class="text-body2">Start new conversation</q-tooltip>
          </q-btn>
        </template>
        <template #append>
          <q-btn flat icon="fullscreen" @click="() => (isZoom = true)">
            <q-tooltip class="text-body2">Zoom in</q-tooltip>
          </q-btn>
          <q-btn flat icon="image">
            <q-tooltip class="text-body2">Upload images</q-tooltip>
            <q-menu>
              <q-list>
                <q-item clickable v-close-popup>
                  <q-item-section @click="() => openFileDialog()">From File...</q-item-section>
                </q-item>
                <q-item clickable v-close-popup>
                  <q-item-section @click="() => (isFromUrl = true)">From Url...</q-item-section>
                </q-item>
                <q-separator />
                <q-item clickable v-close-popup :disable="uploadedImageUrls.length == 0">
                  <q-item-section @click="showUploaded = uploadedImageUrls.length > 0">
                    View Uploaded
                  </q-item-section>
                </q-item>
              </q-list>
            </q-menu>
          </q-btn>
        </template>
        <template #after>
          <q-btn flat icon="send" :disable="!sendButtonEnabled" @click="sendMessage">
            <q-tooltip class="text-body2">Send message</q-tooltip>
          </q-btn>
        </template>

        <q-inner-loading :showing="isLoading" color="primary" />
      </q-input>
    </div>
    <text-zoom
      v-model:showing="isZoom"
      v-model:text="inputText"
      @submit="async () => await sendMessage()"
    />
    <url-input v-model="isFromUrl" @submit="uploadFromUrls" />
    <image-viewer v-model="showUploaded" :urls="uploadedImageUrls" />
  </div>
</template>

<style scoped lang="scss">
* {
  font-size: large;
}

div {
  margin: auto;
  white-space: pre-wrap;
}

.container {
  height: 90vh;
  width: 90vw;
}

.chat-viewer-container {
  height: 80%;
  width: 60%;
  margin-top: 5%;
}

.input-container {
  height: 15%;
  width: 50%;
}

.input {
  :deep(.q-field__before) {
    align-self: flex-end;
  }

  :deep(.q-field__append) {
    align-self: flex-end;
  }

  :deep(.q-field__after) {
    align-self: flex-end;
  }
}

:deep(.input-height) {
  max-height: 15vh;
}

:deep(code) {
  color: blue;
}
</style>
