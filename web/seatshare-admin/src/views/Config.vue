<template>
  <div>
    <el-tabs v-model="activeTab" @tab-change="filter">
      <el-tab-pane v-for="cat in categories" :key="cat" :label="categoryText(cat)" :name="cat">
        <el-table :data="filtered" border stripe>
          <el-table-column prop="key" label="配置键" width="260" />
          <el-table-column prop="description" label="说明" min-width="180" />
          <el-table-column label="值" min-width="360">
            <template #default="{ row }">
              <el-input v-model="row.value" type="textarea" :autosize="{ minRows: 1, maxRows: 8 }" size="small" @change="() => save(row)" />
            </template>
          </el-table-column>
          <el-table-column label="操作" width="120">
            <template #default="{ row }">
              <el-button size="small" type="primary" @click="save(row)">保存</el-button>
            </template>
          </el-table-column>
        </el-table>
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { configApi } from '../api'

const all = ref([])
const activeTab = ref('ReservationRules')
const categories = ['ReservationRules', 'CreditRules', 'RiskRules', 'ArrivalRules', 'ImageRules', 'SensitiveWords', 'ActivityCategories', 'SwapReasons', 'SeatTags', 'NotificationTemplates']
const filtered = ref([])

onMounted(load)

async function load() {
  try {
    all.value = await configApi.all()
    filter()
  } catch (e) {}
}

function filter() {
  filtered.value = all.value.filter((c) => c.category === activeTab.value)
}

function categoryText(cat) {
  const map = {
    ReservationRules: '预约规则', CreditRules: '信用规则', RiskRules: '风控规则',
    ArrivalRules: '到座规则', ImageRules: '图片规则', SensitiveWords: '敏感词',
    ActivityCategories: '活动分类', SwapReasons: '换座原因', SeatTags: '座位标签',
    NotificationTemplates: '订阅消息模板'
  }
  return map[cat] || cat
}

async function save(row) {
  await configApi.update(row.id, row.value)
  ElMessage.success(`已保存 ${row.key}`)
}
</script>
