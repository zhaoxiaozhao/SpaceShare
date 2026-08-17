<template>
  <div class="designer">
    <el-page-header @back="$router.back()" :content="detail.name + ' · 楼层平面图设计器'" style="margin-bottom: 12px" />

    <div class="toolbar">
      <el-select v-model="currentFloor" @change="onFloorChange" style="width: 130px">
        <el-option v-for="f in detail.floors" :key="f.id" :label="f.name" :value="f.id" />
      </el-select>
      <el-input-number v-model="floorCols" :min="10" :max="80" size="small" style="width: 90px" />
      <span class="dim-label">网格列（决定格子大小）</span>
      <el-input-number v-model="floorRows" :min="10" :max="120" size="small" style="width: 90px" />
      <span class="dim-label">行（地图高度）</span>
      <span class="dim-label">格子 {{ DESIGN_CELL }}px（小程序端自动适配手机屏）</span>
      <el-divider direction="vertical" />
      <el-button size="small" @click="zoomOut">－</el-button>
      <span class="dim-label zoom-label">{{ Math.round(zoom * 100) }}%</span>
      <el-button size="small" @click="zoomIn">＋</el-button>
      <el-slider v-model="zoom" :min="0.5" :max="2.5" :step="0.1" style="width: 120px; margin: 0 8px" />
      <el-divider direction="vertical" />
      <el-button size="small" @click="load">刷新</el-button>
      <el-button size="small" type="success" :loading="saving" @click="saveAll">保存全部</el-button>
      <span class="hint">拖拽组件到画布 · 拖动移动 · 拖角点缩放 · 双击座位区块编辑座位</span>
    </div>

    <!-- 空间区域管理（楼层 → 区域 → 座位区块） -->
    <div class="area-bar">
      <span class="area-bar-label">空间区域：</span>
      <el-select v-model="currentAreaId" style="width: 160px" @change="onAreaChange">
        <el-option v-for="a in currentFloorAreas" :key="a.id" :label="a.name" :value="a.id" />
      </el-select>
      <el-button size="small" @click="openAddArea">＋ 新增区域</el-button>
      <el-button v-if="currentAreaId" size="small" @click="renameArea">重命名</el-button>
      <el-button v-if="currentAreaId" size="small" type="danger" plain @click="deleteArea">删除区域</el-button>
      <span v-if="currentAreaId && currentAreaObj" class="area-count">{{ zoneCountInArea }} 个座位区块</span>
      <span v-else class="area-count">{{ floorZones.length }} 个座位区块</span>
    </div>

    <div class="designer-body">
      <!-- 组件库 -->
      <div class="palette">
        <div class="palette-title">组件库</div>
        <div
          v-for="t in palette"
          :key="t.type"
          class="palette-item"
          draggable="true"
          @dragstart="onDragStart($event, t)"
          @dblclick="quickAdd(t)"
        >
          <span class="pi-icon">{{ t.icon }}</span>
          <span class="pi-name">{{ t.label }}</span>
        </div>
      </div>

      <!-- 画布 -->
      <div
        class="canvas-wrap"
        @dragover.prevent
        @drop="onDrop"
        @mousedown="onCanvasMousedown"
        @mousemove="onCanvasMousemove"
        @mouseup="onCanvasMouseup"
        @mouseleave="onCanvasMouseup"
      >
        <div class="canvas-scaler" :style="scalerStyle">
          <div class="canvas" :style="canvasStyle" :class="{ 'placing': placingItem }">
          <!-- 网格底纹 -->
          <div class="grid-bg" :style="gridBgStyle"></div>
          <!-- 过道/走廊行 -->
          <div
            v-for="r in floorRows"
            :key="'row' + r"
            class="aisle-strip"
            :class="{ aisle: isAisleRow(r) }"
            :style="{ top: (r - 1) * cellPx() + 'px', left: 0, width: floorCols * cellPx() + 'px', height: cellPx() + 'px' }"
          ></div>

          <!-- 座位区块 -->
          <div
            v-for="z in visibleZones"
            :key="z._key"
            class="comp comp-zone"
            :class="{ selected: selectedKey === z._key }"
            :style="zoneStyle(z)"
            @mousedown.stop="onZoneMousedown($event, z)"
            @dblclick.stop="openZoneEditor(z)"
          >
            <div class="comp-name"><span class="zone-letter">{{ zoneLetter(z) }}区</span></div>
            <div class="zone-grid" :style="zoneGridStyle(z)">
              <div
                v-for="(cell, idx) in zoneGridCells(z)"
                :key="idx"
                class="seat-cell"
                :class="cell ? 'occupied' : 'empty'"
              >
                <span v-if="cell" class="seat-label">{{ shortCode(cell.code) }}</span>
              </div>
            </div>
            <template v-if="selectedKey === z._key">
              <div class="handle nw" data-handle="nw" @mousedown.stop="onResizeStart($event, z, 'nw')"></div>
              <div class="handle n" data-handle="n" @mousedown.stop="onResizeStart($event, z, 'n')"></div>
              <div class="handle ne" data-handle="ne" @mousedown.stop="onResizeStart($event, z, 'ne')"></div>
              <div class="handle e" data-handle="e" @mousedown.stop="onResizeStart($event, z, 'e')"></div>
              <div class="handle se" data-handle="se" @mousedown.stop="onResizeStart($event, z, 'se')"></div>
              <div class="handle s" data-handle="s" @mousedown.stop="onResizeStart($event, z, 's')"></div>
              <div class="handle sw" data-handle="sw" @mousedown.stop="onResizeStart($event, z, 'sw')"></div>
              <div class="handle w" data-handle="w" @mousedown.stop="onResizeStart($event, z, 'w')"></div>
              <div class="comp-actions">
                <span class="act-btn" @click.stop="openZoneEditor(z)">✎ 座位</span>
                <span class="act-btn danger" @click.stop="removeZone(z)">✕</span>
              </div>
            </template>
          </div>

          <!-- 标志物 POI（文本/线条特殊渲染，可旋转） -->
          <div
            v-for="p in floorPois"
            :key="p._key"
            class="comp comp-poi"
            :class="{ selected: selectedKey === p._key, 'comp-text': p.type === 'Text', 'comp-line': p.type === 'Line' }"
            :style="poiStyle(p)"
            @mousedown.stop="onPoiMousedown($event, p)"
          >
            <template v-if="p.type === 'Text'">
              <span class="poi-text" :style="{ transform: `rotate(${p.rotation || 0}deg)` }">{{ p.text || p.name }}</span>
            </template>
            <template v-else-if="p.type === 'Line'">
              <span class="poi-line" :style="{ transform: `rotate(${p.rotation || 0}deg)` }"></span>
            </template>
            <template v-else>
              <span class="poi-icon">{{ poiIcon(p.type) }}</span>
              <span class="poi-name">{{ p.name }}</span>
            </template>
            <template v-if="selectedKey === p._key">
              <div class="handle nw" data-handle="nw" @mousedown.stop="onResizeStart($event, p, 'nw')"></div>
              <div class="handle n" data-handle="n" @mousedown.stop="onResizeStart($event, p, 'n')"></div>
              <div class="handle ne" data-handle="ne" @mousedown.stop="onResizeStart($event, p, 'ne')"></div>
              <div class="handle e" data-handle="e" @mousedown.stop="onResizeStart($event, p, 'e')"></div>
              <div class="handle se" data-handle="se" @mousedown.stop="onResizeStart($event, p, 'se')"></div>
              <div class="handle s" data-handle="s" @mousedown.stop="onResizeStart($event, p, 's')"></div>
              <div class="handle sw" data-handle="sw" @mousedown.stop="onResizeStart($event, p, 'sw')"></div>
              <div class="handle w" data-handle="w" @mousedown.stop="onResizeStart($event, p, 'w')"></div>
              <div class="comp-actions">
                <span class="act-btn" @click.stop="openPoiEditor(p)">⚙</span>
                <span class="act-btn danger" @click.stop="removePoi(p)">✕</span>
              </div>
            </template>
          </div>

          <!-- 放置预览 -->
          <div v-if="placingItem" class="place-preview" :style="placePreviewStyle"></div>
          </div>
        </div>
      </div>

      <!-- 属性面板 -->
      <div class="props">
        <div class="props-title">属性</div>
        <template v-if="selectedComp">
          <el-form label-width="52px" size="small">
            <el-form-item label="名称">
              <el-input v-model="selectedComp.name" />
            </el-form-item>
            <el-form-item label="类型">
              <el-select v-model="selectedComp.type" v-if="selectedComp.kind === 'poi'">
                <el-option v-for="(t, k) in poiTypes" :key="k" :label="t" :value="k" />
              </el-select>
              <el-input v-else disabled :value="'座位区块'" />
            </el-form-item>
            <template v-if="selectedComp.kind === 'zone'">
              <el-form-item label="行数"><el-input-number v-model="selectedComp.gridRows" :min="1" :max="40" /></el-form-item>
              <el-form-item label="列数"><el-input-number v-model="selectedComp.gridCols" :min="1" :max="40" /></el-form-item>
            </template>
            <template v-else>
              <el-form-item v-if="selectedComp.type === 'Text'" label="内容">
                <el-input v-model="selectedComp.text" placeholder="文本内容" />
              </el-form-item>
              <el-form-item label="宽"><el-input-number v-model="selectedComp.width" :min="1" :max="30" /></el-form-item>
              <el-form-item label="高"><el-input-number v-model="selectedComp.height" :min="1" :max="20" /></el-form-item>
              <el-form-item v-if="selectedComp.type === 'Text' || selectedComp.type === 'Line'" label="旋转">
                <el-slider v-model="selectedComp.rotation" :min="0" :max="360" />
              </el-form-item>
              <el-form-item v-else label="方向">
                <el-select v-model="selectedComp.direction" clearable>
                  <el-option label="北" value="北" /><el-option label="南" value="南" />
                  <el-option label="东" value="东" /><el-option label="西" value="西" />
                </el-select>
              </el-form-item>
            </template>
          </el-form>
          <el-button v-if="selectedComp.kind === 'zone'" size="small" type="primary" style="width:100%" @click="openZoneEditor(selectedComp)">编辑座位</el-button>
        </template>
        <div v-else class="props-empty">点击画布中的元素查看/编辑属性</div>
      </div>
    </div>

    <!-- 座位子编辑 -->
    <el-dialog v-model="zoneEditorVisible" :title="'编辑座位 · ' + (editingZone?.name || '')" width="640px">
      <div v-if="editingZone">
        <div class="seat-editor-toolbar">
          <el-input v-model="newSeatCode" placeholder="座位编号，如 1F-A-001" size="small" style="width: 200px" />
          <el-button size="small" type="primary" @click="addSeatAt(null)">添加座位</el-button>
          <span class="hint">点击网格添加座位 · 点击已占位编辑/删除 · 编号前不带区域前缀则自动补齐</span>
        </div>
        <div class="seat-editor-grid" :style="zoneGridStyle(editingZone)">
          <div
            v-for="(cell, idx) in zoneGridCells(editingZone)"
            :key="idx"
            class="seat-cell"
            :class="cell ? 'occupied' : 'empty'"
            @click="onSeatCellClick(idx)"
          >
            <span v-if="cell" class="seat-label">{{ shortCode(cell.code) }}</span>
            <span v-else class="add-hint">+</span>
          </div>
        </div>
      </div>
    </el-dialog>

    <!-- 标志物编辑弹窗 -->
    <el-dialog v-model="poiEditorVisible" title="标志物属性" width="420px">
      <el-form label-width="70px">
        <el-form-item label="名称"><el-input v-model="poiEditForm.name" /></el-form-item>
        <el-form-item label="类型">
          <el-select v-model="poiEditForm.type">
            <el-option v-for="(t, k) in poiTypes" :key="k" :label="t" :value="k" />
          </el-select>
        </el-form-item>
        <el-form-item v-if="poiEditForm.type === 'Text'" label="内容">
          <el-input v-model="poiEditForm.text" placeholder="文本内容" />
        </el-form-item>
        <el-form-item v-if="poiEditForm.type === 'Text' || poiEditForm.type === 'Line'" label="旋转">
          <el-slider v-model="poiEditForm.rotation" :min="0" :max="360" />
        </el-form-item>
        <el-form-item v-else label="方向">
          <el-select v-model="poiEditForm.direction" clearable>
            <el-option label="北" value="北" /><el-option label="南" value="南" />
            <el-option label="东" value="东" /><el-option label="西" value="西" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="poiEditorVisible = false">关闭</el-button>
        <el-button type="primary" @click="savePoiEdit">完成</el-button>
      </template>
    </el-dialog>

    <!-- 座位编辑弹窗 -->
    <el-dialog v-model="seatEditVisible" title="编辑座位" width="380px">
      <el-form label-width="70px">
        <el-form-item label="编号"><el-input v-model="seatEditForm.code" /></el-form-item>
        <el-form-item label="靠窗"><el-switch v-model="seatEditForm.window" /></el-form-item>
        <el-form-item label="插座"><el-switch v-model="seatEditForm.powerSocket" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button type="danger" @click="deleteSeat">删除</el-button>
        <el-button @click="seatEditVisible = false">取消</el-button>
        <el-button type="primary" @click="saveSeat">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { venueApi } from '../api'

const route = useRoute()
const router = useRouter()
const venueId = route.query.id

// ============ 常量 ============
// 设计器使用固定舒适格子像素；小程序端把同一逻辑网格自动缩放适配手机屏，布局相对一致
const DESIGN_CELL = 28 // 设计器单格 px（舒适编辑用）
const poiTypes = {
  Toilet: '卫生间', DrinkingWater: '饮水机', Bookshelf: '书架', Elevator: '电梯口',
  Stairs: '楼梯', Corridor: '走廊/过道', Entrance: '入口', Exit: '安全出口',
  ServiceDesk: '服务台', Text: '文本', Line: '线条', Other: '其他'
}
const palette = [
  { type: 'Zone', kind: 'zone', icon: '🪑', label: '座位区块' },
  { type: 'Toilet', kind: 'poi', icon: '🚻', label: '卫生间', w: 2, h: 2 },
  { type: 'DrinkingWater', kind: 'poi', icon: '💧', label: '饮水机', w: 1, h: 1 },
  { type: 'Bookshelf', kind: 'poi', icon: '📚', label: '书架', w: 3, h: 1 },
  { type: 'Elevator', kind: 'poi', icon: '🛗', label: '电梯口', w: 1, h: 1 },
  { type: 'Stairs', kind: 'poi', icon: '🪜', label: '楼梯', w: 2, h: 2 },
  { type: 'Corridor', kind: 'poi', icon: '🛤️', label: '走廊/过道', w: 4, h: 1 },
  { type: 'Entrance', kind: 'poi', icon: '🚪', label: '入口', w: 2, h: 1 },
  { type: 'Exit', kind: 'poi', icon: '🚨', label: '安全出口', w: 1, h: 1 },
  { type: 'ServiceDesk', kind: 'poi', icon: 'ℹ️', label: '服务台', w: 3, h: 1 },
  { type: 'Text', kind: 'poi', icon: '🅣', label: '文本', w: 3, h: 1 },
  { type: 'Line', kind: 'poi', icon: '➖', label: '线条', w: 3, h: 1 },
  { type: 'Other', kind: 'poi', icon: '📍', label: '其他', w: 1, h: 1 }
]

// ============ 状态 ============
const detail = ref({ floors: [] })
const currentFloor = ref(null)
const floorCols = ref(22)
const floorRows = ref(40)
const selectedKey = ref(null)
const saving = ref(false)
const zoom = ref(1)
const currentAreaId = ref(null)

let seq = 1000 // 本地元素临时 key 序号
let dragPayload = null
let placingItem = null
let placePos = null
let interaction = null // { type: 'move'|'resize', item, kind, handle, startX, startY, orig }

const floorZones = computed(() => floor().zones)
const floorPois = computed(() => floor().pois)

// 空间区域相关
const currentFloorAreas = computed(() => floor().areas || [])
const currentAreaObj = computed(() => currentFloorAreas.value.find(a => a.id === currentAreaId.value) || null)
const zoneCountInArea = computed(() =>
  currentAreaId.value ? floorZones.value.filter(z => z.areaId === currentAreaId.value).length : 0
)
// 画布显示的区块：按当前区域过滤（无区域选择时显示全部）
const visibleZones = computed(() =>
  currentAreaId.value ? floorZones.value.filter(z => z.areaId === currentAreaId.value) : floorZones.value
)

// ============ 数据 ============
function floor() {
  return detail.value.floors.find(f => f.id === currentFloor.value) || { zones: [], pois: [] }
}

async function load(preserveFloor = false) {
  try {
    const prevFloor = currentFloor.value
    detail.value = await venueApi.venueDetail(venueId)
    // 为每个组件补充本地字段（_key/kind/_new），用于选中/删除/属性面板等本地操作
    for (const f of detail.value.floors) {
      if (!f.areas) f.areas = []
      for (const z of f.zones) {
        z.kind = 'zone'
        z._new = false
        if (!z._key) z._key = 'z' + (seq++)
        for (const s of z.seats) {
          if (!s._key) s._key = 's' + (seq++)
        }
      }
      for (const p of f.pois) {
        p.kind = 'poi'
        p._new = false
        if (!p._key) p._key = 'p' + (seq++)
      }
    }
    if (detail.value.floors.length) {
      currentFloor.value = preserveFloor && prevFloor
        ? prevFloor
        : detail.value.floors[0].id
      onFloorChange()
    }
  } catch (e) {}
}
load()

function onFloorChange() {
  const f = floor()
  // 依据内容自动扩展画布
  for (const z of f.zones) {
    floorCols.value = Math.max(floorCols.value, (z.offsetX || 0) + (z.gridCols || 1) + 5)
    floorRows.value = Math.max(floorRows.value, (z.offsetY || 0) + (z.gridRows || 1) + 5)
  }
  for (const p of f.pois) {
    floorCols.value = Math.max(floorCols.value, p.positionX + p.width + 5)
    floorRows.value = Math.max(floorRows.value, p.positionY + p.height + 5)
  }
  selectedKey.value = null
  // 默认选中第一个区域；无区域时为空
  const areas = floor().areas || []
  currentAreaId.value = areas.length ? areas[0].id : null
}

function onAreaChange() {
  selectedKey.value = null
}

async function openAddArea() {
  try {
    const { value } = await ElMessageBox.prompt('输入空间区域名称（如 主空间、走廊区域、平台区域）', '新增空间区域')
    if (!value || !value.trim()) return
    const sortOrder = currentFloorAreas.value.length
    const area = { _key: 'a' + (seq++), _new: true, id: null, name: value.trim(), sortOrder }
    floor().areas.push(area)
    // 立即保存区域，拿到真实 id，供区块引用
    const created = await venueApi.addArea({ floorId: currentFloor.value, name: area.name, sortOrder })
    area.id = created?.id || created
    area._new = false
    currentAreaId.value = area.id
  } catch (e) {}
}

async function renameArea() {
  const a = currentAreaObj.value
  if (!a) return
  try {
    const { value } = await ElMessageBox.prompt('输入新的区域名称', '重命名区域', { inputValue: a.name })
    if (!value || !value.trim()) return
    await venueApi.updateArea(a.id, { floorId: currentFloor.value, name: value.trim(), sortOrder: a.sortOrder })
    a.name = value.trim()
  } catch (e) {}
}

async function deleteArea() {
  const a = currentAreaObj.value
  if (!a) return
  try {
    await ElMessageBox.confirm(`删除空间区域「${a.name}」？其下座位区块将保留（回到无区域分组）。`, '确认删除')
    await venueApi.deleteArea(a.id)
    floor().areas = floor().areas.filter(x => x.id !== a.id)
    const areas = floor().areas || []
    currentAreaId.value = areas.length ? areas[0].id : null
    await load(true)
  } catch (e) {}
}

// ============ 画布样式（固定舒适格子像素，缩放看整体/细节） ============
function cellPx() {
  return DESIGN_CELL
}

// 缩放：外层占位尺寸 = 逻辑尺寸 × zoom（transform scale 不影响布局）
const scalerStyle = computed(() => ({
  width: floorCols.value * DESIGN_CELL * zoom.value + 'px',
  height: floorRows.value * DESIGN_CELL * zoom.value + 'px'
}))
const canvasStyle = computed(() => ({
  width: floorCols.value * DESIGN_CELL + 'px',
  height: floorRows.value * DESIGN_CELL + 'px',
  transform: `scale(${zoom.value})`,
  transformOrigin: 'top left'
}))

function zoomIn() {
  zoom.value = Math.min(2.5, Math.round((zoom.value + 0.1) * 10) / 10)
}
function zoomOut() {
  zoom.value = Math.max(0.5, Math.round((zoom.value - 0.1) * 10) / 10)
}
const gridBgStyle = computed(() => ({
  backgroundSize: `${cellPx()}px ${cellPx()}px`,
  width: floorCols.value * cellPx() + 'px',
  height: floorRows.value * cellPx() + 'px'
}))

function isAisleRow(row) {
  return !floorZones.value.some(z => row - 1 >= (z.offsetY || 0) && row - 1 < (z.offsetY || 0) + (z.gridRows || 1))
}

function zoneStyle(z) {
  const c = cellPx()
  return {
    left: (z.offsetX || 0) * c + 'px',
    top: (z.offsetY || 0) * c + 'px',
    width: (z.gridCols || 1) * c + 'px',
    height: (z.gridRows || 1) * c + 'px'
  }
}
function poiStyle(p) {
  const c = cellPx()
  return {
    left: p.positionX * c + 'px',
    top: p.positionY * c + 'px',
    width: p.width * c + 'px',
    height: p.height * c + 'px'
  }
}
function zoneGridStyle(z) {
  return { gridTemplateColumns: `repeat(${z.gridCols || 1}, 1fr)` }
}
function zoneGridCells(z) {
  const rows = z.gridRows || 1
  const cols = z.gridCols || 1
  const grid = new Array(rows * cols).fill(null)
  for (const s of z.seats) {
    const x = Math.floor(s.positionX || 0)
    const y = Math.floor(s.positionY || 0)
    if (y >= 0 && y < rows && x >= 0 && x < cols) grid[y * cols + x] = s
  }
  return grid
}
function shortCode(code) {
  const parts = String(code).split('-')
  return parts[parts.length - 1] || code
}
// 按区块在当前空间区域内的顺序生成字母（A区、B区、C区…），无区域时按楼层内顺序
function zoneLetter(z) {
  // 同一楼层内区块字母唯一：按 区域顺序 → 区块排序（sortOrder → offsetX → id）
  // 不随区域筛选变化，与后端/小程序一致
  const areas = floor().areas || []
  const areaOrder = {}
  areas.forEach((a, i) => { areaOrder[a.id] = i })

  const list = floorZones.value.slice().sort((a, b) => {
    const ao = (areaOrder[a.areaId] ?? 999) - (areaOrder[b.areaId] ?? 999)
    if (ao !== 0) return ao
    return (a.sortOrder || 0) - (b.sortOrder || 0) ||
      (a.offsetX || 0) - (b.offsetX || 0) ||
      (a.id || 0) - (b.id || 0)
  })
  const idx = list.findIndex(x => x._key === z._key)
  if (idx < 0) return '?'
  return String.fromCharCode(65 + idx)
}
function poiIcon(type) {
  const map = {
    Toilet: '🚻', DrinkingWater: '💧', Bookshelf: '📚', Elevator: '🛗', Stairs: '🪜',
    Corridor: '🛤️', Entrance: '🚪', Exit: '🚨', ServiceDesk: 'ℹ️', Other: '📍'
  }
  return map[type] || '📍'
}

// ============ 组件库拖拽 ============
function onDragStart(e, item) {
  dragPayload = item
  e.dataTransfer.effectAllowed = 'copy'
  e.dataTransfer.setData('text/plain', item.type)
  placingItem = item
}
function quickAdd(item) {
  dragPayload = item
  placingItem = item
  placePos = { x: 0, y: 0 }
}
function placePreviewStyle() {
  const w = (dragPayload?.w || 4)
  const h = (dragPayload?.h || 3)
  const c = cellPx()
  return {
    left: (placePos?.x ?? 0) * c + 'px',
    top: (placePos?.y ?? 0) * c + 'px',
    width: (dragPayload?.kind === 'zone' ? 6 : w) * c + 'px',
    height: (dragPayload?.kind === 'zone' ? 4 : h) * c + 'px'
  }
}
// 将鼠标事件坐标换算为画布逻辑坐标（考虑缩放）
function toCanvasPos(e) {
  const el = e.currentTarget
  const rect = el.getBoundingClientRect()
  const lx = (e.clientX - rect.left) / zoom.value
  const ly = (e.clientY - rect.top) / zoom.value
  const c = cellPx()
  return {
    x: Math.max(0, Math.floor(lx / c)),
    y: Math.max(0, Math.floor(ly / c))
  }
}
function onDrop(e) {
  if (!dragPayload) return
  const { x, y } = toCanvasPos(e)
  addComponent(dragPayload, x, y)
  dragPayload = null
  placingItem = null
}
function onCanvasMousemove(e) {
  if (placingItem) {
    placePos = toCanvasPos(e)
  }
  if (interaction) doInteraction(e)
}
function onCanvasMousedown() {
  selectedKey.value = null
}
function onCanvasMouseup() {
  // 缩放区块结束后：铺满座位并自动调整编号
  if (interaction && interaction.type === 'resize' && interaction.kind === 'zone') {
    syncZoneSeats(interaction.item)
  }
  interaction = null
}

function addComponent(item, x, y) {
  const f = floor()
  if (item.kind === 'zone') {
    const key = 'z' + (seq++)
    f.zones.push({
      _key: key, kind: 'zone', _new: true, id: null, areaId: currentAreaId.value,
      name: '座位区块', offsetX: x, offsetY: y, gridRows: 4, gridCols: 6, seats: []
    })
    selectedKey.value = key
    // 自动生成座位网格（默认铺满，可后续调整）
    const z = f.zones.find(z => z._key === key)
    setTimeout(() => autoFillSeats(z), 0)
  } else {
    const key = 'p' + (seq++)
    const isText = item.type === 'Text'
    const isLine = item.type === 'Line'
    f.pois.push({
      _key: key, kind: 'poi', _new: true, id: null, floorId: currentFloor.value,
      type: item.type, name: poiTypes[item.type] || item.type,
      positionX: x, positionY: y, width: item.w || 2, height: item.h || 2,
      direction: '', rotation: 0,
      text: isText ? '文本内容' : (isLine ? '——' : undefined)
    })
    selectedKey.value = key
  }
}

function autoFillSeats(z) {
  // 给新区块自动铺满座位（可删除空位形成形状），编号唯一避免唯一约束冲突
  z.seats = []
  const region = regionPrefix(z)
  for (let r = 0; r < z.gridRows; r++) {
    for (let c = 0; c < z.gridCols; c++) {
      const n = r * (z.gridCols || 1) + c + 1
      z.seats.push({
        _key: 's' + (seq++), _new: true, id: null,
        code: `${region}-${String(n).padStart(3, '0')}`,
        positionX: c, positionY: r,
        type: 'Normal', window: false, powerSocket: false
      })
    }
  }
  ElMessage.info('已自动生成座位网格，双击区块可调整座位')
}

// 缩放/调整区块后：按当前网格铺满座位，并按顺序重新编号
// 保留已存在的座位对象（含 id），新增补位、超界剔除
function syncZoneSeats(z) {
  const rows = Math.max(z.gridRows || 1, 1)
  const cols = Math.max(z.gridCols || 1, 1)
  const region = regionPrefix(z)

  const existing = {}
  for (const s of z.seats) {
    const key = `${s.positionX},${s.positionY}`
    existing[key] = s
  }

  const newSeats = []
  let n = 0
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      n++
      const key = `${c},${r}`
      let seat = existing[key]
      if (seat) {
        // 已在界内：更新编号
        seat.code = `${region}-${String(n).padStart(3, '0')}`
        newSeats.push(seat)
      } else {
        // 补位新座位
        newSeats.push({
          _key: 's' + (seq++), _new: true, id: null,
          code: `${region}-${String(n).padStart(3, '0')}`,
          positionX: c, positionY: r,
          type: 'Normal', window: false, powerSocket: false
        })
      }
    }
  }

  // 超界座位（已存在于后端）标记删除
  const stale = z.seats.filter(s => {
    const x = s.positionX, y = s.positionY
    return x >= cols || y >= rows
  })
  for (const s of stale) {
    if (s.id) venueApi.deleteSeat(s.id).catch(() => {})
  }

  z.seats = newSeats
  ElMessage.info(`已按 ${cols}×${rows} 铺满 ${newSeats.length} 个座位`)
}

function regionPrefix(z) {
  // 依据区块名生成简洁前缀：取第一个中文/字母段，fallback 到 "Z"
  const m = (z.name || '').match(/[0-9A-Za-z\u4e00-\u9fa5]/g)
  return (m ? m.join('').slice(0, 6) : 'Z') || 'Z'
}

// ============ 选择 ============
const selectedComp = computed(() => {
  if (!selectedKey.value) return null
  const z = floorZones.value.find(z => z._key === selectedKey.value)
  if (z) return z
  return floorPois.value.find(p => p._key === selectedKey.value) || null
})

function onZoneMousedown(e, z) {
  selectedKey.value = z._key
  startMove(e, z, 'zone')
}
function onPoiMousedown(e, p) {
  selectedKey.value = p._key
  startMove(e, p, 'poi')
}
function startMove(e, item, kind) {
  interaction = {
    type: 'move', kind, item,
    startX: e.clientX, startY: e.clientY,
    origX: kind === 'zone' ? (item.offsetX || 0) : item.positionX,
    origY: kind === 'zone' ? (item.offsetY || 0) : item.positionY
  }
}
function onResizeStart(e, item, handle) {
  e.stopPropagation()
  selectedKey.value = item._key
  interaction = {
    type: 'resize', kind: item.kind, item, handle,
    startX: e.clientX, startY: e.clientY,
    orig: {
      x: item.kind === 'zone' ? (item.offsetX || 0) : item.positionX,
      y: item.kind === 'zone' ? (item.offsetY || 0) : item.positionY,
      w: item.kind === 'zone' ? (item.gridCols || 1) : item.width,
      h: item.kind === 'zone' ? (item.gridRows || 1) : item.height
    }
  }
}

function doInteraction(e) {
  const c = cellPx()
  const dx = Math.round((e.clientX - interaction.startX) / zoom.value / c)
  const dy = Math.round((e.clientY - interaction.startY) / zoom.value / c)
  const it = interaction.item

  if (interaction.type === 'move') {
    const nx = Math.max(0, interaction.origX + dx)
    const ny = Math.max(0, interaction.origY + dy)
    if (interaction.kind === 'zone') {
      it.offsetX = Math.min(nx, floorCols.value - 1)
      it.offsetY = Math.min(ny, floorRows.value - 1)
    } else {
      it.positionX = Math.min(nx, floorCols.value - 1)
      it.positionY = Math.min(ny, floorRows.value - 1)
    }
    return
  }

  // resize
  const o = interaction.orig
  let x = o.x, y = o.y, w = o.w, h = o.h
  if (interaction.handle.includes('e')) w = Math.max(1, o.w + dx)
  if (interaction.handle.includes('s')) h = Math.max(1, o.h + dy)
  if (interaction.handle.includes('w')) {
    const nw = Math.max(1, o.w - dx)
    x = o.x + (o.w - nw); w = nw
  }
  if (interaction.handle.includes('n')) {
    const nh = Math.max(1, o.h - dy)
    y = o.y + (o.h - nh); h = nh
  }
  x = Math.max(0, x); y = Math.max(0, y)
  if (interaction.kind === 'zone') {
    it.offsetX = x; it.offsetY = y; it.gridCols = w; it.gridRows = h
  } else {
    it.positionX = x; it.positionY = y; it.width = w; it.height = h
  }
}

// ============ 删除 ============
async function removeZone(z) {
  try {
    await ElMessageBox.confirm(`删除区块「${z.name}」及其全部座位？`, '确认')
    const f = floor()
    f.zones = f.zones.filter(x => x._key !== z._key)
    selectedKey.value = null
    if (!z._new && z.id) await venueApi.deleteZone?.(z.id)
  } catch (e) {}
}
async function removePoi(p) {
  try {
    await ElMessageBox.confirm(`删除标志物「${p.name}」？`, '确认')
    const f = floor()
    f.pois = f.pois.filter(x => x._key !== p._key)
    selectedKey.value = null
    if (!p._new && p.id) await venueApi.deletePoi(p.id)
  } catch (e) {}
}

// ============ 座位子编辑 ============
const zoneEditorVisible = ref(false)
const editingZone = ref(null)
const newSeatCode = ref('')
const seatEditVisible = ref(false)
const seatEditForm = ref({})
let editingSeatIdx = -1

function openZoneEditor(z) {
  editingZone.value = z
  zoneEditorVisible.value = true
  newSeatCode.value = ''
}
function onSeatCellClick(idx) {
  const z = editingZone.value
  const cols = z.gridCols || 1
  const cell = z.seats.find(s => Math.floor(s.positionX) === idx % cols && Math.floor(s.positionY) === Math.floor(idx / cols))
  if (cell) {
    editingSeatIdx = idx
    seatEditForm.value = { ...cell }
    seatEditVisible.value = true
  } else {
    addSeatAt(idx)
  }
}
function addSeatAt(idx) {
  const z = editingZone.value
  const cols = z.gridCols || 1
  const px = idx == null ? 0 : idx % cols
  const py = idx == null ? 0 : Math.floor(idx / cols)
  let code = newSeatCode.value.trim()
  if (!code) {
    code = genSeatCode(z)
  }
  z.seats.push({ _key: 's' + (seq++), _new: true, id: null, code, positionX: px, positionY: py, type: 'Normal', window: false, powerSocket: false })
  newSeatCode.value = ''

function genSeatCode(z) {
  // 自动生成座位编号（基于区块名 + 顺序号）
  const region = regionPrefix(z)
  const n = z.seats.length + 1
  return `${region}-${String(n).padStart(3, '0')}`
}
}
async function saveSeat() {
  const z = editingZone.value
  const seat = z.seats.find(s => s._key === seatEditForm.value._key)
  if (seat) {
    seat.code = seatEditForm.value.code
    seat.window = seatEditForm.value.window
    seat.powerSocket = seatEditForm.value.powerSocket
  }
  seatEditVisible.value = false
}
function deleteSeat() {
  const z = editingZone.value
  const seat = z.seats.find(s => s._key === seatEditForm.value._key)
  if (seat) {
    z.seats = z.seats.filter(s => s._key !== seat._key)
    if (seat.id) venueApi.deleteSeat?.(seat.id).catch(() => {})
  }
  seatEditVisible.value = false
}

// ============ 标志物属性弹窗 ============
const poiEditorVisible = ref(false)
const poiEditForm = ref({})
function openPoiEditor(p) {
  poiEditForm.value = { ...p }
  poiEditorVisible.value = true
}
function savePoiEdit() {
  // 将弹窗中的修改写回画布组件
  const target = floorPois.value.find(x => x._key === poiEditForm.value._key)
  if (target) {
    target.name = poiEditForm.value.name
    target.type = poiEditForm.value.type
    target.text = poiEditForm.value.text
    target.rotation = poiEditForm.value.rotation || 0
    target.direction = poiEditForm.value.direction
  }
  poiEditorVisible.value = false
}

// ============ 保存 ============
function normalizeSeatCodes(f) {
  // 设计阶段：编号允许重复，仅保证非空
  for (const z of f.zones) {
    for (const s of z.seats) {
      if (!(s.code || '').trim()) {
        s.code = genSeatCode(z)
      }
    }
  }
}

async function saveAll() {
  saving.value = true
  try {
    const f = floor()
    normalizeSeatCodes(f)
    // 保存空间区域
    for (const a of f.areas) {
      const payload = { floorId: currentFloor.value, name: a.name, sortOrder: a.sortOrder || 0 }
      if (a._new) {
        const created = await venueApi.addArea(payload)
        a.id = created?.id || created
        a._new = false
      } else {
        await venueApi.updateArea(a.id, payload)
      }
    }
    // 保存区块
    for (const z of f.zones) {
      const payload = {
        floorId: currentFloor.value,
        areaId: z.areaId || null,
        name: z.name,
        sortOrder: 0,
        gridRows: z.gridRows,
        gridCols: z.gridCols,
        offsetX: z.offsetX,
        offsetY: z.offsetY
      }
      let zoneId = z.id
      if (z._new) {
        const created = await venueApi.addZone(payload)
        zoneId = created?.id || created
        z._new = false
      } else {
        await venueApi.updateZone(z.id, payload)
      }
      // 保存座位
      for (const s of z.seats) {
        const sp = {
          zoneId,
          code: s.code,
          type: s.type,
          window: s.window,
          powerSocket: s.powerSocket,
          positionX: s.positionX,
          positionY: s.positionY
        }
        if (s._new) {
          const created = await venueApi.addSeat(sp)
          s.id = created?.id || created
          s._new = false
        } else {
          await venueApi.updateSeat(s.id, sp)
        }
      }
    }
    // 保存 POI
    for (const p of f.pois) {
      const payload = {
        floorId: currentFloor.value,
        type: p.type,
        name: p.name,
        positionX: p.positionX,
        positionY: p.positionY,
        width: p.width,
        height: p.height,
        direction: p.direction || '',
        rotation: p.rotation || 0,
        text: p.text
      }
      if (p._new) {
        const created = await venueApi.addPoi(payload)
        p.id = created?.id || created
        p._new = false
      } else {
        await venueApi.updatePoi(p.id, payload)
      }
    }
    ElMessage.success('已保存全部变更')
    await load(true)
  } catch (e) {
    ElMessage.error('保存失败，请检查网络')
  } finally {
    saving.value = false
  }
}

// 属性面板调整区块行数/列数时：铺满座位并重新编号
watch(
  () => {
    const z = selectedComp.value
    return z && z.kind === 'zone' ? [z.gridRows, z.gridCols] : null
  },
  (val, old) => {
    if (val && old && (val[0] !== old[0] || val[1] !== old[1])) {
      syncZoneSeats(selectedComp.value)
    }
  }
)
</script>

<style scoped>
.designer {
  min-height: 100%;
}
.toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 10px;
  flex-wrap: wrap;
}
.dim-label {
  color: #888;
  font-size: 12px;
}
.zoom-label {
  min-width: 42px;
  text-align: center;
  font-weight: 600;
  color: #555;
}
.hint {
  color: #999;
  font-size: 12px;
  margin-left: 8px;
}
.designer-body {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}
.palette {
  width: 110px;
  flex-shrink: 0;
  background: #fff;
  border-radius: 8px;
  padding: 8px;
  border: 1px solid #e8e8e8;
  position: sticky;
  top: 0;
}
.palette-title, .props-title {
  font-size: 13px;
  font-weight: 600;
  color: #444;
  margin-bottom: 8px;
  padding-bottom: 6px;
  border-bottom: 1px solid #f0f0f0;
}
.palette-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 8px 4px;
  margin-bottom: 4px;
  border-radius: 6px;
  border: 1px dashed #ddd;
  cursor: grab;
  user-select: none;
}
.palette-item:hover {
  border-color: #3A8A7E;
  background: #f3faf8;
}
.pi-icon {
  font-size: 20px;
}
.pi-name {
  font-size: 11px;
  color: #555;
  margin-top: 2px;
}
.canvas-wrap {
  flex: 1;
  min-width: 0;
  overflow: auto;
  background: #f0efea;
  border-radius: 8px;
  padding: 16px;
  border: 1px solid #e0ded6;
  max-height: calc(100vh - 200px);
  display: flex;
  justify-content: center;
}
.canvas-scaler {
  flex-shrink: 0;
  position: relative;
}
.canvas {
  position: relative;
  background: #faf9f4;
  border: 1px solid #d8d5cb;
}
.canvas.placing {
  cursor: copy;
}
.grid-bg {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(#e6e4db 1px, transparent 1px),
    linear-gradient(90deg, #e6e4db 1px, transparent 1px);
  pointer-events: none;
}
.aisle-strip {
  position: absolute;
  pointer-events: none;
  background: transparent;
}
.aisle-strip.aisle {
  background: repeating-linear-gradient(90deg, #ece9df 0 6px, #f5f3ec 6px 12px);
}
.comp {
  position: absolute;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  cursor: move;
  user-select: none;
}
.comp.selected {
  outline: 2px solid #3A8A7E;
  outline-offset: -1px;
}
.comp-zone {
  background: #fff;
  border: 1px solid #d8d4c8;
  border-radius: 4px;
  padding: 2px;
}
.zone-grid {
  flex: 1;
  display: grid;
  gap: 2px;
  min-height: 0;
  overflow: hidden;
}
.comp-poi {
  background: #6B7FA8;
  border-radius: 6px;
  align-items: center;
  justify-content: center;
  color: #fff;
}
/* 文本组件 */
.comp-poi.comp-text {
  background: rgba(107, 127, 168, 0.12);
  border: 1px dashed #6B7FA8;
  color: #3c4a6e;
  border-radius: 3px;
}
.poi-text {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  padding: 0 2px;
  transform-origin: center center;
}
/* 线条组件 */
.comp-poi.comp-line {
  background: transparent;
  display: flex;
  align-items: center;
  justify-content: center;
}
.poi-line {
  display: block;
  width: 100%;
  height: 3px;
  background: #8a94b0;
  border-radius: 2px;
  transform-origin: center center;
}
.poi-icon {
  font-size: 16px;
}
.poi-name {
  font-size: 9px;
  margin-top: 2px;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  padding: 0 2px;
}
.comp-name {
  font-size: 10px;
  color: #888;
  text-align: center;
  flex-shrink: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.zone-letter {
  font-weight: 700;
  color: #3A8A7E;
  font-size: 12px;
}
.seat-cell {
  border-radius: 3px;
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 0;
  font-size: 8px;
}
.seat-cell.occupied {
  background: #3A8A7E;
  color: #fff;
}
.seat-cell.empty {
  background: #f4f3ee;
  border: 1px dashed #d8d5cb;
}
.seat-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 100%;
  padding: 0 1px;
}
.add-hint {
  color: #bbb;
  font-size: 12px;
}
.handle {
  position: absolute;
  width: 8px;
  height: 8px;
  background: #3A8A7E;
  border: 1px solid #fff;
  border-radius: 2px;
  z-index: 5;
}
.handle.nw { left: -5px; top: -5px; cursor: nwse-resize; }
.handle.n  { left: 50%; margin-left: -4px; top: -5px; cursor: ns-resize; }
.handle.ne { right: -5px; top: -5px; cursor: nesw-resize; }
.handle.e  { right: -5px; top: 50%; margin-top: -4px; cursor: ew-resize; }
.handle.se { right: -5px; bottom: -5px; cursor: nwse-resize; }
.handle.s  { left: 50%; margin-left: -4px; bottom: -5px; cursor: ns-resize; }
.handle.sw { left: -5px; bottom: -5px; cursor: nesw-resize; }
.handle.w  { left: -5px; top: 50%; margin-top: -4px; cursor: ew-resize; }
.comp-actions {
  position: absolute;
  top: -20px;
  right: 0;
  display: flex;
  gap: 4px;
  z-index: 6;
}
.act-btn {
  font-size: 11px;
  background: #3A8A7E;
  color: #fff;
  border-radius: 3px;
  padding: 1px 6px;
  cursor: pointer;
  white-space: nowrap;
}
.act-btn.danger {
  background: #d9534f;
}
.place-preview {
  position: absolute;
  border: 2px dashed #3A8A7E;
  background: rgba(58, 138, 126, 0.15);
  border-radius: 4px;
  pointer-events: none;
  z-index: 3;
}
.props {
  width: 180px;
  flex-shrink: 0;
  background: #fff;
  border-radius: 8px;
  padding: 10px;
  border: 1px solid #e8e8e8;
  position: sticky;
  top: 0;
}
.props-empty {
  color: #aaa;
  font-size: 12px;
  text-align: center;
  padding: 20px 0;
}
.seat-editor-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}
.seat-editor-grid {
  display: grid;
  gap: 6px;
  background: #faf9f4;
  border-radius: 8px;
  padding: 12px;
  max-height: 480px;
  overflow: auto;
}
.seat-editor-grid .seat-cell {
  aspect-ratio: 1;
  min-height: 32px;
  font-size: 12px;
  cursor: pointer;
}
.seat-editor-grid .seat-cell.empty:hover {
  border-color: #3A8A7E;
  background: #eef7f4;
}
</style>
