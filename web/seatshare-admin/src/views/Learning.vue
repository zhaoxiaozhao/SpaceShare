<template>
  <div>
    <!-- 概览卡 -->
    <div class="grid">
      <el-card shadow="hover" class="stat-card">
        <div class="stat-num">{{ fmtHours(overview.studyTotalMinutes) }}</div>
        <div class="stat-label">学习总时长(小时)</div>
      </el-card>
      <el-card shadow="hover" class="stat-card">
        <div class="stat-num">{{ overview.studyUserCount ?? '-' }}</div>
        <div class="stat-label">学习用户数</div>
      </el-card>
      <el-card shadow="hover" class="stat-card">
        <div class="stat-num">{{ overview.studyTodayMinutes ?? '-' }}</div>
        <div class="stat-label">今日学习(分钟)</div>
      </el-card>
      <el-card shadow="hover" class="stat-card">
        <div class="stat-num">{{ fmtHours(overview.readingTotalMinutes) }}</div>
        <div class="stat-label">阅读总时长(小时)</div>
      </el-card>
      <el-card shadow="hover" class="stat-card">
        <div class="stat-num">{{ overview.readingUserCount ?? '-' }}</div>
        <div class="stat-label">阅读用户数</div>
      </el-card>
      <el-card shadow="hover" class="stat-card">
        <div class="stat-num">{{ overview.finishedBookCount ?? '-' }}</div>
        <div class="stat-label">读完书籍</div>
      </el-card>
      <el-card shadow="hover" class="stat-card">
        <div class="stat-num">{{ overview.readingBookCount ?? '-' }}</div>
        <div class="stat-label">书籍总数</div>
      </el-card>
    </div>

    <!-- 趋势 -->
    <el-card shadow="never" style="margin-top: 16px">
      <template #header>
        <div class="card-head">
          <span>近 {{ days }} 日学习/阅读时长（分钟）</span>
          <el-radio-group v-model="days" size="small" @change="load">
            <el-radio-button :value="7">7 天</el-radio-button>
            <el-radio-button :value="14">14 天</el-radio-button>
            <el-radio-button :value="30">30 天</el-radio-button>
          </el-radio-group>
        </div>
      </template>
      <div ref="chartEl" class="chart"></div>
    </el-card>

    <!-- 用户维度 -->
    <el-card shadow="never" style="margin-top: 16px">
      <template #header>
        <div class="card-head">
          <span>用户学习/阅读排行</span>
          <el-input v-model="keyword" placeholder="搜索昵称/用户名" clearable style="width: 220px" />
        </div>
      </template>
      <el-table :data="filteredUsers" border stripe>
        <el-table-column prop="userId" label="用户ID" width="90" />
        <el-table-column prop="nickname" label="昵称" min-width="150" />
        <el-table-column label="学习(小时)" width="120" sortable :sort-method="(a, b) => a.studyMinutes - b.studyMinutes">
          <template #default="{ row }">{{ fmtHours(row.studyMinutes) }}</template>
        </el-table-column>
        <el-table-column label="阅读(小时)" width="120" sortable :sort-method="(a, b) => a.readingMinutes - b.readingMinutes">
          <template #default="{ row }">{{ fmtHours(row.readingMinutes) }}</template>
        </el-table-column>
        <el-table-column label="合计(小时)" width="120" sortable :sort-method="(a, b) => a.totalMinutes - b.totalMinutes">
          <template #default="{ row }">{{ fmtHours(row.totalMinutes) }}</template>
        </el-table-column>
        <el-table-column prop="bookCount" label="书籍" width="90" />
        <el-table-column prop="finishedBookCount" label="读完" width="90" />
        <el-table-column label="最近活跃" width="180">
          <template #default="{ row }">{{ formatTime(row.lastActiveAt) }}</template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import * as echarts from 'echarts'
import { statsApi } from '../api'

const overview = ref({})
const users = ref([])
const keyword = ref('')
const days = ref(7)
const chartEl = ref(null)

const filteredUsers = computed(() => {
  const k = keyword.value.trim().toLowerCase()
  if (!k) return users.value
  return users.value.filter(
    (u) => String(u.nickname || '').toLowerCase().includes(k) || String(u.userId).includes(k)
  )
})

onMounted(load)

async function load() {
  try {
    const [o, list] = await Promise.all([statsApi.learning(days.value), statsApi.learningUsers(200)])
    overview.value = o
    users.value = list || []
    await nextTick()
    renderChart()
  } catch (e) {}
}

function renderChart() {
  if (!chartEl.value) return
  const trend = overview.value
  const chart = echarts.init(chartEl.value)
  chart.setOption({
    tooltip: { trigger: 'axis' },
    legend: { data: ['学习', '阅读'] },
    grid: { left: 50, right: 30, top: 40, bottom: 30 },
    xAxis: { type: 'category', data: trend.dates || [] },
    yAxis: { type: 'value' },
    series: [
      { name: '学习', type: 'bar', data: trend.studyMinutesTrend || [], itemStyle: { color: '#3A8A7E' } },
      { name: '阅读', type: 'bar', data: trend.readingMinutesTrend || [], itemStyle: { color: '#D9822B' } }
    ]
  })
}

function fmtHours(min) {
  if (min == null) return '-'
  return (min / 60).toFixed(1).replace('.0', '')
}

function formatTime(s) {
  if (!s) return ''
  const d = new Date(s)
  if (isNaN(d.getTime())) return ''
  const p = (x) => (x < 10 ? '0' + x : x)
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}`
}
</script>

<style scoped>
.grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 16px;
}
.stat-card {
  text-align: center;
}
.stat-num {
  font-size: 26px;
  font-weight: 700;
  color: #3a8a7e;
}
.stat-label {
  margin-top: 6px;
  color: #909399;
  font-size: 13px;
}
.card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.chart {
  height: 300px;
}
</style>
