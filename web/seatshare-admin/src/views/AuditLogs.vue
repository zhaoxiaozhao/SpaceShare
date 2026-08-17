<template>
  <el-table :data="list" border stripe>
    <el-table-column prop="id" label="ID" width="70" />
    <el-table-column prop="adminUserId" label="管理员ID" width="100" />
    <el-table-column prop="action" label="操作" width="200" />
    <el-table-column prop="entityType" label="对象类型" width="140" />
    <el-table-column prop="entityId" label="对象ID" width="100" />
    <el-table-column prop="detail" label="详情" min-width="220" show-overflow-tooltip />
    <el-table-column prop="ipAddress" label="IP" width="130" />
    <el-table-column label="时间" width="170">
      <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
    </el-table-column>
  </el-table>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { reportApi } from '../api'

const list = ref([])

onMounted(load)

async function load() {
  try {
    list.value = await reportApi.auditLogs()
  } catch (e) {}
}

function formatTime(iso) {
  if (!iso) return '-'
  return new Date(iso).toLocaleString('zh-CN', { hour12: false })
}
</script>
