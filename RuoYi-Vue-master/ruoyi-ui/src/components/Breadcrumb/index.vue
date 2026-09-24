<template>
  <el-breadcrumb class="app-breadcrumb" separator="/">
    <transition-group name="breadcrumb">
      <el-breadcrumb-item v-for="(item, index) in levelList" :key="item.path">
        <span v-if="item.redirect === 'noRedirect' || index == levelList.length - 1" class="no-redirect">{{ crumbTitle(item, index) }}</span>
        <a v-else @click.prevent="handleLink(item)">{{ crumbTitle(item, index) }}</a>
      </el-breadcrumb-item>
    </transition-group>
  </el-breadcrumb>
</template>

<script>
export default {
  data() {
    return {
      levelList: null
    }
  },
  watch: {
    $route(route) {
      // if you go to the redirect page, do not update the breadcrumbs
      if (route.path.startsWith('/redirect/')) {
        return
      }
      this.getBreadcrumb()
    }
  },
  created() {
    this.getBreadcrumb()
  },
  methods: {
    getBreadcrumb() {
      // only show routes with meta.title
      let matched = []
      const router = this.$route
      const pathNum = this.findPathNum(router.path)
      // multi-level menu
      if (pathNum > 2) {
        // 路径段要含连字符：原正则 /\/\w+/ 会在连字符处断开，
        // 像 alarm-record/900006 会被拆成 ['/meter','/alarm','/900006']，导致中间几级匹配不上、面包屑少几级
        const reg = /\/[\w-]+/gi
        const pathList = router.path.match(reg).map((item, index) => {
          if (index !== 0) item = item.slice(1)
          return item
        })
        this.getMatched(pathList, this.$store.getters.defaultRoutes, matched)
      } else {
        matched = router.matched.filter(item => item.meta && item.meta.title)
      }
      // 判断是否为首页
      if (!this.isDashboard(matched[0])) {
        matched = [{ path: "/index", meta: { title: "首页" } }].concat(matched)
      }
      this.levelList = matched.filter(item => item.meta && item.meta.title && item.meta.breadcrumb !== false)
    },
    findPathNum(str, char = "/") {
      let index = str.indexOf(char)
      let num = 0
      while (index !== -1) {
        num++
        index = str.indexOf(char, index + 1)
      }
      return num
    },
    getMatched(pathList, routeList, matched) {
      // 路由 path 带参数时（如 alarm-record/:meterId?）按整段比较会匹配不上，
      // 于是末级面包屑整条消失。这里把静态部分（冒号之前）拿来比。
      const staticPart = path => String(path).split('/:')[0]
      let data = routeList.find(item => staticPart(item.path) == pathList[0] || (item.name += '').toLowerCase() == pathList[0])
      if (data) {
        matched.push(data)
        if (data.children && pathList.length) {
          pathList.shift()
          this.getMatched(pathList, data.children, matched)
        }
      }
    },
    /**
     * 末级面包屑优先用"当前视图"的标题：页面可以给标签页设按实例的 title
     * （如设备详情页显示设备名），这里跟着一起显示。
     * 不能靠改 $route.meta.title 实现 —— meta 是路由记录上的共享对象，会串到下一次导航。
     */
    crumbTitle(item, index) {
      if (index === this.levelList.length - 1) {
        const views = this.$store.state.tagsView && this.$store.state.tagsView.visitedViews
        const current = views && views.find(view => view.path === this.$route.path)
        if (current && current.title) return current.title
      }
      return item.meta.title
    },
    isDashboard(route) {
      const name = route && route.name
      if (!name) {
        return false
      }
      return name.trim() === 'Index'
    },
    handleLink(item) {
      const { redirect, path } = item
      if (redirect) {
        this.$router.push(redirect)
        return
      }
      this.$router.push(path)
    }
  }
}
</script>

<style lang="scss" scoped>
.app-breadcrumb.el-breadcrumb {
  display: inline-block;
  font-size: 14px;
  line-height: 50px;
  .no-redirect {
    color: #97a8be;
    cursor: text;
  }
}
</style>
