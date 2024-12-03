<script setup lang="ts">
import type { Ref } from 'vue'
import { ref } from 'vue'
import { chat } from '@/api/llm'
import { AuthorRole } from '@/types/authorRole.ts'
import type { ChatMessage } from '@/types/chatMessage.ts'
import { ContentType } from '@/types/content'

const inputText: Ref<string | null> = ref('')
const chatMessages: Ref<ChatMessage[]> = ref<ChatMessage[]>([])

async function sendMessage() {
  if (inputText.value) {
    const text = inputText.value
    inputText.value = ''

    chatMessages.value.push({ author: AuthorRole.User, text: text })

    chatMessages.value.push({ author: AuthorRole.Assistant, text: '' })
    const responseMessage = chatMessages.value[chatMessages.value.length - 1]
    await chat([{ $type: ContentType.Text, text: text }], (x) => {
      responseMessage.text += x
    })
  }
}
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
          <q-btn flat icon="image">
            <q-tooltip class="text-body2">Upload images</q-tooltip>
          </q-btn>
        </template>
        <template #after>
          <q-btn flat icon="send" :disable="!inputText?.trim()" @click="sendMessage">
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
