<template>
  <div class="app-container">
    <el-row :gutter="20">
      <el-col :span="6" :xs="24">
        <el-card class="box-card">
          <div slot="header" class="clearfix">
            <span>个人信息</span>
          </div>
          <div>
            <div class="text-center">
              <userAvatar />
            </div>
            <ul class="list-group list-group-striped">
              <li class="list-group-item">
                <svg-icon icon-class="user" />用户名称
                <div class="pull-right">{{ user.userName }}</div>
              </li>
              <li class="list-group-item">
                <svg-icon icon-class="phone" />手机号码
                <div class="pull-right">{{ user.phonenumber }}</div>
              </li>
              <li class="list-group-item">
                <svg-icon icon-class="email" />用户邮箱
                <div class="pull-right">{{ user.email }}</div>
              </li>
              <li class="list-group-item">
                <svg-icon icon-class="tree" />所属部门
                <div class="pull-right" v-if="user.dept">{{ user.dept.deptName }} / {{ postGroup }}</div>
              </li>
              <li class="list-group-item">
                <svg-icon icon-class="peoples" />所属角色
                <div class="pull-right">{{ roleGroup }}</div>
              </li>
              <li v-if="isSuperAdmin" class="list-group-item">
                <svg-icon icon-class="monitor" />电表模拟数据
                <div class="pull-right">
                  <el-switch v-model="meterSimulation" active-text="开启" inactive-text="关闭" @change="handleMeterSimulationChange" />
                </div>
              </li>
              <li class="list-group-item">
                <svg-icon icon-class="date" />创建日期
                <div class="pull-right">{{ user.createTime }}</div>
              </li>
            </ul>
          </div>
        </el-card>
      </el-col>
      <el-col :span="18" :xs="24">
        <el-card>
          <div slot="header" class="clearfix">
            <span>基本资料</span>
          </div>
          <el-tabs v-model="selectedTab">
            <el-tab-pane label="基本资料" name="userinfo">
              <userInfo :user="user" />
            </el-tab-pane>
            <el-tab-pane label="修改密码" name="resetPwd">
              <resetPwd />
            </el-tab-pane>
          </el-tabs>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script>
import userAvatar from "./userAvatar"
import userInfo from "./userInfo"
import resetPwd from "./resetPwd"
import { getUserProfile } from "@/api/system/user"
import { isMeterSimulationEnabled, setMeterSimulationEnabled } from "@/utils/meter-simulation"

export default {
  name: "Profile",
  components: { userAvatar, userInfo, resetPwd },
  data() {
    return {
      user: {},
      roleGroup: {},
      postGroup: {},
      meterSimulation: isMeterSimulationEnabled(),
      selectedTab: "userinfo"
    }
  },
  computed: {
    isSuperAdmin() {
      return (this.$store.state.user.roles || []).includes("admin")
    }
  },
  created() {
    const activeTab = this.$route.params && this.$route.params.activeTab
    if (activeTab) {
      this.selectedTab = activeTab
    }
    this.getUser()
  },
  methods: {
    handleMeterSimulationChange(enabled) {
      setMeterSimulationEnabled(enabled)
      this.$modal.msgSuccess(enabled ? "已开启电表模拟模式，正在刷新页面" : "已关闭电表模拟模式，正在刷新页面")
      window.setTimeout(() => window.location.reload(), 600)
    },
    getUser() {
      getUserProfile().then(response => {
        this.user = response.data
        this.roleGroup = response.roleGroup
        this.postGroup = response.postGroup
      })
    }
  }
}
</script>
