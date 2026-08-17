<template>
	<view>
		<view class="card" v-if="contact">
			<text class="section-label">分享者授权的联系方式</text>
			<view class="contact-box">
				<text class="contact-type">{{contactTypeText}}</text>
				<text class="contact-value">{{contact.contactValue}}</text>
			</view>
			<button class="btn-outline" style="margin-top:20rpx;" @click="copy">复制</button>
			<text class="note">联系方式仅在预约成功后可见，预约结束后自动隐藏。</text>
		</view>
		<view class="card" v-else-if="loaded">
			<text class="empty-text">分享者未授权联系方式，或你尚未成功预约该座位。</text>
			<text class="note">请预约成功后，在预约记录中查看联系方式。</text>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				shareId: null,
				contact: null,
				loaded: false
			}
		},
		onLoad(options) {
			this.shareId = options.shareId
		},
		onShow() {
			this.load()
		},
		computed: {
			contactTypeText() {
				const map = { WechatId: '微信号', WechatQrCode: '微信二维码', Phone: '手机号', Other: '其他' }
				return this.contact ? (map[this.contact.contactType] || this.contact.contactType) : ''
			}
		},
		methods: {
			async load() {
				if (!this.shareId) {
					this.loaded = true
					return
				}
				try {
					this.contact = await api.getShareContact(this.shareId)
					this.loaded = true
				} catch (e) {
					this.loaded = true
				}
			},
			copy() {
				uni.setClipboardData({
					data: this.contact.contactValue,
					success: () => uni.showToast({ title: '已复制', icon: 'success' })
				})
			}
		}
	}
</script>

<style scoped>
	.section-label {
		font-size: 26rpx;
		color: #8A8A86;
		display: block;
		margin-bottom: 20rpx;
	}
	.contact-box {
		background: #F7F5EF;
		border-radius: 16rpx;
		padding: 30rpx;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 12rpx;
	}
	.contact-type {
		font-size: 24rpx;
		color: #3A8A7E;
	}
	.contact-value {
		font-size: 36rpx;
		font-weight: 700;
		color: #33332E;
	}
	.note {
		display: block;
		margin-top: 20rpx;
		font-size: 22rpx;
		color: #B0B0AB;
		text-align: center;
	}
	.empty-text {
		font-size: 28rpx;
		color: #55554F;
		display: block;
		text-align: center;
	}
</style>
