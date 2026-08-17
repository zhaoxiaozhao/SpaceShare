<template>
	<view>
		<view class="card hero">
			<text class="hero-title">支持项目</text>
			<text class="hero-desc">你的支持将用于服务器的运行与维护，让更多空闲座位继续流动</text>
		</view>

		<view class="card costs">
			<view class="cost-row">
				<text>本月运行成本</text>
				<text>¥{{summary.monthCost || 200}}</text>
			</view>
			<view class="cost-row">
				<text>累计支持</text>
				<text>¥{{summary.totalAmount || 0}}（{{summary.totalCount || 0}}人）</text>
			</view>
		</view>

		<view class="card amounts">
			<text class="amounts-title">选择支持金额</text>
			<view class="amount-grid">
				<view
					class="amount-chip"
					:class="{ active: amount === a }"
					v-for="a in [3, 6, 10, 20]"
					:key="a"
					@click="amount = a"
				>¥{{a}}</view>
			</view>
			<view class="form-item">
				<text class="form-label">自定义金额（元）</text>
				<input class="form-input" type="digit" v-model="customAmount" placeholder="输入金额" />
			</view>
			<view class="form-item switch-row">
				<text class="form-label">公开支持记录</text>
				<switch :checked="isPublic" color="#3A8A7E" @change="onPublic" />
			</view>
			<button class="btn-primary" style="margin-top:30rpx;" @click="submit">支持项目</button>
		</view>

		<view class="card notice">
			<text class="notice-title">重要说明</text>
			<text class="notice-line">支持项目是自愿行为，与预约、信用、风控完全无关。</text>
			<text class="notice-line">支持不会带来任何预约特权、信用加分或热门座位解锁。</text>
			<text class="notice-line">所有座位功能始终免费。</text>
		</view>

		<view class="section" v-if="summary.myDonations && summary.myDonations.length">
			<text class="section-title">我的支持记录</text>
			<view class="card" v-for="d in summary.myDonations" :key="d.id">
				<view class="don-row">
					<text class="don-amount">¥{{d.amount}}</text>
					<text class="don-status">{{statusText(d.status)}}</text>
				</view>
				<text class="don-time">{{formatTime(d.createdAt)}}</text>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime, statusText } from '../../utils/format.js'

	export default {
		data() {
			return {
				summary: {},
				amount: 10,
				customAmount: '',
				isPublic: true
			}
		},
		onShow() {
			this.load()
		},
		methods: {
			formatTime,
			statusText,
			async load() {
				try {
					this.summary = await api.getDonations()
				} catch (e) {}
			},
			onPublic(e) {
				this.isPublic = e.detail.value
			},
			async submit() {
				let val = this.amount
				if (this.customAmount) {
					val = parseFloat(this.customAmount)
				}
				if (!val || val <= 0) {
					uni.showToast({ title: '请输入有效的支持金额', icon: 'none' })
					return
				}
				try {
					const r = await api.createDonation({ amount: val, isPublic: this.isPublic })
					uni.showModal({
						title: '谢谢你的支持',
						content: '已生成支持记录（MVP 阶段演示，支付功能按微信审核结果接入）。',
						showCancel: false
					})
					this.load()
				} catch (e) {
					uni.showToast({ title: e.message || '操作失败', icon: 'none' })
				}
			}
		}
	}
</script>

<style scoped>
	.hero {
		background: linear-gradient(160deg, #3A8A7E, #5BA48D);
		display: flex;
		flex-direction: column;
		align-items: center;
		padding: 50rpx 30rpx;
		gap: 12rpx;
	}
	.hero-title {
		font-size: 40rpx;
		font-weight: 700;
		color: #FFFFFF;
	}
	.hero-desc {
		font-size: 24rpx;
		color: #FFFFFF;
		opacity: 0.9;
		text-align: center;
	}
	.cost-row {
		display: flex;
		justify-content: space-between;
		padding: 16rpx 0;
		font-size: 28rpx;
	}
	.amounts-title {
		font-size: 30rpx;
		font-weight: 600;
		display: block;
		margin-bottom: 20rpx;
	}
	.amount-grid {
		display: flex;
		gap: 20rpx;
	}
	.amount-chip {
		flex: 1;
		text-align: center;
		padding: 20rpx 0;
		background: #F7F5EF;
		border-radius: 16rpx;
		font-size: 32rpx;
		color: #55554F;
	}
	.amount-chip.active {
		background: #3A8A7E;
		color: #FFFFFF;
		font-weight: 700;
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
	.switch-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.notice-title {
		font-size: 30rpx;
		font-weight: 600;
		display: block;
		margin-bottom: 12rpx;
	}
	.notice-line {
		display: block;
		font-size: 24rpx;
		color: #55554F;
		padding: 4rpx 0;
	}
	.don-row {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.don-amount {
		font-size: 30rpx;
		font-weight: 600;
		color: #3A8A7E;
	}
	.don-status {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.don-time {
		font-size: 22rpx;
		color: #B0B0AB;
	}
</style>
