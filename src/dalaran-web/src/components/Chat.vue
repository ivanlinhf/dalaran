<script setup lang="ts">
import { marked } from 'marked'
import { QScrollArea } from 'quasar'
import type { Ref } from 'vue'
import { computed, onMounted, ref, watch } from 'vue'
import { addMessages, create, run, uploadImages } from '@/api/chat'
import { AuthorRole } from '@/types/authorRole'
import { ChatMessageType } from '@/types/chatMessage'
import type { ChatMessage } from '@/types/chatMessage'
import { ContentType } from '@/types/content'
import type { ChatMessageContent, ImageContent } from '@/types/content'
import { useClipboard, useFileDialog } from '@vueuse/core'

const chatScroll = ref<QScrollArea | null>(null)

const threadId: Ref<string> = ref('')

const imageFromUrls: Ref<string> = ref('')
const uploadedImageUrls: Ref<string[]> = ref([])
const inputText: Ref<string | null> = ref('')
const chatMessages: Ref<ChatMessage[]> = ref<ChatMessage[]>([])

const isLoading = ref(false)
const isZoom = ref(false)
const isFromUrl = ref(false)
const showUploaded = ref(false)
const carousel = ref(1)

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
    uploadedImageUrls.value.push(...result.urls)
  }
})

async function uploadFromUrls() {
  if (imageFromUrls.value) {
    const urls = imageFromUrls.value.split('\n').filter((x) => x)
    imageFromUrls.value = ''

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
        :text="[chatMessage.content]"
        :bg-color="chatMessage.author == AuthorRole.User ? 'green-3' : 'grey-3'"
        :avatar="chatMessage.author == AuthorRole.User ? 'question.png' : 'answer.png'"
      >
        <template v-if="chatMessage.type == ChatMessageType.Image" v-slot:default>
          <q-img
            class="img-message"
            spinner-color="white"
            fit="contain"
            :src="chatMessage.content"
          />
        </template>
        <template v-else-if="chatMessage.type == ChatMessageType.Html" v-slot:default>
          <p v-html="marked(chatMessage.content)" />
        </template>
        <template v-slot:stamp>
          <q-spinner-dots v-if="isLoading && index == chatMessages.length - 1" size="md" />
          <div v-else>
            <q-btn flat round size="xs" icon="content_copy" @click="copyText(chatMessage.content)">
              <q-tooltip>Copy</q-tooltip>
            </q-btn>
            <q-btn
              v-if="chatMessage.type == ChatMessageType.Text"
              flat
              round
              size="xs"
              icon="visibility"
              @click="chatMessage.type = ChatMessageType.Html"
            >
              <q-tooltip>Preview</q-tooltip>
            </q-btn>
            <q-btn
              v-else-if="chatMessage.type == ChatMessageType.Html"
              flat
              round
              size="xs"
              icon="visibility_off"
              @click="chatMessage.type = ChatMessageType.Text"
            >
              <q-tooltip>Quit preview</q-tooltip>
            </q-btn>
          </div>
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
                  <q-item-section @click="() => (showUploaded = uploadedImageUrls.length > 0)">
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
    <q-dialog v-model="isFromUrl" backdrop-filter="grayscale(60%)" persistent>
      <q-card class="from-url-container">
        <q-card-section>
          <q-input
            label="Enter Urls (per line)"
            class="input"
            outlined
            autogrow
            autofocus
            v-model="imageFromUrls"
          />
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="OK" @click="uploadFromUrls" v-close-popup />
          <q-btn flat label="Cancel" @click="() => (imageFromUrls = '')" v-close-popup />
        </q-card-actions>
      </q-card>
    </q-dialog>
    <q-dialog v-model="showUploaded" backdrop-filter="brightness(60%)">
      <div class="row no-wrap">
        <q-img
          class="img-view"
          v-for="(url, index) of uploadedImageUrls"
          fit="scale-down"
          :key="index"
          :src="url"
        />
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

.img-message {
  height: 15vh;
  width: 15vw;
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

.from-url-container {
  height: auto;
  width: 100%;
  max-height: 50vh;
}

.img-view {
  height: 50vh;
  width: 50vw;
  margin: 1vh;
}

:deep(code) {
  color: blue;
}
</style>
