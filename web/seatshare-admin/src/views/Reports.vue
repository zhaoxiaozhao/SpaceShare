<template>
  <div>
    <div class="toolbar">
      <el-select v-model="status" placeholder="状态筛选" clearable style="width: 180px" @change="load">
        <el-option v-for="s in statuses" :key="s" :label="statusText(s)" :value="s" />
      </el-select>
    </div>

    <el-table :data="list" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="targetType" label="对象" width="100" />
      <el-table-column prop="targetId" label="对象ID" width="90" />
      <el-table-column label="举报人" width="120">
        <template #default="{ row }">{{ row.reporterNickname || ('#' + row.reporterUserId) }}</template>
      </el-table-column>
      <el-table-column label="被举报人" width="130">
        <template #default="{ row }">
          <span v-if="row.targetUserNickname || row.targetUserId">{{ row.targetUserNickname || ('#' + row.targetUserId) }}</span>
          <span v-else class="muted">未指定</span>
        </template>
      </el-table-column>
      <el-table-column prop="reason" label="原因" width="140" />
      <el-table-column prop="description" label="描述" min-width="160" show-overflow-tooltip />
      <el-table-column label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="row.status === 'Pending' ? 'warning' : 'info'">{{ statusText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="180" fixed="right">
        <template #default="{ row }">
          <el-dropdown @command="(cmd) => handle(row, cmd)">
            <el-button size="small" type="primary">处理 ▾</el-button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="Ignored">忽略</el-dropdown-item>
                <el-dropdown-item command="Warned">警告</el-dropdown-item>
                <el-dropdown-item command="CreditDeducted">扣信用</el-dropdown-item>
                <el-dropdown-item command="ReservationCancelled">取消预约</el-dropdown-item>
                <el-dropdown-item command="AccountRestricted">限制账号</el-dropdown-item>
                <el-dropdown-item command="Banned">封禁</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
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
const statuses = ['Pending', 'Ignored', 'Warned', 'CreditDeducted', 'ReservationCancelled', 'AccountRestricted', 'Banned']

onMounted(load)

async function load() {
  try {
    list.value = await reportApi.reports(status.value)
  } catch (e) {}
}

function statusText(s) {
  const map = {
    Pending: '待处理', Ignored: '已忽略', Warned: '已警告', CreditDeducted: '已扣信用',
    ReservationCancelled: '已取消预约', AccountRestricted: '已限制账号', Banned: '已封禁', Resolved: '已解决'
  }
  return map[s] || s
}

async function handle(row, cmd) {
  try {
    const note = await ElMessageBox.prompt('处理备注（选填）', '处理举报', { inputPlaceholder: '备注', inputValue: '' })
    await reportApi.handle(row.id, cmd, note.value || '')
    ElMessage.success('处理成功')
    load()
  } catch (e) {}
}
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
}
.muted {
  color: #bbb;
}
</style>
