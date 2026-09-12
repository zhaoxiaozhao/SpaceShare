// 微信订阅消息授权工具
// 关键业务节点调用 subscribeFor(key) 请求用户订阅对应消息模板
// 模板 ID 由后端 SystemConfigs 配置（管理端「订阅消息模板」），未配置时静默跳过
import { api } from './request.js'

let templatesCache = null
let cacheAt = 0

async function getTemplates() {
	const now = Date.now()
	if (templatesCache && now - cacheAt < 5 * 60 * 1000) return templatesCache
	try {
		templatesCache = await api.getSubscribeTemplates()
		cacheAt = now
	} catch (e) {
		templatesCache = {}
	}
	return templatesCache
}

// 订阅指定场景的消息模板（key 对应后端配置键），一次可传多个
// 返回 Promise：在微信授权弹窗结束后 resolve，便于在关键请求前先完成订阅
export async function subscribeFor(keys) {
	// #ifdef MP-WEIXIN
	const list = Array.isArray(keys) ? keys : [keys]
	const templates = await getTemplates()
	const tmplIds = list.map(k => templates[k]).filter(id => !!id)
	if (!tmplIds.length) return false
	return new Promise((resolve) => {
		try {
			wx.requestSubscribeMessage({
				tmplIds,
				success: () => resolve(true),
				fail: () => resolve(false)
			})
		} catch (e) {
			resolve(false)
		}
	})
	// #endif
}