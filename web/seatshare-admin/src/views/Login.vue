<template>
  <div class="login-page">
    <div class="login-card">
      <h1 class="brand">友邻座</h1>
      <p class="sub">一席相邻，善意相续 · 管理后台</p>
      <el-form @submit.prevent>
        <el-form-item>
          <el-input v-model="form.username" placeholder="用户名" size="large" />
        </el-form-item>
        <el-form-item>
          <el-input v-model="form.password" type="password" placeholder="密码" size="large" show-password @keyup.enter="login" />
        </el-form-item>
        <el-button type="primary" size="large" class="login-btn" @click="login">登 录</el-button>
      </el-form>
      <p class="tip">默认账号 admin / admin123</p>
    </div>
  </div>
</template>

<script setup>
import { reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { authApi } from '../api'

const router = useRouter()
const form = reactive({ username: '', password: '' })

async function login() {
  if (!form.username || !form.password) {
    ElMessage.warning('请输入用户名和密码')
    return
  }
  try {
    const res = await authApi.login(form)
    localStorage.setItem('admin_token', res.token)
    localStorage.setItem('admin_name', res.admin.displayName)
    localStorage.setItem('admin_role', res.admin.role)
    ElMessage.success('登录成功')
    router.push('/dashboard')
  } catch (e) {
    // 拦截器已提示
  }
}
</script>

<style scoped>
.login-page {
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(160deg, #2B403C, #3A8A7E);
}
.login-card {
  width: 380px;
  background: #fff;
  border-radius: 12px;
  padding: 48px 40px;
  box-shadow: 0 8px 32px rgba(0,0,0,0.2);
  text-align: center;
}
.brand {
  font-size: 32px;
  color: #3A8A7E;
  letter-spacing: 8px;
}
.sub {
  color: #8a8a8a;
  font-size: 13px;
  margin: 8px 0 32px;
}
.login-btn {
  width: 100%;
  background: #3A8A7E;
  border-color: #3A8A7E;
}
.login-btn:hover {
  background: #337A6F;
  border-color: #337A6F;
}
.tip {
  margin-top: 20px;
  font-size: 12px;
  color: #b0b0b0;
}
</style>
