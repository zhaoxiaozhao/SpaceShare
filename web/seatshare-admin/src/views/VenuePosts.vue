<template>
  <div>
    <el-tabs v-model="tab" @tab-change="load">
      <el-tab-pane label="帖子" name="post" />
      <el-tab-pane label="评论" name="comment" />
    </el-tabs>

    <div class="toolbar">
      <el-select v-model="status" placeholder="状态筛选" clearable style="width: 200px" @change="load">
        <el-option label="正常展示" value="Visible" />
        <el-option label="已隐藏（待审核）" value="Hidden" />
      </el-select>
      <span class="tip">被举报的内容会自动隐藏，通过后恢复展示，驳回则删除</span>
    </div>

    <!-- 帖子 -->
    <el-table v-if="tab === 'post'" :data="posts" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="venueName" label="场馆" width="150" show-overflow-tooltip />
      <el-table-column prop="categoryLabel" label="板块" width="100" />
      <el-table-column prop="title" label="标题" min-width="180" show-overflow-tooltip />
      <el-table-column prop="content" label="内容" min-width="220" show-overflow-tooltip />
      <el-table-column label="作者" width="130">
        <template #default="{ row }">{{ row.ownerName || ('#' + row.userId) }}</template>
      </el-table-column>
      <el-table-column label="数据" width="130">
        <template #default="{ row }">赞 {{ row.likeCount }} · 评 {{ row.commentCount }}</template>
      </el-table-column>
      <el-table-column label="状态" width="110">
        <template #default="{ row }">
          <el-tag :type="row.status === 'Visible' ? 'success' : 'danger'">{{ row.status === 'Hidden' ? '已隐藏' : '正常' }}</el-tag>
          <el-tag v-if="row.isPinned" type="warning" style="margin-left: 4px">置顶</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="时间" width="170">
        <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
      </el-table-column>
      <el-table-column label="操作" width="230" fixed="right">
        <template #default="{ row }">
          <template v-if="row.status === 'Hidden'">
            <el-button size="small" type="success" @click="review(row, true)">通过</el-button>
            <el-button size="small" type="danger" @click="review(row, false)">驳回</el-button>
          </template>
          <el-button size="small" @click="pin(row, !row.isPinned)">{{ row.isPinned ? '取消置顶' : '置顶' }}</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 评论 -->
    <el-table v-else :data="comments" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="postId" label="帖子ID" width="90" />
      <el-table-column label="类型" width="150">
        <template #default="{ row }">
          <el-tag v-if="row.parentCommentId" type="warning" size="small">
            回复 {{ row.replyToName || '' }}<span v-if="!row.replyToName">#{{ row.replyToUserId }}</span>
          </el-tag>
          <el-tag v-else type="info" size="small">评论</el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="content" label="评论" min-width="300" show-overflow-tooltip />
      <el-table-column label="作者" width="140">
        <template #default="{ row }">{{ row.ownerName || ('#' + row.userId) }}</template>
      </el-table-column>
      <el-table-column label="状态" width="110">
        <template #default="{ row }">
          <el-tag :type="row.status === 'Visible' ? 'success' : 'danger'">{{ row.status === 'Hidden' ? '已隐藏' : '正常' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="时间" width="170">
        <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
      </el-table-column>
      <el-table-column label="操作" width="170" fixed="right">
        <template #default="{ row }">
          <template v-if="row.status === 'Hidden'">
            <el-button size="small" type="success" @click="reviewComment(row, true)">通过</el-button>
            <el-button size="small" type="danger" @click="reviewComment(row, false)">驳回</el-button>
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
import { venuePostApi } from '../api'

const tab = ref('post')
const status = ref('')
const posts = ref([])
const comments = ref([])

onMounted(load)

async function load() {
  try {
    if (tab.value === 'post') {
      posts.value = await venuePostApi.list(status.value)
    } else {
      comments.value = await venuePostApi.comments(status.value)
    }
  } catch (e) {}
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
      await ElMessageBox.confirm('驳回将删除该帖子，确定吗？', '驳回', { type: 'warning' })
    }
    await venuePostApi.review(row.id, approve)
    ElMessage.success(approve ? '已通过' : '已驳回并删除')
    load()
  } catch (e) {}
}

async function pin(row, pinned) {
  try {
    await venuePostApi.pin(row.id, pinned)
    ElMessage.success(pinned ? '已置顶' : '已取消置顶')
    load()
  } catch (e) {}
}

async function reviewComment(row, approve) {
  try {
    if (!approve) {
      await ElMessageBox.confirm(row.parentCommentId ? '驳回将删除该回复，确定吗？' : '驳回将删除该评论及其下全部回复，确定吗？', '驳回', { type: 'warning' })
    }
    await venuePostApi.commentReview(row.id, approve)
    ElMessage.success(approve ? '已通过' : '已驳回并删除')
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
