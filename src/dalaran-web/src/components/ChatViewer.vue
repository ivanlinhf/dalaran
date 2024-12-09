<script setup lang="ts">
import { marked } from 'marked'
import { QScrollArea } from 'quasar'
import { ref, watch } from 'vue'
import { useClipboard } from '@vueuse/core'
import { ChatMessageType } from '@/types/chatMessage.ts'
import type { ChatMessage } from '@/types/chatMessage.ts'
import { AuthorRole } from '@/types/authorRole.ts'

const model = defineModel<ChatMessage[]>({ required: true })

const props = defineProps<{
  isLoading: boolean
}>()

const chatScroll = ref<QScrollArea | null>(null)

const { copy } = useClipboard()

watch(
  model,
  () => {
    if (chatScroll.value) {
      chatScroll.value.setScrollPercentage('vertical', 1.0)
    }
  },
  { deep: true },
)
</script>

<template>
  <q-scroll-area ref="chatScroll">
    <q-chat-message
      class="q-pa-lg"
      v-for="(chatMessage, index) in model"
      :key="index"
      :sent="chatMessage.author == AuthorRole.User"
      :text="[chatMessage.content]"
      :bg-color="chatMessage.author == AuthorRole.User ? 'green-3' : 'grey-3'"
      :avatar="chatMessage.author == AuthorRole.User ? 'question.png' : 'answer.png'"
    >
      <template v-if="chatMessage.type == ChatMessageType.Image" v-slot:default>
        <q-img class="img-message" spinner-color="white" fit="contain" :src="chatMessage.content" />
      </template>
      <template v-else-if="chatMessage.type == ChatMessageType.Html" v-slot:default>
        <p v-html="marked(chatMessage.content)" />
      </template>

      <template v-slot:stamp>
        <q-spinner-dots v-if="props.isLoading && index == model.length - 1" size="md" />
        <div v-else>
          <q-btn flat round size="xs" icon="content_copy" @click="copy(chatMessage.content)">
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
</template>

<style scoped lang="scss">
.img-message {
  height: 15vh;
  width: 15vw;
}
</style>
