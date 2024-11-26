<script setup lang="ts">
import { ref } from 'vue'
import type { Ref } from 'vue'

const toolTipText: Ref<string> = ref('Ask me anything...')
const inputText: Ref<string | null> = ref('')
</script>

<template>
  <div class="container">
    <div class="message-container">
      <h3>{{ inputText }}</h3>
    </div>
    <div class="tooltip-container">
      <div class="text-body1">{{ toolTipText }}</div>
    </div>
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
          <q-btn flat icon="send">
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
}

.container {
  height: 90vh;
  width: 90vw;
}

.message-container {
  height: 80%;
  width: 60%;
}

.tooltip-container {
  height: 5%;
  width: 40%;
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
