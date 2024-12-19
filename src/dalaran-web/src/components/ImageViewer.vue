<script setup lang="ts">
import { ref, watchEffect } from 'vue'

const model = defineModel<boolean>({ required: true })

const props = defineProps<{
  urls: string[]
  start?: number
}>()

const index = ref(0)

function previous() {
  index.value = Math.max(index.value - 1, 0)
}

function next() {
  index.value = Math.min(index.value + 1, props.urls.length - 1)
}

watchEffect(() => {
  if (model.value) {
    index.value = props.start ?? 0
  }
})

watchEffect(() => {
  if (index.value > props.urls.length - 1) {
    index.value = 0
  }
})
</script>

<template>
  <q-dialog
    v-model="model"
    backdrop-filter="brightness(60%)"
    @keydown.left.prevent="previous"
    @keydown.right.prevent="next"
  >
    <div class="row no-wrap">
      <q-icon
        class="self-center img-arrow"
        size="xl"
        :color="index > 0 ? 'white' : 'grey-7'"
        name="arrow_back_ios"
        @click="previous"
      />
      <q-img class="img-view" fit="scale-down" :src="props.urls[index]" />
      <q-icon
        class="self-center img-arrow"
        size="xl"
        :color="index < props.urls.length - 1 ? 'white' : 'grey-7'"
        name="arrow_forward_ios"
        @click="next"
      />
    </div>
  </q-dialog>
</template>

<style scoped lang="scss">
.img-view {
  height: 80vh;
  width: 80vw;
}

.img-arrow {
  cursor: pointer;
}
</style>
