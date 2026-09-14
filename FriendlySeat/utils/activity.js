import { getAppOptions, optionLabel } from './options.js'

// 活动分类（来自后台配置，见 ConfigOptionsService.ActivityCategories）
export function getActivityCategories() {
	return getAppOptions().activityCategories
}

export function activityCategoryLabel(code) {
	return optionLabel(getAppOptions().activityCategories, code)
}
