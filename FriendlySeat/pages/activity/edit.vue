<template>
	<page-meta :page-style="pageThemeStyle" />
	<view class="page">
		<view class="card">
			<text class="lb">活动标题</text>
			<input class="inp" v-model="form.title" placeholder="例如：周末读书分享会" :maxlength="50" />

			<text class="lb">分类</text>
			<picker mode="selector" :range="categoryOptions" range-key="label" :value="categoryIdx" @change="categoryIdx = Number($event.detail.value)">
				<view class="pick">{{categoryOptions[categoryIdx].label}}</view>
			</picker>

			<text class="lb">场馆（可选）</text>
			<picker mode="selector" :range="venueOptions" range-key="name" :value="venueIdx" @change="onVenueChange">
				<view class="pick">{{venueOptions[venueIdx] ? venueOptions[venueIdx].name : '不关联场馆'}}</view>
			</picker>

			<text class="lb">具体地点（可选）</text>
			<input class="inp" v-model="form.locationText" placeholder="例如：3F 主空间 A区" :maxlength="50" />

			<text class="lb">开始时间</text>
			<view class="row">
				<picker class="col" mode="date" :value="startDate" :start="today" @change="startDate = $event.detail.value">
					<view class="pick">{{startDate}}</view>
				</picker>
				<picker class="col" mode="time" :value="startTime" @change="startTime = $event.detail.value">
					<view class="pick">{{startTime}}</view>
				</picker>
			</view>

			<text class="lb">结束时间</text>
			<view class="row">
				<picker class="col" mode="date" :value="endDate" :start="startDate" @change="endDate = $event.detail.value">
					<view class="pick">{{endDate}}</view>
				</picker>
				<picker class="col" mode="time" :value="endTime" @change="endTime = $event.detail.value">
					<view class="pick">{{endTime}}</view>
				</picker>
			</view>

			<text class="lb">报名截止（可选，默认活动开始前）</text>
			<view class="row">
				<picker class="col" mode="date" :value="deadlineDate" :start="today" @change="deadlineDate = $event.detail.value">
					<view class="pick">{{deadlineDate || '不设置'}}</view>
				</picker>
				<picker class="col" mode="time" :value="deadlineTime" @change="deadlineTime = $event.detail.value">
					<view class="pick">{{deadlineTime || '23:59'}}</view>
				</picker>
			</view>
			<text class="clear-btn" v-if="deadlineDate" @click="clearDeadline">清除截止时间</text>

			<text class="lb">报名名额</text>
			<input class="inp" type="number" v-model="form.capacity" placeholder="例如：20" />

			<text class="lb">活动介绍</text>
			<textarea class="area" v-model="form.description" placeholder="介绍一下活动内容、适合人群、注意事项等" :maxlength="1000" />

			<text class="lb">活动海报（可选，用于分享）</text>
			<view class="cover-box">
				<image v-if="form.coverImage" class="cover-img" :src="form.coverImage" mode="aspectFill" @click="chooseCover" />
				<view v-else class="cover-empty" @click="chooseCover">＋ 上传海报</view>
				<text v-if="form.coverImage" class="cover-remove" @click="removeCover">删除</text>
			</view>
		</view>

		<view class="actions">
			<button class="btn-outline" @click="back">取消</button>
			<button class="btn-primary" :loading="submitting" @click="submit">{{id ? '保存并重新提交审核' : '提交审核'}}</button>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { uploadImage } from '../../utils/profile.js'
	import { ACTIVITY_CATEGORIES } from '../../utils/activity.js'

	function pad(n) { return n < 10 ? '0' + n : '' + n }
	function parts(iso) {
		const d = new Date(iso)
		return { date: `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`, time: `${pad(d.getHours())}:${pad(d.getMinutes())}` }
	}

	export default {
		data() {
			const now = new Date()
			const d = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
			return {
				id: null,
				submitting: false,
				form: { title: '', locationText: '', capacity: 20, description: '', coverImage: '' },
				categoryOptions: ACTIVITY_CATEGORIES,
				categoryIdx: 0,
				venueOptions: [{ id: null, name: '不关联场馆' }],
				venueIdx: 0,
				today: d,
				startDate: d,
				startTime: '10:00',
				endDate: d,
				endTime: '12:00',
				deadlineDate: '',
				deadlineTime: '23:59'
			}
		},
		onLoad(options) {
			this.id = options.id ? Number(options.id) : null
			this.loadVenues()
			if (this.id) this.loadActivity()
		},
		methods: {
			async loadVenues() {
				try {
					const vs = await api.getVenues({ page: 1, pageSize: 100 })
					this.venueOptions = [{ id: null, name: '不关联场馆' }].concat(vs.map(v => ({ id: v.id, name: v.name })))
				} catch (e) {}
			},
			async loadActivity() {
				try {
					const a = await api.getActivity(this.id)
					this.form.title = a.title
					this.form.locationText = a.locationText || ''
					this.form.capacity = a.capacity
					this.form.description = a.description || ''
					this.form.coverImage = a.coverImage || ''
					this.categoryIdx = Math.max(0, this.categoryOptions.findIndex(c => c.code === a.category))
					if (a.venueId) {
						const i = this.venueOptions.findIndex(v => v.id === a.venueId)
						this.venueIdx = i >= 0 ? i : 0
					}
					const s = parts(a.startAt), e = parts(a.endAt)
					this.startDate = s.date; this.startTime = s.time
					this.endDate = e.date; this.endTime = e.time
					if (a.signupDeadline) {
						const dl = parts(a.signupDeadline)
						this.deadlineDate = dl.date; this.deadlineTime = dl.time
					}
				} catch (e) {
					uni.showToast({ title: '加载失败', icon: 'none' })
				}
			},
			onVenueChange(e) {
				this.venueIdx = Number(e.detail.value)
			},
			chooseCover() {
				uni.chooseImage({
					count: 1,
					sizeType: ['compressed'],
					sourceType: ['album', 'camera'],
					success: async (res) => {
						const filePath = res.tempFilePaths && res.tempFilePaths[0]
						if (!filePath) return
						uni.showLoading({ title: '上传中', mask: true })
						try {
							this.form.coverImage = await uploadImage(filePath, 'activities')
						} catch (e) {
							uni.showToast({ title: '上传失败，请重试', icon: 'none' })
						} finally {
							uni.hideLoading()
						}
					}
				})
			},
			removeCover() {
				this.form.coverImage = ''
			},
			clearDeadline() {
				this.deadlineDate = ''
				this.deadlineTime = '23:59'
			},
			combine(date, time) {
				return new Date(`${date}T${time}:00`)
			},
			back() {
				uni.navigateBack()
			},
			async submit() {
				if (this.submitting) return
				if (!this.form.title.trim()) {
					uni.showToast({ title: '请填写活动标题', icon: 'none' })
					return
				}
				const startAt = this.combine(this.startDate, this.startTime)
				const endAt = this.combine(this.endDate, this.endTime)
				if (endAt <= startAt) {
					uni.showToast({ title: '结束时间需晚于开始时间', icon: 'none' })
					return
				}
				const capacity = parseInt(this.form.capacity, 10)
				if (!capacity || capacity <= 0) {
					uni.showToast({ title: '请填写报名名额', icon: 'none' })
					return
				}
				const payload = {
					title: this.form.title.trim(),
					category: this.categoryOptions[this.categoryIdx].code,
					venueId: this.venueOptions[this.venueIdx] ? this.venueOptions[this.venueIdx].id : null,
					locationText: this.form.locationText.trim(),
					startAt: startAt.toISOString(),
					endAt: endAt.toISOString(),
					signupDeadline: this.deadlineDate ? this.combine(this.deadlineDate, this.deadlineTime).toISOString() : null,
					capacity,
					description: this.form.description,
					coverImage: this.form.coverImage || null
				}
				this.submitting = true
				try {
					if (this.id) await api.updateActivity(this.id, payload)
					else await api.createActivity(payload)
					uni.showToast({ title: '已提交，等待审核', icon: 'none' })
					setTimeout(() => uni.navigateBack(), 600)
				} catch (e) {
					uni.showToast({ title: (e && e.message) || '提交失败', icon: 'none' })
				} finally {
					this.submitting = false
				}
			}
		}
	}
</script>

<style scoped>
	.page {
		padding: 24rpx;
	}
	.card {
		display: flex;
		flex-direction: column;
	}
	.lb {
		font-size: 24rpx;
		color: #8A8A86;
		margin: 22rpx 0 10rpx;
	}
	.inp, .area, .pick {
		background: #F8F7F3;
		border: 1rpx solid #ECEAE3;
		border-radius: 12rpx;
		padding: 18rpx 20rpx;
		font-size: 28rpx;
		color: #33332E;
	}
	.area {
		min-height: 180rpx;
		width: 100%;
		box-sizing: border-box;
	}
	.cover-box {
		position: relative;
		width: 100%;
		height: 300rpx;
		border-radius: 14rpx;
		overflow: hidden;
	}
	.cover-img {
		width: 100%;
		height: 300rpx;
	}
	.cover-empty {
		width: 100%;
		height: 300rpx;
		display: flex;
		align-items: center;
		justify-content: center;
		background: #F8F7F3;
		border: 1rpx dashed #DAD7CE;
		border-radius: 14rpx;
		color: #A5A39D;
		font-size: 28rpx;
	}
	.cover-remove {
		position: absolute;
		right: 16rpx;
		bottom: 16rpx;
		font-size: 24rpx;
		color: #fff;
		background: rgba(0, 0, 0, 0.5);
		padding: 6rpx 20rpx;
		border-radius: 999rpx;
	}
	.row {
		display: flex;
		gap: 16rpx;
	}
	.row .col {
		flex: 1;
	}
	.clear-btn {
		font-size: 24rpx;
		color: var(--primary);
		margin-top: 10rpx;
	}
	.actions {
		display: flex;
		gap: 16rpx;
		margin-top: 30rpx;
	}
	.actions button {
		flex: 1;
	}
</style>
