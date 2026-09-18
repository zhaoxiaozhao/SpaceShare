<template>
	<page-meta :page-style="pageThemeStyle" />
	<PrivacyPopup />
	<view v-if="current" class="quiz">
		<view class="q-top">
			<text class="q-index">{{index + 1}} / {{questions.length}}</text>
			<view class="q-bar">
				<view class="q-bar-fill" :style="{ width: ((index + 1) / questions.length * 100) + '%' }"></view>
			</view>
			<text class="q-dim">{{current.dimensionLabel}}</text>
		</view>

		<view class="q-card">
			<text class="q-text">{{current.text}}</text>
			<view
				class="q-opt"
				v-for="(o, i) in current.options"
				:key="i"
				:class="{ on: answers[current.id] === i }"
				@click="pick(i)"
			>{{o.label}}</view>
		</view>

		<view class="q-foot">
			<text v-if="index > 0" class="q-back" @click="back">← 上一题</text>
			<text v-else class="q-hint">凭第一感觉选就好，没有对错</text>
		</view>
	</view>
	<view v-else class="empty">加载中…</view>
</template>

<script>
	import { api } from '../../utils/request.js'

	export default {
		data() {
			return {
				questions: [],
				index: 0,
				answers: {},
				submitting: false
			}
		},
		computed: {
			current() {
				return this.questions[this.index] || null
			}
		},
		onLoad() {
			if (!uni.getStorageSync('token')) {
				uni.redirectTo({ url: '/pages/login/login' })
				return
			}
			this.load()
		},
		methods: {
			async load() {
				try {
					this.questions = (await api.getPersonaQuestions()) || []
				} catch (e) {}
			},
			back() {
				if (this.index > 0) this.index--
			},
			async pick(i) {
				if (!this.current || this.submitting) return
				this.answers[this.current.id] = i
				if (this.index < this.questions.length - 1) {
					this.index++
					return
				}
				await this.submit()
			},
			async submit() {
				this.submitting = true
				uni.showLoading({ title: '生成中', mask: true })
				try {
					const list = Object.keys(this.answers).map((questionId) => ({
						questionId,
						optionIndex: this.answers[questionId]
					}))
					await api.submitPersona({ answers: list })
					uni.hideLoading()
					uni.redirectTo({ url: '/pages/persona/result' })
				} catch (e) {
					uni.hideLoading()
					this.submitting = false
					uni.showToast({ title: e.message || '生成失败', icon: 'none' })
				}
			}
		}
	}
</script>

<style scoped>
	.quiz { padding: 30rpx 30rpx 60rpx; }
	.q-top { display: flex; align-items: center; gap: 16rpx; }
	.q-index { font-size: 24rpx; color: var(--primary); font-weight: 600; }
	.q-dim { font-size: 22rpx; color: #B0B0AB; margin-left: auto; }
	.q-bar { flex: 1; height: 10rpx; background: #EFEEE9; border-radius: 5rpx; overflow: hidden; }
	.q-bar-fill { height: 100%; background: var(--primary); border-radius: 5rpx; transition: width 0.25s; }
	.q-card { margin-top: 60rpx; }
	.q-text { display: block; font-size: 40rpx; font-weight: 700; line-height: 1.5; color: #2B2B27; }
	.q-opt {
		margin-top: 24rpx;
		background: #FFFFFF;
		border-radius: 20rpx;
		padding: 32rpx 30rpx;
		font-size: 30rpx;
		color: #33332E;
		box-shadow: 0 4rpx 16rpx rgba(0,0,0,0.04);
		border: 2rpx solid transparent;
	}
	.q-opt.on { border-color: var(--primary); background: var(--primary-bg); color: var(--primary); font-weight: 600; }
	.q-foot { margin-top: 40rpx; text-align: center; }
	.q-back { font-size: 26rpx; color: var(--primary); }
	.q-hint { font-size: 24rpx; color: #B0B0AB; }
	.empty { display: flex; align-items: center; justify-content: center; min-height: 60vh; color: #B0B0AB; font-size: 26rpx; }
</style>
