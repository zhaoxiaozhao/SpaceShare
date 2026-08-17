<template>
  <div>
    <div class="toolbar">
      <el-button type="primary" @click="dialogVisible = true">新增管理员</el-button>
    </div>

    <el-table :data="list" border stripe>
      <el-table-column prop="id" label="ID" width="70" />
      <el-table-column prop="username" label="用户名" />
      <el-table-column prop="displayName" label="显示名称" />
      <el-table-column label="角色" width="120">
        <template #default="{ row }">
          <el-tag>{{ roleText(row.role) }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="row.status === 'Active' ? 'success' : 'danger'">{{ row.status === 'Active' ? '启用' : '禁用' }}</el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="200">
        <template #default="{ row }">
          <el-button size="small" @click="resetPwd(row)">重置密码</el-button>
          <el-button size="small" :type="row.status === 'Active' ? 'warning' : 'success'" @click="toggleStatus(row)">
            {{ row.status === 'Active' ? '禁用' : '启用' }}
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="dialogVisible" title="新增管理员" width="420px">
      <el-form label-width="80px">
        <el-form-item label="用户名"><el-input v-model="form.username" /></el-form-item>
        <el-form-item label="显示名称"><el-input v-model="form.displayName" /></el-form-item>
        <el-form-item label="密码"><el-input v-model="form.password" type="password" show-password /></el-form-item>
        <el-form-item label="角色">
          <el-select v-model="form.role">
            <el-option label="超级管理员" value="SuperAdmin" />
            <el-option label="管理员" value="Admin" />
            <el-option label="审核员" value="Moderator" />
            <el-option label="广告管理员" value="AdManager" />
            <el-option label="商家管理员" value="MerchantManager" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="create">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { userApi } from '../api'

const list = ref([])
const dialogVisible = ref(false)
const form = ref({ username: '', displayName: '', password: '', role: 'Moderator' })

onMounted(load)

async function load() {
  try {
    list.value = await userApi.admins()
  } catch (e) {}
}

function roleText(role) {
  const map = { SuperAdmin: '超级管理员', Admin: '管理员', Moderator: '审核员', AdManager: '广告管理员', MerchantManager: '商家管理员' }
  return map[role] || role
}

async function create() {
  await userApi.createAdmin(form.value)
  ElMessage.success('创建成功')
  dialogVisible.value = false
  form.value = { username: '', displayName: '', password: '', role: 'Moderator' }
  load()
}

async function toggleStatus(row) {
  const status = row.status === 'Active' ? 'Disabled' : 'Active'
  try {
    await ElMessageBox.confirm(`确定${status === 'Disabled' ? '禁用' : '启用'}管理员「${row.username}」吗？`, '确认')
    await userApi.setAdminStatus(row.id, status)
    ElMessage.success('操作成功')
    load()
  } catch (e) {}
}

async function resetPwd(row) {
  try {
    const { value } = await ElMessageBox.prompt(`请输入「${row.username}」的新密码`, '重置密码')
    await userApi.resetPassword(row.id, value)
    ElMessage.success('密码已重置')
  } catch (e) {}
}
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
}
</style>
