<template>
	<view>
		<view class="card profile-card">
			<image class="avatar" :src="user.avatarUrl || '/static/logo.png'" mode="aspectFill" />
			<view class="profile-info">
				<text class="nickname">{{user.nickname || '友邻座友邻'}}</text>
				<view class="credit-row" @click="goCredit">
					<text class="credit-label">友邻信用</text>
					<text class="credit-score">{{user.creditScore || 100}}分</text>
					<text class="credit-level">{{user.creditLevel || '正常'}}</text>
				</view>
			</view>
		</view>

		<view class="card menu">
			<view class="menu-item" @click="goReservations">
				<text>📅 我的预约</text>
				<text class="arrow">›</text>
			</view>
			<view class="menu-item" @click="goCredit">
				<text>💚 友邻信用</text>
				<text class="arrow">›</text>
			</view>
			<view class="menu-item" @click="goContribution">
				<text>🏅 友邻贡献</text>
				<text class="arrow">›</text>
			</view>
			<view class="menu-item" @click="goNotifications">
				<text>🔔 消息通知</text>
				<text class="arrow">›</text>
			</view>
			<view class="menu-item" @click="goReports">
				<text>📝 我的举报</text>
				<text class="arrow">›</text>
			</view>
			<view class="menu-item" @click="openAgreement">
				<text>📄 用户服务协议</text>
				<text class="arrow">›</text>
			</view>
			<view class="menu-item" @click="openPrivacy">
				<text>🔒 隐私保护指引</text>
				<text class="arrow">›</text>
			</view>
		</view>

		<view class="card about">
			<text class="about-line">一席相邻，善意相续</text>
			<text class="about-line small">友邻座 · 公共学习空间座位共享与预约平台</text>
			<text class="about-line small">免费共享 · 免费预约 · 不卖座 · 不炒座 · 不占座</text>
		</view>

		<button class="btn-outline logout" @click="logout">退出登录</button>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				user: {}
			}
		},
		onShow() {
			if (!uni.getStorageSync('token')) {
				uni.navigateTo({ url: '/pages/login/login' })
				return
			}
			this.user = uni.getStorageSync('user') || {}
			this.load()
		},
		methods: {
			async load() {
				try {
					this.user = await api.getMy()
					uni.setStorageSync('user', this.user)
				} catch (e) {}
			},
			goReservations() {
				uni.switchTab({ url: '/pages/reservations/reservations' })
			},
			goCredit() {
				uni.navigateTo({ url: '/pages/credit/credit' })
			},
			goContribution() {
				uni.navigateTo({ url: '/pages/contribution/contribution' })
			},
			goNotifications() {
				uni.navigateTo({ url: '/pages/notifications/notifications' })
			},
			goReports() {
				uni.navigateTo({ url: '/pages/report/report' })
			},
			openAgreement() {
				uni.navigateTo({ url: '/pages/agreement/agreement' })
			},
			openPrivacy() {
				uni.navigateTo({ url: '/pages/privacy/privacy' })
			},
			logout() {
				uni.removeStorageSync('token')
				uni.removeStorageSync('user')
				uni.showToast({ title: '已退出登录', icon: 'none' })
				this.user = {}
			}
		}
	}
</script>

<style scoped>
	.profile-card {
		display: flex;
		align-items: center;
		gap: 24rpx;
	}
	.avatar {
		width: 120rpx;
		height: 120rpx;
		border-radius: 50%;
		background: #EAF3F0;
	}
	.profile-info {
		flex: 1;
	}
	.nickname {
		font-size: 34rpx;
		font-weight: 600;
		display: block;
		margin-bottom: 12rpx;
	}
	.credit-row {
		display: flex;
		align-items: center;
		gap: 12rpx;
	}
	.credit-label {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.credit-score {
		font-size: 28rpx;
		font-weight: 600;
		color: #3A8A7E;
	}
	.credit-level {
		font-size: 22rpx;
		padding: 4rpx 12rpx;
		background: #EAF3F0;
		color: #3A8A7E;
		border-radius: 8rpx;
	}
	.menu {
		padding: 0;
	}
	.menu-item {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 28rpx;
		border-bottom: 1rpx solid #F0EFEA;
		font-size: 30rpx;
	}
	.menu-item:last-child {
		border-bottom: none;
	}
	.arrow {
		color: #C0C0BB;
		font-size: 36rpx;
	}
	.about {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
		align-items: center;
	}
	.about-line {
		font-size: 26rpx;
		color: #3A8A7E;
	}
	.about-line.small {
		font-size: 22rpx;
		color: #B0B0AB;
	}
	.logout {
		margin: 30rpx;
		color: #B85450;
		border-color: #B85450;
	}
</style>
