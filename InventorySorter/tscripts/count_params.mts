// 单个方法的参数个数权威计数（含 tuple 泛型参数）。
// 关键: 签名自身的 '(' 计入深度, 泛型内的 '()' 成对平衡, 故只有真正的收尾 ')' 会让深度归零。
import * as fs from 'node:fs'

const file = process.argv[2]
const names = process.argv.slice(3)
const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/)

function countParams(name: string) {
  for (let i = 0; i < lines.length; i++) {
    if (!new RegExp(`\\b${name}\\s*\\(`).test(lines[i])) continue
    // 找到 name 后紧跟的 '('
    const at = lines[i].indexOf(name)
    const open = lines[i].indexOf('(', at + name.length)
    if (open < 0) continue
    // 跨行收集, 深度含首个 '('
    let depth = 0
    let buf = ''
    let done = false
    for (let j = i; j < lines.length && !done; j++) {
      const seg = j === i ? lines[j].slice(open) : lines[j]
      for (const ch of seg) {
        if (ch === '(') {
          depth++
          if (depth === 1 && buf === '') continue // 跳过签名自身的 '('
          buf += ch
          continue
        }
        if (ch === ')') {
          depth--
          if (depth === 0) {
            done = true
            break
          }
          buf += ch
          continue
        }
        if (depth >= 1) buf += ch
      }
      if (!done) buf += ' '
    }
    // 顶层逗号切分(跟踪 () [] <>)
    const parts: string[] = []
    let d = 0
    let cur = ''
    for (const ch of buf) {
      if ('([<'.includes(ch)) d++
      else if (')]>'.includes(ch)) d--
      else if (ch === ',' && d === 0) {
        parts.push(cur.trim())
        cur = ''
        continue
      }
      cur += ch
    }
    if (cur.trim()) parts.push(cur.trim())
    const clean = parts.filter((p) => p.length > 0)
    return { line: i + 1, n: clean.length, parts: clean }
  }
  return null
}

for (const n of names) {
  const r = countParams(n)
  if (!r) {
    console.log(`${n}: 未找到`)
    continue
  }
  console.log(`${n} (L${r.line}): ${r.n} 参`)
  r.parts.forEach((p, k) => console.log(`    [${k}] ${p}`))
}
