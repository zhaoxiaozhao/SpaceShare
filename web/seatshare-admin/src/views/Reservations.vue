<template>
  <div>
    <div class="toolbar">
      <el-select v-model="status" placeholder="状态筛选" clearable style="width: 180px" @change="load">
        <el-option v-for="s in statuses" :key="s" :label="statusText(s)" :value="s" />
      </el-select>
    </div>

    <el-table :data="list" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="seatCode" label="座位" width="120" />
      <el-table-column prop="userId" label="用户ID" width="90" />
      <el-table-column label="开始时间" width="160">
        <template #default="{ row }">{{ formatTime(row.startAt) }}</template>
      </el-table-column>
      <el-table-column label="结束时间" width="160">
        <template #default="{ row }">{{ formatTime(row.endAt) }}</template>
      </el-table-column>
      <el-table-column label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="tagType(row.status)">{{ statusText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="120" fixed="right">
        <template #default="{ row }">
          <el-button size="small" type="danger" @click="forceCancel(row)">强制取消</el-button>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { reportApi } from '../api'

const list = ref([])
const status = ref('')
const statuses = ['Reserved', 'Arrived', 'Using', 'Completed', 'Cancelled', 'NoShow', 'Expired']

onMounted(load)

async function load() {
  try {
    list.value = await reportApi.reservations(status.value)
  } catch (e) {}
}

function statusText(s) {
  const map = {
    Reserved: '待到达', Arrived: '已到座', Using: '使用中', Completed: '已完成',
    Cancelled: '已取消', NoShow: '爽约', Expired: '已过期'
  }
  return map[s] || s
}

function tagType(s) {
  const map = { Reserved: 'warning', Arrived: 'success', Using: 'primary', Completed: 'info', Cancelled: 'info', NoShow: 'danger', Expired: 'info' }
  return map[s] || 'info'
}

function formatTime(iso) {
  if (!iso) return '-'
  return new Date(iso).toLocaleString('zh-CN', { hour12: false })
}

async function forceCancel(row) {
  try {
    const { value } = await ElMessageBox.prompt(`强制取消预约 #${row.id}？请输入原因`, '强制取消', { type: 'warning' })
    await reportApi.forceCancel(row.id, value)
    ElMessage.success('已强制取消')
    load()
  } catch (e) {}
}
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
}
</style>
