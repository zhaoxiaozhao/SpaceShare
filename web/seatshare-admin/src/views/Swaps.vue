<template>
  <div>
    <div class="toolbar">
      <el-select v-model="status" placeholder="状态筛选" clearable style="width: 180px" @change="load">
        <el-option v-for="s in statuses" :key="s" :label="statusText(s)" :value="s" />
      </el-select>
    </div>

    <el-table :data="list" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column label="发布人" width="130">
        <template #default="{ row }">{{ row.userNickname || ('#' + row.userId) }}</template>
      </el-table-column>
      <el-table-column prop="venueName" label="场馆" width="150" />
      <el-table-column label="当前位置" min-width="160">
        <template #default="{ row }">{{ locText(row) }}</template>
      </el-table-column>
      <el-table-column label="期望位置" min-width="160">
        <template #default="{ row }">{{ wantText(row) }}</template>
      </el-table-column>
      <el-table-column label="原因" min-width="160">
        <template #default="{ row }">
          <el-tag v-for="r in row.reasons" :key="r" size="small" style="margin-right: 4px">{{ reasonLabel(r) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="row.status === 'Open' ? 'success' : 'info'">{{ statusText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="响应数" width="80">
        <template #default="{ row }">{{ (row.responses || []).length }}</template>
      </el-table-column>
      <el-table-column label="操作" width="110" fixed="right">
        <template #default="{ row }">
          <el-button v-if="row.status === 'Open'" size="small" type="danger" @click="takeDown(row)">下架</el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { swapApi } from '../api'

const list = ref([])
const status = ref('')
const statuses = ['Open', 'Matched', 'Cancelled', 'Expired']
const reasons = {
  light: '光线问题', cold: '位置偏冷', hot: '位置偏热', noise: '附近有人交谈',
  together: '想与同伴相邻', window: '想靠窗', socket: '需要插座', other: '其他'
}

onMounted(load)

async function load() {
  try {
    list.value = await swapApi.list(status.value)
  } catch (e) {}
}

function statusText(s) {
  const map = { Open: '进行中', Matched: '已匹配', Cancelled: '已取消', Expired: '已过期' }
  return map[s] || s
}

function reasonLabel(code) {
  return reasons[code] || code
}

function locText(row) {
  const parts = [row.floorName, row.areaName, row.zoneName].filter(Boolean)
  return parts.length ? parts.join(' / ') : '未填写'
}

function wantText(row) {
  const parts = [row.wantFloorName || '不限楼层', row.wantAreaName, row.wantZoneName].filter(Boolean)
  return parts.join(' / ')
}

async function takeDown(row) {
  try {
    await ElMessageBox.confirm('确定下架这条换座意向吗？', '下架', { type: 'warning' })
    await swapApi.takeDown(row.id)
    ElMessage.success('已下架')
    load()
  } catch (e) {}
}
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
}
</style>
