"""记录语料规模事实(供判断布局优化收益): 离线语料 vs 实机日志的件数分布。

结论: 离线语料 n<=28(无重实例), 实机日志 n 最大 76 ⇒ 优化/并行评估必须按实机规模算。
"""
import sys
sys.path.insert(0, '.')
import bench_banded as bn

sess = bn.dump_sessions()
sizes = sorted(len(s[2]) for s in sess)
n = len(sizes)
print(f"离线语料 {n} 会话: min={sizes[0]} p25={sizes[n//4]} p50={sizes[n//2]} "
      f"p75={sizes[3*n//4]} p90={sizes[int(n*0.9)]} max={sizes[-1]}")
print(f"  件数 >=40: {sum(1 for s in sizes if s >= 40)}   >=70: {sum(1 for s in sizes if s >= 70)}")
print()
print("实机 12-17-43.log 的 n(10 次排序): 16 35 17 76 76 28 70 76 16 18  ⇒ max=76")
print()
print("=> 离线语料不足以代表最重实例; 布局候选数上限 6, TryFillRefine 每件一次全网格扫描,")
print("   n=76 时约 6*76=456 次扫描(24x10 网格), 属微秒级 ⇒ 并行化收益不抵线程安全风险。")
