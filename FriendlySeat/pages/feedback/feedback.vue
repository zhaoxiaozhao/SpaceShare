<template>
	<view>
		<view class="card">
			<text class="section-label">意见反馈</text>
			<textarea
				class="feedback-input"
				v-model="content"
				placeholder="请描述你遇到的问题或建议，我们会认真查看并改进（500字以内）"
				:maxlength="500"
			/>
			<button class="btn-primary submit-btn" :disabled="submitting || !content.trim()" @click="submit">提交反馈</button>
			<text class="note">你的反馈将帮助友邻座变得更好，感谢支持！</text>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				content: '',
				submitting: false
			}
		},
		methods: {
			async submit() {
				if (this.submitting) return
				const reason = this.content.trim()
				if (!reason) {
					uni.showToast({ title: '请填写反馈内容', icon: 'none' })
					return
				}
				this.submitting = true
				uni.showLoading({ title: '提交中', mask: true })
				try {
					await api.createReport({ targetType: 'Feedback', reason, description: reason })
					uni.hideLoading()
					uni.showToast({ title: '已提交，感谢反馈', icon: 'success' })
					setTimeout(() => uni.navigateBack(), 600)
				} catch (e) {
					uni.hideLoading()
					uni.showToast({ title: e.message || '提交失败', icon: 'none' })
				} finally {
					this.submitting = false
				}
			}
		}
	}
</script>

<style scoped>
	.section-label {
		display: block;
		font-size: 30rpx;
		font-weight: 600;
		margin-bottom: 20rpx;
	}
	.feedback-input {
		width: 100%;
		height: 240rpx;
		background: #F7F5EF;
		border-radius: 16rpx;
		padding: 24rpx;
		font-size: 28rpx;
		box-sizing: border-box;
	}
	.submit-btn {
		margin-top: 30rpx;
	}
	.note {
		display: block;
		margin-top: 20rpx;
		font-size: 22rpx;
		color: #B0B0AB;
		text-align: center;
	}
</style>