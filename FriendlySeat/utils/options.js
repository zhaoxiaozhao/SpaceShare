import { api } from './request.js'

// 可配置选项的默认值（与后端 ConfigOptionsService 默认一致），接口不可用时兜底
export const DEFAULT_OPTIONS = {
	activityCategories: [
		{ code: 'reading', label: '读书' },
		{ code: 'lecture', label: '讲座' },
		{ code: 'exhibition', label: '展览' },
		{ code: 'study', label: '自习' },
		{ code: 'kaoyan', label: '考研' },
		{ code: 'kaogong', label: '考公' },
		{ code: 'ai', label: 'AI' },
		{ code: 'coding', label: '编程' },
		{ code: 'sharing', label: '分享交流' },
		{ code: 'workshop', label: '工作坊' },
		{ code: 'film', label: '观影' },
		{ code: 'music', label: '音乐' },
		{ code: 'art', label: '艺术' },
		{ code: 'sports', label: '运动' },
		{ code: 'competition', label: '比赛' },
		{ code: 'volunteer', label: '志愿' },
		{ code: 'other', label: '其他' }
	],
	swapReasons: [
		{ code: 'light', label: '光线问题' },
		{ code: 'cold', label: '位置偏冷' },
		{ code: 'hot', label: '位置偏热' },
		{ code: 'noise', label: '附近有人交谈' },
		{ code: 'together', label: '想与同伴相邻' },
		{ code: 'window', label: '想靠窗' },
		{ code: 'socket', label: '需要插座' },
		{ code: 'other', label: '其他' }
	],
	seatTags: [
		{ code: 'window', label: '靠窗' },
		{ code: 'socket', label: '有插座' },
		{ code: 'quiet', label: '安静' },
		{ code: 'light', label: '光线好' }
	],
	venuePostCategories: [
		{ code: 'help', label: '求助' },
		{ code: 'study', label: '组队自习' },
		{ code: 'books', label: '书籍推荐' },
		{ code: 'advice', label: '场馆建议' },
		{ code: 'lost', label: '失物招领' },
		{ code: 'chat', label: '闲聊' }
	]
}

const STORAGE_KEY = 'appOptions'

export function getAppOptions() {
	try {
		const cached = uni.getStorageSync(STORAGE_KEY)
		if (cached && Array.isArray(cached.activityCategories) && cached.activityCategories.length) {
			return {
				activityCategories: cached.activityCategories,
				swapReasons: cached.swapReasons && cached.swapReasons.length ? cached.swapReasons : DEFAULT_OPTIONS.swapReasons,
				seatTags: cached.seatTags && cached.seatTags.length ? cached.seatTags : DEFAULT_OPTIONS.seatTags,
				venuePostCategories: cached.venuePostCategories && cached.venuePostCategories.length ? cached.venuePostCategories : DEFAULT_OPTIONS.venuePostCategories
			}
		}
	} catch (e) {}
	return DEFAULT_OPTIONS
}

export async function refreshAppOptions() {
	try {
		const data = await api.getAppOptions()
		if (data && Array.isArray(data.activityCategories) && data.activityCategories.length) {
			uni.setStorageSync(STORAGE_KEY, data)
			return data
		}
	} catch (e) {}
	return getAppOptions()
}

export function optionLabel(list, code) {
	const o = (list || []).find(x => x.code === code)
	return o ? o.label : code
}
