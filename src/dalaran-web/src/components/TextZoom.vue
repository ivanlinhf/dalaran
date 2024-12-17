<script setup lang="ts">
const showing = defineModel<boolean>('showing', { required: true })
const text = defineModel<string | null>('text', { required: true })

defineEmits<{
  (e: 'submit'): void
}>()
</script>

<template>
  <q-dialog v-model="showing" backdrop-filter="blur(4px)">
    <div class="container">
      <q-input
        class="input"
        standout
        dark
        autogrow
        autofocus
        v-model="text"
        @keydown.shift.enter.prevent="
          () => {
            showing = false
            $emit('submit')
          }
        "
      >
      </q-input>
    </div>
  </q-dialog>
</template>

<style scoped lang="scss">
.container {
  height: auto;
  width: 100%;
  max-height: 80vh;
}

.input {
  font-size: large;

  :deep(.q-field__append) {
    align-self: flex-end;
  }
}
</style>
