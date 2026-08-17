<script>
	import { api } from './utils/request.js'
	import { CLOUD_ENV, USE_CLOUD } from './utils/config.js'

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

			const token = uni.getStorageSync('token')
			if (token) {
				api.getUnreadCount().then(count => {
					uni.setStorageSync('unreadCount', count)
				}).catch(() => {})
			}
		},
		onShow: function() {},
		onHide: function() {}
	}
</script>

<style>
	/* 友邻座品牌色：暖青 + 米白 */
	page {
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
		background-color: #3A8A7E;
		color: #FFFFFF;
		border-radius: 44rpx;
		font-size: 30rpx;
		line-height: 2.4;
	}

	.btn-primary[disabled] {
		background-color: #A9C7C1;
		color: #FFFFFF;
	}

	.btn-outline {
		background-color: transparent;
		color: #3A8A7E;
		border: 2rpx solid #3A8A7E;
		border-radius: 44rpx;
		font-size: 30rpx;
		line-height: 2.3;
	}

	.tag {
		display: inline-block;
		padding: 4rpx 16rpx;
		border-radius: 10rpx;
		font-size: 22rpx;
		background: #EAF3F0;
		color: #3A8A7E;
		margin-right: 12rpx;
	}

	.status-available { color: #3A8A7E; }
	.status-reserved { color: #D9822B; }
	.status-completed { color: #8A8A86; }
	.status-cancelled { color: #B85450; }
	.status-active { color: #3A8A7E; }

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
