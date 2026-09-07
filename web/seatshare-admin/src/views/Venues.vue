<template>
  <div>
    <div class="toolbar">
      <el-button type="primary" @click="openCity()">新增城市</el-button>
      <el-button v-if="cityForm.id" type="success" @click="openCity(undefined)">编辑城市</el-button>
      <el-divider direction="vertical" />
      <el-button type="primary" @click="openVenue()">新增场馆</el-button>
    </div>

    <el-table :data="venues" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="name" label="场馆名称" />
      <el-table-column prop="type" label="类型" width="110">
        <template #default="{ row }">{{ typeLabel(row.type) }}</template>
      </el-table-column>
      <el-table-column prop="address" label="地址" show-overflow-tooltip />
      <el-table-column prop="openingTime" label="开放" width="90" />
      <el-table-column prop="closingTime" label="关闭" width="90" />
      <el-table-column label="状态" width="90">
        <template #default="{ row }">
          <el-switch
            :model-value="row.status === 'Active'"
            active-text="显示"
            inactive-text="隐藏"
            @change="(v) => onToggleStatus(row, v)"
          />
        </template>
      </el-table-column>
      <el-table-column label="操作" width="320" fixed="right">
        <template #default="{ row }">
          <el-button size="small" @click="openVenue(row)">编辑</el-button>
          <el-button size="small" @click="openFloor(row)">楼层</el-button>
          <el-button size="small" type="primary" @click="openMap(row)">座位排布</el-button>
          <el-button size="small" type="danger" plain @click="removeVenue(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 新增/编辑城市 -->
    <el-dialog v-model="cityVisible" :title="cityForm.id ? '编辑城市' : '新增城市'" width="420px">
      <el-form label-width="80px">
        <el-form-item label="城市名"><el-input v-model="cityForm.name" /></el-form-item>
        <el-form-item label="省份"><el-input v-model="cityForm.province" /></el-form-item>
        <el-form-item label="经度"><el-input-number v-model="cityForm.longitude" :precision="6" /></el-form-item>
        <el-form-item label="纬度"><el-input-number v-model="cityForm.latitude" :precision="6" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="cityVisible = false">取消</el-button>
        <el-button type="primary" @click="saveCity">确定</el-button>
      </template>
    </el-dialog>

    <!-- 新增/编辑场馆 -->
    <el-dialog v-model="venueVisible" :title="venueForm.id ? '编辑场馆' : '新增场馆'" width="480px">
      <el-form label-width="80px">
        <el-form-item label="城市">
          <el-select v-model="venueForm.cityId">
            <el-option v-for="c in cities" :key="c.id" :label="c.name" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="名称"><el-input v-model="venueForm.name" /></el-form-item>
        <el-form-item label="类型">
          <el-select v-model="venueForm.type">
            <el-option label="公共图书馆" value="Library" />
            <el-option label="高校图书馆" value="UniversityLibrary" />
            <el-option label="阅览室" value="ReadingRoom" />
            <el-option label="自习空间" value="StudySpace" />
            <el-option label="共享办公" value="Coworking" />
          </el-select>
        </el-form-item>
        <el-form-item label="地址"><el-input v-model="venueForm.address" /></el-form-item>
        <el-form-item label="经度"><el-input-number v-model="venueForm.longitude" :precision="6" /></el-form-item>
        <el-form-item label="纬度"><el-input-number v-model="venueForm.latitude" :precision="6" /></el-form-item>
        <el-form-item label="开放"><el-time-select v-model="venueForm.openingTime" start="00:00" end="23:30" step="00:30" /></el-form-item>
        <el-form-item label="关闭"><el-time-select v-model="venueForm.closingTime" start="00:00" end="23:30" step="00:30" /></el-form-item>
        <el-form-item label="描述"><el-input v-model="venueForm.description" type="textarea" :rows="3" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="venueVisible = false">取消</el-button>
        <el-button type="primary" @click="saveVenue">确定</el-button>
      </template>
    </el-dialog>

    <!-- 楼层管理 -->
    <el-dialog v-model="floorVisible" :title="'楼层管理 · ' + floorVenueName" width="520px">
      <div class="floor-toolbar">
        <el-input v-model="newFloorName" placeholder="如 6F / 夹层 / 自习区" style="width: 180px" size="small" />
        <el-button size="small" type="primary" @click="addFloor">添加楼层</el-button>
      </div>
      <el-table :data="floors" border stripe size="small">
        <el-table-column prop="name" label="楼层名称" />
        <el-table-column prop="sortOrder" label="排序" width="70" />
        <el-table-column prop="areas.length" label="区域数" width="80" />
        <el-table-column label="操作" width="160">
          <template #default="{ row }">
            <el-button size="small" @click="renameFloor(row)">重命名</el-button>
            <el-button size="small" type="danger" plain @click="removeFloor(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { venueApi } from '../api'

const router = useRouter()

const cities = ref([])
const venues = ref([])
const cityVisible = ref(false)
const venueVisible = ref(false)
const cityForm = ref({ id: null, name: '', province: '', longitude: null, latitude: null })
const venueForm = ref({ id: null, cityId: null, name: '', type: 'Library', address: '', longitude: null, latitude: null, openingTime: '09:00', closingTime: '22:00', description: '' })

const floorVisible = ref(false)
const floorVenueId = ref(null)
const floorVenueName = ref('')
const floors = ref([])
const newFloorName = ref('')

const typeMap = { Library: '公共图书馆', UniversityLibrary: '高校图书馆', ReadingRoom: '阅览室', StudySpace: '自习空间', Coworking: '共享办公' }
function typeLabel(t) { return typeMap[t] || t }

onMounted(load)

async function load() {
  try {
    const [c, v] = await Promise.all([venueApi.cities(), venueApi.venues()])
    cities.value = c
    venues.value = v
  } catch (e) {}
}

function openCity(c) {
  cityForm.value = c ? { ...c } : { id: null, name: '', province: '', longitude: null, latitude: null }
  cityVisible.value = true
}

async function saveCity() {
  if (!cityForm.value.name || !cityForm.value.name.trim()) return ElMessage.warning('请输入城市名')
  try {
    if (cityForm.value.id) {
      await venueApi.updateCity(cityForm.value.id, cityForm.value)
      ElMessage.success('已保存')
    } else {
      await venueApi.createCity(cityForm.value)
      ElMessage.success('创建成功')
    }
    cityVisible.value = false
    load()
  } catch (e) {}
}

async function openVenue(v) {
  if (v) {
    let detail = v
    try { detail = await venueApi.venueDetail(v.id) } catch (e) {}
    venueForm.value = {
      id: v.id, cityId: detail.cityId || null, name: v.name, type: v.type,
      address: v.address, longitude: v.longitude, latitude: v.latitude,
      openingTime: v.openingTime, closingTime: v.closingTime,
      description: detail.description || ''
    }
  } else {
    venueForm.value = { id: null, cityId: null, name: '', type: 'Library', address: '', longitude: null, latitude: null, openingTime: '09:00', closingTime: '22:00', description: '' }
  }
  venueVisible.value = true
}

async function saveVenue() {
  if (!venueForm.value.name || !venueForm.value.name.trim()) return ElMessage.warning('请输入场馆名称')
  const body = venueForm.value
  try {
    if (body.id) {
      await venueApi.updateVenue(body.id, body)
      ElMessage.success('已保存')
    } else {
      await venueApi.createVenue(body)
      ElMessage.success('创建成功')
    }
    venueVisible.value = false
    load()
  } catch (e) {}
}

async function onToggleStatus(row, visible) {
  try {
    await venueApi.setVenueStatus(row.id, visible)
    row.status = visible ? 'Active' : 'Disabled'
    ElMessage.success(visible ? '已显示' : '已隐藏')
  } catch (e) {
    load()
  }
}

async function removeVenue(row) {
  try {
    await ElMessageBox.confirm(`删除场馆「${row.name}」？其下楼层/区域/座位将一并删除，且不可恢复。`, '确认删除', { type: 'warning' })
    await venueApi.deleteVenue(row.id)
    ElMessage.success('已删除')
    load()
  } catch (e) { /* 取消或后端拒绝 */ }
}

async function openFloor(row) {
  floorVenueId.value = row.id
  floorVenueName.value = row.name
  newFloorName.value = ''
  await loadFloors()
  floorVisible.value = true
}

async function loadFloors() {
  if (!floorVenueId.value) return
  try {
    const d = await venueApi.venueDetail(floorVenueId.value)
    floors.value = d.floors || []
  } catch (e) {}
}

async function addFloor() {
  const name = newFloorName.value.trim()
  if (!name) return ElMessage.warning('请输入楼层名称')
  try {
    await venueApi.addFloor({ venueId: floorVenueId.value, name, sortOrder: floors.value.length + 1 })
    ElMessage.success('已添加')
    newFloorName.value = ''
    await loadFloors()
  } catch (e) {}
}

async function renameFloor(row) {
  try {
    const { value } = await ElMessageBox.prompt('输入新楼层名称', '重命名楼层', { inputValue: row.name })
    if (!value || !value.trim()) return
    await venueApi.updateFloor(row.id, { venueId: floorVenueId.value, name: value.trim(), sortOrder: row.sortOrder })
    ElMessage.success('已重命名')
    await loadFloors()
  } catch (e) {}
}

async function removeFloor(row) {
  try {
    await ElMessageBox.confirm(`删除楼层「${row.name}」？若其下已有座位将无法删除。`, '确认删除', { type: 'warning' })
    await venueApi.deleteFloor(row.id)
    ElMessage.success('已删除')
    await loadFloors()
  } catch (e) { /* 取消或后端拒绝 */ }
}

function openMap(row) {
  router.push({ path: '/venue-map', query: { id: row.id } })
}
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  gap: 10px;
}
.floor-toolbar {
  margin-bottom: 12px;
  display: flex;
  gap: 8px;
}
</style>