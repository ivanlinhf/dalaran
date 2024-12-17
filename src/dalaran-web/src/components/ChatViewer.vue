<script setup lang="ts">
import { marked } from 'marked'
import { QScrollArea } from 'quasar'
import { ref, watch } from 'vue'
import { ChatMessageType } from '@/types/chatMessage.ts'
import type { ChatMessage } from '@/types/chatMessage.ts'
import { AuthorRole } from '@/types/authorRole.ts'

const messages = defineModel<ChatMessage[]>('messages', { required: true })
const types = defineModel<ChatMessageType[]>('types', { required: true })

const props = defineProps<{
  isResponding: boolean
}>()

const chatScroll = ref<QScrollArea | null>(null)

async function copy(index: number) {
  const type = types.value[index]
  if (type == ChatMessageType.Html) {
    const html = await marked(messages.value[index].content)
    const blob = new Blob([html], { type: 'text/html' })
    await navigator.clipboard.write([
      new ClipboardItem({
        'text/html': blob,
      }),
    ])
  } else {
    await navigator.clipboard.writeText(messages.value[index].content)
  }
}

watch(
  messages,
  () => {
    setTimeout(() => {
      chatScroll.value?.setScrollPercentage('vertical', 1.0)
    }, 100)
  },
  { deep: true },
)
</script>

<template>
  <q-scroll-area ref="chatScroll">
    <q-chat-message
      class="chat q-pa-lg"
      v-for="(message, index) in messages"
      :key="index"
      :sent="message.author == AuthorRole.User"
      :text="[message.content]"
      :bg-color="message.author == AuthorRole.User ? 'green-3' : 'grey-3'"
      :avatar="message.author == AuthorRole.User ? 'question.png' : 'answer.png'"
    >
      <template v-if="types[index] == ChatMessageType.Image" v-slot:default>
        <q-img class="img-message" spinner-color="white" fit="contain" :src="message.content" />
      </template>
      <template v-else-if="types[index] == ChatMessageType.Html" v-slot:default>
        <div class="html" v-html="marked(message.content)" />
      </template>

      <template v-slot:stamp>
        <q-spinner-dots
          v-if="
            props.isResponding &&
            index == messages.length - 1 &&
            message.author == AuthorRole.Assistant
          "
          size="md"
        />
        <div v-else>
          <q-btn flat round size="xs" icon="content_copy" @click="copy(index)">
            <q-tooltip>Copy</q-tooltip>
          </q-btn>
          <q-btn
            v-if="types[index] == ChatMessageType.Text"
            flat
            round
            size="xs"
            icon="visibility"
            @click="types[index] = ChatMessageType.Html"
          >
            <q-tooltip>Preview</q-tooltip>
          </q-btn>
          <q-btn
            v-else-if="types[index] == ChatMessageType.Html"
            flat
            round
            size="xs"
            icon="visibility_off"
            @click="types[index] = ChatMessageType.Text"
          >
            <q-tooltip>Quit preview</q-tooltip>
          </q-btn>
        </div>
      </template>
    </q-chat-message>
  </q-scroll-area>
</template>

<style scoped lang="scss">
.chat {
  font-size: large;
  white-space: pre-wrap;
}

.img-message {
  height: 15vh;
  width: 15vw;
}

.html * {
  margin: auto;

  :deep(code) {
    color: blue;
    white-space: pre-wrap;
  }
}
</style>
