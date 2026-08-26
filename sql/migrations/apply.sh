#!/usr/bin/env bash
# =============================================================================
# 迁移查看 / 应用脚本
#
# 默认只“看”，不改库。要真的应用必须显式给出版本号，避免误把不想上线的迁移带上去
# （例如 002 温控器：当前业务决定暂不上线，就不该被自动应用）。
#
# 用法：
#   ./apply.sh                 列出全部迁移及其应用状态
#   ./apply.sh --apply 002     只应用 002，并记入 schema_migration
#   ./apply.sh --verify        校验已应用迁移的文件是否被改动过（checksum 比对）
#
# 连接参数走标准 PG 环境变量，默认对应本项目 App.config：
#   PGHOST=localhost PGPORT=11451 PGDATABASE=ma PGUSER=postgres
# 口令请用 PGPASSWORD 或 ~/.pgpass 提供，不要写进本脚本。
# =============================================================================
set -euo pipefail

cd "$(dirname "$0")"

export PGHOST="${PGHOST:-localhost}"
export PGPORT="${PGPORT:-11451}"
export PGDATABASE="${PGDATABASE:-ma}"
export PGUSER="${PGUSER:-postgres}"
# 迁移文件是 UTF-8。Windows 上 psql 在输出被重定向时会退回 ANSI 代码页（中文环境是 GBK），
# 于是文件里的中文注释会报 “字符 0x.. 在编码 UTF8 没有相应值”。显式钉住客户端编码。
export PGCLIENTENCODING="${PGCLIENTENCODING:-UTF8}"

PSQL=(psql -v ON_ERROR_STOP=1 -t -A -q)

# Windows 上 psql 输出用 CRLF 行尾，直接读会让字段末尾多一个回车符
# （曾导致空 checksum 被读成非空，于是回填记录被误报成“文件已被改动”）。统一剥掉。
psql_q() {
  "${PSQL[@]}" "$@" | tr -d '\r'
}

checksum_of() {
  sha256sum "$1" | awk '{print $1}'
}

ensure_ledger() {
  local exists
  exists=$(psql_q -c "select count(*) from information_schema.tables where table_schema='public' and table_name='schema_migration';")
  if [ "$exists" = "0" ]; then
    echo "schema_migration 表不存在，先应用 004 建表..."
    "${PSQL[@]}" -f 004_schema_migration.sql >/dev/null
    echo "已建立迁移记录表。"
  fi
}

list_status() {
  echo "版本  状态      文件"
  echo "----  --------  --------------------------------------------"
  local applied
  applied=$(psql_q -c "select version from schema_migration order by version;" || true)
  for file in [0-9][0-9][0-9]_*.sql; do
    local version="${file:0:3}"
    if echo "$applied" | grep -qx "$version"; then
      printf "%-4s  已应用    %s\n" "$version" "$file"
    else
      printf "%-4s  未应用    %s\n" "$version" "$file"
    fi
  done
}

verify_checksums() {
  echo "校验已应用迁移的文件是否被改动过："
  local rows
  rows=$(psql_q -c "select version || '|' || coalesce(checksum,'') from schema_migration order by version;")
  local dirty=0
  while IFS='|' read -r version recorded; do
    [ -z "$version" ] && continue
    local file
    file=$(ls "${version}"_*.sql 2>/dev/null | head -1 || true)
    if [ -z "$file" ]; then
      echo "  $version  ⚠ 记录为已应用，但找不到对应文件"
      dirty=1
      continue
    fi
    if [ -z "$recorded" ]; then
      echo "  $version  - 无 checksum（回填记录，跳过）"
      continue
    fi
    local actual
    actual=$(checksum_of "$file")
    if [ "$actual" = "$recorded" ]; then
      echo "  $version  ✓ 一致"
    else
      echo "  $version  ✗ 文件已被改动！迁移文件应视为不可变，请改用新迁移。"
      dirty=1
    fi
  done <<< "$rows"
  return $dirty
}

apply_one() {
  local version="$1"
  local file
  file=$(ls "${version}"_*.sql 2>/dev/null | head -1 || true)
  if [ -z "$file" ]; then
    echo "找不到版本 $version 对应的迁移文件。" >&2
    exit 1
  fi

  local already
  already=$(psql_q -c "select count(*) from schema_migration where version='${version}';")
  if [ "$already" != "0" ]; then
    echo "$version 已记录为已应用，无需重复执行。"
    return 0
  fi

  echo "即将应用 $file"
  "${PSQL[@]}" -f "$file"
  "${PSQL[@]}" -c "insert into schema_migration (version, name, checksum) values ('${version}', '${file}', '$(checksum_of "$file")') on conflict (version) do nothing;" >/dev/null
  echo "$version 应用完成并已记入 schema_migration。"
}

case "${1:-}" in
  --apply)
    [ $# -ge 2 ] || { echo "用法: $0 --apply <版本号，如 002>" >&2; exit 1; }
    ensure_ledger
    apply_one "$2"
    ;;
  --verify)
    ensure_ledger
    verify_checksums
    ;;
  "")
    ensure_ledger
    list_status
    ;;
  *)
    echo "未知参数: $1" >&2
    echo "用法: $0 [--apply <版本号> | --verify]" >&2
    exit 1
    ;;
esac
