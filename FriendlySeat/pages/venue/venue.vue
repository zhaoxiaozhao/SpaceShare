<template>
	<page-meta :page-style="pageThemeStyle" />
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
			<view class="legend-item"><view class="dot unknown"></view><text>待分享</text></view>
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
								<view class="zone-body" :style="zoneBodyStyle(f, z)">
									<block v-if="(z.layoutMode || 'grid') === 'table'">
										<view v-for="(t, ti) in zoneTables(z)" :key="'t' + ti" class="table-rect" :style="tableRectStyle(f, z, t)"></view>
									</block>
									<block v-else-if="(z.layoutMode || 'grid') === 'arc' || (z.layoutMode || 'grid') === 'ellipse'">
										<view v-for="(ring, ri) in zoneArcRings(z)" :key="'r' + ri" class="arc-ring" :style="arcRingStyle(f, z, ring)"></view>
									</block>
									<block v-else-if="(z.layoutMode || 'grid') === 'spiral'">
										<view v-for="(ring, ri) in zoneSpiralRings(z)" :key="'r' + ri" class="arc-ring" :style="arcRingStyle(f, z, ring)"></view>
									</block>
									<block v-else-if="(z.layoutMode || 'grid') === 'sine'">
										<view v-for="(w, wi) in zoneSineGuide(z)" :key="'w' + wi" class="sine-guide" :style="sineGuideStyle(f, z, w)"></view>
									</block>
									<block v-else-if="(z.layoutMode || 'grid') === 'slant'">
										<view v-for="(s, si) in zoneSlantGuide(z)" :key="'s' + si" class="slant-guide" :style="slantGuideStyle(f, z, s)"></view>
									</block>
									<block v-else-if="(z.layoutMode || 'grid') === 'curve'">
										<view v-for="(s, si) in zoneCurveSegments(z)" :key="'c' + si" class="slant-guide" :style="curveSegmentStyle(f, z, s)"></view>
									</block>
									<view
										v-for="item in zoneAbsSeats(z)"
										:key="'s' + item.s.id"
										class="map-cell abs"
										:class="seatClass(item.s)"
										:style="absSeatStyle(f, z, item)"
										@click.stop="goSeat(item.s.id)"
									>
										<text v-if="item.s" class="seat-code" :style="seatCodeStyle()">{{seatShortCode(item.s.code)}}</text>
									</view>
								</view>
								<text class="zone-label" :style="zoneLabelStyle()"><text class="zone-letter">{{zoneLetter(z, f)}}区</text></text>
							</view>
							<view
								v-for="p in visiblePois(f)"
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
						<view class="zone-body" :style="zoneBodyStyle(f, z)">
							<block v-if="(z.layoutMode || 'grid') === 'table'">
								<view v-for="(t, ti) in zoneTables(z)" :key="'t' + ti" class="table-rect" :style="tableRectStyle(f, z, t)"></view>
							</block>
							<block v-else-if="(z.layoutMode || 'grid') === 'arc' || (z.layoutMode || 'grid') === 'ellipse'">
								<view v-for="(ring, ri) in zoneArcRings(z)" :key="'r' + ri" class="arc-ring" :style="arcRingStyle(f, z, ring)"></view>
							</block>
							<block v-else-if="(z.layoutMode || 'grid') === 'spiral'">
								<view v-for="(ring, ri) in zoneSpiralRings(z)" :key="'r' + ri" class="arc-ring" :style="arcRingStyle(f, z, ring)"></view>
							</block>
							<block v-else-if="(z.layoutMode || 'grid') === 'sine'">
								<view v-for="(w, wi) in zoneSineGuide(z)" :key="'w' + wi" class="sine-guide" :style="sineGuideStyle(f, z, w)"></view>
							</block>
							<block v-else-if="(z.layoutMode || 'grid') === 'slant'">
								<view v-for="(s, si) in zoneSlantGuide(z)" :key="'s' + si" class="slant-guide" :style="slantGuideStyle(f, z, s)"></view>
							</block>
							<block v-else-if="(z.layoutMode || 'grid') === 'curve'">
								<view v-for="(s, si) in zoneCurveSegments(z)" :key="'c' + si" class="slant-guide" :style="curveSegmentStyle(f, z, s)"></view>
							</block>
							<view
								v-for="item in zoneAbsSeats(z)"
								:key="'s' + item.s.id"
								class="map-cell abs"
								:class="seatClass(item.s)"
								:style="absSeatStyle(f, z, item)"
								@click.stop="goSeat(item.s.id)"
							>
								<text v-if="item.s" class="seat-code" :style="seatCodeStyle()">{{seatShortCode(item.s.code)}}</text>
							</view>
						</view>
						<text class="zone-label" :style="zoneLabelStyle()"><text class="zone-letter">{{zoneLetter(z, f)}}区</text></text>
					</view>

					<!-- 标志物（文本/线条可旋转） -->
					<view
						v-for="p in visiblePois(f)"
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
		onShareAppMessage() {
			const v = this.venue
			const title = v
				? `${v.name}${v.availableCount > 0 ? ` 有${v.availableCount}个座位可预约` : ''}`
				: '友邻座 - 发现身边的共享座位'
			return { title, path: `/pages/venue/venue?id=${this.id}` }
		},
		onShareTimeline() {
			const v = this.venue
			const title = v
				? `${v.name}${v.availableCount > 0 ? ` 有${v.availableCount}个座位可预约` : ''}`
				: '友邻座 - 发现身边的共享座位'
			return { title, query: `id=${this.id}` }
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
			visiblePois(f) {
				if (this.currentAreaId === null) return f.pois
				return f.pois.filter(p => (p.areaId || 0) === this.currentAreaId)
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
				// +18px：区块顶部标签条高度（区块为绝对定位不撑开父容器，需在此补偿，否则最后一排被外层裁切）
				return {
					width: `${cols * cell}px`,
					height: `${rows * cell + 18}px`
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
				const cols = z.gridCols || 1
				const rows = z.gridRows || 1
				return {
					position: 'absolute',
					left: `${(z.offsetX || 0) * cell}px`,
					top: `${(z.offsetY || 0) * cell}px`,
					width: `${cols * cell}px`,
					height: `${rows * cell + 18}px`
				}
			},
			// ===== 布局几何（桌椅格局 / 弧形排布）与设计器一致 =====
			tableGeometry(z) {
				const tc = Math.max(1, z.tableSeatCols || 2)
				const tr = Math.max(1, z.tableSeatRows || 2)
				const gx = Math.max(0, z.tableGapX == null ? 1 : z.tableGapX)
				const gy = Math.max(0, z.tableGapY == null ? 1 : z.tableGapY)
				const tx = Math.max(1, z.tablesX || 1)
				const ty = Math.max(1, z.tablesY || 1)
				const tables = []
				for (let j = 0; j < ty; j++) {
					for (let i = 0; i < tx; i++) {
						const ox = i * (tc + gx)
						const oy = j * (tr + gy)
						tables.push({ x: ox + 0.5, y: oy + 0.5, w: tc, h: tr })
					}
				}
				return { tables }
			},
			arcGeometry(z) {
				const rows = Math.max(1, z.arcRows || 3)
				const isEllipse = (z.layoutMode || 'grid') === 'ellipse'
				const a0 = (z.arcStartAngle == null ? 180 : z.arcStartAngle) * Math.PI / 180
				const a1 = (z.arcEndAngle == null ? 360 : z.arcEndAngle) * Math.PI / 180
				const axisA = Math.max(2, z.arcRadius || 8)
				const axisB = isEllipse ? Math.max(2, z.arcAxisB || axisA) : axisA
				const step = Math.max(0.5, z.arcRadiusStep || 1.5)
				const raw = []
				for (let r = 0; r < rows; r++) {
					const ar = axisA + r * step
					const br = isEllipse ? axisB + r * step : ar
					const cols = Math.max(1, z.arcSeatsPerRow || 8)
					for (let c = 0; c < cols; c++) {
						const ang = cols === 1 ? (a0 + a1) / 2 : a0 + (a1 - a0) * (c / (cols - 1))
						raw.push({ x: ar * Math.cos(ang), y: br * Math.sin(ang) })
					}
				}
				const minX = Math.min(...raw.map(p => p.x))
				const minY = Math.min(...raw.map(p => p.y))
				return {
					center: { x: -minX, y: -minY },
					rings: Array.from({ length: rows }, (_, r) => {
						const ar = axisA + r * step
						const br = isEllipse ? axisB + r * step : ar
						return { a: ar, b: br }
					})
				}
			},
			spiralGeometry(z) {
				const rows = Math.max(1, z.arcRows || 3)
				const cols = Math.max(1, z.arcSeatsPerRow || 8)
				const r0 = Math.max(1, z.arcRadius || 8)
				const pitch = Math.max(0.5, z.arcRadiusStep || 1.5)
				const a0 = (z.arcStartAngle == null ? 0 : z.arcStartAngle) * Math.PI / 180
				const totalDeg = (z.arcEndAngle == null ? 720 : z.arcEndAngle) === 0 ? 1 : (z.arcEndAngle == null ? 720 : z.arcEndAngle)
				const a1 = a0 + totalDeg * Math.PI / 180
				const total = rows * cols
				const raw = []
				for (let i = 0; i < total; i++) {
					const t = total === 1 ? 0.5 : i / (total - 1)
					const ang = a0 + (a1 - a0) * t
					const rad = r0 + Math.abs(ang - a0) / (2 * Math.PI) * pitch
					raw.push({ x: rad * Math.cos(ang), y: rad * Math.sin(ang) })
				}
				const minX = Math.min(...raw.map(p => p.x))
				const minY = Math.min(...raw.map(p => p.y))
				return {
					center: { x: -minX, y: -minY },
					rings: [
						{ a: r0, b: r0 },
						{ a: r0 + pitch * rows, b: r0 + pitch * rows }
					]
				}
			},
			sineGeometry(z) {
				const rows = Math.max(1, z.arcRows || 3)
				const cols = Math.max(1, z.arcSeatsPerRow || 8)
				const amp = Math.max(0.5, z.curveAmplitude || 2)
				const wl = Math.max(2, z.curveWavelength || 6)
				const phase = (z.curvePhase || 0) * Math.PI / 180
				const rowGap = Math.max(0.5, z.curveRowGap || 2)
				const raw = []
				for (let r = 0; r < rows; r++) {
					for (let c = 0; c < cols; c++) {
						const x = c
						const y = r * rowGap + amp * Math.sin((x / wl) * 2 * Math.PI + phase)
						raw.push({ x, y })
					}
				}
				const minX = Math.min(...raw.map(p => p.x))
				const minY = Math.min(...raw.map(p => p.y))
				const maxX = Math.max(...raw.map(p => p.x))
				return {
					guides: Array.from({ length: rows }, (_, r) => {
						const y = r * rowGap + amp
						return { y: y - minY + 0.5, x0: -minX + 0.5, x1: maxX - minX + 0.5 }
					})
				}
			},
			slantGeometry(z) {
				const rows = Math.max(1, z.arcRows || 3)
				const cols = Math.max(1, z.arcSeatsPerRow || 8)
				const ang = (z.curveAngle == null ? 30 : z.curveAngle) * Math.PI / 180
				const gap = Math.max(0.5, z.curveSlantGap || 2)
				const raw = []
				for (let r = 0; r < rows; r++) {
					for (let c = 0; c < cols; c++) {
						raw.push({
							x: c * Math.cos(ang) + r * Math.sin(ang),
							y: c * Math.sin(ang) - r * Math.cos(ang) + r * gap,
							row: r
						})
					}
				}
				const minX = Math.min(...raw.map(p => p.x))
				const minY = Math.min(...raw.map(p => p.y))
				const guides = []
				for (let r = 0; r < rows; r++) {
					const row = raw.filter(p => p.row === r)
					if (row.length) {
						guides.push({
							x0: row[0].x - minX + 0.5, y0: row[0].y - minY + 0.5,
							x1: row[row.length - 1].x - minX + 0.5, y1: row[row.length - 1].y - minY + 0.5
						})
					}
				}
				return { guides }
			},
			zoneTables(z) {
				return this.tableGeometry(z).tables
			},
			zoneArcRings(z) {
				const g = this.arcGeometry(z)
				return g.rings.map(ring => ({ cx: g.center.x, cy: g.center.y, a: ring.a, b: ring.b }))
			},
			zoneSpiralRings(z) {
				const g = this.spiralGeometry(z)
				return g.rings.map(ring => ({ cx: g.center.x, cy: g.center.y, a: ring.a, b: ring.b }))
			},
			zoneSineGuide(z) {
				return this.sineGeometry(z).guides || []
			},
			zoneSlantGuide(z) {
				return this.slantGeometry(z).guides || []
			},
			// ===== 自定义曲线（curve）：锚点即座位，仅渲染行向引导线 =====
			curvePoints(z) {
				if (!z.pathPoints) return []
				try {
					const arr = typeof z.pathPoints === 'string' ? JSON.parse(z.pathPoints) : z.pathPoints
					return (Array.isArray(arr) ? arr : []).filter(p => p && p.x != null && p.y != null)
				} catch (e) { return [] }
			},
			// 逐行连线引导：同一行的锚点（座位）相连，便于看清行方向
			zoneCurveSegments(z) {
				const pts = this.curvePoints(z)
				const cols = Math.max(1, z.arcSeatsPerRow || pts.length || 1)
				const segs = []
				for (let r = 0; r * cols < pts.length; r++) {
					const rowPts = pts.slice(r * cols, r * cols + cols)
					for (let i = 0; i < rowPts.length - 1; i++) {
						segs.push({ x0: rowPts[i].x, y0: rowPts[i].y, x1: rowPts[i + 1].x, y1: rowPts[i + 1].y })
					}
				}
				return segs
			},
			curveSegmentStyle(f, z, s) {
				const cell = this.cellPx(f)
				const dx = (s.x1 - s.x0) * cell
				const dy = (s.y1 - s.y0) * cell
				const len = Math.sqrt(dx * dx + dy * dy)
				const rot = Math.atan2(dy, dx) * 180 / Math.PI
				return {
					position: 'absolute',
					left: `${(s.x0 - 0.5) * cell}px`,
					top: `${(s.y0 - 0.5) * cell}px`,
					width: `${len}px`,
					height: '2px',
					transform: `rotate(${rot}deg)`,
					transformOrigin: 'left center'
				}
			},
			// 区块座位区显式高度（不依赖 bottom 撑开，避免小程序渲染差异）
			zoneBodyStyle(f, z) {
				const cell = this.cellPx(f)
				return {
					height: `${(z.gridRows || 1) * cell}px`
				}
			},
			// 座位绝对定位（支持小数坐标；统一为「格子中心」坐标，网格模式整数坐标 +0.5 对齐）
			zoneAbsSeats(z) {
				const isGrid = (z.layoutMode || 'grid') === 'grid'
				return (z.seats || []).map(s => ({
					s,
					x: (Number(s.positionX) || 0) + (isGrid ? 0.5 : 0),
					y: (Number(s.positionY) || 0) + (isGrid ? 0.5 : 0)
				}))
			},
			tableRectStyle(f, z, t) {
				const cell = this.cellPx(f)
				return {
					position: 'absolute',
					left: `${(t.x - 0.5) * cell}px`,
					top: `${(t.y - 0.5) * cell}px`,
					width: `${t.w * cell}px`,
					height: `${t.h * cell}px`
				}
			},
			arcRingStyle(f, z, ring) {
				const cell = this.cellPx(f)
				return {
					position: 'absolute',
					left: `${(ring.cx + 0.5 - ring.a) * cell}px`,
					top: `${(ring.cy + 0.5 - ring.b) * cell}px`,
					width: `${ring.a * 2 * cell}px`,
					height: `${ring.b * 2 * cell}px`
				}
			},
			sineGuideStyle(f, z, w) {
				const cell = this.cellPx(f)
				return {
					position: 'absolute',
					left: `${(w.x0 - 0.5) * cell}px`,
					top: `${(w.y - 0.5) * cell}px`,
					width: `${(w.x1 - w.x0) * cell}px`,
					height: '2px'
				}
			},
			slantGuideStyle(f, z, s) {
				const cell = this.cellPx(f)
				const ang = (z.curveAngle == null ? 30 : z.curveAngle) * Math.PI / 180
				const dx = (s.x1 - s.x0) * cell
				const dy = (s.y1 - s.y0) * cell
				const len = Math.sqrt(dx * dx + dy * dy)
				const rot = Math.atan2(dy, dx) * 180 / Math.PI
				return {
					position: 'absolute',
					left: `${(s.x0 - 0.5) * cell}px`,
					top: `${(s.y0 - 0.5) * cell}px`,
					width: `${len}px`,
					height: '2px',
					transform: `rotate(${rot}deg)`,
					transformOrigin: 'left center'
				}
			},
			absSeatStyle(f, z, item) {
				const cell = this.cellPx(f)
				return {
					position: 'absolute',
					left: `${(item.x - 0.5) * cell + 1}px`,
					top: `${(item.y - 0.5) * cell + 1}px`,
					width: `${cell - 2}px`,
					height: `${cell - 2}px`
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
		color: var(--primary);
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
		color: var(--primary);
	}
	.stat-label {
		font-size: 22rpx;
		color: #8A8A86;
	}
	.fs-btn {
		font-size: 28rpx;
		font-weight: 600;
		color: var(--primary);
		padding: 8rpx 20rpx;
		background: var(--primary-bg);
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
		background: var(--primary);
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
	.dot.avail { background: var(--primary); }
	.dot.reserved { background: #8C8C86; }
	.dot.off { background: #B85450; }
	.dot.unknown { background: var(--primary-bg); border: 2rpx dashed var(--primary-disabled); }
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
		background: var(--primary);
		color: #FFFFFF;
		border-color: var(--primary);
	}
	.floor-tab.active {
		background: var(--primary);
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
		position: relative;
		min-width: 0;
		min-height: 0;
		overflow: hidden;
		box-sizing: content-box;
	}
	.zone-body {
		position: absolute;
		left: 0;
		top: 18px;
		width: 100%;
		box-sizing: border-box;
		overflow: hidden;
	}
	/* 桌椅格局桌面 */
	.table-rect {
		position: absolute;
		background: #F0E6CC;
		border: 1px solid #D9C48F;
		border-radius: 4px;
		box-sizing: border-box;
		pointer-events: none;
	}
	/* 弧形排布引导圈 */
	.arc-ring {
		position: absolute;
		border: 1px dashed #C6D8D2;
		border-radius: 50%;
		box-sizing: border-box;
		pointer-events: none;
	}
	/* S形正弦波引导线 */
	.sine-guide {
		position: absolute;
		background: repeating-linear-gradient(90deg, #C6D8D2 0 4px, transparent 4px 8px);
		opacity: 0.6;
		pointer-events: none;
	}
	/* 斜线排布引导线 */
	.slant-guide {
		position: absolute;
		background: repeating-linear-gradient(90deg, #C6D8D2 0 4px, transparent 4px 8px);
		opacity: 0.6;
		pointer-events: none;
	}
	.map-cell.abs {
		position: absolute;
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
	.map-cell.avail {
		background: var(--primary);
	}
	.map-cell.reserved {
		background: #8C8C86;
	}
	.map-cell.unknown {
		background: var(--primary-bg);
		border: 1px dashed var(--primary-disabled);
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
	.map-cell.unknown .seat-code, .map-cell.off .seat-code {
		color: #33332E;
	}
	.zone-label {
		font-size: 10px;
		color: var(--primary);
		font-weight: 600;
		position: absolute;
		left: 0;
		right: 0;
		top: 0;
		height: 18px;
		line-height: 18px;
		text-align: center;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		z-index: 3;
	}
	.zone-letter {
		font-weight: 700;
		color: var(--primary);
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
		box-sizing: border-box;
		overflow: hidden;
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
		line-height: 1;
	}
	.poi-name {
		font-size: 9px;
		margin-top: 2px;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		max-width: 100%;
		line-height: 1.2;
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
		color: var(--primary);
	}
	.share-loc {
		display: flex;
		gap: 12rpx;
		align-items: center;
	}
	.share-floor {
		font-size: 24rpx;
		color: var(--primary);
		background: var(--primary-bg);
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
