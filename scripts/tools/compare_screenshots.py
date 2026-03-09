"""Compare current screenshots against baseline PNGs.

Usage:
    python compare_screenshots.py --current reports/screenshot/full/ --baseline reports/baselines/full/
    python compare_screenshots.py --current reports/screenshot/quick/ --baseline reports/baselines/quick/

Output: JSON to stdout with per-image comparison results.
Exit codes: 0 = all pass, 1 = any regression, 2 = missing dependency.
"""

import argparse
import json
import os
import re
import sys

try:
    from PIL import Image, ImageChops
except ImportError:
    print("ERROR: Pillow not installed. Run: pip install Pillow", file=sys.stderr)
    sys.exit(2)


def extract_phase_label(filename):
    """Strip tick/timestamp suffix to get the phase label for matching.

    Examples:
        boot_0001_17730640.png -> boot
        dock_market_0042_17730641.png -> dock_market
        npc_combat_f01_0099_17730641.png -> npc_combat_f01
    """
    stem = os.path.splitext(filename)[0]
    # Remove trailing _DIGITS groups (tick and timestamp)
    cleaned = re.sub(r"(_\d+)+$", "", stem)
    return cleaned


def compute_metrics(baseline_img, current_img):
    """Compute pixel-level comparison metrics between two images."""
    # Ensure same size
    if baseline_img.size != current_img.size:
        current_img = current_img.resize(baseline_img.size, Image.LANCZOS)

    # Convert to RGB (drop alpha if present)
    b = baseline_img.convert("RGB")
    c = current_img.convert("RGB")

    # Pixel-by-pixel difference
    diff = ImageChops.difference(b, c)
    diff_data = list(diff.getdata())

    total_pixels = len(diff_data)
    if total_pixels == 0:
        return {"mad": 0.0, "max_diff": 0, "changed_pct": 0.0}

    # Mean Absolute Difference (normalized to 0.0-1.0)
    sum_diff = 0.0
    max_diff = 0
    changed_count = 0
    threshold = int(255 * 0.05)  # 5% of max channel value

    for r, g, b_val in diff_data:
        pixel_diff = r + g + b_val
        sum_diff += pixel_diff
        pixel_max = max(r, g, b_val)
        if pixel_max > max_diff:
            max_diff = pixel_max
        if pixel_max > threshold:
            changed_count += 1

    mad = sum_diff / (total_pixels * 3 * 255)  # normalize
    changed_pct = changed_count / total_pixels * 100.0

    return {
        "mad": round(mad, 6),
        "max_diff": max_diff,
        "changed_pct": round(changed_pct, 2),
    }


def classify(metrics):
    """Return PASS / WARN / FAIL based on metrics."""
    mad = metrics["mad"]
    changed_pct = metrics["changed_pct"]

    if mad < 0.02 and changed_pct < 5.0:
        return "PASS"
    elif mad < 0.05 or changed_pct < 15.0:
        return "WARN"
    else:
        return "FAIL"


def find_matching_current(phase_label, current_files):
    """Find the current screenshot that matches the given phase label."""
    for f in current_files:
        if extract_phase_label(f) == phase_label:
            return f
    return None


def main():
    parser = argparse.ArgumentParser(description="Compare screenshots against baselines")
    parser.add_argument("--current", required=True, help="Directory with current screenshots")
    parser.add_argument("--baseline", required=True, help="Directory with baseline screenshots")
    parser.add_argument("--threshold-mad", type=float, default=0.02, help="MAD threshold for PASS")
    parser.add_argument("--threshold-pct", type=float, default=5.0, help="Changed%% threshold for PASS")
    args = parser.parse_args()

    if not os.path.isdir(args.baseline):
        print(f"ERROR: Baseline directory not found: {args.baseline}", file=sys.stderr)
        sys.exit(2)

    if not os.path.isdir(args.current):
        print(f"ERROR: Current directory not found: {args.current}", file=sys.stderr)
        sys.exit(2)

    baseline_pngs = sorted(f for f in os.listdir(args.baseline) if f.lower().endswith(".png"))
    current_pngs = sorted(f for f in os.listdir(args.current) if f.lower().endswith(".png"))

    if not baseline_pngs:
        print("ERROR: No baseline PNGs found", file=sys.stderr)
        sys.exit(2)

    results = []
    any_fail = False

    for baseline_file in baseline_pngs:
        phase = extract_phase_label(baseline_file)
        current_file = find_matching_current(phase, current_pngs)

        if current_file is None:
            result = {
                "baseline": baseline_file,
                "current": None,
                "phase": phase,
                "verdict": "FAIL",
                "reason": "no matching current screenshot",
            }
            any_fail = True
        else:
            baseline_img = Image.open(os.path.join(args.baseline, baseline_file))
            current_img = Image.open(os.path.join(args.current, current_file))
            metrics = compute_metrics(baseline_img, current_img)
            verdict = classify(metrics)
            result = {
                "baseline": baseline_file,
                "current": current_file,
                "phase": phase,
                "verdict": verdict,
                **metrics,
            }
            if verdict == "FAIL":
                any_fail = True

        results.append(result)

    # JSON output to stdout
    print(json.dumps(results, indent=2))

    # Human-readable summary to stderr
    print("\n--- Screenshot Regression Report ---", file=sys.stderr)
    print(f"{'Phase':<30} {'Verdict':<6} {'MAD':<10} {'Changed%':<10} {'MaxDiff':<8}", file=sys.stderr)
    print("-" * 74, file=sys.stderr)
    for r in results:
        phase = r.get("phase", "?")
        verdict = r.get("verdict", "?")
        mad = f"{r.get('mad', 0):.4f}" if "mad" in r else "N/A"
        pct = f"{r.get('changed_pct', 0):.1f}%" if "changed_pct" in r else "N/A"
        maxd = str(r.get("max_diff", "N/A"))
        reason = r.get("reason", "")
        line = f"{phase:<30} {verdict:<6} {mad:<10} {pct:<10} {maxd:<8}"
        if reason:
            line += f" ({reason})"
        print(line, file=sys.stderr)

    pass_count = sum(1 for r in results if r["verdict"] == "PASS")
    warn_count = sum(1 for r in results if r["verdict"] == "WARN")
    fail_count = sum(1 for r in results if r["verdict"] == "FAIL")
    print(f"\nTotal: {pass_count} PASS, {warn_count} WARN, {fail_count} FAIL", file=sys.stderr)

    sys.exit(1 if any_fail else 0)


if __name__ == "__main__":
    main()
