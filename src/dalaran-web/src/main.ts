import { Quasar } from 'quasar'
import { createApp } from 'vue'

// Import icon libraries
import '@quasar/extras/material-icons/material-icons.css'

// Import Quasar css
import 'quasar/src/css/index.sass'

import App from '@/App.vue'
import router from '@/router'

const app = createApp(App)

app.use(router)
app.use(Quasar, {
  plugins: {},
})

app.mount('#app')
