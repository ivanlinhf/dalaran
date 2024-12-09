<script setup lang="ts">
import { computed } from 'vue'
import { useFileDialog } from '@vueuse/core'
import { uploadImages } from '@/api/chat.ts'

const text = defineModel<string | null>('text', { required: true })
const uploadedImageUrls = defineModel<string[]>('uploadedImageUrls', { required: true })
const isZoom = defineModel<boolean>('isZoom', { required: true })
const isFromUrl = defineModel<boolean>('isFromUrl', { required: true })
const showUploaded = defineModel<boolean>('showUploaded', { required: true })

const props = defineProps<{
  threadId: string
  isLoading: boolean
}>()

defineEmits<{
  (e: 'new'): void
  (e: 'submit'): void
}>()

const sendButtonEnabled = computed(() => !props.isLoading.value && text.value)

const { open, onChange } = useFileDialog({
  accept: 'image/*',
})

onChange(async (x) => {
  if (x) {
    const result = await uploadImages(props.threadId, x)
    uploadedImageUrls.value.push(...result.urls)
  }
})
</script>

<template>
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
      <q-btn flat icon="fullscreen" @click="() => (isZoom = true)">
        <q-tooltip class="text-body2">Zoom in</q-tooltip>
      </q-btn>
      <q-btn flat icon="image">
        <q-tooltip class="text-body2">Upload images</q-tooltip>
        <q-menu>
          <q-list>
            <q-item clickable v-close-popup>
              <q-item-section @click="() => open()">From File...</q-item-section>
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
      <q-btn flat icon="send" :disable="!sendButtonEnabled" @click="$emit('submit')">
        <q-tooltip class="text-body2">Send message</q-tooltip>
      </q-btn>
    </template>

    <q-inner-loading :showing="props.isLoading" color="primary" />
  </q-input>
</template>

<style scoped lang="scss">
.input {
  margin: auto;
  font-size: large;

  :deep(.q-field__append) {
    align-self: flex-end;
  }
}

:deep(.input-height) {
  max-height: 15vh;
}
</style>
