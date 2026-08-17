<template>
	<view>
		<view v-if="targetType">
			<!-- 被举报人 -->
			<view class="card" v-if="targetNickname || targetUserId">
				<text class="form-label">被举报人</text>
				<view class="target-box">
					<text class="target-name">{{ targetNickname || ('用户#' + targetUserId) }}</text>
				</view>
			</view>
			<view class="card">
				<text class="form-label">举报原因</text>
				<radio-group @change="onReason">
					<label class="radio-row" v-for="r in reasons" :key="r">
						<radio :value="r" :checked="reason === r" color="#3A8A7E" />
						<text class="radio-text">{{r}}</text>
					</label>
				</radio-group>
			</view>
			<view class="card">
				<text class="form-label">详细描述</text>
				<textarea class="form-textarea" v-model="description" maxlength="100" placeholder="请描述具体情况（选填，100字以内）" />
				<button class="btn-primary" style="margin-top:30rpx;" @click="submit">提交举报</button>
			</view>
		</view>
		<view v-else>
			<view v-if="reports.length">
				<view class="card" v-for="r in reports" :key="r.id">
					<view class="rep-top">
						<text class="rep-reason">{{r.reason}}</text>
						<text class="tag">{{statusText(r.status)}}</text>
					</view>
					<text class="rep-desc" v-if="r.description">{{r.description}}</text>
					<text class="rep-time">{{formatTime(r.createdAt)}}</text>
				</view>
			</view>
			<view v-else class="empty">暂无举报记录</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime, statusText } from '../../utils/format.js'

	export default {
		data() {
			return {
				targetType: '',
				targetId: null,
				targetUserId: null,
				targetNickname: '',
				reasons: ['虚假座位', '座位不存在', '座位被占用', '座位交易', '恶意占座', '联系方式违规', '其他'],
				reason: '',
				description: '',
				reports: []
			}
		},
		onLoad(options) {
			if (options.targetType) {
				this.targetType = options.targetType
				this.targetId = options.targetId ? parseInt(options.targetId) : null
				this.targetUserId = options.targetUserId ? parseInt(options.targetUserId) : null
				this.targetNickname = options.targetNickname ? decodeURIComponent(options.targetNickname) : ''
			}
		},
		onShow() {
			if (!this.targetType) {
				this.loadReports()
			}
		},
		methods: {
			formatTime,
			statusText,
			onReason(e) {
				this.reason = e.detail.value
			},
			async loadReports() {
				try {
					this.reports = await api.getMyReports()
				} catch (e) {}
			},
			async submit() {
				if (!this.reason) {
					uni.showToast({ title: '请选择举报原因', icon: 'none' })
					return
				}
				try {
					await api.createReport({
						targetType: this.targetType,
						targetId: this.targetId,
						targetUserId: this.targetUserId,
						reason: this.reason,
						description: this.description
					})
					uni.showToast({ title: '举报已提交', icon: 'success' })
					setTimeout(() => uni.navigateBack(), 800)
				} catch (e) {
					uni.showToast({ title: e.message || '提交失败', icon: 'none' })
				}
			}
		}
	}
</script>

<style scoped>
	.form-label {
		font-size: 28rpx;
		font-weight: 600;
		display: block;
		margin-bottom: 16rpx;
	}
	.radio-row {
		display: flex;
		align-items: center;
		padding: 14rpx 0;
		gap: 12rpx;
	}
	.radio-text {
		font-size: 28rpx;
	}
	.form-textarea {
		width: 100%;
		height: 200rpx;
		background: #F7F5EF;
		border-radius: 12rpx;
		padding: 20rpx;
		font-size: 28rpx;
		box-sizing: border-box;
	}
	.target-box {
		background: #F7F5EF;
		border-radius: 12rpx;
		padding: 20rpx 24rpx;
	}
	.target-name {
		font-size: 30rpx;
		font-weight: 600;
		color: #33332E;
	}
	.rep-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.rep-reason {
		font-size: 30rpx;
		font-weight: 600;
	}
	.rep-desc {
		display: block;
		font-size: 26rpx;
		color: #55554F;
		margin: 10rpx 0;
	}
	.rep-time {
		font-size: 22rpx;
		color: #B0B0AB;
	}
</style>
