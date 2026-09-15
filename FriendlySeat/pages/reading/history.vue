<template>
	<page-meta :page-style="pageThemeStyle" />
		<view>
		<view v-if="sessions.length">
			<view class="card session-item" v-for="s in sessions" :key="s.id" @click="goBook(s.bookId)">
				<view class="s-top">
					<view class="s-book">
						<image class="inline-icon" :src="`/static/icons/book-${season}.png`" mode="aspectFit" />
						<text>{{s.bookTitle}}</text>
					</view>
					<text class="s-time">{{s.durationMinutes}} 分钟</text>
				</view>
				<text class="s-date">{{formatTime(s.startedAt)}} ~ {{formatTime(s.endedAt)}}</text>
			</view>
		</view>
		<view v-else class="empty">还没有阅读记录，去读一本书吧</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'
	import { getSeasonKey } from '../../utils/theme.js'

	export default {
		data() {
			return { season: getSeasonKey(), sessions: [] }
		},
		onShow() {
			this.load()
		},
		methods: {
			formatTime,
			async load() {
				try {
					this.sessions = await api.getReadingSessions(100)
				} catch (e) {}
			},
			goBook(id) {
				uni.navigateTo({ url: `/pages/reading/book?id=${id}` })
			}
		}
	}
</script>

<style scoped>
	.session-item { display: flex; flex-direction: column; gap: 8rpx; }
	.s-top { display: flex; justify-content: space-between; align-items: center; }
	.s-book { display: flex; align-items: center; gap: 8rpx; font-size: 30rpx; font-weight: 600; min-width: 0; }
	.inline-icon { width: 34rpx; height: 34rpx; flex-shrink: 0; }
	.s-time { font-size: 28rpx; color: var(--primary); font-weight: 600; }
	.s-date { font-size: 24rpx; color: #8A8A86; }
</style>