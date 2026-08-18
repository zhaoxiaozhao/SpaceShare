<template>
	<view>
		<!-- 今日学习卡片 -->
		<view class="card today-card" :class="{ active: today.activeSession }">
			<text class="today-label">{{today.activeSession ? '正在学习' : '今日学习'}}</text>
			<text class="today-time">{{formatMinutes(today.activeSession ? activeElapsed : today.todayMinutes)}}</text>
			<text class="today-sub">
				{{today.activeSession ? '本次已学习' : '今天已学习'}} · {{today.sessionCount}} 次
				<text v-if="today.consecutiveDays > 0"> · 连续 {{today.consecutiveDays}} 天</text>
			</text>
		</view>

		<!-- 学习类型选择（未学习时） -->
		<view class="card" v-if="!today.activeSession">
			<text class="section-label">本次学习类型</text>
			<view class="type-grid">
				<view
					class="type-chip"
					:class="{ active: studyType === t.value }"
					v-for="t in studyTypes"
					:key="t.value"
					@click="studyType = t.value"
				>{{t.label}}</view>
			</view>
			<button class="btn-primary start-btn" @click="startStudy">开始学习</button>
		</view>

		<!-- 学习计时中 -->
		<view class="card" v-else>
			<view class="active-info">
				<text class="active-type">{{typeLabel(today.activeSession.type)}}</text>
				<text class="active-time">{{activeElapsedText}}</text>
			</view>
			<button class="btn-outline end-btn" @click="endStudy">结束学习</button>
		</view>

		<!-- 今日目标 -->
		<view class="card">
			<view class="goal-head">
				<text class="section-label">今日目标</text>
				<text class="goal-edit" @click="showGoalModal = true">{{today.targetMinutes ? '调整' : '设置'}}</text>
			</view>
			<template v-if="today.targetMinutes">
				<view class="progress-bar">
					<view class="progress-fill" :style="{ width: Math.min(today.targetProgress, 100) + '%' }"></view>
				</view>
				<text class="progress-text">{{today.todayMinutes}} / {{today.targetMinutes}} 分钟 · {{today.targetProgress}}%</text>
			</template>
			<text class="goal-empty" v-else>设置今日目标，让学习更有方向</text>
		</view>

		<!-- 快捷入口 -->
		<view class="quick-row">
			<view class="quick-btn" @click="goReport('weekly')">
				<text class="quick-icon">📊</text>
				<text>周报</text>
			</view>
			<view class="quick-btn" @click="goReport('monthly')">
				<text class="quick-icon">📈</text>
				<text>月报</text>
			</view>
			<view class="quick-btn" @click="goAchievements">
				<text class="quick-icon">🏅</text>
				<text>成就</text>
			</view>
			<view class="quick-btn" @click="goHistory">
				<text class="quick-icon">📚</text>
				<text>记录</text>
			</view>
		</view>

		<!-- 最近学习记录 -->
		<view class="section" v-if="sessions.length">
			<text class="section-title">最近学习</text>
			<view class="card" v-for="s in sessions.slice(0, 5)" :key="s.id">
				<view class="record-row">
					<text class="record-type">{{typeLabel(s.type)}}</text>
					<text class="record-duration">{{formatMinutes(s.durationMinutes)}}</text>
				</view>
				<text class="record-time">{{formatDateTime(s.startedAt)}}</text>
			</view>
		</view>

		<view v-if="!today.activeSession && !sessions.length" class="empty">还没有学习记录，开始第一次学习吧</view>

		<!-- 目标设置弹层 -->
		<view class="modal-mask" v-if="showGoalModal" @click="showGoalModal = false">
			<view class="modal" @click.stop>
				<text class="modal-title">设置今日目标</text>
				<view class="goal-options">
					<view
						class="goal-opt"
						:class="{ active: goalMinutes === g }"
						v-for="g in [30, 60, 120, 240, 360]"
						:key="g"
						@click="goalMinutes = g"
					>{{g}} 分钟</view>
				</view>
				<button class="btn-primary modal-btn" @click="saveGoal">保存目标</button>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	const STUDY_TYPES = [
		{ label: '阅读', value: 'Reading' },
		{ label: '编程', value: 'Programming' },
		{ label: '英语', value: 'English' },
		{ label: '考研', value: 'Exam' },
		{ label: '考公', value: 'Postgraduate' },
		{ label: '论文', value: 'Papers' },
		{ label: '其他', value: 'Other' }
	]

	export default {
		data() {
			return {
				studyTypes: STUDY_TYPES,
				studyType: 'Reading',
				today: {},
				sessions: [],
				showGoalModal: false,
				goalMinutes: 120,
				activeElapsed: 0,
				activeElapsedText: '00:00',
				timer: null
			}
		},
		onShow() {
			this.load()
			this.startTimer()
		},
		onHide() {
			this.stopTimer()
		},
		onUnload() {
			this.stopTimer()
		},
		methods: {
			async load() {
				try {
					this.today = await api.getStudyToday()
					this.sessions = await api.getStudySessions(20)
					if (this.today.targetMinutes) this.goalMinutes = this.today.targetMinutes
				} catch (e) {}
			},
			startTimer() {
				this.stopTimer()
				this.tick()
				this.timer = setInterval(() => this.tick(), 1000)
			},
			stopTimer() {
				if (this.timer) {
					clearInterval(this.timer)
					this.timer = null
				}
			},
			tick() {
				if (!this.today || !this.today.activeSession) return
				const started = new Date(this.today.activeSession.startedAt)
				const now = Date.now()
				const ms = Math.max(0, now - started.getTime())
				this.activeElapsed = Math.floor(ms / 60000)
				const totalSec = Math.floor(ms / 1000)
				const h = String(Math.floor(totalSec / 3600)).padStart(2, '0')
				const m = String(Math.floor((totalSec % 3600) / 60)).padStart(2, '0')
				this.activeElapsedText = `${h}:${m}`
			},
			async startStudy() {
				uni.showLoading({ title: '开始学习', mask: true })
				try {
					await api.startStudy({ type: this.studyType })
					uni.hideLoading()
					uni.showToast({ title: '开始学习', icon: 'success' })
					this.load()
				} catch (e) {
					uni.hideLoading()
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			},
			async endStudy() {
				const that = this
				uni.showModal({
					title: '结束学习',
					content: '确定结束本次学习吗？',
					success: async (res) => {
						if (!res.confirm) return
						uni.showLoading({ title: '保存中', mask: true })
						try {
							await api.endActiveStudy()
							uni.hideLoading()
							uni.showToast({ title: '已记录', icon: 'success' })
							that.load()
						} catch (e) {
							uni.hideLoading()
							uni.showToast({ title: e.message || '操作失败', icon: 'none' })
						}
					}
				})
			},
			async saveGoal() {
				uni.showLoading({ title: '保存中', mask: true })
				try {
					await api.setStudyGoal({ period: 'Daily', targetMinutes: this.goalMinutes })
					uni.hideLoading()
					this.showGoalModal = false
					uni.showToast({ title: '已保存', icon: 'success' })
					this.load()
				} catch (e) {
					uni.hideLoading()
					uni.showToast({ title: e.message || '保存失败', icon: 'none' })
				}
			},
			goReport(period) {
				uni.navigateTo({ url: `/pages/study/report?period=${period}` })
			},
			goAchievements() {
				uni.navigateTo({ url: '/pages/study/achievements' })
			},
			goHistory() {
				uni.navigateTo({ url: '/pages/study/history' })
			},
			typeLabel(v) {
				const t = STUDY_TYPES.find(x => x.value === v)
				return t ? t.label : v
			},
			formatMinutes(min) {
				if (!min && min !== 0) return '0 分钟'
				if (min >= 60) {
					const h = Math.floor(min / 60)
					const m = min % 60
					return m ? `${h} 小时 ${m} 分` : `${h} 小时`
				}
				return `${min} 分钟`
			},
			formatDateTime(s) {
				if (!s) return ''
				const d = new Date(s)
				return `${d.getMonth() + 1}月${d.getDate()}日 ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
			}
		}
	}
</script>

<style scoped>
	.today-card {
		background: linear-gradient(160deg, #3A8A7E, #5BA48D);
		color: #FFFFFF;
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 50rpx 30rpx;
		gap: 8rpx;
	}
	.today-card.active {
		background: linear-gradient(160deg, #2F6F65, #4A9183);
	}
	.today-label {
		font-size: 26rpx;
		opacity: 0.9;
	}
	.today-time {
		font-size: 64rpx;
		font-weight: 700;
	}
	.today-sub {
		font-size: 24rpx;
		opacity: 0.85;
	}
	.section-label {
		font-size: 30rpx;
		font-weight: 600;
		display: block;
		margin-bottom: 20rpx;
	}
	.type-grid {
		display: flex;
		flex-wrap: wrap;
		gap: 20rpx;
	}
	.type-chip {
		padding: 16rpx 36rpx;
		background: #F7F5EF;
		border-radius: 40rpx;
		font-size: 28rpx;
		color: #55554F;
	}
	.type-chip.active {
		background: #3A8A7E;
		color: #FFFFFF;
	}
	.start-btn {
		margin-top: 30rpx;
	}
	.active-info {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.active-type {
		font-size: 30rpx;
		font-weight: 600;
	}
	.active-time {
		font-size: 44rpx;
		font-weight: 700;
		color: #3A8A7E;
	}
	.end-btn {
		margin-top: 30rpx;
	}
	.goal-head {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.goal-edit {
		font-size: 26rpx;
		color: #3A8A7E;
	}
	.progress-bar {
		height: 20rpx;
		background: #F0EFEA;
		border-radius: 10rpx;
		overflow: hidden;
		margin-top: 20rpx;
	}
	.progress-fill {
		height: 100%;
		background: linear-gradient(90deg, #3A8A7E, #5BA48D);
		border-radius: 10rpx;
		transition: width 0.3s;
	}
	.progress-text {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin-top: 12rpx;
	}
	.goal-empty {
		font-size: 26rpx;
		color: #B0B0AB;
	}
	.quick-row {
		display: flex;
		margin: 20rpx;
		gap: 20rpx;
	}
	.quick-btn {
		flex: 1;
		background: #FFFFFF;
		border-radius: 20rpx;
		padding: 24rpx 0;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 8rpx;
		font-size: 24rpx;
		color: #55554F;
		box-shadow: 0 4rpx 16rpx rgba(0, 0, 0, 0.04);
	}
	.quick-icon {
		font-size: 40rpx;
	}
	.record-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.record-type {
		font-size: 30rpx;
		font-weight: 600;
	}
	.record-duration {
		font-size: 28rpx;
		color: #3A8A7E;
		font-weight: 600;
	}
	.record-time {
		font-size: 22rpx;
		color: #B0B0AB;
	}
	.modal-mask {
		position: fixed;
		top: 0; left: 0; right: 0; bottom: 0;
		background: rgba(0, 0, 0, 0.4);
		z-index: 100;
		display: flex;
		align-items: center;
		justify-content: center;
	}
	.modal {
		background: #FFFFFF;
		border-radius: 24rpx;
		padding: 40rpx;
		width: 600rpx;
	}
	.modal-title {
		font-size: 32rpx;
		font-weight: 600;
		display: block;
		text-align: center;
		margin-bottom: 30rpx;
	}
	.goal-options {
		display: flex;
		flex-wrap: wrap;
		gap: 20rpx;
	}
	.goal-opt {
		padding: 16rpx 30rpx;
		background: #F7F5EF;
		border-radius: 16rpx;
		font-size: 28rpx;
	}
	.goal-opt.active {
		background: #3A8A7E;
		color: #FFFFFF;
	}
	.modal-btn {
		margin-top: 40rpx;
	}
</style>
