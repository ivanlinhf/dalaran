<script setup lang="ts">
import type { Ref } from 'vue'
import { computed, onMounted, ref } from 'vue'
import { addMessages, create, run } from '@/api/chat'
import { AuthorRole } from '@/types/authorRole'
import type { ChatMessage } from '@/types/chatMessage'
import { ContentType } from '@/types/content'

const threadId: Ref<string> = ref('')
const inputText: Ref<string | null> = ref('')
const chatMessages: Ref<ChatMessage[]> = ref<ChatMessage[]>([])

const inputTextEnabled = computed(() => !!threadId.value)
const uploadButtonEnabled = computed(() => !!threadId.value)
const sendButtonEnabled = computed(
  () => threadId.value && inputText.value && inputText.value.trim(),
)

async function sendMessage() {
  const text = inputText.value!

  await addMessages(threadId.value, [{ $type: ContentType.Text, text: text }])
  inputText.value = ''

  chatMessages.value.push({ author: AuthorRole.User, text: text })
  chatMessages.value.push({ author: AuthorRole.Assistant, text: '' })

  const responseMessage = chatMessages.value[chatMessages.value.length - 1]
  await run(threadId.value, (x) => {
    responseMessage.text += x
  })
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
          <q-btn flat icon="image" :disable="!uploadButtonEnabled">
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
