<script setup lang="ts">
import { ref } from 'vue'

const model = defineModel({ type: Boolean })

defineEmits<{
  (e: 'submit', urlsText: string): void
}>()

const urlsText = ref('')
</script>

<template>
  <q-dialog v-model="model" backdrop-filter="grayscale(60%)" persistent>
    <q-card class="container">
      <q-card-section>
        <q-input
          label="Enter Urls (per line)"
          class="input"
          outlined
          autogrow
          autofocus
          v-model="urlsText"
        />
      </q-card-section>
      <q-card-actions align="right">
        <q-btn
          flat
          label="OK"
          @click="
            () => {
              $emit('submit', urlsText)
              urlsText = ''
            }
          "
          v-close-popup
        />
        <q-btn flat label="Cancel" @click="() => (urlsText = '')" v-close-popup />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<style scoped lang="scss">
.container {
  height: auto;
  width: 100%;
  max-height: 50vh;
}

.input {
  font-size: large;
}
</style>
