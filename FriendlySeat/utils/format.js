export function formatTime(iso) {
	if (!iso) return ''
	const date = new Date(iso)
	const pad = (n) => (n < 10 ? '0' + n : n)
	return `${date.getMonth() + 1}月${date.getDate()}日 ${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function formatDate(iso) {
	if (!iso) return ''
	const date = new Date(iso)
	const pad = (n) => (n < 10 ? '0' + n : n)
	return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function formatDuration(start, end) {
	if (!start || !end) return ''
	const ms = new Date(end) - new Date(start)
	const min = Math.round(ms / 60000)
	if (min < 60) return `${min}分钟`
	return `${Math.floor(min / 60)}小时${min % 60 > 0 ? (min % 60) + '分' : ''}`
}

export function statusText(status) {
	const map = {
		Reserved: '待到达',
		Arrived: '已到座',
		Using: '使用中',
		Completed: '已完成',
		Cancelled: '已取消',
		NoShow: '爽约',
		Expired: '已过期',
		Available: '可预约',
		Waiting: '候补中',
		Notified: '可预约',
		Pending: '待处理',
		Paid: '已支持',
		Failed: '失败'
	}
	return map[status] || status
}

export function creditLevel(score) {
	if (score >= 90) return '优秀'
	if (score >= 70) return '正常'
	if (score >= 50) return '观察'
	if (score >= 30) return '限制'
	return '高风险'
}
