<template>
	<view class="login-page">
		<view class="brand">
			<text class="brand-name">友邻座</text>
			<text class="brand-slogan">一席相邻，善意相续</text>
		</view>
		<view class="login-desc">
			<text>公共学习空间座位共享与预约平台</text>
			<text>免费共享 · 免费预约 · 真实到座</text>
		</view>
		<button class="btn-primary login-btn" @click="wxLogin">微信登录</button>
		<view class="agreement">
			<text class="agreement-text">登录即表示同意</text>
			<text class="agreement-link" @click="openAgreement">《用户服务协议》</text>
			<text class="agreement-text">和</text>
			<text class="agreement-link" @click="openPrivacy">《隐私保护指引》</text>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				loading: false
			}
		},
		methods: {
			openAgreement() {
				uni.navigateTo({ url: '/pages/agreement/agreement' })
			},
			openPrivacy() {
				uni.navigateTo({ url: '/pages/privacy/privacy' })
			},
			wxLogin() {
				if (this.loading) return
				this.loading = true
				uni.showLoading({ title: '登录中', mask: true })

				const doLogin = (code) => {
					api.login(code, '友邻座友邻', '').then((res) => {
						uni.setStorageSync('token', res.token)
						uni.setStorageSync('user', res.user)
						uni.hideLoading()
						uni.showToast({ title: '登录成功', icon: 'success' })
						setTimeout(() => uni.switchTab({ url: '/pages/index/index' }), 500)
					}).catch((err) => {
						uni.hideLoading()
						this.loading = false
						uni.showToast({ title: err.message || '登录失败', icon: 'none' })
					})
				}

				// 未配置真实 AppID 时，直接走模拟登录（本地开发 / 微信开发者工具测试号）
				let appId = ''
				try {
					const info = uni.getAccountInfoSync()
					appId = info && info.miniProgram ? info.miniProgram.appId : ''
				} catch (e) {}

				if (!appId) {
					doLogin('mock_dev_' + Date.now())
					return
				}

				uni.login({
					provider: 'weixin',
					success: (loginRes) => doLogin(loginRes.code),
					fail: () => doLogin('mock_dev_' + Date.now())
				})
			}
		}
	}
</script>

<style scoped>
	.login-page {
		padding: 160rpx 50rpx;
		display: flex;
		flex-direction: column;
		align-items: center;
	}
	.brand {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 16rpx;
	}
	.brand-name {
		font-size: 64rpx;
		font-weight: 700;
		color: #3A8A7E;
		letter-spacing: 10rpx;
	}
	.brand-slogan {
		font-size: 30rpx;
		color: #55554F;
	}
	.login-desc {
		margin-top: 80rpx;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 8rpx;
		font-size: 26rpx;
		color: #8A8A86;
	}
	.login-btn {
		margin-top: 80rpx;
		width: 80%;
	}
	.agreement {
		margin-top: 40rpx;
		font-size: 22rpx;
		color: #B0B0AB;
	}
	.agreement-text {
		font-size: 22rpx;
		color: #B0B0AB;
	}
	.agreement-link {
		font-size: 22rpx;
		color: #3A8A7E;
		text-decoration: underline;
	}
</style>
