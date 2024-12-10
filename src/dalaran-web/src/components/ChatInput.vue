<script setup lang="ts">
import { computed, ref } from 'vue'
import { useFileDialog } from '@vueuse/core'
import { uploadImages } from '@/api/chat.ts'

import ImageViewer from '@/components/ImageViewer.vue'
import TextZoom from '@/components/TextZoom.vue'
import UrlInput from '@/components/UrlInput.vue'

const text = defineModel<string | null>('text', { required: true })
const uploadedImageUrls = defineModel<string[]>('uploadedImageUrls', { required: true })

const props = defineProps<{
  threadId: string
  isLoading: boolean
}>()

defineEmits<{
  (e: 'new'): void
  (e: 'submit'): void
}>()

const isTextZoom = ref(false)
const isUrlInput = ref(false)
const isImageViewer = ref(false)

const sendButtonEnabled = computed(() => !props.isLoading && text.value)

const { open, onChange, reset } = useFileDialog({
  accept: 'image/*',
})

onChange(async (x) => {
  if (x) {
    const result = await uploadImages(props.threadId, x)
    uploadedImageUrls.value.push(...result.urls)

    reset()
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

    const result = await uploadImages(props.threadId, files)
    uploadedImageUrls.value.push(...result.urls)
  }
}
</script>

<template>
  <div class="container">
    <q-input
      class="input"
      input-class="input-height"
      outlined
      clearable
      clear-icon="close"
      autogrow
      autofocus
      v-model="text"
      :disable="props.isLoading"
      @keydown.shift.enter.prevent="$emit('submit')"
    >
      <template #before>
        <q-btn flat icon="add" @click="$emit('new')">
          <q-tooltip class="text-body2">Start new conversation</q-tooltip>
        </q-btn>
      </template>
      <template #append>
        <q-btn flat icon="fullscreen" @click="() => (isTextZoom = true)">
          <q-tooltip class="text-body2">Zoom in</q-tooltip>
        </q-btn>
        <q-btn flat icon="image">
          <q-tooltip class="text-body2">Upload images</q-tooltip>
          <q-menu>
            <q-list class="menu-list">
              <q-item clickable v-close-popup>
                <q-item-section @click="() => open()">From File...</q-item-section>
              </q-item>
              <q-item clickable v-close-popup>
                <q-item-section @click="() => (isUrlInput = true)">From Url...</q-item-section>
              </q-item>
              <q-separator />
              <q-item clickable v-close-popup :disable="uploadedImageUrls.length == 0">
                <q-item-section @click="isImageViewer = uploadedImageUrls.length > 0">
                  View Uploaded
                </q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
      </template>
      <template #after>
        <q-btn flat icon="send" :disable="!sendButtonEnabled" @click="$emit('submit')">
          <q-tooltip class="text-body2">Send message</q-tooltip>
        </q-btn>
      </template>

      <q-inner-loading :showing="props.isLoading" color="primary" />
    </q-input>
    <text-zoom v-model:showing="isTextZoom" v-model:text="text" @submit="$emit('submit')" />
    <url-input v-model="isUrlInput" @submit="async (x) => await uploadFromUrls(x)" />
    <image-viewer v-model="isImageViewer" :urls="uploadedImageUrls" />
  </div>
</template>

<style scoped lang="scss">
.container {
  margin: auto;
}

.input {
  font-size: large;

  :deep(.q-field__append) {
    align-self: flex-end;
  }
}

:deep(.input-height) {
  max-height: 15vh;
}

.menu-list {
  font-size: large;
}
</style>
