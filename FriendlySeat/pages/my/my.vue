<template>
	<view>
		<view class="card profile-card">
			<view class="avatar-btn" @click="chooseAvatar">
				<image class="avatar" :src="previewAvatar || user.avatarUrl || '/static/logo.png'" mode="aspectFill" />
				<view class="avatar-edit">更换头像</view>
			</view>
			<view class="profile-info">
				<view class="nickname-row">
					<input class="nickname-input" v-model="editNickname" placeholder="请输入昵称" />
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
			<view class="menu-item" @click="goReading">
				<text>📚 我的阅读</text>
				<text class="arrow">›</text>
			</view>
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
				<view class="menu-right">
					<text v-if="unreadCount > 0" class="badge">{{unreadCount > 99 ? '99+' : unreadCount}}</text>
					<text class="arrow">›</text>
				</view>
			</view>
			<view class="menu-item" @click="goReports">
				<text>📝 我的举报</text>
				<text class="arrow">›</text>
			</view>
			<view class="menu-item" @click="openFeedback">
				<text>💬 意见反馈</text>
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
		</view>

		<button class="btn-outline logout" @click="deleteAccount">注销账号</button>
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
				saving: false,
				unreadCount: 0
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
				try {
					this.unreadCount = await api.getUnreadCount()
					this.setTabBarBadge(this.unreadCount)
				} catch (e) {}
			},
			setTabBarBadge(count) {
				try {
					if (count > 0) {
						uni.setTabBarBadge({ index: 2, text: count > 99 ? '99+' : String(count) })
					} else {
						uni.removeTabBarBadge({ index: 2 })
					}
				} catch (e) {}
			},
			chooseAvatar() {
				// 从相册/拍摄选择本地图片上传，不通过微信头像授权接口
				uni.chooseImage({
					count: 1,
					sizeType: ['compressed'],
					sourceType: ['album', 'camera'],
					success: (res) => {
						const filePath = res.tempFilePaths && res.tempFilePaths[0]
						if (!filePath) return
						this.avatarFile = filePath
						this.previewAvatar = filePath
					}
				})
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
			goReading() {
				uni.navigateTo({ url: '/pages/reading/reading' })
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
				uni.switchTab({ url: '/pages/notifications/notifications' })
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
			openFeedback() {
				uni.navigateTo({ url: '/pages/feedback/feedback' })
			},
			deleteAccount() {
				uni.showModal({
					title: '注销账号',
					content: '注销后你的个人信息将被匿名化处理，且无法再使用原账号登录。确定注销吗？',
					confirmText: '确认注销',
					confirmColor: '#B85450',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.deleteAccount()
							uni.removeStorageSync('token')
							uni.removeStorageSync('user')
							uni.showToast({ title: '已注销', icon: 'success' })
							setTimeout(() => uni.navigateTo({ url: '/pages/login/login' }), 600)
						} catch (e) {
							uni.showToast({ title: e.message || '注销失败', icon: 'none' })
						}
					}
				})
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
		position: relative;
		padding: 0;
		margin: 0;
		background: transparent;
		border: none;
		line-height: 1;
	}
	.avatar-edit {
		position: absolute;
		left: 0;
		right: 0;
		bottom: 0;
		background: rgba(0, 0, 0, 0.45);
		color: #FFFFFF;
		font-size: 18rpx;
		text-align: center;
		line-height: 36rpx;
		border-radius: 0 0 60rpx 60rpx;
		height: 36rpx;
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
	.menu-right {
		display: flex;
		align-items: center;
		gap: 12rpx;
	}
	.badge {
		min-width: 36rpx;
		height: 36rpx;
		line-height: 36rpx;
		padding: 0 10rpx;
		border-radius: 18rpx;
		background: #D9822B;
		color: #FFFFFF;
		font-size: 22rpx;
		text-align: center;
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
