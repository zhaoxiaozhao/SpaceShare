<template>
	<view v-if="venue">
		<view class="card venue-header">
			<text class="venue-name">{{venue.name}}</text>
			<text class="venue-type">{{venue.type}} · {{venue.openingTime}} - {{venue.closingTime}}</text>
			<text class="venue-addr">{{venue.address}}</text>
			<text class="venue-desc" v-if="venue.description">{{venue.description}}</text>
		</view>

		<view class="card status-row">
			<view class="stat">
				<text class="stat-num">{{venue.seatCount}}</text>
				<text class="stat-label">座位总数</text>
			</view>
			<view class="stat">
				<text class="stat-num green">{{venue.availableCount}}</text>
				<text class="stat-label">可预约</text>
			</view>
			<view class="stat">
				<text class="fs-btn" @click="toggleFullscreen">{{fullscreen ? '退出全屏' : '全屏'}}</text>
				<text class="stat-label">{{fullscreen ? '' : '查看地图'}}</text>
			</view>
		</view>

		<view class="card legend">
			<view class="legend-item"><view class="dot avail"></view><text>可预约</text></view>
			<view class="legend-item"><view class="dot reserved"></view><text>已预约</text></view>
			<view class="legend-item"><view class="dot off"></view><text>不可用</text></view>
			<view class="legend-item"><view class="dot unknown"></view><text>未知</text></view>
			<view class="legend-item"><view class="dot-poi"></view><text>设施</text></view>
		</view>

		<view class="floor-tabs" v-if="venue.floors.length">
			<view
				class="floor-tab"
				:class="{ active: currentFloor === f.id }"
				v-for="f in floorTabs"
				:key="f.id"
				@click="selectFloor(f.id)"
			>{{f.name}}<text class="floor-count" v-if="f.totalSeats">·{{f.totalSeats}}</text></view>
		</view>

		<!-- 空间区域切换（默认选中第一个区域，避免全部区块重叠） -->
		<view class="area-tabs" v-if="currentFloorObj && currentFloorObj.areas && currentFloorObj.areas.length">
			<view
				class="area-tab"
				:class="{ active: currentAreaId === a.id }"
				v-for="a in currentFloorObj.areas"
				:key="a.id"
				@click="selectArea(a.id)"
			>{{a.name}}</view>
		</view>

		<!-- 全屏地图：可滑动 + 缩放 -->
		<view v-if="fullscreen" class="fs-overlay">
			<view class="fs-toolbar">
				<text class="fs-title">{{currentFloorName}} · {{currentAreaName}}</text>
				<view class="fs-zoom">
					<text class="fs-zbtn" @click="mapScale = Math.max(1, mapScale - 0.4)">－</text>
					<text class="fs-zlabel">{{Math.round(mapScale * 100)}}%</text>
					<text class="fs-zbtn" @click="mapScale = Math.min(4, mapScale + 0.4)">＋</text>
				</view>
				<text class="fs-close" @click="toggleFullscreen">✕</text>
			</view>
			<scroll-view scroll-y class="fs-scroll" :enhanced="true" :show-scrollbar="false">
				<scroll-view scroll-x class="fs-scroll-x" :enhanced="true" :show-scrollbar="false">
					<view v-for="f in venue.floors" :key="f.id" v-show="f.id === currentFloor">
						<view class="fs-map" :style="floorMapStyle(f)">
							<view
								class="aisle-strip"
								v-for="row in floorGridRows(f)"
								:key="'a' + row"
								:class="isAisleRow(f, row) ? 'aisle' : ''"
								:style="aisleRowStyle(f, row)"
							></view>
							<view
								v-for="z in visibleZones(f)"
								:key="'z' + z.id"
								class="zone-rect"
								:style="zoneRectStyle(f, z)"
								@click="tapZone(z)"
							>
								<view class="zone-grid" :style="zoneGridStyle(z)">
									<view
										class="map-cell"
										:class="cell ? seatClass(cell) : 'vacant'"
										v-for="(cell, idx) in zoneGrid(z)"
										:key="idx"
										@click.stop="cell && goSeat(cell.id)"
									>
										<text v-if="cell" class="seat-code" :style="seatCodeStyle()">{{seatShortCode(cell.code)}}</text>
									</view>
								</view>
								<text class="zone-label" :style="zoneLabelStyle()"><text class="zone-letter">{{zoneLetter(z, f)}}区</text></text>
							</view>
							<view
								v-for="p in f.pois"
								:key="'p' + p.id"
								class="poi-rect"
								:class="poiClass(p.type)"
								:style="poiRectStyle(f, p)"
							>
								<template v-if="p.type === 'Text'">
									<text class="poi-text" :style="poiRotateStyle(p)">{{p.text || p.name}}</text>
								</template>
								<template v-else-if="p.type === 'Line'">
									<view class="poi-line" :style="poiRotateStyle(p)"></view>
								</template>
								<template v-else>
									<text class="poi-icon" :style="poiIconStyle()">{{poiIcon(p.type)}}</text>
									<text class="poi-name" :style="poiNameStyle()">{{p.name}}</text>
								</template>
							</view>
						</view>
					</view>
				</scroll-view>
			</scroll-view>
		</view>

		<!-- 当前楼层平面图：区块 + 座位 + 标志物 统一渲染（绝对定位，与设计器一致） -->
		<view v-for="f in venue.floors" :key="f.id" v-show="f.id === currentFloor && !fullscreen">
			<view class="card" v-if="f.zones.length || f.pois.length">
				<scroll-view scroll-x class="map-scroll-x" :enhanced="true" :show-scrollbar="false">
				<view class="floor-map" :style="floorMapStyle(f)">
					<!-- 过道/走廊背景：铺整层 -->
					<view
						class="aisle-strip"
						v-for="row in floorGridRows(f)"
						:key="'a' + row"
						:class="isAisleRow(f, row) ? 'aisle' : ''"
						:style="aisleRowStyle(f, row)"
					></view>

					<!-- 区块（按当前区域过滤显示） -->
					<view
						v-for="z in visibleZones(f)"
						:key="'z' + z.id"
						class="zone-rect"
						:style="zoneRectStyle(f, z)"
						@click="tapZone(z)"
					>
						<view class="zone-grid" :style="zoneGridStyle(z)">
							<view
								class="map-cell"
								:class="cell ? seatClass(cell) : 'vacant'"
								v-for="(cell, idx) in zoneGrid(z)"
								:key="idx"
								@click.stop="cell && goSeat(cell.id)"
							>
								<text v-if="cell" class="seat-code" :style="seatCodeStyle()">{{seatShortCode(cell.code)}}</text>
							</view>
						</view>
						<text class="zone-label" :style="zoneLabelStyle()"><text class="zone-letter">{{zoneLetter(z, f)}}区</text></text>
					</view>

					<!-- 标志物（文本/线条可旋转） -->
					<view
						v-for="p in f.pois"
						:key="'p' + p.id"
						class="poi-rect"
						:class="poiClass(p.type)"
						:style="poiRectStyle(f, p)"
					>
						<template v-if="p.type === 'Text'">
							<text class="poi-text" :style="poiRotateStyle(p)">{{p.text || p.name}}</text>
						</template>
						<template v-else-if="p.type === 'Line'">
							<view class="poi-line" :style="poiRotateStyle(p)"></view>
						</template>
						<template v-else>
							<text class="poi-icon" :style="poiIconStyle()">{{poiIcon(p.type)}}</text>
							<text class="poi-name" :style="poiNameStyle()">{{p.name}}</text>
						</template>
					</view>
				</view>
				</scroll-view>
			</view>
			<view v-else class="empty">该楼层暂无座位数据</view>
		</view>

		<view class="section" v-if="shares.length">
			<text class="section-title">当前可预约座位</text>
			<view class="card share-card" v-for="s in shares" :key="s.id" @click="goReserve(s)">
				<view class="share-top">
					<text class="share-seat">{{s.displayCode || s.seatCode}}</text>
					<text class="tag">可预约</text>
				</view>
				<view class="share-loc">
					<text class="share-floor">{{s.floorName || ''}}</text>
					<text class="share-area" v-if="s.areaName">{{s.areaName}}</text>
				</view>
				<view class="share-time">预计释放：{{formatTime(s.endAt)}}</view>
				<text class="share-note" v-if="s.note">{{s.note}}</text>
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
				venue: null,
				currentFloor: null,
				currentAreaId: null,
				shares: [],
				windowWidth: 375,
				fullscreen: false,
				mapScale: 1.6
			}
		},
		computed: {
			floorTabs() {
				if (!this.venue) return []
				return this.venue.floors.map(f => ({
					id: f.id,
					name: f.name,
					totalSeats: f.zones.reduce((sum, z) => sum + z.seats.length, 0)
				}))
			},
			currentFloorObj() {
				if (!this.venue || !this.currentFloor) return null
				return this.venue.floors.find(f => f.id === this.currentFloor) || null
			},
			currentFloorName() {
				return this.currentFloorObj ? this.currentFloorObj.name : ''
			},
			currentAreaName() {
				const f = this.currentFloorObj
				if (!f) return ''
				const a = (f.areas || []).find(a => a.id === this.currentAreaId)
				return a ? a.name : ''
			}
		},
		onLoad(options) {
			this.id = options.id
			try {
				const info = uni.getSystemInfoSync()
				this.windowWidth = info.windowWidth || 375
			} catch (e) {}
			this.loadedOnce = false
		},
		onShow() {
			this.load()
		},
		methods: {
			formatTime,
			async load() {
				try {
					this.venue = await api.getVenue(this.id)
					// 首次加载时设置默认楼层；从座位详情返回时保留当前楼层/区域
					if (!this.loadedOnce && this.venue.floors.length) {
						const withSeats = this.venue.floors.find(f => f.zones.length)
						this.currentFloor = withSeats ? withSeats.id : this.venue.floors[0].id
						this.initArea()
						this.loadedOnce = true
					}
					this.shares = await api.getVenueShares(this.id)
				} catch (e) {
					uni.showToast({ title: '加载失败', icon: 'none' })
				}
			},
			initArea() {
				// 默认选中第一个区域；无区域时显示全部
				const f = this.venue.floors.find(x => x.id === this.currentFloor)
				const areas = f && f.areas ? f.areas : []
				this.currentAreaId = areas.length ? areas[0].id : null
			},
			selectFloor(id) {
				this.currentFloor = id
				this.initArea()
			},
			selectArea(id) {
				this.currentAreaId = id
			},
			toggleFullscreen() {
				this.fullscreen = !this.fullscreen
				if (this.fullscreen) {
					uni.setNavigationBarTitle({ title: '楼层平面图' })
				} else {
					uni.setNavigationBarTitle({ title: this.venue ? this.venue.name : '' })
				}
			},
			visibleZones(f) {
				if (this.currentAreaId === null) return f.zones
				return f.zones.filter(z => (z.areaId || 0) === this.currentAreaId)
			},

			// ===== 楼层平面图计算 =====
			// 计算整层网格尺寸：所有区块(offset + grid) + 标志物 的并集
			floorBounds(f) {
				let maxX = 0, maxY = 0
				for (const z of f.zones) {
					maxX = Math.max(maxX, (z.offsetX || 0) + (z.gridCols || z.seats.length))
					maxY = Math.max(maxY, (z.offsetY || 0) + (z.gridRows || 1))
				}
				for (const p of f.pois) {
					maxX = Math.max(maxX, p.positionX + p.width)
					maxY = Math.max(maxY, p.positionY + p.height)
				}
				return { rows: Math.max(maxY, 1), cols: Math.max(maxX, 1) }
			},
			// 固定舒适单格尺寸：地图超出屏幕时通过滚动查看，不压缩座位
			// 全屏模式下应用 mapScale 放大，画布可滑动浏览
			cellPx(f) {
				let cell = 24
				if (this.fullscreen) {
					cell = Math.round(cell * this.mapScale)
				}
				return cell
			},
			floorMapStyle(f) {
				const { rows, cols } = this.floorBounds(f)
				const cell = this.cellPx(f)
				return {
					width: `${cols * cell}px`,
					height: `${rows * cell}px`
				}
			},
			floorGridRows(f) {
				const { rows } = this.floorBounds(f)
				return Array.from({ length: rows }, (_, i) => i)
			},
			isAisleRow(f, row) {
				// 简化：没有区块覆盖的行视为过道/走廊
				return !f.zones.some(z => row >= (z.offsetY || 0) && row < (z.offsetY || 0) + (z.gridRows || 1))
			},
			aisleRowStyle(f, row) {
				const cell = this.cellPx(f)
				return {
					position: 'absolute',
					left: '0',
					top: `${row * cell}px`,
					width: '100%',
					height: `${cell}px`
				}
			},
			// 区块绝对定位（像素级，与设计器一致）
			zoneRectStyle(f, z) {
				const cell = this.cellPx(f)
				const cols = z.gridCols || z.seats.length
				return {
					position: 'absolute',
					left: `${(z.offsetX || 0) * cell}px`,
					top: `${(z.offsetY || 0) * cell}px`,
					width: `${cols * cell}px`
					// 高度由座位格子（aspect-ratio）自动撑开
				}
			},
			zoneGridStyle(z) {
				const cols = Math.max(z.gridCols, 1)
				const cell = this.cellPx(this.currentFloorObj)
				return {
					gridTemplateColumns: `repeat(${cols}, 1fr)`,
					gridAutoRows: `${cell}px` // 每行固定高度，保证格子可见
				}
			},
			// 座位编号字号随格子缩放
			seatCodeStyle() {
				const f = this.currentFloorObj
				const cell = f ? this.cellPx(f) : 24
				return { fontSize: `${Math.max(7, Math.round(cell * 0.32))}px` }
			},
			zoneLabelStyle() {
				const f = this.currentFloorObj
				const cell = f ? this.cellPx(f) : 24
				return { fontSize: `${Math.max(8, Math.round(cell * 0.38))}px` }
			},
			poiIconStyle() {
				const f = this.currentFloorObj
				const cell = f ? this.cellPx(f) : 24
				return { fontSize: `${Math.max(10, Math.round(cell * 0.5))}px` }
			},
			poiNameStyle() {
				const f = this.currentFloorObj
				const cell = f ? this.cellPx(f) : 24
				return { fontSize: `${Math.max(6, Math.round(cell * 0.26))}px` }
			},
			zoneGrid(zone) {
				const rows = Math.max(zone.gridRows, 1)
				const cols = Math.max(zone.gridCols, 1)
				const grid = new Array(rows * cols).fill(null)
				for (const s of zone.seats) {
					const x = Math.floor(s.positionX || 0)
					const y = Math.floor(s.positionY || 0)
					if (y >= 0 && y < rows && x >= 0 && x < cols) {
						grid[y * cols + x] = s
					}
				}
				return grid
			},
			seatClass(s) {
				if (s.status === 'Unavailable') return 'off'
				if (s.currentShareCount > 0) return 'avail'
				if (s.currentReservedCount > 0) return 'reserved'
				return 'unknown'
			},
			seatShortCode(code) {
				const parts = String(code).split('-')
				return parts[parts.length - 1] || code
			},
			zoneLetter(zone, f) {
				// 同一楼层内区块字母唯一：按 区域顺序 → 区块排序（sortOrder → offsetX → id）
				// 不随区域筛选变化，与后端一致
				const areas = f.areas || []
				const areaOrder = {}
				areas.forEach((a, i) => { areaOrder[a.id] = i })

				const list = f.zones.slice().sort((a, b) => {
					const aoA = areaOrder[a.areaId] !== undefined ? areaOrder[a.areaId] : 999
					const aoB = areaOrder[b.areaId] !== undefined ? areaOrder[b.areaId] : 999
					const ao = aoA - aoB
					if (ao !== 0) return ao
					return (a.sortOrder || 0) - (b.sortOrder || 0) ||
						(a.offsetX || 0) - (b.offsetX || 0) ||
						(a.id || 0) - (b.id || 0)
				})
				const idx = list.findIndex(z => z.id === zone.id)
				if (idx < 0) return '?'
				return String.fromCharCode(65 + idx)
			},
			zoneAvailable(zone) {
				return zone.seats.filter(s => s.currentShareCount > 0).length
			},
			tapZone(z) {
				// 点击区块背景：高亮提示该区块可预约数（MVP 不展开）
				uni.showToast({ title: `${z.name}：${this.zoneAvailable(z)} 个座位可预约`, icon: 'none' })
			},

			// ===== 标志物 =====
			poiIcon(type) {
				const map = {
					Toilet: '🚻', DrinkingWater: '💧', Bookshelf: '📚', Elevator: '🛗',
					Stairs: '🪜', Corridor: '🛤️', Entrance: '🚪', Exit: '🚨', ServiceDesk: 'ℹ️', Other: '📍'
				}
				return map[type] || '📍'
			},
			poiRectStyle(f, p) {
				const cell = this.cellPx(f)
				return {
					position: 'absolute',
					left: `${p.positionX * cell}px`,
					top: `${p.positionY * cell}px`,
					width: `${p.width * cell}px`,
					height: `${p.height * cell}px`
				}
			},
			poiClass(type) {
				if (type === 'Text') return 'poi-rect-text'
				if (type === 'Line') return 'poi-rect-line'
				return ''
			},
			poiRotateStyle(p) {
				return { transform: `rotate(${p.rotation || 0}deg)` }
			},
			goSeat(id) {
				uni.navigateTo({ url: `/pages/seat/seat?id=${id}` })
			},
			goReserve(share) {
				uni.navigateTo({ url: `/pages/seat/seat?id=${share.seatId}&shareId=${share.id}` })
			}
		}
	}
</script>

<style scoped>
	.venue-header {
		display: flex;
		flex-direction: column;
		gap: 8rpx;
	}
	.venue-name {
		font-size: 36rpx;
		font-weight: 600;
	}
	.venue-type {
		font-size: 24rpx;
		color: #3A8A7E;
	}
	.venue-addr, .venue-desc {
		font-size: 24rpx;
		color: #8A8A86;
	}
	.status-row {
		display: flex;
	}
	.stat {
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 4rpx;
	}
	.stat-num {
		font-size: 44rpx;
		font-weight: 700;
		color: #33332E;
	}
	.stat-num.green {
		color: #3A8A7E;
	}
	.stat-label {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.fs-btn {
		font-size: 28rpx;
		font-weight: 600;
		color: #3A8A7E;
		padding: 8rpx 20rpx;
		background: #EAF3F0;
		border-radius: 30rpx;
	}
	/* 全屏地图 */
	.fs-overlay {
		position: fixed;
		inset: 0;
		z-index: 999;
		background: #F7F5EF;
		display: flex;
		flex-direction: column;
	}
	.fs-toolbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 16rpx 24rpx;
		background: #FFFFFF;
		border-bottom: 1rpx solid #E0DED6;
	}
	.fs-title {
		font-size: 28rpx;
		font-weight: 600;
		color: #33332E;
		flex: 1;
	}
	.fs-zoom {
		display: flex;
		align-items: center;
		gap: 12rpx;
	}
	.fs-zbtn {
		width: 56rpx;
		height: 56rpx;
		line-height: 52rpx;
		text-align: center;
		background: #3A8A7E;
		color: #FFFFFF;
		border-radius: 50%;
		font-size: 32rpx;
	}
	.fs-zlabel {
		font-size: 24rpx;
		color: #55554F;
		min-width: 72rpx;
		text-align: center;
	}
	.fs-close {
		font-size: 36rpx;
		color: #8A8A86;
		padding: 0 10rpx;
		margin-left: 16rpx;
	}
	.fs-scroll {
		flex: 1;
		height: 100%;
	}
	.fs-scroll-x {
		height: 100%;
		white-space: nowrap;
	}
	.fs-map {
		position: relative;
		background: #F2F0EA;
		display: inline-block;
	}
	.legend {
		display: flex;
		gap: 20rpx;
		flex-wrap: wrap;
	}
	.legend-item {
		display: flex;
		align-items: center;
		gap: 8rpx;
		font-size: 22rpx;
		color: #55554F;
	}
	.dot {
		width: 20rpx;
		height: 20rpx;
		border-radius: 6rpx;
	}
	.dot.avail { background: #3A8A7E; }
	.dot.reserved { background: #D9822B; }
	.dot.off { background: #B85450; }
	.dot.unknown { background: #EAF3F0; border: 2rpx dashed #A9C7C1; }
	.dot-poi {
		width: 20rpx;
		height: 20rpx;
		border-radius: 50%;
		background: #6B7FA8;
	}
	.floor-tabs {
		display: flex;
		padding: 0 20rpx 10rpx;
		gap: 16rpx;
		overflow-x: auto;
	}
	.floor-tab {
		flex-shrink: 0;
		padding: 10rpx 30rpx;
		background: #FFFFFF;
		border-radius: 30rpx;
		font-size: 26rpx;
		color: #55554F;
	}
	/* 空间区域切换 */
	.area-tabs {
		display: flex;
		padding: 0 20rpx 14rpx;
		gap: 14rpx;
		overflow-x: auto;
	}
	.area-tab {
		flex-shrink: 0;
		padding: 8rpx 24rpx;
		background: #FFFFFF;
		border-radius: 24rpx;
		font-size: 24rpx;
		color: #55554F;
		border: 1rpx solid #E0DED6;
	}
	.area-tab.active {
		background: #3A8A7E;
		color: #FFFFFF;
		border-color: #3A8A7E;
	}
	.floor-tab.active {
		background: #3A8A7E;
		color: #FFFFFF;
	}
	.floor-count {
		font-size: 20rpx;
		opacity: 0.8;
	}

	/* 楼层平面图：绝对定位画布（与设计器一致），横向可滚动 */
	.map-scroll-x {
		width: 100%;
		white-space: nowrap;
	}
	.floor-map {
		position: relative;
		background: #F2F0EA;
		border-radius: 12rpx;
		display: inline-block;
	}
	.aisle-strip {
		background: #F2F0EA;
		pointer-events: none;
	}
	.aisle-strip.aisle {
		background: repeating-linear-gradient(90deg, #E8E5DC 0 8px, #F2F0EA 8px 16px);
	}
	.zone-rect {
		background: #FFFFFF;
		border-radius: 6px;
		border: 1px solid #D8D4C8;
		padding: 18px 2px 2px;
		position: relative;
		min-width: 0;
		min-height: 0;
		overflow: hidden;
		box-sizing: border-box;
	}
	.zone-grid {
		display: grid;
		width: 100%;
		box-sizing: border-box;
	}
	.map-cell {
		border-radius: 4px;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
		height: 100%;
		min-width: 0;
		min-height: 0;
	}
	.map-cell.vacant {
		background: transparent;
	}
	.map-cell.avail {
		background: #3A8A7E;
	}
	.map-cell.reserved {
		background: #FBEEDD;
		border: 1px solid #D9822B;
	}
	.map-cell.unknown {
		background: #EAF3F0;
		border: 1px dashed #A9C7C1;
	}
	.map-cell.off {
		background: #F6DEDE;
		border: 1px solid #B85450;
	}
	.seat-code {
		font-size: 10px;
		color: #FFFFFF;
		font-weight: 600;
	}
	.map-cell.reserved .seat-code, .map-cell.unknown .seat-code, .map-cell.off .seat-code {
		color: #33332E;
	}
	.zone-label {
		font-size: 9px;
		color: #8A8A86;
		text-align: center;
		position: absolute;
		left: 0;
		right: 0;
		top: 2px;
		line-height: 14px;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}
	.zone-letter {
		font-weight: 700;
		color: #3A8A7E;
	}
	/* 标志物 */
	.poi-rect {
		background: #6B7FA8;
		border-radius: 8px;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		color: #FFFFFF;
		padding: 2px;
		min-width: 0;
	}
	/* 文本标志物 */
	.poi-rect.poi-rect-text {
		background: rgba(107, 127, 168, 0.12);
		border: 1px dashed #6B7FA8;
		color: #3C4A6E;
		border-radius: 4px;
		display: flex;
	}
	.poi-text {
		font-size: 11px;
		font-weight: 600;
		white-space: nowrap;
		max-width: 100%;
		overflow: hidden;
		text-overflow: ellipsis;
		padding: 0 2px;
		transform-origin: center center;
		display: inline-block;
	}
	/* 线条标志物 */
	.poi-rect.poi-rect-line {
		background: transparent;
		display: flex;
	}
	.poi-line {
		width: 100%;
		height: 3px;
		background: #8A94B0;
		border-radius: 2px;
		transform-origin: center center;
	}
	.poi-icon {
		font-size: 18px;
	}
	.poi-name {
		font-size: 9px;
		margin-top: 2px;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		max-width: 100%;
	}
	.share-card {
		display: flex;
		flex-direction: column;
		gap: 10rpx;
	}
	.share-top {
		display: flex;
		justify-content: space-between;
	}
	.share-seat {
		font-size: 30rpx;
		font-weight: 600;
		color: #3A8A7E;
	}
	.share-loc {
		display: flex;
		gap: 12rpx;
		align-items: center;
	}
	.share-floor {
		font-size: 24rpx;
		color: #3A8A7E;
		background: #EAF3F0;
		padding: 2rpx 14rpx;
		border-radius: 8rpx;
	}
	.share-area {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.share-time {
		font-size: 26rpx;
	}
	.share-note {
		font-size: 24rpx;
		color: #8A8A86;
	}
</style>
