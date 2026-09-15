import App from './App'

// #ifndef VUE3
import Vue from 'vue'
import './uni.promisify.adaptor'
Vue.config.productionTip = false
App.mpType = 'app'
const app = new Vue({
	...App
})
app.$mount()
// #endif

// #ifdef VUE3
import {
	createSSRApp
} from 'vue'
import { getPageStyle } from './utils/theme.js'
import { formatMinutes } from './utils/format.js'
export function createApp() {
	const app = createSSRApp(App)
	// 全局注入四季主题：页面模板用 <page-meta :page-style="pageThemeStyle" /> 挂载 CSS 变量
	// 同时注入统一的时长格式化方法，避免各页面格式不一致
	app.mixin({
		computed: {
			pageThemeStyle() {
				return getPageStyle()
			}
		},
		methods: {
			formatMinutes
		}
	})
	return {
		app
	}
}
// #endif
