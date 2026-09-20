// 分析 sort-sig 日志: 检验「点几遍才稳定」的收敛现象。
// 用法: node analyze_sortsig.ts <logfile>
// 每条 sort-sig 记录一次排序结果: uid@x,y:orientation 列表。
// 关注: 同一 uid 集合(同一容器)被重复排序时, 布局是否逐位相同(幂等)。
import * as fs from 'node:fs'
import * as crypto from 'node:crypto'

const path = process.argv[2]
const raw = fs.readFileSync(path, 'utf8')
const lines = raw.split(/\r?\n/)

type Row = {
  t: string
  n: number
  restored: number
  nu: number
  uids: string
  assign: string
  hash: string
}

const rows: Row[] = []
for (const L of lines) {
  const m = L.match(/^\[(\d\d:\d\d:\d\d\.\d+)\].*sort-sig packed n=(\d+) restored=(\d+) : (.*)$/)
  if (!m) continue
  const items = m[4]
    .trim()
    .split(/\s+/)
    .map((s) => {
      const q = s.match(/^(\d+)@(\d+),(\d+):(\d+)$/)
      return q ? { uid: +q[1], x: +q[2], y: +q[3], o: +q[4] } : null
    })
    .filter((x): x is { uid: number; x: number; y: number; o: number } => x !== null)
  const uids = items.map((i) => i.uid).sort((a, b) => a - b).join(',')
  const assign = items.map((i) => `${i.uid}@${i.x},${i.y}:${i.o}`).sort().join(' ')
  rows.push({
    t: m[1],
    n: +m[2],
    restored: +m[3],
    nu: new Set(items.map((i) => i.uid)).size,
    uids,
    assign,
    hash: crypto.createHash('md5').update(assign).digest('hex').slice(0, 8),
  })
}

console.log(`file: ${path}`)
console.log(`total sort-sig events: ${rows.length}\n`)
console.log('idx time         n  uniq restored hash')
rows.forEach((r, i) => {
  const dupUid = r.n !== r.nu ? ` !!n>uniq(${r.n - r.nu})` : ''
  console.log(
    `${String(i).padStart(3)} ${r.t} ${String(r.n).padStart(3)} ${String(r.nu).padStart(5)} ${String(r.restored).padStart(8)}   ${r.hash}${dupUid}`,
  )
})

console.log('\n=== same-uid-set repeats (convergence) ===')
const bySet: Record<string, number[]> = {}
rows.forEach((r, i) => {
  ;(bySet[r.uids] = bySet[r.uids] || []).push(i)
})
let anyRepeat = false
for (const k in bySet) {
  const idxs = bySet[k]
  if (idxs.length < 2) continue
  anyRepeat = true
  const hashes = idxs.map((i) => rows[i].hash)
  const same = new Set(hashes).size === 1
  console.log(
    `  uniq=${rows[idxs[0]].nu} occurrences=${idxs.length} idx=${JSON.stringify(idxs)} identical=${same} hashes=${JSON.stringify(hashes)}`,
  )
}
if (!anyRepeat) console.log('  (no uid-set repeated -> every sort hit a different container)')

console.log('\n=== n vs uniq distribution ===')
const dist: Record<string, number> = {}
for (const r of rows) {
  const k = `n=${r.n} uniq=${r.nu}`
  dist[k] = (dist[k] || 0) + 1
}
Object.entries(dist)
  .sort((a, b) => b[1] - a[1])
  .slice(0, 12)
  .forEach(([k, v]) => console.log(`  ${k}  x${v}`))

const merged = rows.filter((r) => r.n !== r.nu)
console.log(`\nevents with n>uniq (merged/stacks present): ${merged.length}/${rows.length}`)
console.log(`restored!=0 events: ${rows.filter((r) => r.restored !== 0).length}`)
