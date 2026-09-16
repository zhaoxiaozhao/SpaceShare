<template>
	<view v-if="visible" class="pv-mask">
		<view class="pv-box">
			<text class="pv-title">用户隐私保护提示</text>
			<text class="pv-text">感谢你使用「友邻座」。为提供附近场馆排序、上传图片、保存图片等功能，我们需收集你的位置信息、相册（选中的照片）等必要信息。请阅读并同意后继续使用。</text>
			<text class="pv-link" @click="openContract">查看《隐私保护指引》</text>
			<view class="pv-actions">
				<view class="pv-btn ghost" @click="reject">拒绝</view>
				<view class="pv-btn primary" @click="agree">同意并继续</view>
			</view>
		</view>
	</view>
</template>

<script>
	import { resolvePrivacy, openPrivacyContract } from '../../utils/privacy.js'

	export default {
		name: 'PrivacyPopup',
		data() {
			return { visible: false }
		},
		mounted() {
			uni.$on('privacy:need', this.onNeed)
		},
		beforeUnmount() {
			uni.$off('privacy:need', this.onNeed)
		},
		methods: {
			onNeed() {
				this.visible = true
			},
			openContract() {
				openPrivacyContract()
			},
			agree() {
				this.visible = false
				resolvePrivacy(true)
			},
			reject() {
				this.visible = false
				resolvePrivacy(false)
			}
		}
	}
</script>

<style scoped>
	.pv-mask {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.5);
		z-index: 9999;
		display: flex;
		align-items: center;
		justify-content: center;
	}
	.pv-box {
		width: 580rpx;
		background: #FFFFFF;
		border-radius: 24rpx;
		padding: 40rpx 36rpx 30rpx;
		display: flex;
		flex-direction: column;
	}
	.pv-title {
		font-size: 32rpx;
		font-weight: 700;
		color: #2B2B27;
		text-align: center;
	}
	.pv-text {
		font-size: 26rpx;
		color: #55554F;
		line-height: 1.7;
		margin-top: 24rpx;
	}
	.pv-link {
		font-size: 26rpx;
		color: var(--primary);
		margin-top: 14rpx;
	}
	.pv-actions {
		display: flex;
		gap: 20rpx;
		margin-top: 40rpx;
	}
	.pv-btn {
		flex: 1;
		text-align: center;
		padding: 22rpx 0;
		border-radius: 40rpx;
		font-size: 28rpx;
	}
	.pv-btn.ghost {
		background: #F1EFE9;
		color: #8A8A86;
	}
	.pv-btn.primary {
		background: var(--primary);
		color: #FFFFFF;
	}
</style>
