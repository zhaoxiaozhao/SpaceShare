<template>
  <div>
    <div class="toolbar">
      <el-button type="primary" @click="openCity">新增城市</el-button>
      <el-button type="primary" @click="openVenue">新增场馆</el-button>
    </div>

    <el-table :data="venues" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="name" label="场馆名称" />
      <el-table-column prop="type" label="类型" width="120" />
      <el-table-column prop="address" label="地址" />
      <el-table-column prop="openingTime" label="开放时间" width="110" />
      <el-table-column prop="closingTime" label="关闭时间" width="110" />
      <el-table-column label="操作" width="120" fixed="right">
        <template #default="{ row }">
          <el-button size="small" type="primary" @click="openMap(row)">座位排布</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 新增城市 -->
    <el-dialog v-model="cityVisible" title="新增城市" width="420px">
      <el-form label-width="80px">
        <el-form-item label="城市名"><el-input v-model="cityForm.name" /></el-form-item>
        <el-form-item label="省份"><el-input v-model="cityForm.province" /></el-form-item>
        <el-form-item label="经度"><el-input-number v-model="cityForm.longitude" :precision="6" /></el-form-item>
        <el-form-item label="纬度"><el-input-number v-model="cityForm.latitude" :precision="6" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="cityVisible = false">取消</el-button>
        <el-button type="primary" @click="createCity">确定</el-button>
      </template>
    </el-dialog>

    <!-- 新增场馆 -->
    <el-dialog v-model="venueVisible" title="新增场馆" width="480px">
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
      </el-form>
      <template #footer>
        <el-button @click="venueVisible = false">取消</el-button>
        <el-button type="primary" @click="createVenue">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { venueApi } from '../api'

const router = useRouter()

const cities = ref([])
const venues = ref([])
const cityVisible = ref(false)
const venueVisible = ref(false)
const cityForm = ref({ name: '', province: '', longitude: null, latitude: null })
const venueForm = ref({ cityId: null, name: '', type: 'Library', address: '', longitude: null, latitude: null, openingTime: '09:00', closingTime: '22:00' })

onMounted(load)

async function load() {
  try {
    const [c, v] = await Promise.all([venueApi.cities(), venueApi.venues()])
    cities.value = c
    venues.value = v
  } catch (e) {}
}

function openCity() { cityVisible.value = true }
function openVenue() { venueVisible.value = true }

async function createCity() {
  await venueApi.createCity(cityForm.value)
  ElMessage.success('创建成功')
  cityVisible.value = false
  cityForm.value = { name: '', province: '', longitude: null, latitude: null }
  load()
}

async function createVenue() {
  await venueApi.createVenue(venueForm.value)
  ElMessage.success('创建成功')
  venueVisible.value = false
  venueForm.value = { cityId: null, name: '', type: 'Library', address: '', longitude: null, latitude: null, openingTime: '09:00', closingTime: '22:00' }
  load()
}

function openMap(row) {
  router.push({ path: '/venue-map', query: { id: row.id } })
}
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
  display: flex;
  gap: 10px;
}
</style>
