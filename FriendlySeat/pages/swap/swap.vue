<template>
	<view class="page">
		<view class="head">
			<view class="head-tabs">
				<view class="ht" :class="{ active: tab === 'board' }" @click="tab = 'board'">换座广场</view>
				<view class="ht" :class="{ active: tab === 'mine' }" @click="tab = 'mine'">我的发布</view>
				<view class="ht" :class="{ active: tab === 'responded' }" @click="tab = 'responded'">我的响应</view>
			</view>
			<button class="btn-primary small" @click="openPublish">发布换座</button>
		</view>

		<text class="hint">平台仅提供换座信息撮合，免费自愿、线下自行交换，座位使用以场馆规定为准。</text>

		<!-- 广场 -->
		<view v-if="tab === 'board'">
			<view v-if="board.length">
				<view class="card item" v-for="s in board" :key="s.id">
					<view class="item-top">
						<text class="nick">{{s.userNickname || '友邻'}}</text>
						<text class="tag status-open" v-if="s.status === 'Open'">进行中</text>
					</view>
					<view class="loc"><text class="loc-k">当前位置</text><text class="loc-v">{{locText(s)}}</text></view>
					<view class="loc"><text class="loc-k">期望位置</text><text class="loc-v">{{wantText(s)}}</text></view>
					<view class="reasons"><text class="chip" v-for="r in s.reasons" :key="r">{{reasonLabel(r)}}</text></view>
					<view class="item-foot">
						<text class="remain">剩余 {{remainMinutes(s.expireAt)}} 分钟</text>
						<button v-if="s.isMine" class="btn-outline small" disabled>我发布的</button>
						<button v-else-if="s.respondedByMe" class="btn-outline small" disabled>{{myResponseText(s.myResponseStatus)}}</button>
						<button v-else class="btn-primary small" @click="openRespond(s)">我愿意换</button>
					</view>
				</view>
			</view>
			<view v-else class="empty">暂无进行中的换座意向</view>
		</view>

		<!-- 我的发布 -->
		<view v-if="tab === 'mine'">
			<view v-if="mine.length">
				<view class="card item" v-for="s in mine" :key="s.id">
					<view class="item-top">
						<text class="nick">{{statusText(s.status)}}</text>
						<text class="remain" v-if="s.status === 'Open'">剩余 {{remainMinutes(s.expireAt)}} 分钟</text>
					</view>
					<view class="loc"><text class="loc-k">当前位置</text><text class="loc-v">{{locText(s)}}</text></view>
					<view class="loc"><text class="loc-k">期望位置</text><text class="loc-v">{{wantText(s)}}</text></view>
					<view class="reasons"><text class="chip" v-for="r in s.reasons" :key="r">{{reasonLabel(r)}}</text></view>

					<view class="resp-title" v-if="s.responses && s.responses.length">响应者（{{s.responses.length}}）</view>
					<view class="resp" v-for="p in (s.responses || [])" :key="p.id">
						<view class="resp-info">
							<text class="resp-nick">{{p.userNickname || '友邻'}}</text>
							<text class="resp-loc">{{respLocText(p)}}</text>
							<text class="resp-status" :class="'rs-' + p.status.toLowerCase()">{{respStatusText(p.status, s)}}</text>
						</view>
						<button v-if="s.status === 'Open' && p.status === 'Pending'" class="btn-primary small" @click="accept(s, p)">同意换座</button>
					</view>

					<view class="item-foot" v-if="s.status === 'Open'">
						<button class="btn-outline small" @click="cancel(s)">取消意向</button>
					</view>
				</view>
			</view>
			<view v-else class="empty">你还没有发布换座意向</view>
		</view>

		<!-- 我的响应 -->
		<view v-if="tab === 'responded'">
			<view v-if="responded.length">
				<view class="card item" v-for="s in responded" :key="s.id">
					<view class="item-top">
						<text class="nick">{{s.userNickname || '友邻'}} 的换座</text>
						<text class="tag" :class="'rs-' + (s.myResponseStatus || '').toLowerCase()">{{myResponseText(s.myResponseStatus)}}</text>
					</view>
					<view class="loc"><text class="loc-k">对方位置</text><text class="loc-v">{{locText(s)}}</text></view>
					<view class="loc"><text class="loc-k">期望位置</text><text class="loc-v">{{wantText(s)}}</text></view>
					<view class="reasons"><text class="chip" v-for="r in s.reasons" :key="r">{{reasonLabel(r)}}</text></view>
					<text class="ok-tip" v-if="s.myResponseStatus === 'Accepted'">对方已同意，可按双方位置线下物理交换</text>
				</view>
			</view>
			<view v-else class="empty">你还没有响应换座</view>
		</view>

		<!-- 发布弹窗 -->
		<view v-if="showPublish" class="mask" @click="showPublish = false">
			<view class="pop" @click.stop>
				<text class="pop-title">发布换座意向</text>

				<text class="lb">我当前的位置</text>
				<view class="loc-row">
					<picker mode="selector" :range="floorOptions" range-key="name" :value="cur.floor" @change="onLoc('cur', 'floor', $event)">
						<view class="pick">{{pickText('cur', 'floor')}}</view>
					</picker>
					<picker mode="selector" :range="curAreas" range-key="name" :value="cur.area" @change="onLoc('cur', 'area', $event)">
						<view class="pick">{{pickText('cur', 'area')}}</view>
					</picker>
					<picker mode="selector" :range="curZones" range-key="name" :value="cur.zone" @change="onLoc('cur', 'zone', $event)">
						<view class="pick">{{pickText('cur', 'zone')}}</view>
					</picker>
				</view>

				<text class="lb">期望换到（可不限）</text>
				<view class="loc-row">
					<picker mode="selector" :range="wantFloorOptions" range-key="name" :value="want.floor" @change="onLoc('want', 'floor', $event)">
						<view class="pick">{{pickText('want', 'floor')}}</view>
					</picker>
					<picker mode="selector" :range="wantAreas" range-key="name" :value="want.area" @change="onLoc('want', 'area', $event)">
						<view class="pick">{{pickText('want', 'area')}}</view>
					</picker>
					<picker mode="selector" :range="wantZones" range-key="name" :value="want.zone" @change="onLoc('want', 'zone', $event)">
						<view class="pick">{{pickText('want', 'zone')}}</view>
					</picker>
				</view>

				<text class="lb">换座原因（客观因素，可多选）</text>
				<view class="reasons pick-reasons">
					<text class="chip" :class="{ on: publishReasons.includes(r.code) }" v-for="r in reasonOptions" :key="r.code" @click="toggleReason(r.code)">{{r.label}}</text>
				</view>

				<text class="lb">有效期</text>
				<view class="durations">
					<text class="chip" :class="{ on: durationIdx === i }" v-for="(d, i) in durationOptions" :key="d.value" @click="durationIdx = i">{{d.label}}</text>
				</view>

				<view class="pop-actions">
					<button class="btn-outline small" @click="showPublish = false">取消</button>
					<button class="btn-primary small" :loading="submitting" @click="publish">发布</button>
				</view>
			</view>
		</view>

		<!-- 响应弹窗 -->
		<view v-if="showRespond" class="mask" @click="showRespond = false">
			<view class="pop" @click.stop>
				<text class="pop-title">提交我的位置</text>
				<text class="hint">对方确认后即可线下物理交换</text>
				<view class="loc-row">
					<picker mode="selector" :range="floorOptions" range-key="name" :value="resp.floor" @change="onLoc('resp', 'floor', $event)">
						<view class="pick">{{pickText('resp', 'floor')}}</view>
					</picker>
					<picker mode="selector" :range="respAreas" range-key="name" :value="resp.area" @change="onLoc('resp', 'area', $event)">
						<view class="pick">{{pickText('resp', 'area')}}</view>
					</picker>
					<picker mode="selector" :range="respZones" range-key="name" :value="resp.zone" @change="onLoc('resp', 'zone', $event)">
						<view class="pick">{{pickText('resp', 'zone')}}</view>
					</picker>
				</view>
				<view class="pop-actions">
					<button class="btn-outline small" @click="showRespond = false">取消</button>
					<button class="btn-primary small" :loading="submitting" @click="submitRespond">提交</button>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import { api } from '../../utils/request.js'
	import { formatTime } from '../../utils/format.js'

	export default {
		data() {
			return {
				venueId: 0,
				venue: null,
				tab: 'board',
				board: [],
				mine: [],
				responded: [],
				showPublish: false,
				showRespond: false,
				submitting: false,
				currentSwap: null,
				publishReasons: [],
				durationIdx: 1,
				reasonOptions: [
					{ code: 'light', label: '光线问题' },
					{ code: 'cold', label: '位置偏冷' },
					{ code: 'hot', label: '位置偏热' },
					{ code: 'noise', label: '附近有人交谈' },
					{ code: 'together', label: '想与同伴相邻' },
					{ code: 'window', label: '想靠窗' },
					{ code: 'socket', label: '需要插座' },
					{ code: 'other', label: '其他' }
				],
				durationOptions: [
					{ label: '30分钟', value: 30 },
					{ label: '1小时', value: 60 },
					{ label: '2小时', value: 120 }
				],
				cur: { floor: 0, area: 0, zone: 0 },
				want: { floor: 0, area: 0, zone: 0 },
				resp: { floor: 0, area: 0, zone: 0 }
			}
		},
		computed: {
			floorOptions() {
				return this.venue && this.venue.floors
					? this.venue.floors.map(f => ({ id: f.id, name: f.name }))
					: []
			},
			wantFloorOptions() {
				return [{ id: null, name: '不限楼层' }].concat(this.floorOptions)
			},
			curAreas() { return this.areaOptions('cur') },
			curZones() { return this.zoneOptions('cur') },
			wantAreas() { return this.areaOptions('want') },
			wantZones() { return this.zoneOptions('want') },
			respAreas() { return this.areaOptions('resp') },
			respZones() { return this.zoneOptions('resp') }
		},
		onLoad(options) {
			this.venueId = Number(options.venueId || 0)
			if (!uni.getStorageSync('token')) {
				uni.navigateTo({ url: '/pages/login/login' })
				return
			}
			this.loadVenue()
		},
		onShow() {
			if (this.venueId) this.loadLists()
		},
		methods: {
			formatTime,
			async loadVenue() {
				if (!this.venueId) return
				try {
					this.venue = await api.getVenue(this.venueId)
				} catch (e) {}
			},
			async loadLists() {
				try { this.board = await api.getSwaps(this.venueId) } catch (e) {}
				try { this.mine = await api.getMySwaps() } catch (e) {}
				try { this.responded = await api.getRespondedSwaps() } catch (e) {}
			},
			floorList(ctx) {
				const base = this.venue && this.venue.floors ? this.venue.floors : []
				return ctx === 'want' ? [{ id: null, name: '不限楼层' }].concat(base) : base
			},
			floorObj(ctx) {
				const list = this.floorList(ctx)
				return list[this[ctx].floor] || null
			},
			areaOptions(ctx) {
				const f = this.floorObj(ctx)
				const base = [{ id: null, name: '不限区域' }]
				if (!f || !f.areas) return base
				return base.concat(f.areas.map(a => ({ id: a.id, name: a.name })))
			},
			zoneOptions(ctx) {
				const f = this.floorObj(ctx)
				const base = [{ id: null, name: '不限区块' }]
				if (!f || !f.zones) return base
				const area = this.areaOptions(ctx)[this[ctx].area]
				let zones = f.zones
				if (area && area.id) zones = zones.filter(z => z.areaId === area.id)
				return base.concat(zones.map(z => ({ id: z.id, name: z.name })))
			},
			pickText(ctx, level) {
				if (level === 'floor') {
					const o = this.floorList(ctx)[this[ctx].floor]
					return o ? o.name : '选择楼层'
				}
				if (level === 'area') {
					const o = this.areaOptions(ctx)[this[ctx].area]
					return o ? o.name : '不限区域'
				}
				const o = this.zoneOptions(ctx)[this[ctx].zone]
				return o ? o.name : '不限区块'
			},
			onLoc(ctx, level, e) {
				const v = Number(e.detail.value)
				if (level === 'floor') {
					this[ctx].floor = v
					this[ctx].area = 0
					this[ctx].zone = 0
				} else if (level === 'area') {
					this[ctx].area = v
					this[ctx].zone = 0
				} else {
					this[ctx].zone = v
				}
			},
			buildLoc(ctx) {
				const f = this.floorObj(ctx)
				const area = this.areaOptions(ctx)[this[ctx].area]
				const zone = this.zoneOptions(ctx)[this[ctx].zone]
				return {
					floorId: f ? f.id : null,
					areaId: area ? area.id : null,
					zoneId: zone ? zone.id : null
				}
			},
			locText(s) {
				const parts = [s.floorName, s.areaName, s.zoneName].filter(Boolean)
				return parts.length ? parts.join(' / ') : '未填写'
			},
			wantText(s) {
				const parts = [s.wantFloorName || '不限楼层', s.wantAreaName, s.wantZoneName].filter(Boolean)
				return parts.join(' / ')
			},
			respLocText(p) {
				const parts = [p.floorName, p.areaName, p.zoneName].filter(Boolean)
				return parts.length ? parts.join(' / ') : '未填写'
			},
			reasonLabel(code) {
				const o = this.reasonOptions.find(r => r.code === code)
				return o ? o.label : code
			},
			statusText(s) {
				const map = { Open: '进行中', Matched: '已匹配', Cancelled: '已取消', Expired: '已过期' }
				return map[s] || s
			},
			respStatusText(status, s) {
				const map = { Pending: '待确认', Accepted: '已同意', Rejected: '未选中' }
				return map[status] || status
			},
			myResponseText(status) {
				const map = { Pending: '等待对方确认', Accepted: '已同意', Rejected: '未被选中' }
				return map[status] || '已响应'
			},
			remainMinutes(expireAt) {
				const left = Math.max(0, Math.round((new Date(expireAt).getTime() - Date.now()) / 60000))
				return left
			},
			openPublish() {
				this.cur = { floor: 0, area: 0, zone: 0 }
				this.want = { floor: 0, area: 0, zone: 0 }
				this.publishReasons = []
				this.durationIdx = 1
				this.showPublish = true
			},
			toggleReason(code) {
				const i = this.publishReasons.indexOf(code)
				if (i >= 0) this.publishReasons.splice(i, 1)
				else this.publishReasons.push(code)
			},
			async publish() {
				if (this.submitting) return
				if (!this.publishReasons.length) {
					uni.showToast({ title: '请选择换座原因', icon: 'none' })
					return
				}
				this.submitting = true
				try {
					await api.createSwap({
						venueId: this.venueId,
						...this.buildLoc('cur'),
						wantFloorId: this.buildLoc('want').floorId,
						wantAreaId: this.buildLoc('want').areaId,
						wantZoneId: this.buildLoc('want').zoneId,
						reasons: this.publishReasons,
						durationMinutes: this.durationOptions[this.durationIdx].value
					})
					this.showPublish = false
					uni.showToast({ title: '已发布换座意向', icon: 'none' })
					this.tab = 'mine'
					this.loadLists()
				} catch (e) {
					uni.showToast({ title: (e && e.message) || '发布失败', icon: 'none' })
				} finally {
					this.submitting = false
				}
			},
			openRespond(s) {
				this.currentSwap = s
				this.resp = { floor: 0, area: 0, zone: 0 }
				this.showRespond = true
			},
			async submitRespond() {
				if (this.submitting || !this.currentSwap) return
				this.submitting = true
				try {
					await api.respondSwap(this.currentSwap.id, this.buildLoc('resp'))
					this.showRespond = false
					uni.showToast({ title: '已提交，等待对方确认', icon: 'none' })
					this.loadLists()
				} catch (e) {
					uni.showToast({ title: (e && e.message) || '提交失败', icon: 'none' })
				} finally {
					this.submitting = false
				}
			},
			async accept(s, p) {
				uni.showModal({
					title: '确认换座',
					content: `同意与「${p.userNickname || '友邻'}」交换座位？确认后请线下物理交换。`,
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.acceptSwap(s.id, p.id)
							uni.showModal({ title: '已确认', content: '已同意换座，请按双方位置进行线下物理交换。', showCancel: false })
							this.loadLists()
						} catch (e) {
							uni.showToast({ title: (e && e.message) || '操作失败', icon: 'none' })
						}
					}
				})
			},
			async cancel(s) {
				uni.showModal({
					title: '取消换座意向',
					content: '确定取消这条换座意向吗？',
					success: async (res) => {
						if (!res.confirm) return
						try {
							await api.cancelSwap(s.id)
							uni.showToast({ title: '已取消', icon: 'none' })
							this.loadLists()
						} catch (e) {
							uni.showToast({ title: (e && e.message) || '操作失败', icon: 'none' })
						}
					}
				})
			}
		}
	}
</script>

<style scoped>
	.page {
		padding: 24rpx;
	}
	.head {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.head-tabs {
		display: flex;
		gap: 32rpx;
	}
	.ht {
		font-size: 30rpx;
		color: #8A8A86;
		padding: 8rpx 0;
	}
	.ht.active {
		color: var(--primary);
		font-weight: 700;
		border-bottom: 4rpx solid var(--primary);
	}
	.hint {
		display: block;
		font-size: 22rpx;
		color: #8A8A86;
		margin: 16rpx 0 20rpx;
	}
	.item {
		margin-bottom: 20rpx;
	}
	.item-top {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.nick {
		font-size: 30rpx;
		font-weight: 600;
	}
	.remain {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.loc {
		display: flex;
		margin-top: 12rpx;
	}
	.loc-k {
		width: 140rpx;
		font-size: 24rpx;
		color: #8A8A86;
	}
	.loc-v {
		flex: 1;
		font-size: 26rpx;
	}
	.reasons {
		display: flex;
		flex-wrap: wrap;
		gap: 12rpx;
		margin-top: 14rpx;
	}
	.chip {
		font-size: 22rpx;
		padding: 6rpx 18rpx;
		border-radius: 999rpx;
		background: var(--primary-bg, #EAF3F1);
		color: var(--primary);
	}
	.chip.on {
		background: var(--primary);
		color: #fff;
	}
	.item-foot {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-top: 18rpx;
	}
	.resp-title {
		font-size: 24rpx;
		color: #8A8A86;
		margin: 18rpx 0 8rpx;
	}
	.resp {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding: 12rpx 0;
		border-top: 1rpx solid #F0EEE8;
	}
	.resp-info {
		display: flex;
		flex-direction: column;
		gap: 4rpx;
	}
	.resp-nick {
		font-size: 26rpx;
		font-weight: 600;
	}
	.resp-loc {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.resp-status {
		font-size: 22rpx;
	}
	.rs-pending { color: #C78A2B; }
	.rs-accepted { color: var(--primary); }
	.rs-rejected { color: #B0AEA8; }
	.ok-tip {
		display: block;
		font-size: 24rpx;
		color: var(--primary);
		margin-top: 14rpx;
	}
	.empty {
		text-align: center;
		color: #B0AEA8;
		font-size: 26rpx;
		padding: 80rpx 0;
	}
	.mask {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.4);
		display: flex;
		align-items: flex-end;
		z-index: 99;
	}
	.pop {
		width: 100%;
		background: #fff;
		border-radius: 24rpx 24rpx 0 0;
		padding: 32rpx;
	}
	.pop-title {
		font-size: 32rpx;
		font-weight: 700;
		display: block;
		margin-bottom: 20rpx;
	}
	.lb {
		display: block;
		font-size: 24rpx;
		color: #8A8A86;
		margin: 20rpx 0 10rpx;
	}
	.loc-row {
		display: flex;
		gap: 12rpx;
	}
	.pick {
		background: var(--primary-bg, #EAF3F1);
		border-radius: 12rpx;
		padding: 14rpx 16rpx;
		font-size: 24rpx;
		text-align: center;
	}
	.pick-reasons {
		margin-top: 0;
	}
	.durations {
		display: flex;
		gap: 12rpx;
	}
	.pop-actions {
		display: flex;
		justify-content: flex-end;
		gap: 16rpx;
		margin-top: 30rpx;
	}
</style>
