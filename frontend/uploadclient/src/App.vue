<script setup>
import {Dashboard} from '@uppy/vue'
</script>

<template>
  <div id="app">
    <Dashboard
        :uppy="uppy1"
        :plugins="[]"
    />
  </div>
</template>

<script>
import Uppy from '@uppy/core'
import Tus from '@uppy/tus'
import {defineComponent} from 'vue'

export default defineComponent({
  computed: {
    uppy1: () =>
        new Uppy({
          id: 'uppy1',
          autoProceed: false,
          restrictions: {
            maxNumberOfFiles: 2,
            minNumberOfFiles: 1
          },
        })
            .use(Tus, {
              endpoint: "http://localhost:5034/upload/",
              headers: {
                "Authorization": "Bearer TOKEN_VALUE"
              },
            }),
  }
})
</script>

<style src="@uppy/core/dist/style.min.css"></style>
<style src="@uppy/dashboard/dist/style.min.css"></style>
<style src="@uppy/drag-drop/dist/style.min.css"></style>