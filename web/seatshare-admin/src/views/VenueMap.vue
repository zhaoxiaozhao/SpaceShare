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
            <div class="comp-name"><span class="zone-letter">{{ zoneLetter(z) }}区</span><span v-if="z.layoutMode && z.layoutMode !== 'grid'" class="mode-tag">{{ layoutModeLabel(z.layoutMode) }}</span></div>
            <div class="zone-body">
              <!-- 桌椅格局：渲染桌面矩形 -->
              <template v-if="z.layoutMode === 'table'">
                <div v-for="(t, ti) in zoneTables(z)" :key="'t' + ti" class="table-rect" :style="tableRectStyle(z, t)"></div>
              </template>
              <!-- 圆弧/椭圆弧：渲染弧线引导圈 -->
              <template v-else-if="z.layoutMode === 'arc' || z.layoutMode === 'ellipse'">
                <div v-for="(ring, ri) in zoneArcRings(z)" :key="'r' + ri" class="arc-ring" :style="arcRingStyle(z, ring)"></div>
              </template>
              <!-- 螺旋：渲染参考圆环（起始/最外圈） -->
              <template v-else-if="z.layoutMode === 'spiral'">
                <div v-for="(ring, ri) in zoneSpiralRings(z)" :key="'r' + ri" class="arc-ring" :style="arcRingStyle(z, ring)"></div>
              </template>
              <!-- S形正弦波：渲染波峰引导线 -->
              <template v-else-if="z.layoutMode === 'sine'">
                <div v-for="(w, wi) in zoneSineGuide(z)" :key="'w' + wi" class="sine-guide" :style="sineGuideStyle(z, w)"></div>
              </template>
              <!-- 斜线：渲染行向引导线 -->
              <template v-else-if="z.layoutMode === 'slant'">
                <div v-for="(s, si) in zoneSlantGuide(z)" :key="'s' + si" class="slant-guide" :style="slantGuideStyle(z, s)"></div>
              </template>
              <!-- 自定义曲线：座位锚点直接渲染为可拖动手柄（仅选中区块时显示） -->
              <template v-else-if="z.layoutMode === 'curve'">
                <svg v-if="zoneCurvePath(z)" class="curve-svg" :viewBox="curveSvgViewBox(z)">
                  <path :d="zoneCurvePath(z)" fill="none" stroke="#b9cfc9" stroke-width="2" stroke-dasharray="5,4" />
                </svg>
                <template v-if="selectedKey === z._key">
                  <div
                    v-for="(p, pi) in zoneCurveControls(z)"
                    :key="'cp' + pi"
                    class="curve-point"
                    :style="curvePointStyle(z, p)"
                    @mousedown.stop="onCurvePointMousedown($event, z, pi)"
                  >
                    <span class="curve-point-idx">{{ pi + 1 }}</span>
                  </div>
                </template>
              </template>
              <!-- 参数化布局：座位按坐标绝对定位 -->
              <template v-if="isParametric(z)">
                <div
                  v-for="item in zoneAbsSeats(z)"
                  :key="item.s._key"
                  class="abs-seat"
                  :style="absSeatStyle(z, item)"
                >
                  <span class="seat-label">{{ shortCode(item.s.code) }}</span>
                </div>
              </template>
              <!-- 网格布局：保留网格铺位渲染 -->
              <div v-else class="zone-grid" :style="zoneGridStyle(z)">
                <div
                  v-for="(cell, idx) in zoneGridCells(z)"
                  :key="idx"
                  class="seat-cell"
                  :class="cell ? 'occupied' : 'empty'"
                >
                  <span v-if="cell" class="seat-label">{{ shortCode(cell.code) }}</span>
                </div>
              </div>
            </div>
            <template v-if="selectedKey === z._key">
              <template v-if="!isParametric(z)">
                <div class="handle nw" data-handle="nw" @mousedown.stop="onResizeStart($event, z, 'nw')"></div>
                <div class="handle n" data-handle="n" @mousedown.stop="onResizeStart($event, z, 'n')"></div>
                <div class="handle ne" data-handle="ne" @mousedown.stop="onResizeStart($event, z, 'ne')"></div>
                <div class="handle e" data-handle="e" @mousedown.stop="onResizeStart($event, z, 'e')"></div>
                <div class="handle se" data-handle="se" @mousedown.stop="onResizeStart($event, z, 'se')"></div>
                <div class="handle s" data-handle="s" @mousedown.stop="onResizeStart($event, z, 's')"></div>
                <div class="handle sw" data-handle="sw" @mousedown.stop="onResizeStart($event, z, 'sw')"></div>
                <div class="handle w" data-handle="w" @mousedown.stop="onResizeStart($event, z, 'w')"></div>
              </template>
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
              <el-form-item label="布局">
                <el-select v-model="selectedComp.layoutMode" size="small" @change="onLayoutModeChange(selectedComp)">
                  <el-option label="普通网格" value="grid" />
                  <el-option label="桌椅格局" value="table" />
                  <el-option label="圆弧排布" value="arc" />
                  <el-option label="椭圆弧排布" value="ellipse" />
                  <el-option label="螺旋排布" value="spiral" />
                  <el-option label="S形正弦波" value="sine" />
                  <el-option label="斜线排布" value="slant" />
                  <el-option label="自定义曲线" value="curve" />
                </el-select>
              </el-form-item>
              <template v-if="(selectedComp.layoutMode || 'grid') === 'grid'">
                <el-form-item label="行数"><el-input-number v-model="selectedComp.gridRows" :min="1" :max="40" /></el-form-item>
                <el-form-item label="列数"><el-input-number v-model="selectedComp.gridCols" :min="1" :max="40" /></el-form-item>
              </template>
              <template v-else-if="selectedComp.layoutMode === 'table'">
                <el-form-item label="格局">
                  <span class="dim-label">{{ selectedComp.tablesX }}×{{ selectedComp.tablesY }} 桌 · 每桌 {{ selectedComp.tableSeatCols }}×{{ selectedComp.tableSeatRows }} 座</span>
                </el-form-item>
                <el-form-item label="每桌列"><el-input-number v-model="selectedComp.tableSeatCols" :min="1" :max="6" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="每桌行"><el-input-number v-model="selectedComp.tableSeatRows" :min="1" :max="6" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="横向桌数"><el-input-number v-model="selectedComp.tablesX" :min="1" :max="12" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="纵向桌数"><el-input-number v-model="selectedComp.tablesY" :min="1" :max="12" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="过道横"><el-input-number v-model="selectedComp.tableGapX" :min="0" :max="4" :step="0.5" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="过道纵"><el-input-number v-model="selectedComp.tableGapY" :min="0" :max="4" :step="0.5" @change="regenParametric(selectedComp)" /></el-form-item>
              </template>
              <template v-else-if="selectedComp.layoutMode === 'arc' || selectedComp.layoutMode === 'ellipse'">
                <el-form-item label="圈数"><el-input-number v-model="selectedComp.arcRows" :min="1" :max="10" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="每圈座数"><el-input-number v-model="selectedComp.arcSeatsPerRow" :min="2" :max="30" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item v-if="selectedComp.layoutMode === 'ellipse'" label="长半轴"><el-input-number v-model="selectedComp.arcRadius" :min="2" :max="40" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item v-else label="内圈半径"><el-input-number v-model="selectedComp.arcRadius" :min="2" :max="40" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item v-if="selectedComp.layoutMode === 'ellipse'" label="短半轴"><el-input-number v-model="selectedComp.arcAxisB" :min="2" :max="40" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="圈距"><el-input-number v-model="selectedComp.arcRadiusStep" :min="0.5" :max="5" :step="0.5" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="起始角"><el-slider v-model="selectedComp.arcStartAngle" :min="0" :max="360" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="结束角"><el-slider v-model="selectedComp.arcEndAngle" :min="0" :max="360" @change="regenParametric(selectedComp)" /></el-form-item>
              </template>
              <template v-else-if="selectedComp.layoutMode === 'spiral'">
                <el-form-item label="圈数"><el-input-number v-model="selectedComp.arcRows" :min="1" :max="10" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="每圈座数"><el-input-number v-model="selectedComp.arcSeatsPerRow" :min="2" :max="30" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="起始半径"><el-input-number v-model="selectedComp.arcRadius" :min="1" :max="40" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="螺距"><el-input-number v-model="selectedComp.arcRadiusStep" :min="0.5" :max="5" :step="0.5" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="起始角"><el-input-number v-model="selectedComp.arcStartAngle" :min="-720" :max="720" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="旋转角"><el-input-number v-model="selectedComp.arcEndAngle" :min="-1440" :max="1440" @change="regenParametric(selectedComp)" /></el-form-item>
              </template>
              <template v-else-if="selectedComp.layoutMode === 'sine'">
                <el-form-item label="行数"><el-input-number v-model="selectedComp.arcRows" :min="1" :max="10" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="每行座数"><el-input-number v-model="selectedComp.arcSeatsPerRow" :min="2" :max="30" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="振幅"><el-input-number v-model="selectedComp.curveAmplitude" :min="0.5" :max="10" :step="0.5" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="波长"><el-input-number v-model="selectedComp.curveWavelength" :min="2" :max="40" :step="0.5" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="相位"><el-input-number v-model="selectedComp.curvePhase" :min="0" :max="360" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="行距"><el-input-number v-model="selectedComp.curveRowGap" :min="0.5" :max="10" :step="0.5" @change="regenParametric(selectedComp)" /></el-form-item>
              </template>
              <template v-else-if="selectedComp.layoutMode === 'slant'">
                <el-form-item label="行数"><el-input-number v-model="selectedComp.arcRows" :min="1" :max="20" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="每行座数"><el-input-number v-model="selectedComp.arcSeatsPerRow" :min="2" :max="30" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="倾角"><el-input-number v-model="selectedComp.curveAngle" :min="-90" :max="90" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="行距"><el-input-number v-model="selectedComp.curveSlantGap" :min="0.5" :max="10" :step="0.5" @change="regenParametric(selectedComp)" /></el-form-item>
              </template>
              <template v-else-if="selectedComp.layoutMode === 'curve'">
                <el-form-item label="行数"><el-input-number v-model="selectedComp.arcRows" :min="1" :max="20" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="每行座数"><el-input-number v-model="selectedComp.arcSeatsPerRow" :min="2" :max="60" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="行距"><el-input-number v-model="selectedComp.curveRowGap" :min="0.5" :max="10" :step="0.5" @change="regenParametric(selectedComp)" /></el-form-item>
                <el-form-item label="操作">
                  <el-button size="small" type="primary" @click="resetCurveAnchors(selectedComp)">重新生成锚点</el-button>
                </el-form-item>
                <el-form-item label="提示">
                  <span class="dim-label">拖动画布中的点即可直接移动对应座位</span>
                </el-form-item>
              </template>
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
          <span class="hint">点击网格/空白处添加座位 · 点击已占位编辑/删除 · 编号前不带区域前缀则自动补齐</span>
        </div>
        <!-- 参数化布局（桌椅/弧形）：按坐标绝对定位 -->
        <div
          v-if="isParametric(editingZone)"
          class="seat-editor-abs"
          :style="{ width: (editingZone.gridCols || 1) * DESIGN_CELL + 'px', height: (editingZone.gridRows || 1) * DESIGN_CELL + 'px' }"
          @click="onAbsEditorClick($event, editingZone)"
        >
          <template v-if="editingZone.layoutMode === 'table'">
            <div v-for="(t, ti) in zoneTables(editingZone)" :key="'et' + ti" class="table-rect" :style="tableRectStyle(editingZone, t)"></div>
          </template>
          <template v-else-if="editingZone.layoutMode === 'arc' || editingZone.layoutMode === 'ellipse'">
            <div v-for="(ring, ri) in zoneArcRings(editingZone)" :key="'er' + ri" class="arc-ring" :style="arcRingStyle(editingZone, ring)"></div>
          </template>
          <template v-else-if="editingZone.layoutMode === 'spiral'">
            <div v-for="(ring, ri) in zoneSpiralRings(editingZone)" :key="'er' + ri" class="arc-ring" :style="arcRingStyle(editingZone, ring)"></div>
          </template>
          <template v-else-if="editingZone.layoutMode === 'sine'">
            <div v-for="(w, wi) in zoneSineGuide(editingZone)" :key="'ew' + wi" class="sine-guide" :style="sineGuideStyle(editingZone, w)"></div>
          </template>
          <template v-else-if="editingZone.layoutMode === 'slant'">
            <div v-for="(s, si) in zoneSlantGuide(editingZone)" :key="'es' + si" class="slant-guide" :style="slantGuideStyle(editingZone, s)"></div>
          </template>
          <template v-else-if="editingZone.layoutMode === 'curve'">
            <svg v-if="zoneCurvePath(editingZone)" class="curve-svg" :viewBox="curveSvgViewBox(editingZone)">
              <path :d="zoneCurvePath(editingZone)" fill="none" stroke="#b9cfc9" stroke-width="2" stroke-dasharray="5,4" />
            </svg>
          </template>
          <div
            v-for="item in zoneAbsSeats(editingZone)"
            :key="item.s._key"
            class="abs-seat editable"
            :style="absSeatStyle(editingZone, item)"
            @click.stop="editSeat(item.s)"
          >
            <span class="seat-label">{{ shortCode(item.s.code) }}</span>
          </div>
        </div>
        <!-- 网格布局 -->
        <div v-else class="seat-editor-grid" :style="zoneGridStyle(editingZone)">
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
const ZONE_LABEL_H = 18 // 区块顶部标签条高度 px（与小程序一致，不挤占座位区）
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
let interaction = null // { type: 'move'|'resize'|'curve-point', item, kind, handle, startX, startY, orig }

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
        // 布局模式与参数默认值（兼容旧数据）
        z.layoutMode = z.layoutMode || 'grid'
        if (z.tableSeatCols == null) z.tableSeatCols = 2
        if (z.tableSeatRows == null) z.tableSeatRows = 2
        if (z.tableGapX == null) z.tableGapX = 1
        if (z.tableGapY == null) z.tableGapY = 1
        if (z.tablesX == null) z.tablesX = 1
        if (z.tablesY == null) z.tablesY = 1
        if (z.arcRadius == null) z.arcRadius = 8
        if (z.arcRadiusStep == null) z.arcRadiusStep = 1.5
        if (z.arcStartAngle == null) z.arcStartAngle = 180
        if (z.arcEndAngle == null) z.arcEndAngle = 360
        if (z.arcRows == null) z.arcRows = 3
        if (z.arcSeatsPerRow == null) z.arcSeatsPerRow = 8
        if (z.arcAxisB == null) z.arcAxisB = 8
        if (z.curveAmplitude == null) z.curveAmplitude = 2
        if (z.curveWavelength == null) z.curveWavelength = 6
        if (z.curvePhase == null) z.curvePhase = 0
        if (z.curveRowGap == null) z.curveRowGap = 2
        if (z.curveAngle == null) z.curveAngle = 30
        if (z.curveSlantGap == null) z.curveSlantGap = 2
        if (z.pathPoints == null) z.pathPoints = []
        for (const s of z.seats) {
          if (!s._key) s._key = 's' + (seq++)
        }
        // curve 区块：pathPoints 有锚点但座位缺失时自动补齐（兼容历史数据）
        if (z.layoutMode === 'curve' && curvePoints(z).length && !z.seats.length) {
          syncCurveSeatsFromPoints(z)
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
    height: (z.gridRows || 1) * c + ZONE_LABEL_H + 'px'
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
  // 缩放区块结束后：网格模式铺满座位；参数化模式重新按参数生成
  if (interaction && interaction.type === 'resize' && interaction.kind === 'zone') {
    const z = interaction.item
    if (isParametric(z)) regenParametric(z)
    else syncZoneSeats(z)
  }
  // 曲线锚点拖动结束：校准座位与锚点完全一致
  if (interaction && interaction.type === 'curve-point' && interaction.z) {
    syncCurveSeatsFromPoints(interaction.z)
  }
  interaction = null
}

function addComponent(item, x, y) {
  const f = floor()
  if (item.kind === 'zone') {
    const key = 'z' + (seq++)
    f.zones.push({
      _key: key, kind: 'zone', _new: true, id: null, areaId: currentAreaId.value,
      name: '座位区块', offsetX: x, offsetY: y, gridRows: 4, gridCols: 6, seats: [],
      layoutMode: 'grid',
      tableSeatCols: 2, tableSeatRows: 2, tableGapX: 1, tableGapY: 1, tablesX: 2, tablesY: 2,
      arcRadius: 8, arcRadiusStep: 1.5, arcStartAngle: 180, arcEndAngle: 360, arcRows: 3, arcSeatsPerRow: 8,
      arcAxisB: 8, curveAmplitude: 2, curveWavelength: 6, curvePhase: 0, curveRowGap: 2, curveAngle: 30, curveSlantGap: 2, pathPoints: []
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

// ============ 布局几何（桌椅格局 / 弧形排布） ============
function isParametric(z) {
  return z.layoutMode === 'table' || z.layoutMode === 'arc' || z.layoutMode === 'ellipse' ||
    z.layoutMode === 'spiral' || z.layoutMode === 'sine' || z.layoutMode === 'slant' || z.layoutMode === 'curve'
}
const layoutModeLabels = {
  grid: '网格', table: '桌椅', arc: '圆弧', ellipse: '椭圆弧',
  spiral: '螺旋', sine: 'S形', slant: '斜线', curve: '曲线'
}
function layoutModeLabel(mode) {
  return layoutModeLabels[mode] || mode || ''
}

// 桌椅格局几何：桌子按 (tc x tr) 座位 + 桌间过道(gx/gy) 排列成 (tx x ty) 桌
function tableGeometry(z) {
  const tc = Math.max(1, z.tableSeatCols || 2)
  const tr = Math.max(1, z.tableSeatRows || 2)
  const gx = Math.max(0, z.tableGapX ?? 1)
  const gy = Math.max(0, z.tableGapY ?? 1)
  const tx = Math.max(1, z.tablesX || 1)
  const ty = Math.max(1, z.tablesY || 1)
  const tables = []
  const positions = []
  for (let j = 0; j < ty; j++) {
    for (let i = 0; i < tx; i++) {
      const ox = i * (tc + gx)
      const oy = j * (tr + gy)
      tables.push({ x: ox + 0.5, y: oy + 0.5, w: tc, h: tr })
      for (let r = 0; r < tr; r++) {
        for (let c = 0; c < tc; c++) {
          positions.push({ x: ox + c + 0.5, y: oy + r + 0.5 })
        }
      }
    }
  }
  return {
    cols: tx * tc + (tx - 1) * gx,
    rows: ty * tr + (ty - 1) * gy,
    tables,
    positions
  }
}

// 圆弧/椭圆弧几何：座位沿同心弧（圆或椭圆）排列，返回归一化坐标与引导环
// ellipse 时长半轴=ArcRadius、短半轴=ArcAxisB；arc 时为圆（a==b）
function arcGeometry(z) {
  const rows = Math.max(1, z.arcRows || 3)
  const cols = Math.max(1, z.arcSeatsPerRow || 8)
  const a0 = (z.arcStartAngle ?? 180) * Math.PI / 180
  const a1 = (z.arcEndAngle ?? 360) * Math.PI / 180
  const isEllipse = z.layoutMode === 'ellipse'
  const axisA = Math.max(2, z.arcRadius || 8)
  const axisB = isEllipse ? Math.max(2, z.arcAxisB || axisA) : axisA
  const step = Math.max(0.5, z.arcRadiusStep || 1.5)
  const raw = []
  const rings = []
  for (let r = 0; r < rows; r++) {
    const ar = axisA + r * step
    const br = isEllipse ? axisB + r * step : ar
    rings.push({ a: ar, b: br })
    for (let c = 0; c < cols; c++) {
      const ang = cols === 1 ? (a0 + a1) / 2 : a0 + (a1 - a0) * (c / (cols - 1))
      raw.push({ x: ar * Math.cos(ang), y: br * Math.sin(ang), row: r })
    }
  }
  const minX = Math.min(...raw.map(p => p.x))
  const minY = Math.min(...raw.map(p => p.y))
  const maxX = Math.max(...raw.map(p => p.x))
  const maxY = Math.max(...raw.map(p => p.y))
  const pad = 0.5 // 座位自身占位半格
  const points = raw.map(p => ({
    x: +(p.x - minX + pad).toFixed(3),
    y: +(p.y - minY + pad).toFixed(3),
    row: p.row
  }))
  return {
    points,
    cols: Math.ceil(maxX - minX + pad * 2),
    rows: Math.ceil(maxY - minY + pad * 2),
    center: { x: -minX, y: -minY },
    rings
  }
}

// 螺旋几何：阿基米德螺旋，半径随角度线性递增
function spiralGeometry(z) {
  const rows = Math.max(1, z.arcRows || 3) // 圈数
  const cols = Math.max(1, z.arcSeatsPerRow || 8) // 每圈座位数
  const r0 = Math.max(1, z.arcRadius || 8)
  const pitch = Math.max(0.5, z.arcRadiusStep || 1.5) // 螺距（每圈半径增量）
  const a0 = (z.arcStartAngle ?? 0) * Math.PI / 180
  const totalDeg = (z.arcEndAngle ?? 720) === 0 ? 1 : (z.arcEndAngle ?? 720) // 总旋转角度（度，负值=反向旋转）
  const a1 = a0 + totalDeg * Math.PI / 180
  const total = rows * cols
  const raw = []
  for (let i = 0; i < total; i++) {
    const t = total === 1 ? 0.5 : i / (total - 1)
    const ang = a0 + (a1 - a0) * t
    const rad = r0 + Math.abs(ang - a0) / (2 * Math.PI) * pitch
    raw.push({ x: rad * Math.cos(ang), y: rad * Math.sin(ang), row: i })
  }
  const minX = Math.min(...raw.map(p => p.x))
  const minY = Math.min(...raw.map(p => p.y))
  const maxX = Math.max(...raw.map(p => p.x))
  const maxY = Math.max(...raw.map(p => p.y))
  const pad = 0.5
  const points = raw.map(p => ({
    x: +(p.x - minX + pad).toFixed(3),
    y: +(p.y - minY + pad).toFixed(3),
    row: p.row
  }))
  return {
    points,
    cols: Math.ceil(maxX - minX + pad * 2),
    rows: Math.ceil(maxY - minY + pad * 2),
    center: { x: -minX, y: -minY },
    rings: [
      { a: r0, b: r0 },
      { a: r0 + pitch * rows, b: r0 + pitch * rows }
    ]
  }
}

// S形正弦波几何：每行座位沿正弦曲线起伏
function sineGeometry(z) {
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
      raw.push({ x, y, row: r })
    }
  }
  const minX = Math.min(...raw.map(p => p.x))
  const minY = Math.min(...raw.map(p => p.y))
  const maxX = Math.max(...raw.map(p => p.x))
  const maxY = Math.max(...raw.map(p => p.y))
  const pad = 0.5
  const points = raw.map(p => ({
    x: +(p.x - minX + pad).toFixed(3),
    y: +(p.y - minY + pad).toFixed(3),
    row: p.row
  }))
  return {
    points,
    cols: Math.ceil(maxX - minX + pad * 2),
    rows: Math.ceil(maxY - minY + pad * 2),
    // 每行波峰引导线：水平虚线在归一化后的基线 y = row*rowGap + amp
    guides: Array.from({ length: rows }, (_, r) => {
      const y = r * rowGap + amp
      return { y: y - minY + pad, x0: -minX + pad, x1: maxX - minX + pad }
    })
  }
}

// 斜线排布几何：座位沿倾斜直线排列，行间垂直错开
function slantGeometry(z) {
  const rows = Math.max(1, z.arcRows || 3)
  const cols = Math.max(1, z.arcSeatsPerRow || 8)
  const ang = (z.curveAngle || 30) * Math.PI / 180
  const gap = Math.max(0.5, z.curveSlantGap || 2)
  const raw = []
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const x = c * Math.cos(ang) + r * Math.sin(ang)
      const y = c * Math.sin(ang) - r * Math.cos(ang) + r * gap
      raw.push({ x, y, row: r })
    }
  }
  const minX = Math.min(...raw.map(p => p.x))
  const minY = Math.min(...raw.map(p => p.y))
  const maxX = Math.max(...raw.map(p => p.x))
  const maxY = Math.max(...raw.map(p => p.y))
  const pad = 0.5
  const points = raw.map(p => ({
    x: +(p.x - minX + pad).toFixed(3),
    y: +(p.y - minY + pad).toFixed(3),
    row: p.row
  }))
  return {
    points,
    cols: Math.ceil(maxX - minX + pad * 2),
    rows: Math.ceil(maxY - minY + pad * 2)
  }
}

function zoneTables(z) {
  return tableGeometry(z).tables
}
function zoneArcRings(z) {
  const g = arcGeometry(z)
  return g.rings.map((ring, i) => ({ cx: g.center.x, cy: g.center.y, a: ring.a, b: ring.b }))
}
function zoneSpiralRings(z) {
  const g = spiralGeometry(z)
  return g.rings.map((ring, i) => ({ cx: g.center.x, cy: g.center.y, a: ring.a, b: ring.b }))
}
function zoneSineGuide(z) {
  return sineGeometry(z).guides || []
}
function zoneSlantGuide(z) {
  const g = slantGeometry(z)
  const guides = []
  for (let r = 0; r < (z.arcRows || 3); r++) {
    const row = g.points.filter(p => p.row === r)
    if (row.length) {
      guides.push({ x0: row[0].x, y0: row[0].y, x1: row[row.length - 1].x, y1: row[row.length - 1].y })
    }
  }
  return guides
}

// ============ 自定义曲线（curve）：座位锚点模式 ============
// pathPoints 直接存每行座位的坐标（中心语义），数量 = arcRows × arcSeatsPerRow
// 每个点就是一个座位，用户可拖动点直接排布座位（不做样条插值）
function curvePoints(z) {
  if (!z.pathPoints) return []
  try {
    const arr = typeof z.pathPoints === 'string' ? JSON.parse(z.pathPoints) : z.pathPoints
    return (Array.isArray(arr) ? arr : []).filter(p => p && p.x != null && p.y != null)
  } catch (e) {
    return []
  }
}
// 生成初始锚点：每行 arcSeatsPerRow 个，沿水平线排布；多行按行距纵向展开
function generateCurveSeats(z) {
  const rows = Math.max(1, z.arcRows || 1)
  const cols = Math.max(1, z.arcSeatsPerRow || 2)
  const gap = Math.max(0.5, z.curveRowGap || 2)
  const positions = []
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      positions.push({ x: c + 0.5, y: r * gap + 0.5, row: r })
    }
  }
  applyGeneratedSeats(z, positions, cols, Math.ceil((rows - 1) * gap) + 1)
  z.pathPoints = z.seats.map((s, i) => ({ x: Number(s.positionX), y: Number(s.positionY), row: Math.floor(i / cols) }))
}
// 锚点即座位：pathPoints 坐标直接作为座位坐标（拖动后调用）
function syncCurveSeatsFromPoints(z) {
  const pts = curvePoints(z)
  if (!pts.length) return
  const cols = Math.max(1, z.arcSeatsPerRow || 2)
  const region = regionPrefix(z)
  const existing = {}
  for (const s of z.seats) existing[`${s.positionX},${s.positionY}`] = s
  const newSeats = []
  pts.forEach((p, i) => {
    const key = `${p.x},${p.y}`
    let seat = existing[key]
    if (!seat) {
      seat = { _key: 's' + (seq++), _new: true, id: null, type: 'Normal', window: false, powerSocket: false }
    }
    seat.positionX = p.x
    seat.positionY = p.y
    seat.code = `${region}-${String(i + 1).padStart(3, '0')}`
    newSeats.push(seat)
  })
  const stale = z.seats.filter(s => !pts.some(p => p.x === Number(s.positionX) && p.y === Number(s.positionY)))
  for (const s of stale) if (s.id) venueApi.deleteSeat(s.id).catch(() => {})
  z.seats = newSeats
  const maxX = Math.max(...pts.map(p => p.x))
  const maxY = Math.max(...pts.map(p => p.y))
  z.gridCols = Math.ceil(maxX) + 1
  z.gridRows = Math.ceil(maxY) + 1
}
// 锚点列表（供渲染拖动手柄）
function zoneCurveControls(z) {
  return curvePoints(z)
}
// 逐行连线引导：同一行的座位连线，便于看清行方向
function zoneCurvePath(z) {
  const pts = curvePoints(z)
  if (pts.length < 2) return ''
  const c = cellPx()
  const cols = Math.max(1, z.arcSeatsPerRow || pts.length)
  const lines = []
  for (let r = 0; r * cols < pts.length; r++) {
    const rowPts = pts.slice(r * cols, r * cols + cols)
    for (let i = 0; i < rowPts.length - 1; i++) {
      lines.push(`${(rowPts[i].x * c).toFixed(1)},${(rowPts[i].y * c).toFixed(1)} ${(rowPts[i + 1].x * c).toFixed(1)},${(rowPts[i + 1].y * c).toFixed(1)}`)
    }
  }
  return 'M ' + lines.join(' L ')
}
function curveSvgViewBox(z) {
  const c = cellPx()
  return `0 0 ${(z.gridCols || 1) * c} ${(z.gridRows || 1) * c}`
}
function curvePointStyle(z, p) {
  const c = cellPx()
  return {
    left: (p.x - 0.5) * c + 'px',
    top: (p.y - 0.5) * c + 'px'
  }
}
function zoneAbsSeats(z) {
  return (z.seats || []).map(s => ({
    s,
    x: Number(s.positionX) || 0,
    y: Number(s.positionY) || 0
  }))
}
function tableRectStyle(z, t) {
  const c = cellPx()
  return { left: (t.x - 0.5) * c + 'px', top: (t.y - 0.5) * c + 'px', width: t.w * c + 'px', height: t.h * c + 'px' }
}
function arcRingStyle(z, ring) {
  const c = cellPx()
  return {
    left: (ring.cx + 0.5 - ring.a) * c + 'px',
    top: (ring.cy + 0.5 - ring.b) * c + 'px',
    width: ring.a * 2 * c + 'px',
    height: ring.b * 2 * c + 'px'
  }
}
function absSeatStyle(z, item) {
  const c = cellPx()
  return {
    left: (item.x - 0.5) * c + 1 + 'px',
    top: (item.y - 0.5) * c + 1 + 'px',
    width: c - 2 + 'px',
    height: c - 2 + 'px'
  }
}
function sineGuideStyle(z, w) {
  const c = cellPx()
  return {
    left: (w.x0 - 0.5) * c + 'px',
    top: (w.y - 0.5) * c + 'px',
    width: (w.x1 - w.x0) * c + 'px',
    height: '2px'
  }
}
function slantGuideStyle(z, s) {
  const c = cellPx()
  const ang = (z.curveAngle || 30) * Math.PI / 180
  const dx = (s.x1 - s.x0) * c
  const dy = (s.y1 - s.y0) * c
  const len = Math.sqrt(dx * dx + dy * dy)
  const rot = Math.atan2(dy, dx) * 180 / Math.PI
  return {
    left: (s.x0 - 0.5) * c + 'px',
    top: (s.y0 - 0.5) * c + 'px',
    width: len + 'px',
    height: '2px',
    transform: `rotate(${rot}deg)`,
    transformOrigin: 'left center'
  }
}

// 依据布局参数生成座位：保留已有座位对象（含 id），按生成顺序重新编号
function applyGeneratedSeats(z, positions, cols, rows) {
  const region = regionPrefix(z)
  const existing = {}
  for (const s of z.seats) existing[`${s.positionX},${s.positionY}`] = s
  const staleKeys = new Set(Object.keys(existing))
  const newSeats = []
  let n = 0
  for (const p of positions) {
    n++
    const key = `${p.x},${p.y}`
    staleKeys.delete(key)
    let seat = existing[key]
    if (!seat) {
      seat = { _key: 's' + (seq++), _new: true, id: null, type: 'Normal', window: false, powerSocket: false }
    }
    seat.positionX = p.x
    seat.positionY = p.y
    seat.code = `${region}-${String(n).padStart(3, '0')}`
    newSeats.push(seat)
  }
  for (const k of staleKeys) {
    const s = existing[k]
    if (s.id) venueApi.deleteSeat(s.id).catch(() => {})
  }
  z.seats = newSeats
  z.gridCols = cols
  z.gridRows = rows
}

function generateTableSeats(z) {
  const g = tableGeometry(z)
  applyGeneratedSeats(z, g.positions, g.cols, g.rows)
}
function generateArcSeats(z) {
  const g = arcGeometry(z)
  applyGeneratedSeats(z, g.points, g.cols, g.rows)
}

// 属性面板参数变更 / 布局切换时重新生成座位
function regenParametric(z) {
  if (!z || !isParametric(z)) return
  if (z.layoutMode === 'table') {
    generateTableSeats(z)
  } else if (z.layoutMode === 'arc' || z.layoutMode === 'ellipse') {
    generateArcSeats(z)
  } else if (z.layoutMode === 'spiral') {
    const g = spiralGeometry(z)
    applyGeneratedSeats(z, g.points, g.cols, g.rows)
  } else if (z.layoutMode === 'sine') {
    const g = sineGeometry(z)
    applyGeneratedSeats(z, g.points, g.cols, g.rows)
  } else if (z.layoutMode === 'slant') {
    const g = slantGeometry(z)
    applyGeneratedSeats(z, g.points, g.cols, g.rows)
  } else if (z.layoutMode === 'curve') {
    generateCurveSeats(z)
    ElMessage.info('已按每行座位数生成锚点，拖动点即可排布座位')
    return
  }
  ElMessage.info('已按新参数重新生成座位')
}
function onLayoutModeChange(z) {
  if (!z) return
  const mode = z.layoutMode || 'grid'
  if (mode === 'grid') {
    syncZoneSeats(z)
  } else {
    regenParametric(z)
  }
}

// ============ 自定义曲线（curve）：锚点拖动 ============
// 拖动锚点 = 移动对应座位，像素级精准调整
function onCurvePointMousedown(e, z, idx) {
  e.stopPropagation()
  selectedKey.value = z._key
  interaction = {
    type: 'curve-point', z, idx,
    startX: e.clientX, startY: e.clientY,
    origX: curvePoints(z)[idx]?.x ?? 0,
    origY: curvePoints(z)[idx]?.y ?? 0
  }
}
function resetCurveAnchors(z) {
  generateCurveSeats(z)
  ElMessage.info('已按每行座位数重新生成锚点，拖动点即可排布座位')
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

  // 曲线锚点拖动：连续坐标（不取整），像素级精准
  if (interaction.type === 'curve-point') {
    const dx = (e.clientX - interaction.startX) / zoom.value / c
    const dy = (e.clientY - interaction.startY) / zoom.value / c
    const it = interaction.z
    const pts = curvePoints(it)
    const p = pts[interaction.idx]
    if (!p) return
    p.x = Math.max(0.5, interaction.origX + dx)
    p.y = Math.max(0.5, interaction.origY + dy)
    it.pathPoints = pts
    // 同步到对应座位：确保座位数组与锚点一一对应（缺失则按索引补齐）
    while (it.seats.length <= interaction.idx) {
      it.seats.push({ _key: 's' + (seq++), _new: true, id: null, code: genSeatCode(it), type: 'Normal', window: false, powerSocket: false })
    }
    const seat = it.seats[interaction.idx]
    seat.positionX = p.x
    seat.positionY = p.y
    // 扩展区块 bbox
    const maxX = Math.max(...pts.map(q => q.x))
    const maxY = Math.max(...pts.map(q => q.y))
    it.gridCols = Math.max(it.gridCols || 1, Math.ceil(maxX) + 1)
    it.gridRows = Math.max(it.gridRows || 1, Math.ceil(maxY) + 1)
    return
  }

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
  if (!z) return
  const cols = z.gridCols || 1
  const px = idx == null ? 0 : idx % cols
  const py = idx == null ? 0 : Math.floor(idx / cols)
  addSeatAtPos(px, py)
}

function addSeatAtPos(px, py) {
  const z = editingZone.value
  if (!z) return
  let code = newSeatCode.value.trim()
  if (!code) {
    code = genSeatCode(z)
  }
  z.seats.push({ _key: 's' + (seq++), _new: true, id: null, code, positionX: px, positionY: py, type: 'Normal', window: false, powerSocket: false })
  newSeatCode.value = ''
}

// 参数化布局编辑器：点击空白处按坐标加座，点击座位编辑
function onAbsEditorClick(e, z) {
  const rect = e.currentTarget.getBoundingClientRect()
  const c = DESIGN_CELL
  const x = Math.max(0, Math.round((e.clientX - rect.left) / c - 0.5))
  const y = Math.max(0, Math.round((e.clientY - rect.top) / c - 0.5))
  const hit = z.seats.find(s => Math.abs((Number(s.positionX) || 0) - x) < 0.5 && Math.abs((Number(s.positionY) || 0) - y) < 0.5)
  if (hit) {
    editSeat(hit)
  } else {
    addSeatAtPos(x, y)
  }
}

function editSeat(s) {
  seatEditForm.value = { ...s }
  seatEditVisible.value = true
}

function genSeatCode(z) {
  // 自动生成座位编号（基于区块名 + 顺序号）
  const region = regionPrefix(z)
  const n = z.seats.length + 1
  return `${region}-${String(n).padStart(3, '0')}`
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
    // curve 区块：保存前确保座位与锚点一一对应（防止只存了 pathPoints 没存座位）
    for (const z of f.zones) {
      if (z.layoutMode === 'curve') {
        const pts = curvePoints(z)
        if (pts.length && z.seats.length !== pts.length) {
          syncCurveSeatsFromPoints(z)
        }
      }
    }
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
        offsetY: z.offsetY,
        layoutMode: z.layoutMode || 'grid',
        tableSeatCols: z.tableSeatCols ?? 2,
        tableSeatRows: z.tableSeatRows ?? 2,
        tableGapX: z.tableGapX ?? 1,
        tableGapY: z.tableGapY ?? 1,
        tablesX: z.tablesX ?? 1,
        tablesY: z.tablesY ?? 1,
        arcRadius: z.arcRadius ?? 8,
        arcRadiusStep: z.arcRadiusStep ?? 1.5,
        arcStartAngle: z.arcStartAngle ?? 180,
        arcEndAngle: z.arcEndAngle ?? 360,
        arcRows: z.arcRows ?? 3,
        arcSeatsPerRow: z.arcSeatsPerRow ?? 8,
        arcAxisB: z.arcAxisB ?? 8,
        curveAmplitude: z.curveAmplitude ?? 2,
        curveWavelength: z.curveWavelength ?? 6,
        curvePhase: z.curvePhase ?? 0,
        curveRowGap: z.curveRowGap ?? 2,
        curveAngle: z.curveAngle ?? 30,
        curveSlantGap: z.curveSlantGap ?? 2,
        pathPoints: z.pathPoints && z.pathPoints.length ? JSON.stringify(z.pathPoints) : null
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

// 属性面板调整区块行数/列数时：网格模式铺满座位并重新编号
watch(
  () => {
    const z = selectedComp.value
    return z && z.kind === 'zone' ? [z.gridRows, z.gridCols] : null
  },
  (val, old) => {
    if (val && old && (val[0] !== old[0] || val[1] !== old[1])) {
      const z = selectedComp.value
      if (z && !isParametric(z)) syncZoneSeats(z)
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
/* 区块内容容器：网格模式为 grid，参数化模式为绝对定位 */
.zone-body {
  position: relative;
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}
.mode-tag {
  margin-left: 4px;
  font-size: 9px;
  color: #6B7FA8;
  border: 1px solid #6B7FA8;
  border-radius: 3px;
  padding: 0 3px;
}
/* 桌椅格局桌面 */
.table-rect {
  position: absolute;
  background: #EAD9B0;
  border: 1px solid #C9A96A;
  border-radius: 4px;
  box-sizing: border-box;
  pointer-events: none;
}
/* 弧形排布引导圈 */
.arc-ring {
  position: absolute;
  border: 1px dashed #b9cfc9;
  border-radius: 50%;
  box-sizing: border-box;
  pointer-events: none;
}
/* S形正弦波引导线 */
.sine-guide {
  position: absolute;
  background: repeating-linear-gradient(90deg, #b9cfc9 0 4px, transparent 4px 8px);
  opacity: 0.6;
  pointer-events: none;
}
/* 斜线排布引导线 */
.slant-guide {
  position: absolute;
  background: repeating-linear-gradient(90deg, #b9cfc9 0 4px, transparent 4px 8px);
  opacity: 0.6;
  pointer-events: none;
}
/* 自定义曲线引导 */
.curve-svg {
  position: absolute;
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  overflow: visible;
}
.curve-point {
  position: absolute;
  width: 18px;
  height: 18px;
  margin-left: -9px;
  margin-top: -9px;
  background: transparent;
  border: none;
  border-radius: 50%;
  z-index: 5;
  cursor: grab;
  display: flex;
  align-items: center;
  justify-content: center;
}
.curve-point::before {
  content: '';
  width: 10px;
  height: 10px;
  background: #fff;
  border: 2px solid #3A8A7E;
  border-radius: 50%;
  box-sizing: border-box;
}
.curve-point:active {
  cursor: grabbing;
}
.curve-point-idx {
  position: absolute;
  top: -12px;
  font-size: 9px;
  color: #3A8A7E;
  background: rgba(255, 255, 255, 0.85);
  border-radius: 3px;
  padding: 0 3px;
  line-height: 14px;
  white-space: nowrap;
  pointer-events: none;
}
/* 弹窗内只读锚点：纯展示不挡座位点击 */
.curve-point-ro {
  width: 8px;
  height: 8px;
  margin-left: -4px;
  margin-top: -4px;
  pointer-events: none;
  cursor: default;
}
.curve-point-ro::before {
  width: 8px;
  height: 8px;
  border-width: 1px;
}
/* 参数化布局座位（绝对定位） */
.abs-seat {
  position: absolute;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #3A8A7E;
  color: #fff;
  border-radius: 3px;
  font-size: 8px;
  box-sizing: border-box;
  z-index: 2;
}
.seat-editor-abs {
  position: relative;
  background: #faf9f4;
  border-radius: 8px;
  box-sizing: border-box;
}
.seat-editor-abs .abs-seat.editable {
  cursor: pointer;
  border: 1px solid #2c6f64;
}
.seat-editor-abs .abs-seat.editable:hover {
  background: #d9822b;
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
  height: 18px;
  line-height: 18px;
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
