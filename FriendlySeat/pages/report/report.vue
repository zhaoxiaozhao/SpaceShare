<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
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
						<radio :value="r" :checked="reason === r" color="var(--primary)" />
						<text class="radio-text">{{r}}</text>
					</label>
				</radio-group>
			</view>
			<view class="card">
				<text class="form-label">详细描述</text>
				<textarea class="form-textarea" v-model="description" maxlength="100" placeholder="请描述具体情况（选填，100字以内）" />
				<text class="form-label" style="margin-top:24rpx;">图片证据（选填）</text>
				<view class="evidence-box">
					<image v-if="evidenceUrl" class="evidence-img" :src="evidenceUrl" mode="aspectFill" @click="previewEvidence" />
					<view v-else class="evidence-empty" @click="chooseEvidence">＋ 上传图片</view>
					<text v-if="evidenceUrl" class="evidence-remove" @click="evidenceUrl = ''">删除</text>
				</view>
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
	import { uploadImage, getTempFileUrl } from '../../utils/profile.js'
	import { subscribeFor } from '../../utils/subscribe.js'

	export default {
		data() {
			return {
				targetType: '',
				targetId: null,
				targetUserId: null,
				targetNickname: '',
				reason: '',
				description: '',
				evidenceUrl: '',
				reports: []
			}
		},
		computed: {
			reasons() {
				if (this.targetType === 'Activity') {
					return ['虚假活动', '内容违规', '广告导流', '联系方式违规', '涉嫌诈骗', '其他']
				}
				if (this.targetType === 'BookListShare') {
					return ['内容违规', '广告导流', '联系方式违规', '虚假信息', '侵犯隐私', '其他']
				}
				if (this.targetType === 'SeatNote') {
					return ['内容违规', '广告导流', '联系方式违规', '辱骂骚扰', '虚假信息', '其他']
				}
				if (this.targetType === 'ActivityComment') {
					return ['内容违规', '广告导流', '联系方式违规', '辱骂骚扰', '虚假信息', '其他']
				}
				if (this.targetType === 'VenuePost' || this.targetType === 'VenuePostComment') {
					return ['内容违规', '广告导流', '联系方式违规', '辱骂骚扰', '虚假信息', '其他']
				}
				return ['虚假座位', '座位不存在', '座位被占用', '座位交易', '恶意占座', '联系方式违规', '其他']
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
			chooseEvidence() {
				uni.chooseImage({
					count: 1,
					sizeType: ['compressed'],
					sourceType: ['album', 'camera'],
					success: async (res) => {
						const filePath = res.tempFilePaths && res.tempFilePaths[0]
						if (!filePath) return
						uni.showLoading({ title: '上传中', mask: true })
						try {
							this.evidenceUrl = await uploadImage(filePath, 'reports')
						} catch (e) {
							uni.showToast({ title: '上传失败，请重试', icon: 'none' })
						} finally {
							uni.hideLoading()
						}
					}
				})
			},
			async previewEvidence() {
				if (!this.evidenceUrl) return
				let url = this.evidenceUrl
				try { url = (await getTempFileUrl(this.evidenceUrl)) || this.evidenceUrl } catch (e) {}
				uni.previewImage({ urls: [url] })
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
						description: this.description,
						evidenceUrl: this.evidenceUrl || undefined
					})
					uni.showToast({ title: '举报已提交', icon: 'success' })
					subscribeFor(['report_result'])
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
	.evidence-box {
		position: relative;
		width: 320rpx;
		height: 320rpx;
	}
	.evidence-img {
		width: 320rpx;
		height: 320rpx;
		border-radius: 12rpx;
	}
	.evidence-empty {
		width: 320rpx;
		height: 320rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		background: #F7F5EF;
		border: 1rpx dashed #DAD7CE;
		border-radius: 12rpx;
		color: #A5A39D;
		font-size: 28rpx;
	}
	.evidence-remove {
		position: absolute;
		right: 12rpx;
		bottom: 12rpx;
		font-size: 22rpx;
		color: #fff;
		background: rgba(0, 0, 0, 0.5);
		padding: 4rpx 16rpx;
		border-radius: 999rpx;
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
