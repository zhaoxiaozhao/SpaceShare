<template>
  <div>
    <el-row :gutter="16">
      <el-col :span="6" v-for="card in cards" :key="card.label">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-num">{{ card.value }}</div>
          <div class="stat-label">{{ card.label }}</div>
        </el-card>
      </el-col>
    </el-row>

    <el-card shadow="hover" class="chart-card">
      <template #header>近 {{ days }} 天趋势</template>
      <div ref="chartEl" style="height: 360px"></div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import * as echarts from 'echarts'
import { statsApi } from '../api'

const overview = ref({})
const trend = ref({ dates: [], reservations: [], newUsers: [] })
const days = ref(14)
const chartEl = ref(null)

const cards = computed(() => [
  { label: '用户总数', value: overview.value.userCount ?? '-' },
  { label: '场馆数', value: overview.value.venueCount ?? '-' },
  { label: '座位数', value: overview.value.seatCount ?? '-' },
  { label: '今日预约', value: overview.value.todayReservations ?? '-' },
  { label: '进行中预约', value: overview.value.activeReservations ?? '-' },
  { label: '到达率', value: overview.value.arrivalRate != null ? overview.value.arrivalRate + '%' : '-' },
  { label: '爽约率', value: overview.value.noShowRate != null ? overview.value.noShowRate + '%' : '-' },
  { label: '待处理举报', value: overview.value.pendingReports ?? '-' }
])

onMounted(async () => {
  const [o, t] = await Promise.all([statsApi.overview(), statsApi.trend(days.value)])
  overview.value = o
  trend.value = t
  renderChart()
})

function renderChart() {
  const chart = echarts.init(chartEl.value)
  chart.setOption({
    tooltip: { trigger: 'axis' },
    legend: { data: ['预约量', '新增用户'] },
    grid: { left: 50, right: 40, top: 40, bottom: 30 },
    xAxis: { type: 'category', data: trend.value.dates },
    yAxis: [{ type: 'value' }, { type: 'value' }],
    series: [
      { name: '预约量', type: 'bar', data: trend.value.reservations, itemStyle: { color: '#3A8A7E' } },
      { name: '新增用户', type: 'line', data: trend.value.newUsers, itemStyle: { color: '#D9822B' } }
    ]
  })
}
</script>

<style scoped>
.stat-card {
  text-align: center;
  margin-bottom: 16px;
}
.stat-num {
  font-size: 30px;
  font-weight: 700;
  color: #3A8A7E;
}
.stat-label {
  margin-top: 8px;
  color: #8a8a8a;
  font-size: 13px;
}
.chart-card {
  margin-top: 8px;
}
</style>
