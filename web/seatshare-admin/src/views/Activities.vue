<template>
  <div>
    <div class="toolbar">
      <el-select v-model="status" placeholder="状态筛选" clearable style="width: 180px" @change="load">
        <el-option v-for="s in statuses" :key="s" :label="statusText(s)" :value="s" />
      </el-select>
    </div>

    <el-table :data="list" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="title" label="标题" min-width="180" show-overflow-tooltip />
      <el-table-column label="分类" width="90">
        <template #default="{ row }">{{ categoryText(row.category) }}</template>
      </el-table-column>
      <el-table-column label="发起人" width="120">
        <template #default="{ row }">{{ row.creatorNickname || ('#' + row.creatorUserId) }}</template>
      </el-table-column>
      <el-table-column label="场馆/地点" min-width="160">
        <template #default="{ row }">{{ locText(row) }}</template>
      </el-table-column>
      <el-table-column label="时间" width="230">
        <template #default="{ row }">{{ ft(row.startAt) }} ~ {{ ft(row.endAt) }}</template>
      </el-table-column>
      <el-table-column label="报名" width="90">
        <template #default="{ row }">{{ row.signupCount }}/{{ row.capacity }}</template>
      </el-table-column>
      <el-table-column label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="tagType(row.status)">{{ statusText(row.status) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="220" fixed="right">
        <template #default="{ row }">
          <el-button v-if="row.status === 'PendingReview' || row.status === 'Rejected'" size="small" type="success" @click="review(row, true)">通过</el-button>
          <el-button v-if="row.status === 'PendingReview'" size="small" type="warning" @click="review(row, false)">驳回</el-button>
          <el-button v-if="row.status === 'Published'" size="small" type="danger" @click="takeDown(row)">下架</el-button>
          <el-button size="small" @click="viewSignups(row)">名单</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="signupVisible" title="报名名单" width="420px">
      <div v-if="current && current.signups && current.signups.length">
        <div v-for="(s, i) in current.signups" :key="s.id" class="signup-row">
          <span>{{ i + 1 }}. {{ s.userNickname || ('#' + s.userId) }}</span>
          <span class="muted">{{ ft(s.createdAt) }}</span>
        </div>
      </div>
      <div v-else class="muted">暂无报名</div>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { activityApi } from '../api'

const list = ref([])
const status = ref('')
const statuses = ['PendingReview', 'Published', 'Rejected', 'Cancelled', 'Finished']
const signupVisible = ref(false)
const current = ref(null)

onMounted(load)

async function load() {
  try {
    list.value = await activityApi.list(status.value)
  } catch (e) {}
}

function statusText(s) {
  const map = { PendingReview: '审核中', Published: '已发布', Rejected: '未通过', Cancelled: '已取消', Finished: '已结束' }
  return map[s] || s
}
function tagType(s) {
  if (s === 'Published') return 'success'
  if (s === 'PendingReview') return 'warning'
  if (s === 'Rejected') return 'danger'
  return 'info'
}
function categoryText(c) {
  const map = {
    reading: '读书', lecture: '讲座', exhibition: '展览', study: '自习', sharing: '分享交流',
    workshop: '工作坊', film: '观影', music: '音乐', art: '艺术', sports: '运动',
    competition: '比赛', volunteer: '志愿', other: '其他'
  }
  return map[c] || '其他'
}
function locText(row) {
  const parts = [row.venueName, row.locationText].filter(Boolean)
  return parts.length ? parts.join(' · ') : '-'
}
function ft(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const p = (n) => (n < 10 ? '0' + n : '' + n)
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}`
}

async function review(row, approve) {
  try {
    let remark = ''
    if (!approve) {
      const r = await ElMessageBox.prompt('驳回原因', '驳回活动', { inputPlaceholder: '请填写驳回原因' })
      remark = r.value || ''
    } else {
      await ElMessageBox.confirm('确认通过该活动的审核？', '审核通过', { type: 'warning' })
    }
    await activityApi.review(row.id, approve, remark)
    ElMessage.success('操作成功')
    load()
  } catch (e) {}
}

async function takeDown(row) {
  try {
    await ElMessageBox.confirm('确认下架该活动？', '下架', { type: 'warning' })
    await activityApi.takeDown(row.id)
    ElMessage.success('已下架')
    load()
  } catch (e) {}
}

function viewSignups(row) {
  current.value = row
  signupVisible.value = true
}
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
}
.muted {
  color: #bbb;
}
.signup-row {
  display: flex;
  justify-content: space-between;
  padding: 6px 0;
  border-bottom: 1px solid #f2f2f2;
}
</style>
