from pathlib import Path
import csv,collections
rows=list(csv.DictReader(Path(r"C:/MMUnityPort/Validation/LinkedWaterAudit_20260920/current.tsv").open(),delimiter="\t"))
by=collections.defaultdict(list)
for r in rows:
 n=r["renderer"].lower()
 if r["active"]!="True" or r["enabled"]!="True": continue
 if n.startswith("water - smoothed") or n=="internal water - smooth" or n=="ocean + central lake water" or n=="global ocean surface":
  by[r["region"]].append((r["renderer"],r["material"],r["matPath"]))
for k in sorted(by):
 print(k, len(by[k]), by[k])
