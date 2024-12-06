<script setup lang="ts">
import type { Ref } from 'vue'
import { computed, onMounted, ref } from 'vue'
import { addMessages, create, run, uploadImages } from '@/api/chat'
import { AuthorRole } from '@/types/authorRole'
import type { ChatMessage } from '@/types/chatMessage'
import { ContentType } from '@/types/content'
import type { ChatMessageContent, ImageContent } from '@/types/content'
import { useFileDialog } from '@vueuse/core'

const threadId: Ref<string> = ref('')
const imageUrls: Ref<string[]> = ref([])
const inputText: Ref<string | null> = ref('')
const chatMessages: Ref<ChatMessage[]> = ref<ChatMessage[]>([])

const inputTextEnabled = computed(() => !!threadId.value)
const uploadButtonEnabled = computed(() => !!threadId.value)
const sendButtonEnabled = computed(
  () => threadId.value && inputText.value && inputText.value.trim(),
)

let responseContent: ChatMessageContent | null = null

const { open: openFileDialog, onChange: onImagesSelected } = useFileDialog({
  accept: 'image/*',
})

onImagesSelected(async (x) => {
  if (x) {
    const result = await uploadImages(threadId.value, x)
    imageUrls.value.push(...result.urls)
  }
})

async function sendMessage() {
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

onMounted(async () => {
  const meta = await create()
  threadId.value = meta.threadId
})
</script>

<template>
  <div class="container">
    <q-scroll-area class="message-container">
      <q-chat-message
        v-for="(chatMessage, index) in chatMessages"
        :key="index"
        :sent="chatMessage.author == AuthorRole.User"
        :text="[chatMessage.text]"
      >
        <template v-if="!chatMessage.text" v-slot:default
          ><q-spinner-dots v-if="!chatMessage.text"
        /></template>
      </q-chat-message>
    </q-scroll-area>
    <div class="input-container">
      <q-input
        class="input"
        input-class="input-height"
        outlined
        clearable
        clear-icon="close"
        autogrow
        v-model="inputText"
        :readonly="!inputTextEnabled"
      >
        <template #before>
          <q-btn flat icon="add">
            <q-tooltip class="text-body2">Start new conversation</q-tooltip>
          </q-btn>
        </template>
        <template #append>
          <q-btn flat icon="zoom_out_map">
            <q-tooltip class="text-body2">Zoom in</q-tooltip>
          </q-btn>
          <q-btn flat icon="image" :disable="!uploadButtonEnabled" @click="() => openFileDialog()">
            <q-tooltip class="text-body2">Upload images</q-tooltip>
          </q-btn>
        </template>
        <template #after>
          <q-btn flat icon="send" :disable="!sendButtonEnabled" @click="sendMessage">
            <q-tooltip class="text-body2">Send message</q-tooltip>
          </q-btn>
        </template>
      </q-input>
    </div>
  </div>
</template>

<style lang="scss" scoped>
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
</style>
