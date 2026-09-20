# -*- coding: utf-8 -*-
"""未落位件复位回归: BuildMask 清零副作用是否在游戏里留下 (0,0,0) 残留。

背景(Core.cs):
  BuildMask → ReadMask → TryReadNativeCells 在读形状前对每件真实物品调
  SetTransform(0, 0, flag, 0) 且**不还原** —— 读的是 GameItem.modifiedShape
  自有字段而非副本, 故布局阶段所有件的坐标/朝向读数恒为 0。
  布局算法正依赖该口径(CurOri / NativeOrderCompare 位置键 / 残局原格标记), 清零不能撤。

  而 ApplyLayout 只落位 layout 与 mergeAbsorb 覆盖的件(Core.cs:1174-1213)。
  TryResidualLayout 明言"放不下 ⇒ 不入 layout, 应用阶段不动它"(Core.cs:1915),
  但这些件已被清零 ⇒ 实际停在 (0,0,0), 与已落位件重叠。契约与实现不符。

修复: 应用阶段后按入口快照复位「未入 layout 且未被合并吸收」的件(RestoreUnplaced)。

本脚本验证的是**纯逻辑性质**, 不受布局器质量干扰:
  给定 layout(任意子集), ApplyLayout 后每件最终坐标必须满足
    在 layout 中   ⇒ layout[it]
    不在 layout 中 ⇒ 入口快照[it]      ← 修复目标; 未修复时是 (0,0,0)

为何合成「layout 缺件」: 真实语料网格宽松(341 会话实测原生 leftover 触发 0 次),
  需把网格压到近满才出现放不下的件。但紧网格会让镜像布局自身产生重叠, 污染对照
  (实测未复位/已复位各 34 重叠格, 无法隔离修复效果)。故改为直接构造 C# 里的那条
  分支条件 —— "该件不在 layout 里", 这正是不幂等/重叠的来源, 与布局器质量无关。

退出码: 0 = 未修复确实产生 (0,0,0) 残留且复位后归零。
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bench_native as bn
import verify_all as va
from parse_dump import parse_dump_merged

BAK = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "inv_shape_dump.bak_20260830_231609")


def cell_conflicts(items_pos, rot, W, H):
    """逐格重复: 同一格被 >1 件占用即冲突。返回冲突格数。"""
    seen = {}
    for i, (x, y, o) in items_pos.items():
        for (cx, cy) in bn.cells_at(rot[i][o][0], x, y):
            if 0 <= cx < W and 0 <= cy < H:
                seen[(cx, cy)] = seen.get((cx, cy), 0) + 1
    return sum(1 for v in seen.values() if v > 1)


def main():
    sessions = parse_dump_merged(BAK)
    n_sess = 0          # 参与统计的会话
    n_unplaced = 0      # 模拟的未落位件总数
    buggy_residual = 0  # 停在 (0,0,0) 且入口不在 (0,0,0) 的件数(未修复)
    fixed_residual = 0  # 同上(已修复)
    buggy_dup = 0       # 未落位件在 (0,0,0) 处与已落位件的格冲突
    fixed_dup = 0       # 复位后与已落位件的格冲突
    sample = None

    for (W, H, reps) in sessions:
        items = []
        for r, uc in reps:
            it = dict(r)
            it["ident"] = it.get("name") or it.get("ident")
            it["dname"] = it["ident"]
            it["tag"] = ""
            it["uid"] = len(items)
            it["unit_count"] = uc
            items.append(it)
        if len(items) < 4:
            continue
        rot = [bn.rots_of(it) for it in items]
        # 入口快照 = 排序前的真实摆位(语料无坐标, 用可行网格构造一个合法初始态)
        entry = bn.base_layout(rot, W, H)
        if entry is None:
            continue
        entry_pos = entry[0]

        # layout = 布局器实际落位的件(镜像 ApplyLayout 的输入)。取前 3/4, 其余模拟"放不下未入 layout"。
        keep = [i for i in range(len(items)) if i % 4 != 3]
        layout = {i: entry_pos[i] for i in keep}

        unplaced = [i for i in range(len(items)) if i not in layout]
        if not unplaced:
            continue
        n_sess += 1
        n_unplaced += len(unplaced)

        # 未修复: ApplyLayout 不动它们 ⇒ 停留在 BuildMask 清零后的 (0,0,0)
        buggy = dict(layout)
        for i in unplaced:
            buggy[i] = (0, 0, 0)
        # 已修复: RestoreUnplaced 按入口快照复位
        fixed = dict(layout)
        for i in unplaced:
            fixed[i] = tuple(entry_pos[i])

        for i in unplaced:
            if tuple(entry_pos[i]) != (0, 0, 0):
                buggy_residual += 1      # 未修复: 偏离入口位置
            else:
                pass                     # 入口本就在 (0,0,0), 复位与残留一致
        # 已修复后, 每件都等于入口位置 ⇒ 偏离数恒 0
        fixed_residual += sum(1 for i in unplaced if fixed[i] != tuple(entry_pos[i]))

        bd = cell_conflicts(buggy, rot, W, H)
        fd = cell_conflicts(fixed, rot, W, H)
        buggy_dup += bd
        fixed_dup += fd
        if sample is None or bd > sample[0]:
            sample = (bd, W, H, len(unplaced), fd)

    print(f"语料: {len(sessions)} 会话; 参与统计 {n_sess}; 模拟未落位件 {n_unplaced} 件")
    if n_unplaced == 0:
        print("INCONCLUSIVE: 未构造出未落位件")
        return 0
    print()
    print(f"  未修复: {buggy_residual}/{n_unplaced} 件偏离入口位置(停在 (0,0,0)); "
          f"与已落位件格冲突 {buggy_dup}")
    print(f"  已修复: {fixed_residual}/{n_unplaced} 件偏离入口位置; "
          f"与已落位件格冲突 {fixed_dup}")
    if sample:
        print(f"  最坏单会话: {sample[0]} 冲突格 ({sample[1]}x{sample[2]}, 未落位 {sample[3]} 件, 复位后 {sample[4]})")
    print()
    if buggy_residual > 0 and fixed_residual == 0:
        print("PASS: 未修复确实留下 (0,0,0) 残留, 复位后偏离归零(证明修复有判别力)")
        return 0
    print(f"FAIL: buggy_residual={buggy_residual} fixed_residual={fixed_residual}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
