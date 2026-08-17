<template>
  <el-container class="layout">
    <el-aside width="200px" class="aside">
      <div class="logo">
        <span class="logo-name">友邻座</span>
        <span class="logo-sub">管理后台</span>
      </div>
      <el-menu :default-active="$route.path" router background-color="#2B403C" text-color="#B8CCC7" active-text-color="#ffffff">
        <el-menu-item index="/dashboard"><el-icon><DataAnalysis /></el-icon>数据统计</el-menu-item>
        <el-menu-item index="/users"><el-icon><User /></el-icon>用户管理</el-menu-item>
        <el-menu-item index="/admins"><el-icon><Avatar /></el-icon>管理员管理</el-menu-item>
        <el-menu-item index="/venues"><el-icon><OfficeBuilding /></el-icon>场馆管理</el-menu-item>
        <el-menu-item index="/reservations"><el-icon><Calendar /></el-icon>预约管理</el-menu-item>
        <el-menu-item index="/reports"><el-icon><Warning /></el-icon>举报管理</el-menu-item>
        <el-menu-item index="/config"><el-icon><Setting /></el-icon>系统配置</el-menu-item>
        <el-menu-item index="/audit"><el-icon><Document /></el-icon>审计日志</el-menu-item>
      </el-menu>
    </el-aside>
    <el-container>
      <el-header class="header">
        <span class="page-title">{{ $route.meta.title || '' }}</span>
        <el-dropdown @command="onCommand">
          <span class="admin-name">{{ adminName }} ▾</span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="logout">退出登录</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </el-header>
      <el-main>
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const adminName = computed(() => localStorage.getItem('admin_name') || '管理员')

function onCommand(command) {
  if (command === 'logout') {
    localStorage.removeItem('admin_token')
    localStorage.removeItem('admin_name')
    router.push('/login')
  }
}
</script>

<style scoped>
.layout {
  height: 100vh;
}
.aside {
  background: #2B403C;
}
.logo {
  height: 60px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  color: #fff;
  background: #24332F;
}
.logo-name {
  font-size: 20px;
  font-weight: 700;
  letter-spacing: 4px;
}
.logo-sub {
  font-size: 12px;
  color: #8FB5AC;
  margin-top: 2px;
}
.el-menu {
  border-right: none;
}
.header {
  background: #fff;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #e8e8e8;
}
.page-title {
  font-size: 16px;
  font-weight: 600;
}
.admin-name {
  cursor: pointer;
  color: #3A8A7E;
}
</style>
