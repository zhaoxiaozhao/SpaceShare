<template>
	<view>
		<view class="card group" v-for="(group, gk) in grouped" :key="gk">
			<text class="group-date">{{gk}}</text>
			<view class="session-row" v-for="s in group" :key="s.id">
				<view class="session-info">
					<text class="session-type">{{typeLabel(s.type)}}</text>
					<text class="session-time">{{formatTime(s.startedAt)}}</text>
				</view>
				<text class="session-duration">{{formatMinutes(s.durationMinutes)}}</text>
			</view>
		</view>

		<view v-if="!sessions.length" class="empty">还没有学习记录</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	const TYPE_LABELS = {
		Reading: '阅读', Programming: '编程', English: '英语', Exam: '考研',
		Postgraduate: '考公', Papers: '论文', Other: '其他'
	}

	export default {
		data() {
			return {
				sessions: []
			}
		},
		computed: {
			grouped() {
				const groups = {}
				for (const s of this.sessions) {
					const d = new Date(s.startedAt)
					const key = `${d.getFullYear()}年${d.getMonth() + 1}月${d.getDate()}日`
					if (!groups[key]) groups[key] = []
					groups[key].push(s)
				}
				return groups
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			async load() {
				try {
					this.sessions = await api.getStudySessions(100)
				} catch (e) {}
			},
			typeLabel(v) {
				return TYPE_LABELS[v] || v
			},
			formatMinutes(min) {
				if (min >= 60) {
					const h = Math.floor(min / 60)
					const m = min % 60
					return m ? `${h}小时${m}分` : `${h}小时`
				}
				return `${min}分钟`
			},
			formatTime(s) {
				const d = new Date(s)
				return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
			}
		}
	}
</script>

<style scoped>
	.group {
		padding: 24rpx 28rpx;
	}
	.group-date {
		font-size: 26rpx;
		font-weight: 600;
		color: #3A8A7E;
		display: block;
		margin-bottom: 16rpx;
	}
	.session-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 14rpx 0;
		border-bottom: 1rpx solid #F0EFEA;
	}
	.session-row:last-child {
		border-bottom: none;
	}
	.session-type {
		font-size: 28rpx;
		font-weight: 600;
	}
	.session-time {
		font-size: 22rpx;
		color: #B0B0AB;
		margin-left: 16rpx;
	}
	.session-duration {
		font-size: 26rpx;
		color: #3A8A7E;
		font-weight: 600;
	}
</style>
