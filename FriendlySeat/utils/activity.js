// 活动分类（用户端/详情/首页共用），code 与后端 ActivityService.ValidCategories 保持一致
export const ACTIVITY_CATEGORIES = [
	{ code: 'reading', label: '读书' },
	{ code: 'lecture', label: '讲座' },
	{ code: 'exhibition', label: '展览' },
	{ code: 'study', label: '自习' },
	{ code: 'sharing', label: '分享交流' },
	{ code: 'workshop', label: '工作坊' },
	{ code: 'film', label: '观影' },
	{ code: 'music', label: '音乐' },
	{ code: 'art', label: '艺术' },
	{ code: 'sports', label: '运动' },
	{ code: 'competition', label: '比赛' },
	{ code: 'volunteer', label: '志愿' },
	{ code: 'other', label: '其他' }
]

export function activityCategoryLabel(code) {
	const o = ACTIVITY_CATEGORIES.find(c => c.code === code)
	return o ? o.label : '其他'
}
