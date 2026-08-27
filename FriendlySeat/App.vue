<script>
	import { api } from './utils/request.js'
	import { CLOUD_ENV, USE_CLOUD } from './utils/config.js'
	import { getTheme, getSeasonKey } from './utils/theme.js'

	export default {
		onLaunch: function() {
			// 微信云托管云调用初始化（微信小程序端）
			// #ifdef MP-WEIXIN
			if (USE_CLOUD && wx && wx.cloud) {
				if (!wx.cloud) {
					console.error('请使用 2.2.3 或以上的基础库以使用云能力')
				} else {
					wx.cloud.init({
						env: CLOUD_ENV,
						traceUser: true
					})
				}
			}
			// #endif

			// 四季主题：动态设置底部导航颜色与图标（页面内容色由 page-meta 注入变量）
			this.applySeasonTheme()

			const token = uni.getStorageSync('token')
			if (token) {
				api.getUnreadCount().then(count => {
					uni.setStorageSync('unreadCount', count)
				}).catch(() => {})
			}
		},
		methods: {
			applySeasonTheme() {
				// #ifdef MP-WEIXIN
				const theme = getTheme()
				const season = getSeasonKey()
				try {
					wx.setTabBarStyle({
						selectedColor: theme.primary,
						fail: () => {}
					})
					// 动态切换 4 个 tab 的选中图标为当季配色（未选中保持灰色）
					const icons = [
						{ index: 0, prefix: 'home' },
						{ index: 1, prefix: 'reserve' },
						{ index: 2, prefix: 'notify' },
						{ index: 3, prefix: 'mine' }
					]
					icons.forEach(({ index, prefix }) => {
						wx.setTabBarItem({
							index,
							selectedIconPath: `static/tabbar/${prefix}-active-${season}.png`,
							fail: () => {}
						})
					})
				} catch (e) {}
				// #endif
			}
		},
		onShow: function() {},
		onHide: function() {}
	}
</script>

<style>
	/* 友邻座品牌色：随四季变化（默认秋季主题，运行时由 page-meta 按季节注入变量覆盖） */
	page {
		--primary: #C98A3D;
		--primary-dark: #A56C24;
		--primary-light: #DBA968;
		--primary-bg: #F7F0E4;
		--primary-disabled: #E2C9A4;
		--accent: #A85432;
		background-color: #F7F5EF;
		color: #33332E;
		font-size: 28rpx;
	}

	.card {
		background: #FFFFFF;
		border-radius: 20rpx;
		padding: 28rpx;
		margin: 20rpx;
		box-shadow: 0 4rpx 16rpx rgba(0, 0, 0, 0.04);
	}

	.btn-primary {
		background-color: var(--primary);
		color: #FFFFFF;
		border-radius: 44rpx;
		font-size: 30rpx;
		line-height: 2.4;
	}

	.btn-primary[disabled] {
		background-color: var(--primary-disabled);
		color: #FFFFFF;
	}

	.btn-outline {
		background-color: transparent;
		color: var(--primary);
		border: 2rpx solid var(--primary);
		border-radius: 44rpx;
		font-size: 30rpx;
		line-height: 2.3;
	}

	.tag {
		display: inline-block;
		padding: 4rpx 16rpx;
		border-radius: 10rpx;
		font-size: 22rpx;
		background: var(--primary-bg);
		color: var(--primary);
		margin-right: 12rpx;
	}

	.status-available { color: var(--primary); }
	.status-reserved { color: #D9822B; }
	.status-completed { color: #8A8A86; }
	.status-cancelled { color: #B85450; }
	.status-active { color: var(--primary); }

	.section-title {
		font-size: 32rpx;
		font-weight: 600;
		margin: 20rpx;
		color: #2B2B27;
	}

	.empty {
		text-align: center;
		color: #999;
		padding: 80rpx 0;
		font-size: 28rpx;
	}
</style>
