<script setup lang="ts">
import { QScrollArea } from 'quasar'
import type { Ref } from 'vue'
import { computed, onMounted, ref, watch } from 'vue'
import { addMessages, create, run, uploadImages } from '@/api/chat'
import { AuthorRole } from '@/types/authorRole'
import type { ChatMessage } from '@/types/chatMessage'
import { ContentType } from '@/types/content'
import type { ChatMessageContent, ImageContent } from '@/types/content'
import { useClipboard, useFileDialog } from '@vueuse/core'

const chatScroll = ref<QScrollArea | null>(null)

const threadId: Ref<string> = ref('')

const imageUrls: Ref<string[]> = ref([])
const inputText: Ref<string | null> = ref('')
const chatMessages: Ref<ChatMessage[]> = ref<ChatMessage[]>([])

const isLoading = ref(false)
const isZoom = ref(false)

const isInputTextValid = computed(() => inputText.value && inputText.value.trim())
const sendButtonEnabled = computed(() => !isLoading.value && isInputTextValid.value)

let responseContent: ChatMessageContent | null = null

const { copy: copyText } = useClipboard()
const { open: openFileDialog, onChange: onImagesSelected } = useFileDialog({
  accept: 'image/*',
})

onImagesSelected(async (x) => {
  if (x) {
    const result = await uploadImages(threadId.value, x)
    imageUrls.value.push(...result.urls)
  }
})

async function startNew() {
  isLoading.value = true

  imageUrls.value = []
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
    const imageContents: ImageContent[] = imageUrls.value.map((x) => ({
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
    imageUrls.value = []
    inputText.value = ''

    // Add input text to chat component.
    chatMessages.value.push({ author: AuthorRole.User, text: text })
    chatMessages.value.push({ author: AuthorRole.Assistant, text: '' })

    // Get response.
    const responseMessage = chatMessages.value[chatMessages.value.length - 1]
    await run(threadId.value, (x) => {
      responseMessage.text += x
    })

    // Store response.
    responseContent = {
      role: { label: 'assistant' },
      items: [{ $type: ContentType.Text, text: responseMessage.text }],
    }
  }

  isLoading.value = false
}

onMounted(async () => {
  await startNew()
})

watch(
  chatMessages,
  () => {
    if (chatScroll.value) {
      chatScroll.value.setScrollPercentage('vertical', 1.0)
    }
  },
  { deep: true },
)
</script>

<template>
  <div class="container">
    <q-scroll-area ref="chatScroll" class="message-container">
      <q-chat-message
        class="q-pa-lg"
        v-for="(chatMessage, index) in chatMessages"
        :key="index"
        :sent="chatMessage.author == AuthorRole.User"
        :text="[chatMessage.text]"
        :bg-color="chatMessage.author == AuthorRole.User ? 'green-3' : 'grey-3'"
        :avatar="chatMessage.author == AuthorRole.User ? 'question.png' : 'answer.png'"
      >
        <template v-if="!chatMessage.text" v-slot:default>
          <q-spinner-dots v-if="!chatMessage.text" />
        </template>
        <template v-slot:stamp>
          <q-btn flat round size="xs" icon="content_copy" @click="copyText(chatMessage.text)" />
        </template>
      </q-chat-message>
    </q-scroll-area>
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
          <q-btn flat icon="zoom_out_map" @click="() => (isZoom = true)">
            <q-tooltip class="text-body2">Zoom in</q-tooltip>
          </q-btn>
          <q-btn flat icon="image" @click="() => openFileDialog()">
            <q-tooltip class="text-body2">Upload images</q-tooltip>
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
    <q-dialog v-model="isZoom" backdrop-filter="blur(4px)">
      <div class="zoom-container">
        <q-input
          class="input"
          standout
          dark
          autogrow
          autofocus
          clearable
          clear-icon="close"
          v-model="inputText"
          @keydown.shift.enter.prevent="sendMessage"
        >
        </q-input>
      </div>
    </q-dialog>
  </div>
</template>

<style lang="scss" scoped>
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

.message-container {
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

.zoom-container {
  height: auto;
  width: 100%;
  max-height: 80vh;
}
</style>
