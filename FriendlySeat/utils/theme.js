// 友邻座四季主题
// 春·新芽 / 夏·青碧 / 秋·银杏 / 冬·黛蓝
// 主题通过 page-meta 注入 CSS 变量（--primary 等），页面样式统一引用变量
// tabBar/导航栏/图标 由 App.vue 启动时通过微信 API 动态设置

const SEASONS = {
	spring: {
		name: '春 · 新芽',
		primary: '#6BAF8B',
		primaryDark: '#4E9070',
		primaryLight: '#8CC5A8',
		primaryBg: '#EEF5EC',
		primaryDisabled: '#BCD9C9',
		gradient: 'linear-gradient(135deg, #6BAF8B 0%, #8CC5A8 100%)',
		accent: '#E8A0B4'
	},
	summer: {
		name: '夏 · 青碧',
		primary: '#2E8B94',
		primaryDark: '#1F6B73',
		primaryLight: '#57AFB8',
		primaryBg: '#E8F2F3',
		primaryDisabled: '#A9CDD1',
		gradient: 'linear-gradient(135deg, #2E8B94 0%, #57AFB8 100%)',
		accent: '#F2C94C'
	},
	autumn: {
		name: '秋 · 银杏',
		primary: '#C98A3D',
		primaryDark: '#A56C24',
		primaryLight: '#DBA968',
		primaryBg: '#F7F0E4',
		primaryDisabled: '#E2C9A4',
		gradient: 'linear-gradient(135deg, #C98A3D 0%, #DBA968 100%)',
		accent: '#A85432'
	},
	winter: {
		name: '冬 · 黛蓝',
		primary: '#5B6E8C',
		primaryDark: '#43536B',
		primaryLight: '#7E90AC',
		primaryBg: '#EEF1F5',
		primaryDisabled: '#B9C2D1',
		gradient: 'linear-gradient(135deg, #5B6E8C 0%, #7E90AC 100%)',
		accent: '#8A93A8'
	}
}

// 按月份划分季节（3-5春 / 6-8夏 / 9-11秋 / 12-2冬）
export function getSeasonKey(month) {
	const m = month != null ? month : new Date().getMonth() + 1
	if (m >= 3 && m <= 5) return 'spring'
	if (m >= 6 && m <= 8) return 'summer'
	if (m >= 9 && m <= 11) return 'autumn'
	return 'winter'
}

export function getTheme(month) {
	return SEASONS[getSeasonKey(month)]
}

// 生成 page-meta 的 page-style（注入 CSS 变量到页面）
export function getPageStyle(month) {
	const t = getTheme(month)
	return [
		`--primary:${t.primary}`,
		`--primary-dark:${t.primaryDark}`,
		`--primary-light:${t.primaryLight}`,
		`--primary-bg:${t.primaryBg}`,
		`--primary-disabled:${t.primaryDisabled}`,
		`--primary-gradient:${t.gradient}`,
		`--accent:${t.accent}`
	].join(';') + ';'
}

export default SEASONS