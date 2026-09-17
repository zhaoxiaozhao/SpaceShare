<template>
  <div>
    <div class="toolbar">
      <el-select v-model="status" placeholder="状态筛选" clearable style="width: 200px" @change="load">
        <el-option label="正常展示" value="Visible" />
        <el-option label="已隐藏（待审核）" value="Hidden" />
      </el-select>
      <span class="tip">被举报的留言会自动隐藏，通过后恢复展示，驳回则删除</span>
    </div>

    <el-table :data="list" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="activityTitle" label="活动" min-width="180" show-overflow-tooltip />
      <el-table-column prop="content" label="留言" min-width="240" show-overflow-tooltip />
      <el-table-column label="作者" width="140">
        <template #default="{ row }">{{ row.ownerName || ('#' + row.activityId) }}</template>
      </el-table-column>
      <el-table-column label="状态" width="130">
        <template #default="{ row }">
          <el-tag :type="row.status === 'Visible' ? 'success' : 'danger'">{{ statusText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="时间" width="180">
        <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
      </el-table-column>
      <el-table-column label="操作" width="170" fixed="right">
        <template #default="{ row }">
          <template v-if="row.status === 'Hidden'">
            <el-button size="small" type="success" @click="review(row, true)">通过</el-button>
            <el-button size="small" type="danger" @click="review(row, false)">驳回</el-button>
          </template>
          <span v-else class="muted">—</span>
        </template>
      </el-table-column>
    </el-table>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { activityCommentApi } from '../api'

const list = ref([])
const status = ref('')

onMounted(load)

async function load() {
  try {
    list.value = await activityCommentApi.list(status.value)
  } catch (e) {}
}

function statusText(s) {
  return s === 'Hidden' ? '已隐藏' : '正常'
}

function formatTime(s) {
  if (!s) return ''
  const d = new Date(s)
  if (isNaN(d.getTime())) return ''
  const p = (x) => (x < 10 ? '0' + x : x)
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}`
}

async function review(row, approve) {
  try {
    if (!approve) {
      await ElMessageBox.confirm('驳回将删除该留言，确定吗？', '驳回', { type: 'warning' })
    }
    await activityCommentApi.review(row.id, approve)
    ElMessage.success(approve ? '已通过，恢复展示' : '已驳回并删除')
    load()
  } catch (e) {}
}
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
  display: flex;
  align-items: center;
  gap: 12px;
}
.tip {
  color: #909399;
  font-size: 13px;
}
.muted {
  color: #c0c4cc;
}
</style>
