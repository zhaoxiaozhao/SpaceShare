<template>
	<view>
		<view class="card profile-card">
			<button class="avatar-btn" open-type="chooseAvatar" @chooseavatar="onChooseAvatar">
				<image class="avatar" :src="previewAvatar || user.avatarUrl || '/static/logo.png'" mode="aspectFill" />
			</button>
			<view class="profile-info">
				<view class="nickname-row">
					<input class="nickname-input" type="nickname" v-model="editNickname" placeholder="请输入昵称" />
					<text class="random-btn" @click="randomNickname">🎲 随机</text>
				</view>
				<view class="credit-row" @click="goCredit">
					<text class="credit-label">友邻信用</text>
					<text class="credit-score">{{user.creditScore || 100}}分</text>
					<text class="credit-level">{{user.creditLevel || '正常'}}</text>
				</view>
				<button class="save-btn" @click="saveProfile">保存资料</button>
			</view>
		</view>

		<view class="card menu">
			<view class="menu-item" @click="goStudy">
				<text>📖 我的学习</text>
				<text class="arrow">›</text>
			</view>
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
	import { randomNickname, uploadAvatar } from '../../utils/profile.js'

	export default {
		data() {
			return {
				user: {},
				editNickname: '',
				avatarFile: '',
				previewAvatar: '',
				saving: false
			}
		},
		onShow() {
			if (!uni.getStorageSync('token')) {
				uni.navigateTo({ url: '/pages/login/login' })
				return
			}
			this.user = uni.getStorageSync('user') || {}
			this.editNickname = this.user.nickname || ''
			this.load()
		},
		methods: {
			async load() {
				try {
					this.user = await api.getMy()
					uni.setStorageSync('user', this.user)
					if (!this.editNickname) this.editNickname = this.user.nickname || ''
				} catch (e) {}
			},
			onChooseAvatar(e) {
				const filePath = e.detail.avatarUrl
				if (!filePath) return
				this.avatarFile = filePath
				this.previewAvatar = filePath
			},
			randomNickname() {
				this.editNickname = randomNickname()
			},
			async saveProfile() {
				if (this.saving) return
				const nickname = (this.editNickname || '').trim()
				if (!nickname) {
					uni.showToast({ title: '请输入昵称', icon: 'none' })
					return
				}
				this.saving = true
				uni.showLoading({ title: '保存中', mask: true })
				try {
					const data = { nickname }
					if (this.avatarFile) {
						data.avatarUrl = await uploadAvatar(this.avatarFile)
					}
					this.user = await api.updateProfile(data)
					uni.setStorageSync('user', this.user)
					this.editNickname = this.user.nickname || ''
					this.avatarFile = ''
					this.previewAvatar = ''
					uni.hideLoading()
					uni.showToast({ title: '保存成功', icon: 'success' })
				} catch (err) {
					uni.hideLoading()
					uni.showToast({ title: err.message || '保存失败', icon: 'none' })
				} finally {
					this.saving = false
				}
			},
			goStudy() {
				uni.navigateTo({ url: '/pages/study/study' })
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
	.avatar-btn {
		padding: 0;
		margin: 0;
		background: transparent;
		border: none;
		line-height: 1;
	}
	.avatar-btn::after {
		border: none;
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
	.nickname-row {
		display: flex;
		align-items: center;
		gap: 16rpx;
		margin-bottom: 12rpx;
	}
	.nickname-input {
		flex: 1;
		font-size: 34rpx;
		font-weight: 600;
	}
	.random-btn {
		font-size: 26rpx;
		color: #3A8A7E;
		padding: 6rpx 16rpx;
		background: #EAF3F0;
		border-radius: 24rpx;
	}
	.save-btn {
		margin-top: 12rpx;
		font-size: 24rpx;
		line-height: 2;
		background: #3A8A7E;
		color: #FFFFFF;
		border-radius: 32rpx;
		padding: 0 40rpx;
		display: inline-block;
	}
	.save-btn::after {
		border: none;
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
