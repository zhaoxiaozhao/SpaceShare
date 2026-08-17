<template>
  <div>
    <div class="toolbar">
      <el-input v-model="keyword" placeholder="搜索昵称" style="width: 240px" clearable @keyup.enter="load" />
      <el-button type="primary" @click="load">搜索</el-button>
    </div>

    <el-table :data="list" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="nickname" label="昵称" />
      <el-table-column label="信用" width="120">
        <template #default="{ row }">
          <el-tag :type="row.creditScore >= 70 ? 'success' : row.creditScore >= 50 ? 'warning' : 'danger'">
            {{ row.creditScore }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="风险" width="120">
        <template #default="{ row }">
          <el-tag :type="row.riskScore > 60 ? 'danger' : row.riskScore > 30 ? 'warning' : 'info'">
            {{ row.riskScore }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="row.status === 'Active' ? 'success' : 'danger'">
            {{ row.status === 'Active' ? '正常' : '封禁' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="预约数" prop="reservationCount" width="90" />
      <el-table-column label="注册时间" width="160">
        <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
      </el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template #default="{ row }">
          <el-button size="small" @click="openDetail(row.id)">详情</el-button>
          <el-button size="small" type="warning" @click="toggleStatus(row)">{{ row.status === 'Active' ? '封禁' : '解禁' }}</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-drawer v-model="drawerVisible" title="用户详情" size="520px">
      <div v-if="detail">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="ID">{{ detail.id }}</el-descriptions-item>
          <el-descriptions-item label="昵称">{{ detail.nickname }}</el-descriptions-item>
          <el-descriptions-item label="信用分">{{ detail.creditScore }}</el-descriptions-item>
          <el-descriptions-item label="风险分">{{ detail.riskScore }}</el-descriptions-item>
          <el-descriptions-item label="状态">{{ detail.status }}</el-descriptions-item>
          <el-descriptions-item label="预约数">{{ detail.reservationCount }}</el-descriptions-item>
        </el-descriptions>

        <div class="section-title">信用 / 风险调整</div>
        <div class="adjust-row">
          <el-input-number v-model="changeValue" :min="-50" :max="50" />
          <el-input v-model="changeReason" placeholder="调整原因" style="flex:1;margin:0 10px" />
          <el-button type="primary" @click="adjustCredit">调信用</el-button>
          <el-button type="danger" @click="adjustRisk">调风险</el-button>
        </div>

        <div class="section-title">信用流水</div>
        <el-table :data="detail.creditTransactions" size="small" max-height="260">
          <el-table-column prop="change" label="变动" width="80">
            <template #default="{ row }">
              <span :style="{ color: row.change >= 0 ? '#3A8A7E' : '#B85450' }">{{ row.change >= 0 ? '+' : '' }}{{ row.change }}</span>
            </template>
          </el-table-column>
          <el-table-column prop="reason" label="原因" />
          <el-table-column label="时间" width="140">
            <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
          </el-table-column>
        </el-table>
      </div>
    </el-drawer>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { userApi } from '../api'

const list = ref([])
const keyword = ref('')
const drawerVisible = ref(false)
const detail = ref(null)
const changeValue = ref(0)
const changeReason = ref('')

onMounted(load)

async function load() {
  try {
    list.value = await userApi.all(keyword.value)
  } catch (e) {}
}

function formatTime(iso) {
  if (!iso) return '-'
  return new Date(iso).toLocaleString('zh-CN', { hour12: false })
}

async function openDetail(id) {
  detail.value = await userApi.detail(id)
  drawerVisible.value = true
}

async function toggleStatus(row) {
  const action = row.status === 'Active' ? '封禁' : '解禁'
  const status = row.status === 'Active' ? 'Banned' : 'Active'
  try {
    await ElMessageBox.confirm(`确定${action}用户「${row.nickname}」吗？`, '确认')
    await userApi.setUserStatus(row.id, status)
    ElMessage.success(`${action}成功`)
    load()
  } catch (e) {}
}

async function adjustCredit() {
  await userApi.adjustCredit(detail.value.id, changeValue.value, changeReason.value)
  ElMessage.success('已调整信用')
  detail.value = await userApi.detail(detail.value.id)
}

async function adjustRisk() {
  await userApi.adjustRisk(detail.value.id, changeValue.value, changeReason.value)
  ElMessage.success('已调整风险')
  detail.value = await userApi.detail(detail.value.id)
}
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
  display: flex;
  gap: 10px;
}
.section-title {
  font-weight: 600;
  margin: 20px 0 12px;
  color: #333;
}
.adjust-row {
  display: flex;
  align-items: center;
}
</style>
