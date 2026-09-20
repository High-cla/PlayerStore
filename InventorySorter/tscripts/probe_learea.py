"""回归: LargestEmptyArea 复用 _histBuf 必须每次清零。

原始 bug: 只依赖 new int[W] 的初始零值, 复用后把上一轮残留高度在 y=0 又 +1,
返回值随调用次数单调增长(实测 30→32→56→80), 可超过网格总格数 —— 物理上不可能。

本脚本镜像 Core.cs:2239 的清零版与漏清零版, 断言:
  1. 清零版: 同输入重复调用结果恒定, 且 <= W*H;
  2. 漏清零版: 确实会漂移(证明本回归有判别力, 不会因为改坏而恒绿);
  3. 两版在"缓冲全新"时一致(说明差异只来自复用)。
"""
W, H = 6, 6
# B: 左上角占 2 格, 其余全空 → 正确值 30
B = [[False] * H for _ in range(W)]
B[0][0] = True
B[1][0] = True
# A: 制造深浅不一的高度残留
A = [[False] * H for _ in range(W)]
for x in range(W):
    for y in range(H):
        if y < 4:
            A[x][y] = (x % 2 == 0)


def area(occ, W, H, buf, clear):
    """clear=True 镜像修复版(循环前清零), clear=False 镜像原始 bug。"""
    best = 0
    heights = buf
    if clear:
        for x in range(W):
            heights[x] = 0
    for y in range(H):
        for x in range(W):
            heights[x] = 0 if occ[x][y] else heights[x] + 1
        stack = []
        for x in range(W + 1):
            hh = heights[x] if x < W else 0
            while stack and heights[stack[-1]] > hh:
                idx = stack.pop()
                left = stack[-1] + 1 if stack else 0
                if heights[idx] * (x - left) > best:
                    best = heights[idx] * (x - left)
            if x < W:
                stack.append(x)
    return best


fail = 0

# 1. 修复版: 反复调用恒定, 且不超网格
buf = [0] * W
got = [area(B, W, H, buf, clear=True) for _ in range(5)]
print("修复版 B x5      :", got)
if len(set(got)) != 1 or got[0] != 30:
    print("  FAIL: 期望恒为 30")
    fail += 1
if max(got) > W * H:
    print(f"  FAIL: 超过网格总格数 {W * H}")
    fail += 1

# 2. 漏清零版: 必须有判别力(确实漂移)
buf2 = [0] * W
area(A, W, H, buf2, clear=False)
buggy = [area(B, W, H, buf2, clear=False) for _ in range(3)]
print("漏清零版 B x3    :", buggy)
if len(set(buggy)) == 1 and buggy[0] == 30:
    print("  FAIL: 本回归无判别力(漏清零竟不漂移)")
    fail += 1
else:
    print("  预期漂移已复现 ⇒ 回归有判别力")

# 3. 全新缓冲时两版一致
fresh_clear = area(B, W, H, [0] * W, clear=True)
fresh_buggy = area(B, W, H, [0] * W, clear=False)
print("全新缓冲 两版    :", fresh_clear, fresh_buggy)
if fresh_clear != fresh_buggy:
    print("  FAIL: 全新缓冲下两版应一致")
    fail += 1

# 4. 24x10 真实尺寸: 修复版不超网格
W2, H2 = 24, 10
C = [[False] * H2 for _ in range(W2)]
for x in range(W2):
    for y in range(H2):
        if 6 <= y < 10:
            C[x][y] = True
buf3 = [0] * W2
got2 = [area(C, W2, H2, buf3, clear=True) for _ in range(5)]
print("修复版 24x10 x5  :", got2, f"(网格 {W2 * H2})")
if len(set(got2)) != 1 or max(got2) > W2 * H2:
    print("  FAIL: 不稳定或超网格")
    fail += 1

print("\n" + ("PASS" if fail == 0 else f"FAIL ({fail})"))
raise SystemExit(1 if fail else 0)
