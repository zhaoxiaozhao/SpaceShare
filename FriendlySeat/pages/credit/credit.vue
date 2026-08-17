<template>
	<view>
		<view class="card score-card">
			<text class="score-label">友邻信用</text>
			<text class="score-num">{{summary.score || 100}}</text>
			<text class="score-level">{{summary.level || '正常'}}</text>
		</view>

		<view class="card rules">
			<text class="rules-title">信用规则</text>
			<text class="rules-line">正常到座：+1</text>
			<text class="rules-line">正常完成预约：+1</text>
			<text class="rules-line">爽约未到：-5</text>
			<text class="rules-line">恶意占座/虚假座位：-10</text>
			<text class="rules-line">座位交易：-20</text>
		</view>

		<view class="section">
			<text class="section-title">信用流水</text>
			<view v-if="summary.transactions && summary.transactions.length">
				<view class="card tx-card" v-for="t in summary.transactions" :key="t.id">
					<view class="tx-top">
						<text class="tx-reason">{{t.reason}}</text>
						<text class="tx-change" :class="t.change >= 0 ? 'plus' : 'minus'">{{t.change >= 0 ? '+' : ''}}{{t.change}}</text>
					</view>
					<text class="tx-time">{{formatTime(t.createdAt)}}</text>
				</view>
			</view>
			<view v-else class="empty">暂无信用流水</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'

	export default {
		data() {
			return {
				summary: {}
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			formatTime,
			async load() {
				try {
					this.summary = await api.getCredit()
				} catch (e) {}
			}
		}
	}
</script>

<style scoped>
	.score-card {
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 50rpx;
		gap: 10rpx;
	}
	.score-label {
		font-size: 26rpx;
		color: #8A8A86;
	}
	.score-num {
		font-size: 90rpx;
		font-weight: 700;
		color: #3A8A7E;
	}
	.score-level {
		font-size: 28rpx;
		color: #55554F;
	}
	.rules-title {
		font-size: 30rpx;
		font-weight: 600;
		display: block;
		margin-bottom: 16rpx;
	}
	.rules-line {
		display: block;
		font-size: 26rpx;
		color: #55554F;
		padding: 6rpx 0;
	}
	.tx-card {
		padding: 24rpx;
	}
	.tx-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.tx-reason {
		font-size: 28rpx;
	}
	.tx-change {
		font-size: 30rpx;
		font-weight: 700;
	}
	.tx-change.plus { color: #3A8A7E; }
	.tx-change.minus { color: #B85450; }
	.tx-time {
		font-size: 22rpx;
		color: #B0B0AB;
	}
</style>
