<template>
	<view>
		<!-- 从座位详情进入分享：正常显示分享表单；无座位时自动去选座 -->
		<view v-if="!seatId" class="empty">请先从座位详情发起分享</view>
		<view v-else-if="!seatInfo && !loadingFail" class="empty">座位信息加载中…</view>
		<view v-else-if="loadingFail" class="empty">
			座位信息加载失败，请返回重试
			<button class="btn-outline" style="margin-top:20rpx;" @click="load">重试</button>
		</view>

		<view class="card" v-if="seatId && seatInfo">
			<view class="section-label">分享座位</view>
			<text class="current-seat">{{seatInfo.displayCode || seatInfo.code}}</text>
			<text class="venue-name">{{seatInfo.venueName}}</text>
		</view>

		<view class="card" v-if="seatInfo">
			<text class="section-label">预计什么时候离开？</text>
			<text class="hint-text">座位会从现在起释放到预计离开时间，留给下一位友邻预约（{{closingTime}} 闭馆）</text>

			<!-- 快捷时长（自动过滤超出闭馆时间的选项） -->
			<view class="duration-grid">
				<view
					class="duration-chip"
					:class="{ active: !customTime && duration === d, disabled: !availableDurations.includes(d) }"
					v-for="d in durationOptions"
					:key="d"
					@click="pickDuration(d)"
				>{{durationText(d)}}</view>
			</view>
			<text class="hint-text" v-if="!availableDurations.length">现在距离闭馆不足1小时，无法分享</text>

			<!-- 自定义预计离开时刻 -->
			<picker mode="time" :value="customTime || '18:00'" @change="onCustomTime" class="custom-picker">
				<view class="custom-chip" :class="{ active: customTime }">
					<text>⏰ 自定义离开时刻</text>
					<text class="custom-value" v-if="customTime">{{customTime}}</text>
					<text class="custom-value" v-else>点击选择</text>
				</view>
			</picker>

			<view class="share-summary">
				<text class="summary-label">预计离开</text>
				<text class="summary-value">{{formatTime(shareEnd)}}</text>
			</view>
			<view class="share-summary sub">
				<text class="summary-label">下一位可预约</text>
				<text class="summary-value">现在 ~ {{formatTime(shareEnd)}}</text>
			</view>

			<view class="form-item">
				<text class="form-label">座位环境（可选）</text>
				<view class="note-tags">
					<view
						class="note-tag"
						:class="{ active: selectedTags.includes(t) }"
						v-for="t in noteOptions"
						:key="t"
						@click="toggleTag(t)"
					>{{t}}</view>
				</view>
			</view>

			<button class="btn-primary" style="margin-top:30rpx;" @click="submit">分享这一席</button>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'

	export default {
		data() {
			return {
				seatId: null,
				seatInfo: null,
				noteOptions: ['靠窗', '有插座', '安静', '光线好'],
				selectedTags: [],
				allowContact: false, // 个人主体阶段隐藏联系方式分享
				duration: 1,
					customTime: '', // 自定义预计离开时刻 "HH:mm"
					loadingFail: false
				}
			},
		computed: {
			durationOptions() {
				return [1, 2, 3, 4, 5, 6, 7, 8]
			},
			// 闭馆时间文本（HH:mm）
			closingTime() {
				return (this.seatInfo && this.seatInfo.closingTime) || '22:00'
			},
			// 今日闭馆时刻（Date）
			closingDate() {
				const [h, m] = this.closingTime.split(':').map(Number)
				const d = new Date()
				d.setHours(h, m, 0, 0)
				return d
			},
			// 可选时长：结束时间不超过闭馆
			availableDurations() {
				const now = new Date()
				return this.durationOptions.filter(d => {
					return now.getTime() + d * 3600000 <= this.closingDate.getTime()
				})
			},
			// 自定义时刻允许的最大小时数
			maxCustomHours() {
				const now = new Date()
				const ms = this.closingDate.getTime() - now.getTime()
				return ms > 0 ? Math.floor(ms / 3600000) : 0
			},
			// 预计离开时间：优先自定义时刻，否则 现在 + 时长（不超过闭馆）
			shareEnd() {
				const now = new Date()
				if (this.customTime) {
					const [h, m] = this.customTime.split(':').map(Number)
					const d = new Date(now)
					d.setHours(h, m, 0, 0)
					// 若所选时刻已过，则视为明天（明天闭馆前）
					if (d <= now) d.setDate(d.getDate() + 1)
					return d.toISOString()
				}
				const ms = now.getTime() + this.duration * 3600000
				return ms <= this.closingDate.getTime()
					? new Date(ms).toISOString()
					: this.closingDate.toISOString()
			}
		},
		onLoad(options) {
			const parsed = options.seatId ? parseInt(options.seatId) : NaN
			if (!isNaN(parsed) && parsed > 0) {
				this.seatId = parsed
			} else {
				// 无有效座位参数：去选择座位（分享应从座位详情发起）
				uni.navigateTo({ url: '/pages/venues/venues' })
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			formatTime,
			async load() {
				if (!uni.getStorageSync('token')) {
					uni.navigateTo({ url: '/pages/login/login' })
					return
				}
				if (!this.seatId) return
				this.loadingFail = false
				try {
					this.seatInfo = await api.getSeat(this.seatId)
				} catch (e) {
					this.loadingFail = true
				}
			},
			durationText(h) {
				return `${h}小时`
			},
			pickDuration(d) {
				if (!this.availableDurations.includes(d)) return
				this.duration = d
				this.customTime = ''
			},
			onCustomTime(e) {
				const t = e.detail.value
				const [h, m] = t.split(':').map(Number)
				const sel = new Date()
				sel.setHours(h, m, 0, 0)
				// 超过闭馆时间则截断到闭馆
				if (sel.getTime() > this.closingDate.getTime()) {
					uni.showToast({ title: `已超过闭馆时间，自动调整到 ${this.closingTime}`, icon: 'none' })
					this.customTime = this.closingTime
				} else {
					this.customTime = t
				}
			},
			onAllowContact(e) { this.allowContact = e.detail.value },
			toggleTag(t) {
				const idx = this.selectedTags.indexOf(t)
				if (idx >= 0) {
					this.selectedTags.splice(idx, 1)
				} else {
					this.selectedTags.push(t)
				}
			},
			async submit() {
				if (!this.seatId) {
					uni.showToast({ title: '未找到当前座位', icon: 'none' })
					return
				}
				const now = new Date()
				const startAt = now.toISOString()
				const endAt = new Date(this.shareEnd).toISOString()
				try {
					await api.createShare({
						seatId: this.seatId,
						startAt,
						endAt,
						note: this.selectedTags.join(' '),
						allowContact: this.allowContact
					})
					uni.showToast({ title: '分享成功，谢谢你的善意', icon: 'success' })
					setTimeout(() => uni.switchTab({ url: '/pages/index/index' }), 600)
				} catch (e) {
					uni.showToast({ title: e.message || '分享失败', icon: 'none' })
				}
			}
		}
	}
</script>

<style scoped>
	.section-label {
		font-size: 26rpx;
		color: #8A8A86;
		display: block;
		margin-bottom: 12rpx;
	}
	.current-seat {
		font-size: 44rpx;
		font-weight: 700;
		color: #3A8A7E;
		display: block;
		margin-bottom: 8rpx;
	}
	.venue-name, .share-time {
		display: block;
		font-size: 26rpx;
		color: #55554F;
		margin-bottom: 6rpx;
	}
	.warn-text {
		display: block;
		color: #B85450;
		font-size: 26rpx;
		margin-bottom: 20rpx;
	}
	/* 离开时长选择 */
	.duration-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 16rpx;
		margin: 24rpx 0 8rpx;
	}
	.duration-chip {
		flex: 1;
		min-width: 120rpx;
		text-align: center;
		padding: 20rpx 0;
		background: #F7F5EF;
		border-radius: 16rpx;
		font-size: 28rpx;
		color: #55554F;
		border: 2rpx solid transparent;
	}
	.duration-chip.active {
		background: #EAF3F0;
		border-color: #3A8A7E;
		color: #3A8A7E;
		font-weight: 600;
	}
	.duration-chip.disabled {
		opacity: 0.35;
		pointer-events: none;
	}
	.hint-text {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin-top: 6rpx;
	}
	/* 自定义离开时刻 */
	.custom-picker {
		margin-top: 20rpx;
		display: block;
	}
	.custom-chip {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 18rpx 24rpx;
		background: #F7F5EF;
		border-radius: 16rpx;
		font-size: 26rpx;
		color: #55554F;
		border: 2rpx solid transparent;
	}
	.custom-chip.active {
		border-color: #3A8A7E;
		color: #3A8A7E;
	}
	.custom-value {
		font-size: 26rpx;
		color: #3A8A7E;
		font-weight: 600;
	}
	.share-summary {
		margin-top: 20rpx;
		background: #F7F5EF;
		border-radius: 12rpx;
		padding: 16rpx 20rpx;
		display: flex;
		align-items: center;
		gap: 16rpx;
	}
	.share-summary.sub {
		margin-top: 10rpx;
		background: #EAF3F0;
	}
	.summary-label {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.summary-value {
		font-size: 26rpx;
		color: #3A8A7E;
		font-weight: 600;
	}
	.form-item {
		margin-top: 20rpx;
	}
	.form-label {
		font-size: 26rpx;
		color: #55554F;
		display: block;
		margin-bottom: 12rpx;
	}
	.form-input {
		background: #F7F5EF;
		border-radius: 12rpx;
		padding: 16rpx 20rpx;
		font-size: 28rpx;
	}
	.note-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 16rpx;
	}
	.note-tag {
		padding: 10rpx 26rpx;
		background: #F7F5EF;
		border-radius: 30rpx;
		font-size: 26rpx;
		color: #55554F;
		border: 2rpx solid transparent;
	}
	.note-tag.active {
		background: #EAF3F0;
		border-color: #3A8A7E;
		color: #3A8A7E;
	}
	.switch-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
</style>
